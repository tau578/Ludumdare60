using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class BusyTrackerTests
    {
        private Crowd crowd;
        private CallService calls;
        private CallbackScheduler callbacks;
        private BusyTracker busy;

        [SetUp]
        public void SetUp()
        {
            crowd = new Crowd(4);
            calls = new CallService();
            callbacks = new CallbackScheduler(calls, new GameClock(), new SeededRandom(3), () => 6);
            busy = new BusyTracker(calls, callbacks);
        }

        [TestCase(CallState.Incoming)]
        [TestCase(CallState.Taken)]
        [TestCase(CallState.Connecting)]
        [TestCase(CallState.Connected)]
        [TestCase(CallState.OnHold)]
        public void BothPartiesOfALiveCallAreBusy(CallState state)
        {
            Call call = calls.Add(crowd.Request(0, 1));
            if (state != CallState.Incoming) calls.Take(call.Id);
            if (state == CallState.Connecting) calls.BeginConnecting(call.Id);
            if (state == CallState.Connected || state == CallState.OnHold) calls.Connect(call.Id);
            if (state == CallState.OnHold) calls.Hold(call.Id);
            Assert.That(call.State, Is.EqualTo(state));

            Assert.That(busy.IsBusy(crowd.People[0]), Is.True, "caller");
            Assert.That(busy.IsBusy(crowd.People[1]), Is.True, "recipient");
            Assert.That(busy.IsBusy(crowd.People[2]), Is.False);
        }

        [Test]
        public void PeopleAreFreeOnceTheirCallEnds()
        {
            Call call = calls.Add(crowd.Request(0, 1));
            calls.End(call.Id, CallEndReason.Finished);
            Assert.That(busy.IsBusy(crowd.People[0]), Is.False);
            Assert.That(busy.IsBusy(crowd.People[1]), Is.False);
        }

        [Test]
        public void BothPartiesOfAPendingCallbackAreBusy()
        {
            Call call = calls.Add(crowd.Request(0, 1));
            calls.End(call.Id, CallEndReason.WrongConnection);
            Assert.That(calls.Live, Is.Empty);
            Assert.That(busy.IsBusy(crowd.People[0]), Is.True, "caller");
            Assert.That(busy.IsBusy(crowd.People[1]), Is.True, "recipient");
            Assert.That(busy.IsBusy(crowd.People[3]), Is.False);
        }
    }
}
