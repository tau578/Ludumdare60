using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    /// <summary>Restoring a session from a save (5.12, D25, D55) and capturing one, plus the career stats.</summary>
    public sealed class SaveSessionTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";
        private static readonly BoardSize Small = new BoardSize(3, 3);

        private static FakeContent Content(IReadOnlyList<CallScript> startPool = null, int slots = 2, long startingCents = 0,
            params IUpgrade[] upgrades) => new FakeContent
        {
            StartingCents = startingCents,
            StartingBoard = Small,
            StartingClientSlots = slots,
            StartPool = startPool ?? Scripts.Pool(5, Small),
            Upgrades = upgrades
        };

        private static List<string> LiveIds(GameSession session)
        {
            var ids = new List<string>();
            foreach (Call call in session.Calls.Live) ids.Add(call.Request.Script.Id);
            return ids;
        }

        private static void Connect(GameSession session, Call call, int cord)
        {
            session.Desk.TryTake(call.Id);
            session.Desk.OnCircuitClosed(new CordId(cord), call.Request.From, call.Request.To);
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "setup");
        }

        private static GameSession RealSession(SaveData save)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            return new GameSession(new GameContent(config.LoadEconomy()), new FakeSwitchboard(), save);
        }

        [Test]
        public void CaptureThenJsonThenLoadGivesTheSameSession()
        {
            FakeContent content = Content(upgrades: FakeUpgrade.FullShop());
            var session = new GameSession(content, new FakeSwitchboard());
            session.Wallet.Earn(5000.375);
            Assert.That(session.Upgrades.TryBuy("income_1"), Is.True);
            Assert.That(session.Upgrades.TryBuy("wires_1"), Is.True);
            session.Tick(0.05);
            Connect(session, session.Calls.Live[0], 0);
            for (int i = 0; i < 40; i++) session.Tick(0.05);
            Call stillRinging = session.Calls.Live[1];

            var storage = new MemoryStorage();
            var file = new SaveFile(storage);
            file.Write(session.CaptureSave());
            var loaded = new GameSession(content, new FakeSwitchboard(), file.Load().Data);

            Assert.That(loaded.Wallet.Cents, Is.EqualTo(session.Wallet.Cents));
            Assert.That(loaded.Wallet.Fraction, Is.EqualTo(session.Wallet.Fraction).Within(1e-12));
            Assert.That(loaded.Upgrades.OwnedIds(), Is.EqualTo(new[] { "income_1", "wires_1" }));
            Assert.That(loaded.Stats.IncomeMultiplier, Is.EqualTo(1.5));
            Assert.That(loaded.Stats.Cords, Is.EqualTo(4));
            Assert.That(loaded.Queue.Position, Is.EqualTo(0), "the oldest call still on a card was call_1");
            Assert.That(loaded.Career.CallsConnected, Is.EqualTo(1));
            Assert.That(loaded.Career.EarnedCents, Is.EqualTo(session.Career.EarnedCents));
            Assert.That(loaded.Career.SecondsPlayed, Is.EqualTo(session.Career.SecondsPlayed).Within(1e-9));
            Assert.That(loaded.IsRestored, Is.True);

            loaded.Tick(0.05);
            Assert.That(LiveIds(loaded), Is.EqualTo(new[] { "call_1", stillRinging.Request.Script.Id }),
                "unfinished calls ring again, in order (D55)");
        }

        [Test]
        public void EmptyStorageStartsWithEconomyJsonsStartingValues()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            EconomyCatalog economy = config.LoadEconomy();
            SaveLoad load = new SaveFile(new MemoryStorage()).Load();

            GameSession session = RealSession(load.Data);

            Assert.That(session.IsRestored, Is.False);
            Assert.That(session.HasProgress, Is.False);
            Assert.That(session.Wallet.Cents, Is.EqualTo(economy.StartingCents));
            Assert.That(session.Wallet.Fraction, Is.EqualTo(0));
            Assert.That(session.Stats.Cords, Is.EqualTo(economy.StartingCords));
            Assert.That(session.Stats.Board, Is.EqualTo(economy.StartingBoard));
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(economy.StartingClientSlots));
            Assert.That(session.Upgrades.OwnsAny, Is.False);
            Assert.That(session.Career.Capture().secondsPlayed, Is.EqualTo(0));
        }

        [Test]
        public void ALoadedBoardTwoOpensFiveByFiveAndDealsFromItsStagePool()
        {
            var save = new SaveData
            {
                version = 1,
                cents = 123,
                upgrades = new List<string> { "board_1", "board_2", "wires_1", "income_1" }
            };

            GameSession session = RealSession(save);

            Assert.That(session.Stats.Board, Is.EqualTo(new BoardSize(5, 5)), "open before the first call rings");
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(4));
            Assert.That(session.Board.IsOpen(new JackId("E5")), Is.True);
            Assert.That(session.Board.IsOpen(new JackId("F1")), Is.False);
            Assert.That(session.Stats.Cords, Is.EqualTo(4));
            Assert.That(session.Stats.IncomeMultiplier, Is.EqualTo(1.5));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(session.Shelf.OfferAt(0) ?? session.Shelf.OfferAt(1), Is.Not.Null);

            session.Tick(0.05);

            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_201", "call_202", "call_203", "call_204" }));
            Assert.That(session.Wallet.Cents, Is.EqualTo(123));
        }

        [Test]
        public void UnknownIdsAreIgnoredAndMissingOnesAreNotBought()
        {
            var save = new SaveData
            {
                version = 1,
                cents = 50,
                upgrades = new List<string> { "income_1", "income_9", "notepad", null, "" }
            };

            var session = new GameSession(Content(upgrades: FakeUpgrade.FullShop()), new FakeSwitchboard(), save);

            Assert.That(session.Upgrades.OwnedIds(), Is.EqualTo(new[] { "income_1" }));
            Assert.That(session.Upgrades.IsOwned("income_2"), Is.False);
            Assert.That(session.Upgrades.IsOwned("wires_1"), Is.False);
            Assert.That(session.Stats.Cords, Is.EqualTo(3));
            Assert.That(session.Upgrades.NextInChain(UpgradeKind.ExtraIncome).Id, Is.EqualTo("income_2"));
        }

        [Test]
        public void RestoringSpendsNothingAndRaisesNoEvents()
        {
            var wallet = new Wallet(50);
            var upgrades = new UpgradeService(FakeUpgrade.FullShop(), wallet);
            int bought = 0;
            upgrades.UpgradeBought += _ => bought++;

            upgrades.Restore(new[] { "income_3", "wires_2", "secret" });

            Assert.That(bought, Is.EqualTo(0));
            Assert.That(wallet.Cents, Is.EqualTo(50));
            Assert.That(upgrades.IsOwned("secret"), Is.True);
            Assert.That(upgrades.IsOwned("income_3"), Is.True);
        }

        [Test]
        public void ALoadedSecretIsPurchasedAndNeitherGoalEventFires()
        {
            var save = new SaveData { version = 1, cents = 99999, upgrades = new List<string> { "secret" } };
            var session = new GameSession(Content(upgrades: FakeUpgrade.FullShop()), new FakeSwitchboard(), save);
            int affordable = 0, purchased = 0;
            session.Goal.GoalAffordable += () => affordable++;
            session.Goal.GoalPurchased += () => purchased++;

            for (int i = 0; i < 10; i++) session.Tick(0.05);

            Assert.That(session.Goal.IsPurchased, Is.True);
            Assert.That(session.Goal.Progress, Is.EqualTo(1));
            Assert.That(affordable, Is.EqualTo(0));
            Assert.That(purchased, Is.EqualTo(0));
            Assert.That(session.Wallet.Cents, Is.EqualTo(99999));
        }

        [Test]
        public void TheOldestLiveCallFromTheCurrentPoolWins()
        {
            var session = new GameSession(Content(Scripts.Pool(6, Small), 3), new FakeSwitchboard());
            session.Tick(0.05);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2", "call_3" }));
            // call_1 finishes, so call_4 takes its slot; call_2 is now the oldest on a card.
            Call first = session.Calls.Live[0];
            Connect(session, first, 0);
            for (int i = 0; i < 20 * 21; i++) session.Tick(0.05);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_2", "call_3", "call_4" }));
            Assert.That(session.Queue.Position, Is.EqualTo(4));

            Assert.That(session.Queue.ResumePosition, Is.EqualTo(1));
            Assert.That(session.CaptureSave().queuePosition, Is.EqualTo(1));
        }

        [Test]
        public void CallsFromAnOlderPoolDoNotCount()
        {
            var stage = Scripts.Pool(4, new BoardSize(4, 4), "stage_");
            FakeContent content = Content(Scripts.Pool(5, Small), 2, 100000,
                FakeUpgrade.BoardUpgrade("board_1", 1000, 4, stage, 3));
            var session = new GameSession(content, new FakeSwitchboard());
            session.Tick(0.05);
            Assert.That(session.Upgrades.TryBuy("board_1"), Is.True);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }), "old cards stay");
            Assert.That(session.Queue.ResumePosition, Is.EqualTo(0), "no live call from the new pool: the queue's Position");

            session.Tick(0.05);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2", "stage_1" }));
            Assert.That(session.Queue.Position, Is.EqualTo(1));
            Assert.That(session.Queue.ResumePosition, Is.EqualTo(0), "stage_1 is the oldest from the current pool");
        }

        [Test]
        public void NoLiveCallsGivesThePosition()
        {
            var session = new GameSession(Content(Scripts.Pool(5, Small), 2), new FakeSwitchboard());
            session.Tick(0.05);
            session.ClearCalls();
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(session.Queue.ResumePosition, Is.EqualTo(2));
        }

        [TestCase(5)]
        [TestCase(99)]
        [TestCase(-1)]
        public void APositionOutsideThePoolStartsAtTheFirstCall(int position)
        {
            var save = new SaveData { version = 1, queuePosition = position };
            var session = new GameSession(Content(Scripts.Pool(5, Small), 2), new FakeSwitchboard(), save);
            Assert.That(session.Queue.Position, Is.EqualTo(0));
            session.Tick(0.05);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }));
        }

        [Test]
        public void ASavedPositionDealsFromThere()
        {
            var save = new SaveData { version = 1, queuePosition = 3 };
            var session = new GameSession(Content(Scripts.Pool(5, Small), 2), new FakeSwitchboard(), save);
            session.Tick(0.05);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_4", "call_5" }));
        }

        [Test]
        public void AConnectionCountsOnceAcrossHoldAndResume()
        {
            var session = new GameSession(Content(Scripts.Pool(5, Small), 2), new FakeSwitchboard());
            session.Tick(0.05);
            Call call = session.Calls.Live[0];
            Connect(session, call, 0);
            session.Desk.OnCircuitOpened(new CordId(0), call.Request.From, call.Request.To);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold), "setup");
            session.Desk.OnCircuitClosed(new CordId(0), call.Request.From, call.Request.To);
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "setup");

            Assert.That(session.Career.CallsConnected, Is.EqualTo(1));
        }

        [Test]
        public void CareerCountsFeesAndPayButNotDevMoney()
        {
            // Pool calls pay a 10¢ fee and 5¢/s for 20 s.
            var session = new GameSession(Content(Scripts.Pool(5, Small), 1), new FakeSwitchboard());
            session.Tick(0.05);
            Connect(session, session.Calls.Live[0], 0);
            for (int i = 0; i < 20 * 21; i++) session.Tick(0.05);
            session.Wallet.Earn(10000);

            Assert.That(session.Career.EarnedCents, Is.EqualTo(110));
            Assert.That(session.Career.SecondsPlayed, Is.EqualTo(0.05 * 421).Within(1e-6));
        }

        [Test]
        public void FinesCountTheAmountTaken()
        {
            var emergency = new[] { Scripts.Emergency("emergency_1", "A1", "B2", penaltyCents: 200) };
            var session = new GameSession(Content(emergency, 1, 150), new FakeSwitchboard());
            for (int i = 0; i < 20 * 31; i++) session.Tick(0.05);

            Assert.That(session.Wallet.Cents, Is.EqualTo(0));
            Assert.That(session.Career.FinesPaidCents, Is.EqualTo(150), "a short balance gives up only what it has");
        }

        [Test]
        public void CareerAddsToTheSavedNumbers()
        {
            var save = new SaveData
            {
                version = 1,
                career = new CareerData { earnedCents = 1000, callsConnected = 7, finesPaidCents = 300, secondsPlayed = 60 }
            };
            var session = new GameSession(Content(Scripts.Pool(5, Small), 1), new FakeSwitchboard(), save);
            session.Tick(0.05);
            Connect(session, session.Calls.Live[0], 0);

            CareerData career = session.CaptureSave().career;
            Assert.That(career.earnedCents, Is.EqualTo(1010));
            Assert.That(career.callsConnected, Is.EqualTo(8));
            Assert.That(career.finesPaidCents, Is.EqualTo(300));
            Assert.That(career.secondsPlayed, Is.EqualTo(60.05).Within(1e-9));
        }

        [Test]
        public void ANewGameHasProgressOnlyOnceSomethingHappened()
        {
            var session = new GameSession(Content(Scripts.Pool(5, Small), 1, 0, FakeUpgrade.FullShop()), new FakeSwitchboard());
            for (int i = 0; i < 100; i++) session.Tick(0.05);
            Assert.That(session.HasProgress, Is.False, "time alone isn't progress");

            Connect(session, session.Calls.Live[0], 0);
            Assert.That(session.HasProgress, Is.True);
        }

        [Test]
        public void ACaptureIsASnapshot()
        {
            var session = new GameSession(Content(upgrades: FakeUpgrade.FullShop(), startingCents: 1000), new FakeSwitchboard());
            SaveData before = session.CaptureSave();
            session.Upgrades.TryBuy("income_1");
            Assert.That(before.upgrades, Is.Empty);
            Assert.That(before.cents, Is.EqualTo(1000));
            Assert.That(before.version, Is.EqualTo(SaveData.CurrentVersion));
        }
    }
}
