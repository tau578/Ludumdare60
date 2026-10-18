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
        public void OpenBoardKnowsWhoIsInTheGame()
        {
            var crowd = new Crowd(36);
            var stats = new StatSheet(new UpgradeService(null, new Wallet()), 3, Small, 3);
            var board = new OpenBoard(stats, crowd.Directory);
            Assert.That(board.IsInGame(crowd.People[0]), Is.True, "A1");
            Assert.That(board.IsInGame(crowd.People[14]), Is.True, "C3");
            Assert.That(board.IsInGame(crowd.People[3]), Is.False, "A4");
            Assert.That(board.IsInGame(crowd.People[18]), Is.False, "D1");
            Assert.That(board.IsInGame(new FakePerson("Not", "Listed")), Is.False);
            Assert.That(board.IsOpen(new JackId("B2")), Is.True);
            Assert.That(board.IsOpen(new JackId("B4")), Is.False);
        }

        [Test]
        public void ASmallBoardOnlyEverRingsOpenPeopleAndFillsOnlyTheOpenSlots()
        {
            var crowd = new Crowd(36);
            crowd.Content.StartingBoard = Small;
            crowd.Content.StartingClientSlots = 3;
            var session = new GameSession(crowd.Content, new SeededRandom(7), new FakeSwitchboard { CordCount = 6 });
            int rung = 0, most = 0;
            session.Calls.CallAdded += e =>
            {
                rung++;
                Assert.That(session.Board.IsInGame(e.Call.Request.CallerPerson), Is.True, e.Call.Request.CallerPerson.Id);
                Assert.That(session.Board.IsInGame(e.Call.Request.Recipient), Is.True, e.Call.Request.Recipient.Id);
            };
            for (int tick = 0; tick < 20 * 600; tick++)
            {
                session.Tick(0.05);
                most = System.Math.Max(most, session.Calls.Live.Count);
            }
            Assert.That(rung, Is.GreaterThanOrEqualTo(3));
            Assert.That(most, Is.EqualTo(3), "live calls never outnumber the open slots");
        }

        [Test]
        public void BuyingABiggerBoardOpensMoreSlotsAtOnce()
        {
            var crowd = new Crowd(36);
            crowd.Content.StartingBoard = Small;
            crowd.Content.StartingClientSlots = 3;
            crowd.Content.Upgrades = BoardShop();
            var session = new GameSession(crowd.Content, new SeededRandom(8), new FakeSwitchboard { CordCount = 6 });
            for (int tick = 0; tick < 20 * 120; tick++) session.Tick(0.05);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(3));
            Assert.That(session.Spawner.TrySpawn(), Is.False, "every open slot is taken");

            Assert.That(session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients), Is.True);
            Assert.That(session.Stats.Board, Is.EqualTo(new BoardSize(4, 4)));
            Assert.That(session.Spawner.TrySpawn(), Is.True, "the 4th slot opened");
            Assert.That(session.Spawner.TrySpawn(), Is.False);
        }

        [Test]
        public void EmergenciesWaitUntilTheBoardOpensOverAService()
        {
            // Police, Fire Brigade and Doctor sit on F6, F5 and F4, so only a 6x6 board reaches them.
            var world = new EmergencyWorld(count: 12, regularWeight: 1, emergencyWeight: 1);
            world.Content.StartingBoard = Small;
            world.Content.StartingClientSlots = 3;
            world.Content.Upgrades = BoardShop();
            var session = new GameSession(world.Content, new SeededRandom(5), new FakeSwitchboard { CordCount = 6 });
            int emergencies = 0;
            session.Calls.CallAdded += e => { if (e.Call.Request.Type == world.Emergency) emergencies++; };

            Assert.That(session.Spawner.HasOpenRecipient(world.Emergency), Is.False);
            Assert.That(session.Spawner.TrySpawn(world.Emergency), Is.False, "no service is open");
            PlayAndClear(session, 600);
            Assert.That(emergencies, Is.EqualTo(0));

            session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            Assert.That(session.Spawner.HasOpenRecipient(world.Emergency), Is.False, "5x5 still misses row F");
            session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            Assert.That(session.Spawner.HasOpenRecipient(world.Emergency), Is.True);
            PlayAndClear(session, 600);
            Assert.That(emergencies, Is.GreaterThan(3));
        }

        // Ends every call as soon as it rings, as a player who keeps up would, so new calls keep coming.
        private static void PlayAndClear(GameSession session, double seconds)
        {
            for (double t = 0; t < seconds; t += 0.05)
            {
                session.Idle.ReportInput();
                session.Tick(0.05);
                while (session.Calls.Live.Count > 0) session.Calls.End(session.Calls.Live[0].Id, CallEndReason.Cleared);
            }
        }

        [Test]
        public void CallbacksWaitForAnOpenSlot()
        {
            var crowd = new Crowd(8, new FloatRange(20, 20));
            var calls = new CallService();
            var clock = new GameClock();
            int slots = 1;
            var callbacks = new CallbackScheduler(calls, clock, new SeededRandom(3), () => slots);
            Call wrong = calls.Add(crowd.Request(0, 1));
            calls.Take(wrong.Id);
            calls.End(wrong.Id, CallEndReason.WrongConnection);
            calls.Add(crowd.Request(2, 3));

            clock.Advance(21);
            callbacks.Tick();
            Assert.That(calls.Live.Count, Is.EqualTo(1), "the one open slot is taken");
            slots = 2;
            callbacks.Tick();
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Assert.That(calls.Live[1].Request.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void ResetClosesTheBoardClearsCallsAndReturnsCordsInClosingJacks()
        {
            var crowd = new Crowd(36);
            crowd.Content.StartingBoard = Small;
            crowd.Content.StartingClientSlots = 3;
            crowd.Content.Upgrades = BoardShop();
            var switchboard = new FakeSwitchboard { CordCount = 6 };
            var session = new GameSession(crowd.Content, new SeededRandom(2), switchboard);
            session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            session.Calls.Add(crowd.Request(0, 1));
            JackId a1 = new JackId("A1"), b2 = new JackId("B2"), d4 = new JackId("D4"), c3 = new JackId("C3"), a4 = new JackId("A4");
            session.Desk.OnCircuitClosed(new CordId(0), a1, b2);
            session.Desk.OnCircuitClosed(new CordId(1), c3, d4);
            session.Desk.OnPlugInserted(new CordId(2), a4);

            session.ResetUpgrades();

            Assert.That(session.Stats.Board, Is.EqualTo(Small));
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(3));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(session.Callbacks.Pending, Is.Empty);
            Assert.That(switchboard.Returned, Is.EquivalentTo(new[] { new CordId(1), new CordId(2) }),
                "only cords with a plug in a closing jack go back");
            Assert.That(session.Shelf.OfferAt(0).Id, Is.EqualTo("board_1"));
        }

        [Test]
        public void TheRealFileStartsOnThreeByThreeWithTheStartingNine()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            EconomyCatalog economy = config.LoadEconomy();
            PeopleCatalog people = config.LoadPeople(economy, ContentValidator.BoardJacks());
            var session = new GameSession(new GameContent(people, economy), new SeededRandom(9), new FakeSwitchboard { CordCount = 3 });
            var expected = new[]
            {
                "person.arthur-hollis", "person.mabel-dunmore", "person.mildred-stanhope", "person.lillian-yardley",
                "person.irene-godfrey", "person.gertrude-rook", "person.bernard-quigley", "person.reginald-underhill",
                "person.ada-moss"
            };
            var involved = new HashSet<string>();
            int most = 0;
            session.Calls.CallAdded += e =>
            {
                involved.Add(e.Call.Request.CallerPerson.Id);
                involved.Add(e.Call.Request.Recipient.Id);
            };

            // Ten minutes in which the player connects every call at once.
            int nextCord = 0;
            var live = new List<Call>();
            for (int tick = 0; tick < 20 * 600; tick++)
            {
                session.Idle.ReportInput();
                session.Tick(0.05);
                most = System.Math.Max(most, session.Calls.Live.Count);
                live.Clear();
                live.AddRange(session.Calls.Live);
                foreach (Call call in live)
                {
                    if (call.State != CallState.Incoming) continue;
                    session.Desk.TryTake(call.Id);
                    session.Desk.OnCircuitClosed(new CordId(nextCord++),
                        session.Directory.JackOf(call.Request.CallerPerson), session.Directory.JackOf(call.Request.Recipient));
                }
            }

            Assert.That(session.Stats.Board, Is.EqualTo(new BoardSize(3, 3)));
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(3));
            Assert.That(involved, Is.SubsetOf(expected));
            Assert.That(involved.Count, Is.GreaterThan(6), "most of the nine take part");
            Assert.That(most, Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void TheRealPeopleFileHasOneServicePerNewRing()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            EconomyCatalog economy = config.LoadEconomy();
            PeopleCatalog people = config.LoadPeople(economy, ContentValidator.BoardJacks());
            var directory = new PhoneDirectory(people.Directory);
            var jacks = new Dictionary<string, string>();
            foreach (IDirectoryEntry entry in people.Directory) jacks[entry.Person.FullName()] = entry.JackLabel;

            Assert.That(jacks["Doctor"], Is.EqualTo("D4"));
            Assert.That(jacks["Fire Brigade"], Is.EqualTo("E5"));
            Assert.That(jacks["Police"], Is.EqualTo("B6"));
            Assert.That(jacks["Reginald Underhill"], Is.EqualTo("C2"));
            Assert.That(directory.PersonAt(new JackId("C2")).FullName(), Is.EqualTo("Reginald Underhill"));

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
