using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    /// <summary>Board growth (D42, D49): the open board and caller slots follow the extra_clients chain.</summary>
    public sealed class BoardGrowthTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private static readonly BoardSize Small = new BoardSize(3, 3);

        private static IUpgrade[] BoardShop() => new IUpgrade[]
        {
            FakeUpgrade.BoardUpgrade("board_1", 1000, 4),
            FakeUpgrade.BoardUpgrade("board_2", 2500, 5),
            FakeUpgrade.BoardUpgrade("board_3", 6000, 6),
            new FakeUpgrade("secret", UpgradeKind.Secret, 25000)
        };

        [TestCase("A1", 1, 1, true)]
        [TestCase("A2", 1, 1, false)]
        [TestCase("B1", 1, 1, false)]
        [TestCase("C3", 3, 3, true)]
        [TestCase("c3", 3, 3, true)]
        [TestCase("C4", 3, 3, false)]
        [TestCase("D1", 3, 3, false)]
        [TestCase("D3", 3, 4, true)]
        [TestCase("A4", 4, 3, true)]
        [TestCase("F6", 6, 6, true)]
        [TestCase("A10", 6, 6, false)]
        [TestCase("G1", 6, 6, false)]
        [TestCase("", 6, 6, false)]
        [TestCase("A", 6, 6, false)]
        [TestCase("AB", 6, 6, false)]
        public void BoardSizeContainsTheTopLeftRectangle(string jack, int cols, int rows, bool open) =>
            Assert.That(new BoardSize(cols, rows).Contains(new JackId(jack)), Is.EqualTo(open));

        [Test]
        public void BoardSizeValidityAndNames()
        {
            Assert.That(BoardSize.Full, Is.EqualTo(new BoardSize(BoardSize.MaxSide, BoardSize.MaxSide)));
            Assert.That(new BoardSize(3, 3).IsValid, Is.True);
            Assert.That(new BoardSize(0, 3).IsValid, Is.False);
            Assert.That(new BoardSize(3, 7).IsValid, Is.False);
            Assert.That(default(BoardSize).IsValid, Is.False);
            Assert.That(new BoardSize(4, 5).ToString(), Is.EqualTo("4x5"));
            Assert.That(new BoardSize(4, 5).LastRow, Is.EqualTo('E'));
        }

        [Test]
        public void StatSheetDefaultsToTheFullBoardAndEverySlot()
        {
            var stats = new StatSheet(new UpgradeService(null, new Wallet()));
            Assert.That(stats.Board, Is.EqualTo(BoardSize.Full));
            Assert.That(stats.ClientSlots, Is.EqualTo(StatSheet.MaxClientSlots));

            var broken = new StatSheet(new UpgradeService(null, new Wallet()), 3, new BoardSize(0, 9), 7);
            Assert.That(broken.Board, Is.EqualTo(BoardSize.Full), "an invalid start opens the full board");
            Assert.That(broken.ClientSlots, Is.EqualTo(StatSheet.MaxClientSlots));
        }

        [Test]
        public void StatSheetFollowsPurchasesAndTheReset()
        {
            var upgrades = new UpgradeService(BoardShop(), new Wallet(100000));
            var stats = new StatSheet(upgrades, 3, Small, 3);
            int changes = 0;
            stats.Changed += () => changes++;
            Assert.That(stats.Board, Is.EqualTo(Small));
            Assert.That(stats.BoardCols, Is.EqualTo(3));
            Assert.That(stats.BoardRows, Is.EqualTo(3));
            Assert.That(stats.ClientSlots, Is.EqualTo(3));

            for (int side = 4; side <= 6; side++)
            {
                Assert.That(upgrades.TryBuy("board_" + (side - 3)), Is.True);
                Assert.That(stats.Board, Is.EqualTo(new BoardSize(side, side)));
                Assert.That(stats.ClientSlots, Is.EqualTo(side));
            }
            Assert.That(changes, Is.EqualTo(3));

            upgrades.ClearOwned();
            stats.Recompute();
            Assert.That(stats.Board, Is.EqualTo(Small));
            Assert.That(stats.ClientSlots, Is.EqualTo(3));
        }

        [Test]
        public void OpenBoardKnowsWhichJacksAreOpen()
        {
            var stats = new StatSheet(new UpgradeService(null, new Wallet()), 3, Small, 3);
            var board = new OpenBoard(stats);
            Assert.That(board.Size, Is.EqualTo(Small));
            Assert.That(board.IsOpen(new JackId("B2")), Is.True);
            Assert.That(board.IsOpen(new JackId("C3")), Is.True);
            Assert.That(board.IsOpen(new JackId("B4")), Is.False);
            Assert.That(board.IsOpen(new JackId("D1")), Is.False);
        }

        [Test]
        public void TheQueueNeverFillsMoreThanTheOpenSlots()
        {
            var content = new FakeContent { StartingBoard = Small, StartingClientSlots = 2, StartPool = Scripts.Pool(9, Small) };
            var session = new GameSession(content, new FakeSwitchboard());
            int most = 0;
            for (int tick = 0; tick < 200; tick++)
            {
                session.Tick(0.05);
                most = System.Math.Max(most, session.Calls.Live.Count);
                if (tick % 10 == 0) session.Calls.End(session.Calls.Live[0].Id, CallEndReason.WrongConnection);
            }
            Assert.That(most, Is.EqualTo(2));
        }

        [Test]
        public void ResetClosesTheBoardClearsCallsAndReturnsCordsInClosingJacks()
        {
            var content = new FakeContent { StartingBoard = Small, StartingClientSlots = 3, Upgrades = BoardShop() };
            var switchboard = new FakeSwitchboard { CordCount = 6 };
            var session = new GameSession(content, switchboard);
            session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            session.Calls.Add(TestWorld.Request(TestWorld.A1, TestWorld.B2));
            JackId a1 = new JackId("A1"), b2 = new JackId("B2"), d4 = new JackId("D4"), c3 = new JackId("C3"), a4 = new JackId("A4");
            session.Desk.OnCircuitClosed(new CordId(0), a1, b2);
            session.Desk.OnCircuitClosed(new CordId(1), c3, d4);
            session.Desk.OnPlugInserted(new CordId(2), a4);

            session.ResetUpgrades();

            Assert.That(session.Stats.Board, Is.EqualTo(Small));
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(3));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(switchboard.Returned, Is.EquivalentTo(new[] { new CordId(1), new CordId(2) }),
                "only cords with a plug in a closing jack go back");
            Assert.That(session.Shelf.OfferAt(0).Id, Is.EqualTo("board_1"));
        }

        [Test]
        public void TheRealFilesPlayTheStartPoolOnThreeByThreeInTwoSlots()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            EconomyCatalog economy = config.LoadEconomy();
            var session = new GameSession(new GameContent(economy), new FakeSwitchboard { CordCount = 2 });
            var played = new List<string>();
            int most = 0;
            session.Calls.CallAdded += e =>
            {
                played.Add(e.Call.Request.Script.Id);
                Assert.That(Small.Contains(e.Call.Request.From) && Small.Contains(e.Call.Request.To), Is.True, e.Call.Request.Script.Id);
                Assert.That(e.Call.Request.Script.IsEmergency, Is.False, "the start pool has no emergencies");
            };

            // Ten minutes in which the player connects every call at once.
            int nextCord = 0;
            var live = new List<Call>();
            for (int tick = 0; tick < 20 * 600; tick++)
            {
                session.Tick(0.05);
                most = System.Math.Max(most, session.Calls.Live.Count);
                live.Clear();
                live.AddRange(session.Calls.Live);
                foreach (Call call in live)
                {
                    if (call.State != CallState.Incoming) continue;
                    session.Desk.TryTake(call.Id);
                    session.Desk.OnCircuitClosed(new CordId(nextCord++), call.Request.From, call.Request.To);
                }
            }

            Assert.That(session.Stats.Board, Is.EqualTo(new BoardSize(3, 3)));
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(2));
            Assert.That(most, Is.EqualTo(2));
            Assert.That(played.Count, Is.GreaterThan(economy.StartPool.Count), "the pool loops");
            for (int i = 0; i < played.Count; i++)
                Assert.That(played[i], Is.EqualTo(economy.StartPool[i % economy.StartPool.Count].Id), "calls play in pool order");
        }

        [Test]
        public void TheRealPeopleFileHasOneServicePerNewRing()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            PeopleCatalog people = config.LoadPeople(ContentValidator.BoardJacks());
            var jacks = new Dictionary<string, string>();
            foreach (IDirectoryEntry entry in people.Directory) jacks[entry.Person.FullName()] = entry.JackLabel;

            Assert.That(jacks["Doctor"], Is.EqualTo("D4"));
            Assert.That(jacks["Fire Brigade"], Is.EqualTo("E5"));
            Assert.That(jacks["Police"], Is.EqualTo("B6"));
            Assert.That(jacks["Reginald Underhill"], Is.EqualTo("C2"));

            // Each bigger board brings exactly one service.
            int[] services = { 0, 1, 2, 3 };
            for (int side = 3; side <= 6; side++)
            {
                var size = new BoardSize(side, side);
                int count = 0;
                foreach (IDirectoryEntry entry in people.Directory)
                    if (entry.Person.Tags.Count > 0 && size.Contains(entry.JackLabel)) count++;
                Assert.That(count, Is.EqualTo(services[side - 3]), size.ToString());
            }
        }
    }
}
