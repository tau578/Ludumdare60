using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class GameSessionTests
    {
        private TestWorld world;
        private FakeSwitchboard board;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            world.Content.StartingCents = 7;
            board = new FakeSwitchboard();
            session = new GameSession(world.Content, board);
        }

        private void Advance(double seconds)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += 0.05) session.Tick(0.05);
        }

        [Test]
        public void StartsWithTheConfiguredMoneyAndTheFirstCallOnTheFirstTick()
        {
            Assert.That(session.Wallet.Cents, Is.EqualTo(7));
            Assert.That(session.Calls.Live, Is.Empty);
            session.Tick(0.05);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));
            Assert.That(session.Calls.Live[0].Request.Script, Is.SameAs(world.AdaCallsBasil));
        }

        [Test]
        public void PatchingTheCallsOwnJacksPaysUntilItFinishes()
        {
            session.Tick(0.05);
            Call call = session.Calls.Live[0];
            Assert.That(session.Desk.TryTake(call.Id), Is.True);
            session.Desk.OnCircuitClosed(new CordId(0), TestWorld.B2, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));

            Advance(call.Request.DurationSeconds + 0.5);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(session.Wallet.Cents, Is.EqualTo(7 + 10 + 5 * 20));
            Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void ClearCallsEndsEverythingWithoutPayOrFines()
        {
            var content = new FakeContent { StartingClientSlots = 4, StartPool = Scripts.Pool(6, BoardSize.Full) };
            var crowded = new GameSession(content, board);
            crowded.Tick(0.05);
            Assert.That(crowded.Calls.Live.Count, Is.EqualTo(4));

            Call connected = crowded.Calls.Live[0];
            crowded.Desk.TryTake(connected.Id);
            crowded.Desk.OnCircuitClosed(new CordId(0), connected.Request.From, connected.Request.To);
            Call taken = crowded.Calls.Live[1];
            crowded.Desk.TryTake(taken.Id);
            crowded.Desk.OnCircuitClosed(new CordId(1), new JackId("F1"), new JackId("F2"));
            crowded.Desk.OnPlugInserted(new CordId(2), taken.Request.From);
            for (int frame = 0; frame < 20; frame++) crowded.Tick(0.05);
            board.Returned.Clear();

            var reasons = new System.Collections.Generic.List<CallEndReason>();
            crowded.Calls.CallEnded += e => reasons.Add(e.Reason);
            long cents = crowded.Wallet.Cents;
            crowded.ClearCalls();

            Assert.That(crowded.Calls.Live, Is.Empty);
            Assert.That(crowded.Calls.InHand, Is.Null);
            Assert.That(reasons, Has.All.EqualTo(CallEndReason.Cleared));
            Assert.That(reasons.Count, Is.EqualTo(4));
            Assert.That(board.Returned, Is.EquivalentTo(new[] { new CordId(0), new CordId(1), new CordId(2) }));
            Assert.That(crowded.Desk.HalfPatched, Is.Empty);
            Assert.That(crowded.Wallet.Cents, Is.EqualTo(cents));
        }

        [Test]
        public void ClearCallsAlsoClearsOnHoldCalls()
        {
            session.Tick(0.05);
            Call call = session.Calls.Live[0];
            session.Desk.TryTake(call.Id);
            session.Desk.OnCircuitClosed(new CordId(0), TestWorld.A1, TestWorld.B2);
            Advance(1);
            session.Desk.OnCircuitOpened(new CordId(0), TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            long cents = session.Wallet.Cents;

            session.ClearCalls();

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Cleared));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(board.Returned, Is.EqualTo(new[] { new CordId(0) }));
            Assert.That(session.Desk.CallOn(new CordId(0)), Is.Null);
            Assert.That(session.Wallet.Cents, Is.EqualTo(cents));
        }

        [Test]
        public void AMissedEmergencyIsFinedAndItsSlotRefillsInTheSameTick()
        {
            var content = new FakeContent
            {
                StartingCents = 1000,
                StartingClientSlots = 1,
                StartPool = new[] { Scripts.Emergency("doctor", "A1", "D4"), Scripts.Call("regular", "B1", "B2") }
            };
            var timed = new GameSession(content, board);
            timed.Tick(0.05);
            Call emergency = timed.Calls.Live[0];
            for (int i = 0; i < 29; i++) timed.Tick(1);
            Assert.That(emergency.State, Is.EqualTo(CallState.Incoming));

            timed.Tick(1);

            Assert.That(emergency.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(timed.Wallet.Cents, Is.EqualTo(800), "200 cents, never multiplied");
            Assert.That(timed.Calls.Live.Count, Is.EqualTo(1));
            Assert.That(timed.Calls.Live[0].Request.Script.Id, Is.EqualTo("regular"));
        }

        [Test]
        public void TheMultiplierPaysOnTheFeeAndTheRate()
        {
            var content = new FakeContent
            {
                StartingCents = 2000,
                StartingClientSlots = 1,
                StartPool = new[] { Scripts.Call("paid", "A1", "B2", 5, 10, 10) },
                Upgrades = new IUpgrade[] { FakeUpgrade.Income("income_1", 800, 2), new FakeUpgrade("secret", UpgradeKind.Secret, 25000) }
            };
            var paid = new GameSession(content, board);
            paid.Upgrades.TryBuy("income_1");
            paid.Tick(0.05);
            Call call = paid.Calls.Live[0];
            paid.Desk.TryTake(call.Id);
            paid.Desk.OnCircuitClosed(new CordId(0), TestWorld.A1, TestWorld.B2);
            for (int i = 0; i < 11; i++) paid.Tick(1);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(paid.Wallet.Cents, Is.EqualTo(1200 + (10 + 5 * 10) * 2));
        }
    }
}
