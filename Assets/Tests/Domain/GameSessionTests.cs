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
            session = new GameSession(world.Content, new SeededRandom(21), board);
        }

        private void Advance(double seconds)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += 0.05) session.Tick(0.05);
        }

        [Test]
        public void StartsWithTheConfiguredMoneyAndAQuietBoard()
        {
            Assert.That(session.Wallet.Cents, Is.EqualTo(7));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(session.Directory.ListedEntries.Count, Is.EqualTo(3));
        }

        [Test]
        public void FirstCallRingsAboutTwoSecondsAfterStart()
        {
            Advance(1.9);
            Assert.That(session.Calls.Live, Is.Empty);
            Advance(0.2);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));
            Assert.That(session.Clock.Now, Is.EqualTo(2.1).Within(1e-6));
        }

        [Test]
        public void PatchingTheRightPairPaysUntilTheCallFinishes()
        {
            Advance(2.1);
            Call call = session.Calls.Live[0];
            Assert.That(session.Desk.TryTake(call.Id), Is.True);
            session.Desk.OnCircuitClosed(new CordId(0),
                session.Directory.JackOf(call.Request.Recipient),
                session.Directory.JackOf(call.Request.CallerPerson));
            Assert.That(call.State, Is.EqualTo(CallState.Connected));

            Advance(call.Request.DurationSeconds + 0.5);

            long expected = 7 + (long)(call.Request.ConnectionFeeCents + call.Request.CentsPerSecond * call.Request.DurationSeconds);
            Assert.That(call.Request.ConnectionFeeCents, Is.GreaterThan(0));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(session.Wallet.Cents, Is.EqualTo(expected));
            Assert.That(board.Returned, Is.EqualTo(new[] { new CordId(0) }));
        }

        [Test]
        public void CallersPileUpToTheCapAndStop()
        {
            var crowd = new Crowd(14);
            var crowded = new GameSession(crowd.Content, new SeededRandom(4), board);
            int most = 0;
            for (int frame = 0; frame < 6000; frame++)
            {
                crowded.Tick(0.05);
                most = System.Math.Max(most, crowded.Calls.Live.Count);
            }
            Assert.That(most, Is.EqualTo(GameSession.DefaultMaxWaitingCalls));
            Assert.That(crowded.Calls.Live.Count, Is.EqualTo(6));
        }

        [Test]
        public void WrongNumberCallsBackThroughTheSession()
        {
            Advance(2.1);
            Call call = session.Calls.Live[0];
            session.Desk.TryTake(call.Id);
            JackId callerJack = session.Directory.JackOf(call.Request.CallerPerson);
            JackId wrongJack = callerJack == TestWorld.A1 ? TestWorld.B2 : TestWorld.A1;
            if (wrongJack == session.Directory.JackOf(call.Request.Recipient)) wrongJack = TestWorld.C3;
            session.Desk.OnCircuitClosed(new CordId(1), callerJack, wrongJack);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));

            Advance(19.9);
            Assert.That(session.Calls.Live, Is.Empty, "both parties wait for their callback; the third person alone can't call");
            Advance(20.2);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));
            Call again = session.Calls.Live[0];
            Assert.That(again.Request.Attempt, Is.EqualTo(2));
            Assert.That(again.Request.Caller, Is.SameAs(call.Request.Caller));
            Assert.That(again.Request.Recipient, Is.SameAs(call.Request.Recipient));
        }

        [Test]
        public void ClearCallsEndsEverythingWithoutPayFinesOrCallbacks()
        {
            var crowd = new Crowd(10);
            var crowded = new GameSession(crowd.Content, new SeededRandom(8), board);
            for (int frame = 0; frame < 1600 && crowded.Calls.Live.Count < 4; frame++) crowded.Tick(0.05);
            Assert.That(crowded.Calls.Live.Count, Is.GreaterThanOrEqualTo(4));

            Call connected = crowded.Calls.Live[0];
            crowded.Desk.TryTake(connected.Id);
            crowded.Desk.OnCircuitClosed(new CordId(0), crowded.Directory.JackOf(connected.Request.CallerPerson),
                crowded.Directory.JackOf(connected.Request.Recipient));
            Call wrong = crowded.Calls.Live[1];
            crowded.Desk.TryTake(wrong.Id);
            crowded.Desk.OnCircuitClosed(new CordId(1), new JackId("J1"), new JackId("J99"));
            Assert.That(crowded.Callbacks.Pending.Count, Is.EqualTo(1));
            crowded.Desk.TryTake(crowded.Calls.Live[1].Id);
            crowded.Desk.OnPlugInserted(new CordId(2), new JackId("J5"));
            for (int frame = 0; frame < 20; frame++) crowded.Tick(0.05);
            board.Returned.Clear();

            var reasons = new System.Collections.Generic.List<CallEndReason>();
            crowded.Calls.CallEnded += e => reasons.Add(e.Reason);
            long cents = crowded.Wallet.Cents;
            crowded.ClearCalls();

            Assert.That(crowded.Calls.Live, Is.Empty);
            Assert.That(crowded.Calls.InHand, Is.Null);
            Assert.That(crowded.Callbacks.Pending, Is.Empty);
            Assert.That(reasons, Has.All.EqualTo(CallEndReason.Cleared));
            Assert.That(reasons.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(board.Returned, Is.EquivalentTo(new[] { new CordId(0), new CordId(2) }));
            Assert.That(crowded.Desk.HalfPatched, Is.Empty);
            Assert.That(crowded.Wallet.Cents, Is.EqualTo(cents));
            crowded.Tick(0.05);
            Assert.That(crowded.Wallet.Cents, Is.EqualTo(cents), "nothing is paid after clearing");
        }

        [Test]
        public void ClearCallsAlsoClearsOnHoldCalls()
        {
            Advance(2.1);
            Call call = session.Calls.Live[0];
            session.Desk.TryTake(call.Id);
            JackId callerJack = session.Directory.JackOf(call.Request.CallerPerson);
            JackId recipientJack = session.Directory.JackOf(call.Request.Recipient);
            session.Desk.OnCircuitClosed(new CordId(0), callerJack, recipientJack);
            Advance(1);
            session.Desk.OnCircuitOpened(new CordId(0), callerJack, recipientJack);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            long cents = session.Wallet.Cents;

            session.ClearCalls();

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Cleared));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(session.Callbacks.Pending, Is.Empty);
            Assert.That(board.Returned, Is.EqualTo(new[] { new CordId(0) }));
            Assert.That(session.Desk.CallOn(new CordId(0)), Is.Null);
            Assert.That(session.Wallet.Cents, Is.EqualTo(cents));
        }
    }
}
