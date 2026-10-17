using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CallbackSchedulerTests
    {
        private Crowd crowd;
        private GameClock clock;
        private CallService calls;
        private CallbackScheduler callbacks;
        private SeededRandom random;

        private void Build(int maxLiveCalls = 6, FloatRange? delay = null)
        {
            crowd = new Crowd(8, delay ?? new FloatRange(20, 40));
            clock = new GameClock();
            calls = new CallService();
            random = new SeededRandom(17);
            callbacks = new CallbackScheduler(calls, clock, random, maxLiveCalls);
        }

        private void Advance(double seconds)
        {
            clock.Advance(seconds);
            callbacks.Tick();
        }

        private Call WrongConnection(int caller, int recipient)
        {
            Call call = calls.Add(crowd.Request(caller, recipient, centsPerSecond: 6, durationSeconds: 25));
            calls.Take(call.Id);
            calls.End(call.Id, CallEndReason.WrongConnection);
            return call;
        }

        [Test]
        public void WrongConnectionRingsAgainAfterTheDelayWithTheSameRequest()
        {
            Build(delay: new FloatRange(30, 30));
            Advance(5);
            Call wrong = WrongConnection(0, 1);
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));
            Assert.That(callbacks.Pending[0].DueAt, Is.EqualTo(35).Within(1e-9));

            Advance(29.9);
            Assert.That(calls.Live, Is.Empty);
            Advance(0.2);

            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Call again = calls.Live[0];
            Assert.That(again.Id, Is.Not.EqualTo(wrong.Id));
            Assert.That(again.State, Is.EqualTo(CallState.Incoming));
            Assert.That(again.Request.Attempt, Is.EqualTo(2));
            Assert.That(again.Request.Caller, Is.SameAs(wrong.Request.Caller));
            Assert.That(again.Request.Recipient, Is.SameAs(wrong.Request.Recipient));
            Assert.That(again.Request.CentsPerSecond, Is.EqualTo(6));
            Assert.That(again.Request.DurationSeconds, Is.EqualTo(25));
            Assert.That(again.Request.ConnectionFeeCents, Is.EqualTo(wrong.Request.ConnectionFeeCents));
            Assert.That(callbacks.Pending, Is.Empty);
        }

        [Test]
        public void DelayIsRolledWithinTheTypesRange()
        {
            Build(maxLiveCalls: 0);
            double shortest = double.MaxValue, longest = 0;
            for (int i = 0; i < 300; i++)
            {
                WrongConnection(i % 8, (i + 1) % 8);
                double dueAt = callbacks.Pending[0].DueAt;
                callbacks.Clear();
                shortest = System.Math.Min(shortest, dueAt);
                longest = System.Math.Max(longest, dueAt);
            }
            Assert.That(shortest, Is.GreaterThanOrEqualTo(20));
            Assert.That(longest, Is.LessThanOrEqualTo(40));
            Assert.That(longest - shortest, Is.GreaterThan(15), "the delay is rolled, not fixed");
        }

        [TestCase(CallEndReason.Finished)]
        [TestCase(CallEndReason.DisconnectedEarly)]
        [TestCase(CallEndReason.Missed)]
        [TestCase(CallEndReason.Cleared)]
        public void OnlyWrongConnectionsCallBack(CallEndReason reason)
        {
            Build();
            Call call = calls.Add(crowd.Request(0, 1));
            calls.End(call.Id, reason);
            Assert.That(callbacks.Pending, Is.Empty);
        }

        [Test]
        public void DueCallbackWaitsWhileEverySlotIsUsed()
        {
            Build(maxLiveCalls: 2, delay: new FloatRange(20, 20));
            WrongConnection(0, 1);
            Call a = calls.Add(crowd.Request(2, 3));
            calls.Add(crowd.Request(4, 5));

            Advance(60);
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));

            calls.End(a.Id, CallEndReason.Finished);
            Advance(0.1);
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Assert.That(calls.Live[1].Request.Attempt, Is.EqualTo(2));
            Assert.That(callbacks.Pending, Is.Empty);
        }

        [Test]
        public void DueCallbackWaitsWhileAPartyIsOnAnotherCall()
        {
            Build(delay: new FloatRange(20, 20));
            WrongConnection(0, 1);
            // The spawner never does this (D18); the guard still holds if something else does.
            Call other = calls.Add(crowd.Request(2, 1));

            Advance(30);
            Assert.That(calls.Live, Is.EqualTo(new[] { other }));
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));

            calls.End(other.Id, CallEndReason.Finished);
            Advance(0.1);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(calls.Live[0].Request.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void DueCallbacksRingInDueOrderAndABlockedOneDoesNotHoldOthersBack()
        {
            Build(delay: new FloatRange(20, 40));
            WrongConnection(0, 1);
            WrongConnection(2, 3);
            WrongConnection(4, 5);
            double first = callbacks.Pending[0].DueAt;
            Assert.That(callbacks.Pending[1].DueAt, Is.GreaterThanOrEqualTo(first));
            Assert.That(callbacks.Pending[2].DueAt, Is.GreaterThanOrEqualTo(callbacks.Pending[1].DueAt));

            CallRequest blocked = callbacks.Pending[0].Request;
            Call busy = calls.Add(new CallRequest(crowd.Callers[6], blocked.Recipient, 5, 20));
            Advance(41);

            Assert.That(calls.Live.Count, Is.EqualTo(3));
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));
            Assert.That(callbacks.Pending[0].Request, Is.SameAs(blocked));
            Assert.That(calls.Live[1].Request.DurationSeconds, Is.EqualTo(25));
            calls.End(busy.Id, CallEndReason.Finished);
            Advance(0.1);
            Assert.That(callbacks.Pending, Is.Empty);
        }

        [Test]
        public void CallbacksGoAheadOfNewSpawns()
        {
            Build(maxLiveCalls: 2, delay: new FloatRange(20, 20));
            var spawner = new CallSpawner(calls, new CallFactory(random), crowd.Directory, crowd.Callers, random,
                new BusyTracker(calls, callbacks), new PacingSettings(0, 1, 0, 0), 2, () => 10);
            WrongConnection(0, 1);
            Call a = calls.Add(crowd.Request(2, 3));
            calls.Add(crowd.Request(4, 5));
            clock.Advance(30);
            callbacks.Tick();
            spawner.Tick(5);
            Assert.That(calls.Live.Count, Is.EqualTo(2), "full: neither rings");

            calls.End(a.Id, CallEndReason.Finished);
            // Tick order of GameSession: callbacks, then the spawner.
            callbacks.Tick();
            spawner.Tick(5);

            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Assert.That(calls.Live[1].Request.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void ClearDropsEveryPendingCallback()
        {
            Build();
            WrongConnection(0, 1);
            WrongConnection(2, 3);
            callbacks.Clear();
            Advance(100);
            Assert.That(callbacks.Pending, Is.Empty);
            Assert.That(calls.Live, Is.Empty);
        }
    }
}
