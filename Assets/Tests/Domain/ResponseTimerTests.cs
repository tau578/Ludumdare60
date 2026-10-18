using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>An emergency must be connected correctly within its response time (D09, Design 5.1).</summary>
    public sealed class ResponseTimerTests
    {
        private EmergencyWorld world;
        private CallService calls;
        private FakeSwitchboard board;
        private OperatorDesk desk;

        [SetUp]
        public void SetUp()
        {
            world = new EmergencyWorld();
            calls = new CallService();
            board = new FakeSwitchboard();
            desk = new OperatorDesk(calls, new ConnectionRule(world.Directory), board);
        }

        private JackId Jack(IPerson person) => world.Directory.JackOf(person);

        [Test]
        public void CountsDownWhileRingingInHandAndConnecting()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            Assert.That(call.HasResponseTime, Is.True);
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(30));

            calls.Tick(5);
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(25).Within(1e-9));
            desk.TryTake(call.Id);
            calls.Tick(5);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(20).Within(1e-9));
            desk.OnPlugInserted(new CordId(0), Jack(call.Request.CallerPerson));
            calls.Tick(5);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(15).Within(1e-9));
        }

        [Test]
        public void KeepsCountingAfterAnotherOrderIsAccepted()
        {
            Call emergency = calls.Add(world.EmergencyRequest(0));
            Call regular = calls.Add(new CallRequest(world.Callers[1], world.People[2], 5, 20));
            desk.TryTake(emergency.Id);
            desk.TryTake(regular.Id);

            calls.Tick(10);

            Assert.That(emergency.State, Is.EqualTo(CallState.Taken));
            Assert.That(regular.State, Is.EqualTo(CallState.Taken));
            Assert.That(emergency.ResponseSecondsLeft, Is.EqualTo(20).Within(1e-9));
        }

        [Test]
        public void StopsOnACorrectConnectionAndStaysStoppedThroughHoldAndResume()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(call.Id);
            calls.Tick(10);
            JackId caller = Jack(call.Request.CallerPerson), service = Jack(call.Request.Recipient);
            desk.OnCircuitClosed(new CordId(0), caller, service);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));

            calls.Tick(5);
            desk.OnCircuitOpened(new CordId(0), caller, service);
            calls.Tick(40);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            desk.OnCircuitClosed(new CordId(0), service, caller);
            calls.Tick(9.9);

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(20).Within(1e-9));
            calls.Tick(0.2);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
        }

        [Test]
        public void RunningOutEndsTheCallAsMissed()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            CallEndReason ended = CallEndReason.None;
            calls.CallEnded += e => ended = e.Reason;

            calls.Tick(29.9);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
            calls.Tick(0.2);

            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(ended, Is.EqualTo(CallEndReason.Missed));
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(0));
            Assert.That(calls.Live, Is.Empty);
        }

        [Test]
        public void RegularCallsWaitAsLongAsItTakes()
        {
            Call call = calls.Add(new CallRequest(world.Callers[0], world.People[1], 5, 20));

            calls.Tick(10000);

            Assert.That(call.HasResponseTime, Is.False);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void MissingAnOrderDoesNotRemoveOrRefuseItsWire()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(call.Id);
            JackId caller = Jack(call.Request.CallerPerson);
            desk.OnPlugInserted(new CordId(0), caller);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));

            calls.Tick(31);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(calls.InHand, Is.Null);
            // The cord is the player's: it stays where it is, and nothing sends it back.
            Assert.That(desk.HalfPatched, Is.EqualTo(new[] { new CordId(0) }));
            Assert.That(board.Returned, Is.Empty);

            desk.OnCircuitClosed(new CordId(0), caller, Jack(call.Request.Recipient));

            Assert.That(board.Refused, Is.Empty);
            Assert.That(desk.HalfPatched, Is.Empty);
            Assert.That(desk.CallOn(new CordId(0)), Is.Null);
        }

        [Test]
        public void AnUnrelatedNewCallAfterAMissDoesNotUseTheOldHalfCord()
        {
            Call missed = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(missed.Id);
            desk.OnPlugInserted(new CordId(0), Jack(missed.Request.CallerPerson));
            calls.Tick(31);
            Call next = calls.Add(new CallRequest(world.Callers[1], world.People[2], 5, 20));

            desk.TryTake(next.Id);

            Assert.That(next.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void ACallbackAfterAMissStartsWithTheFullTime()
        {
            world.Emergency.CallbackAfterMiss = true;
            var clock = new GameClock();
            var callbacks = new CallbackScheduler(calls, clock, new SeededRandom(5), () => 6);
            calls.Add(world.EmergencyRequest(0));
            calls.Tick(31);
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));

            clock.Advance(41);
            callbacks.Tick();

            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(calls.Live[0].Request.Attempt, Is.EqualTo(2));
            Assert.That(calls.Live[0].ResponseSecondsLeft, Is.EqualTo(30));
        }
    }
}
