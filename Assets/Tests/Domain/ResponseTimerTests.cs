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
            desk = new OperatorDesk(calls, new ConnectionRule(), board);
        }

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
            desk.OnPlugInserted(new CordId(0), call.Request.From);
            calls.Tick(5);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            Assert.That(call.ResponseSecondsLeft, Is.EqualTo(15).Within(1e-9));
        }

        [Test]
        public void KeepsCountingAfterAnotherOrderIsAccepted()
        {
            Call emergency = calls.Add(world.EmergencyRequest(0));
            Call regular = calls.Add(world.RegularRequest(1, 2));
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
            JackId caller = call.Request.From, service = call.Request.To;
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
            Call call = calls.Add(world.RegularRequest(0, 1));

            calls.Tick(10000);

            Assert.That(call.HasResponseTime, Is.False);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void MissingAnOrderDoesNotRemoveOrRefuseItsWire()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(call.Id);
            JackId caller = call.Request.From;
            desk.OnPlugInserted(new CordId(0), caller);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));

            calls.Tick(31);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(calls.InHand, Is.Null);
            // The cord is the player's: it stays where it is, and nothing sends it back.
            Assert.That(desk.HalfPatched, Is.EqualTo(new[] { new CordId(0) }));
            Assert.That(board.Returned, Is.Empty);

            desk.OnCircuitClosed(new CordId(0), caller, call.Request.To);

            Assert.That(board.Refused, Is.Empty);
            Assert.That(desk.HalfPatched, Is.Empty);
            Assert.That(desk.CallOn(new CordId(0)), Is.Null);
        }

        [Test]
        public void AnUnrelatedNewCallAfterAMissDoesNotUseTheOldHalfCord()
        {
            Call missed = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(missed.Id);
            desk.OnPlugInserted(new CordId(0), missed.Request.From);
            calls.Tick(31);
            Call next = calls.Add(world.RegularRequest(1, 2));

            desk.TryTake(next.Id);

            Assert.That(next.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void AGivenResponseTimeOnARegularCallIsIgnored()
        {
            var script = new CallScript("regular", "Ada Moss", "caller-01", "Hello", new JackId("A1"), new JackId("B2"), 20, 10, 5,
                emergency: false, responseTimeSeconds: 30, penaltyCents: 200);
            Call call = calls.Add(new CallRequest(script));
            Assert.That(call.HasResponseTime, Is.False);
            Assert.That(script.PenaltyCents, Is.Zero);
            calls.Tick(60);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void EachEmergencyUsesItsOwnResponseTime()
        {
            Call quick = calls.Add(new CallRequest(Scripts.Emergency("quick", "A1", "F4", responseTimeSeconds: 10)));
            Call slow = calls.Add(new CallRequest(Scripts.Emergency("slow", "A2", "F4", responseTimeSeconds: 45)));
            calls.Tick(11);
            Assert.That(quick.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(slow.State, Is.EqualTo(CallState.Incoming));
            Assert.That(slow.ResponseSecondsLeft, Is.EqualTo(34).Within(1e-9));
        }
    }
}
