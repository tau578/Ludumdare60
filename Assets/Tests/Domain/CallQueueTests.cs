using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>The call queues (D52): free slots fill at once, in pool order, looping, and follow the bought board.</summary>
    public sealed class CallQueueTests
    {
        private static readonly BoardSize Small = new BoardSize(3, 3);

        private FakeSwitchboard board;

        private GameSession Session(IReadOnlyList<CallScript> startPool, int slots, params IUpgrade[] upgrades)
        {
            board = new FakeSwitchboard();
            var content = new FakeContent
            {
                StartingCents = 100000,
                StartingBoard = Small,
                StartingClientSlots = slots,
                StartPool = startPool,
                Upgrades = upgrades
            };
            return new GameSession(content, board);
        }

        private static List<string> LiveIds(GameSession session)
        {
            var ids = new List<string>();
            foreach (Call call in session.Calls.Live) ids.Add(call.Request.Script.Id);
            return ids;
        }

        private static void WrongNumber(GameSession session, Call call, int cord = 0)
        {
            session.Desk.TryTake(call.Id);
            var wrong = new JackId(call.Request.To.Label == "A1" ? "B1" : "A1");
            if (wrong == call.Request.From) wrong = new JackId("C1");
            session.Desk.OnCircuitClosed(new CordId(cord), call.Request.From, wrong, call.Request.From);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection), "setup");
        }

        [Test]
        public void ConnectingTutorialImmediatelyOpensTheOtherSlots()
        {
            var session = new GameSession(new FakeContent
            {
                StartingBoard = Small, StartingClientSlots = 3, StartPool = Scripts.Pool(5, Small)
            }, new FakeSwitchboard(), tutorial: true);
            session.Tick(.01);
            Call first = session.Calls.Live[0];
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1" }));
            session.Calls.Take(first.Id);
            session.Calls.Connect(first.Id);
            session.Tick(.01);
            Assert.That(first.State, Is.EqualTo(CallState.Connected));
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2", "call_3" }));
            session.Tick(first.SecondsLeft);
            Assert.That(first.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_2", "call_3", "call_4" }));
        }

        [TestCase(CallEndReason.WrongConnection)]
        [TestCase(CallEndReason.DisconnectedEarly)]
        [TestCase(CallEndReason.Missed)]
        [TestCase(CallEndReason.Cleared)]
        public void AnyTutorialFailureUnlocksNormalQueueWithoutRepeatingTutorial(CallEndReason reason)
        {
            var session = new GameSession(new FakeContent
            {
                StartingBoard = Small, StartingClientSlots = 3, StartPool = Scripts.Pool(5, Small)
            }, new FakeSwitchboard(), tutorial: true);
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));
            session.Calls.End(session.Calls.Live[0].Id, reason);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_2", "call_3", "call_4" }));
            session.ClearCalls();
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(3));
        }

        [Test]
        public void DifferentRequestsFromTheSameCallerWaitUntilTheirLiveCallEnds()
        {
            var first = new CallScript("dorothy_1", "Dorothy Quinn", "caller-08", "First", new JackId("A1"), new JackId("A2"), 20, 10, 5);
            var second = new CallScript("dorothy_2", " dorothy quinn ", "caller-08", "Second", new JackId("A1"), new JackId("B2"), 20, 10, 5);
            var other = Scripts.Call("other", "C1", "C2");
            GameSession session = Session(new[] { first, second, other }, 3);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "dorothy_1", "other" }));
            Call call = session.Calls.Live[0];
            session.Calls.Take(call.Id);
            session.Calls.Connect(call.Id);
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(2));
            session.Calls.Hold(call.Id);
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(2));
            session.Calls.End(call.Id, CallEndReason.DisconnectedEarly);
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(2), "Only one Dorothy may return, even with a free third slot");
        }

        [Test]
        public void NewBoardPoolSkipsCallerStillActiveFromPreviousPool()
        {
            var first = Scripts.Call("old", "A1", "A2");
            var duplicate = new CallScript("new", first.Name, first.Icon, "Another request", first.From, new JackId("B2"), 20, 10, 5);
            var other = Scripts.Call("other", "C1", "C2");
            GameSession session = Session(new[] { first }, 1,
                FakeUpgrade.BoardUpgrade("board_1", 1000, 4, new[] { duplicate, other }, 3));
            session.Tick(.01);
            session.Upgrades.TryBuy("board_1");
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "old", "other" }));
            session.Calls.End(session.Calls.Live[0].Id, CallEndReason.WrongConnection);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "other", "new" }));
        }

        [Test]
        public void NothingRingsBeforeTheFirstTick()
        {
            GameSession session = Session(Scripts.Pool(4, Small), 2);
            Assert.That(session.Calls.Live, Is.Empty);
        }

        [Test]
        public void TheFirstTickFillsEveryFreeSlotAtOnceInOrder()
        {
            GameSession session = Session(Scripts.Pool(5, Small), 2);
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }));
            session.Tick(5);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }), "no timer: only a free slot takes a call");
        }

        [Test]
        public void ThePoolLoopsBackToItsFirstCall()
        {
            GameSession session = Session(Scripts.Pool(3, Small), 1);
            var order = new List<string>();
            session.Calls.CallAdded += e => order.Add(e.Call.Request.Script.Id);
            session.Tick(0.01);
            for (int i = 0; i < 4; i++)
            {
                session.Calls.End(session.Calls.Live[0].Id, CallEndReason.WrongConnection);
                session.Tick(0.01);
            }
            Assert.That(order, Is.EqualTo(new[] { "call_1", "call_2", "call_3", "call_1", "call_2" }));
        }

        [Test]
        public void ACallStillOnACardIsSkipped()
        {
            GameSession session = Session(Scripts.Pool(3, Small), 2);
            session.Tick(0.01);
            Call second = session.Calls.Live[1];
            WrongNumber(session, second);
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_3" }));

            WrongNumber(session, session.Calls.Live[1], 1);
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }), "call_1 is still on its card, so call_2 comes");
        }

        [Test]
        public void WhenEveryCallIsOnACardTheFreeSlotStaysEmpty()
        {
            GameSession session = Session(Scripts.Pool(2, Small), 3);
            session.Tick(0.01);
            session.Tick(10);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }));
        }

        [Test]
        public void AnEmptyPoolRingsNothing()
        {
            GameSession session = Session(new CallScript[0], 2);
            session.Tick(10);
            Assert.That(session.Calls.Live, Is.Empty);
        }

        [Test]
        public void AWrongNumberFreesTheSlotAndTheNextCallAppearsWithNoFine()
        {
            GameSession session = Session(Scripts.Pool(4, Small), 2);
            session.Tick(0.01);
            long cents = session.Wallet.Cents;
            WrongNumber(session, session.Calls.Live[0]);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));

            session.Tick(0.01);

            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_2", "call_3" }));
            Assert.That(session.Wallet.Cents, Is.EqualTo(cents));
        }

        [Test]
        public void AConnectedCallHoldsItsSlotUntilItEndsThenTheNextTakesItInTheSameTick()
        {
            GameSession session = Session(Scripts.Pool(3, Small), 1);
            session.Tick(0.01);
            Call call = session.Calls.Live[0];
            session.Desk.TryTake(call.Id);
            session.Desk.OnCircuitClosed(new CordId(0), call.Request.From, call.Request.To);
            for (int i = 0; i < 19; i++) session.Tick(1);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1" }));

            session.Tick(1.5);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_2" }));
        }

        [Test]
        public void BuyingABiggerBoardSwitchesToItsPoolAndOpensASlotThatFillsAtOnce()
        {
            CallScript[] stage = Scripts.Pool(3, new BoardSize(4, 4), "stage_");
            GameSession session = Session(Scripts.Pool(4, Small), 2, FakeUpgrade.BoardUpgrade("board_1", 1000, 4, stage, 3));
            session.Tick(0.01);
            Call oldCall = session.Calls.Live[0];
            session.Desk.TryTake(oldCall.Id);
            session.Desk.OnCircuitClosed(new CordId(0), oldCall.Request.From, oldCall.Request.To);

            Assert.That(session.Upgrades.TryBuy("board_1"), Is.True);
            Assert.That(session.Queue.Pool, Is.SameAs(stage));
            Assert.That(session.Queue.Position, Is.Zero);
            session.Tick(0.01);

            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2", "stage_1" }),
                "calls already on cards stay; the new slot takes the new pool's first call");
            Assert.That(oldCall.State, Is.EqualTo(CallState.Connected));
            session.Calls.End(session.Calls.Live[1].Id, CallEndReason.WrongConnection);
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "stage_1", "stage_2" }));
        }

        [Test]
        public void ABoardEntryWithAnEmptyPoolKeepsTheQueueItFound()
        {
            GameSession session = Session(Scripts.Pool(4, Small), 1, FakeUpgrade.BoardUpgrade("board_1", 1000, 4, null, 2));
            session.Tick(0.01);
            IReadOnlyList<CallScript> pool = session.Queue.Pool;
            session.Upgrades.TryBuy("board_1");
            Assert.That(session.Queue.Pool, Is.SameAs(pool));
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }), "the queue carries on where it was");
        }

        [Test]
        public void ResetUpgradesGoesBackToTheStartPoolsFirstCall()
        {
            CallScript[] stage = Scripts.Pool(3, new BoardSize(4, 4), "stage_");
            GameSession session = Session(Scripts.Pool(4, Small), 2, FakeUpgrade.BoardUpgrade("board_1", 1000, 4, stage, 3));
            session.Tick(0.01);
            session.Upgrades.TryBuy("board_1");
            session.Tick(0.01);

            session.ResetUpgrades();
            session.Tick(0.01);

            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }));
        }

        [Test]
        public void ResetUpgradesRestartsTheStartPoolEvenWithNothingBought()
        {
            GameSession session = Session(Scripts.Pool(4, Small), 2);
            session.Tick(0.01);
            session.ResetUpgrades();
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_1", "call_2" }));
        }

        [Test]
        public void ClearCallsLetsTheQueueCarryOn()
        {
            GameSession session = Session(Scripts.Pool(5, Small), 2);
            session.Tick(0.01);
            session.ClearCalls();
            session.Tick(0.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "call_3", "call_4" }));
        }
    }
}
