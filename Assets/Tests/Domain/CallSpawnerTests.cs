using System;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CallSpawnerTests
    {
        private GameClock clock;
        private CallService calls;
        private CallbackScheduler callbacks;
        private CallSpawner spawner;
        private int cords;

        private CallSpawner Build(Crowd crowd, IPacingProfile pacing, int maxLiveCalls = 6, int seed = 11)
        {
            clock = new GameClock();
            calls = new CallService();
            var random = new SeededRandom(seed);
            callbacks = new CallbackScheduler(calls, clock, random, maxLiveCalls);
            spawner = new CallSpawner(calls, new CallFactory(random), crowd.Directory, crowd.Callers, random,
                new BusyTracker(calls, callbacks), pacing, maxLiveCalls, () => cords);
            return spawner;
        }

        [SetUp]
        public void SetUp() => cords = 3;

        private void Advance(double seconds, double step = 0.1)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += step)
            {
                clock.Advance(step);
                spawner.Tick(step);
            }
        }

        [Test]
        public void FirstCallRingsAfterTheFirstCallDelay()
        {
            Build(new Crowd(12), new PacingSettings(2, 8, 0, 0));
            Advance(1.9);
            Assert.That(calls.Live, Is.Empty);
            Advance(0.3);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(calls.Live[0].State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void CallsRingEveryBaseIntervalWhileCordsKeepUp()
        {
            cords = 100;
            Build(new Crowd(12), new PacingSettings(2, 8, 0, 0.5));
            Advance(2.2);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Advance(7.7);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Advance(0.5);
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Advance(8);
            Assert.That(calls.Live.Count, Is.EqualTo(3));
        }

        [Test]
        public void JitteredIntervalsStayWithinBounds()
        {
            Build(new Crowd(12), new PacingSettings(2, 8, 0.3, 0));
            double shortest = double.MaxValue, longest = 0;
            for (int i = 0; i < 2000; i++)
            {
                double interval = spawner.NextInterval();
                shortest = Math.Min(shortest, interval);
                longest = Math.Max(longest, interval);
            }
            Assert.That(shortest, Is.GreaterThanOrEqualTo(8 * 0.7));
            Assert.That(longest, Is.LessThanOrEqualTo(8 * 1.3));
            Assert.That(shortest, Is.LessThan(8 * 0.75), "the jitter is used");
            Assert.That(longest, Is.GreaterThan(8 * 1.25), "the jitter is used");
        }

        [Test]
        public void LoadIsOneWhileEveryWaitingCallHasAFreeCord()
        {
            var crowd = new Crowd(12);
            Build(crowd, new PacingSettings(2, 8, 0, 0.5));
            calls.Add(crowd.Request(0, 1));
            calls.Add(crowd.Request(2, 3));
            calls.Add(crowd.Request(4, 5));
            Assert.That(spawner.Load(), Is.EqualTo(1));

            Call connected = calls.Live[0];
            calls.Take(connected.Id);
            calls.Connect(connected.Id);
            calls.Add(crowd.Request(6, 7));
            // Three cords, one in use: two free for three waiting calls.
            Assert.That(spawner.Load(), Is.EqualTo(1.5));
        }

        [Test]
        public void LoadFactorStretchesTheIntervalWhileCallsPileUp()
        {
            cords = 0;
            Build(new Crowd(12), new PacingSettings(2, 10, 0, 1));
            Advance(2.2);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            // One waiting call and no free cord: load 2, so the 10 s interval takes 20 s.
            Advance(19.7);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Advance(0.5);
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            // Two waiting: load 3, so 30 s.
            Advance(29.5);
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Advance(0.5);
            Assert.That(calls.Live.Count, Is.EqualTo(3));
        }

        [TestCase(6)]
        [TestCase(4)]
        public void LiveCallsNeverExceedTheCap(int cap)
        {
            cords = 100;
            Build(new Crowd(20), new PacingSettings(0.5, 1, 0.3, 0), cap);
            int most = 0;
            for (int i = 0; i < 3000; i++)
            {
                clock.Advance(0.1);
                spawner.Tick(0.1);
                most = Math.Max(most, calls.Live.Count);
                Assert.That(spawner.TrySpawn() && calls.Live.Count > cap, Is.False);
                most = Math.Max(most, calls.Live.Count);
                if (i % 40 == 39) calls.End(calls.Live[0].Id, CallEndReason.Finished);
            }
            Assert.That(most, Is.EqualTo(cap));
        }

        [Test]
        public void ConnectedCallsCountTowardsTheCap()
        {
            cords = 100;
            Build(new Crowd(20), new PacingSettings(0, 1, 0, 0), maxLiveCalls: 2);
            Advance(0.5);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Call call = calls.Live[0];
            calls.Take(call.Id);
            calls.Connect(call.Id);
            Advance(10);
            Assert.That(calls.Live.Count, Is.EqualTo(2));
            Assert.That(spawner.TrySpawn(), Is.False);
        }

        [Test]
        public void NoOneIsPickedWhileBusy()
        {
            cords = 100;
            var crowd = new Crowd(7);
            Build(crowd, new PacingSettings(0.2, 0.5, 0.3, 0), seed: 3);
            int added = 0;
            calls.CallAdded += e =>
            {
                added++;
                CallRequest request = e.Call.Request;
                Assert.That(request.Recipient.IsSamePerson(request.CallerPerson), Is.False);
                foreach (Call other in calls.Live)
                {
                    if (other == e.Call) continue;
                    Assert.That(other.Request.Involves(request.CallerPerson), Is.False);
                    Assert.That(other.Request.Involves(request.Recipient), Is.False);
                }
                foreach (PendingCallback pending in callbacks.Pending)
                {
                    Assert.That(pending.Request.Involves(request.CallerPerson), Is.False);
                    Assert.That(pending.Request.Involves(request.Recipient), Is.False);
                }
            };
            for (int i = 0; i < 4000; i++)
            {
                clock.Advance(0.1);
                callbacks.Tick();
                spawner.Tick(0.1);
                if (i % 7 == 6 && calls.Live.Count > 0)
                    calls.End(calls.Live[0].Id, i % 2 == 0 ? CallEndReason.WrongConnection : CallEndReason.Finished);
            }
            Assert.That(added, Is.GreaterThan(40));
        }

        [Test]
        public void AttemptWithNobodyFreeIsSkippedQuietly()
        {
            var crowd = new Crowd(3);
            Build(crowd, new PacingSettings(0, 1, 0, 0));
            calls.Add(crowd.Request(0, 1));
            Assert.That(spawner.TrySpawn(), Is.False, "only one person is free");
            Advance(5);
            Assert.That(calls.Live.Count, Is.EqualTo(1));

            calls.End(calls.Live[0].Id, CallEndReason.Finished);
            Advance(1.1);
            Assert.That(calls.Live.Count, Is.EqualTo(1), "it retries on the next interval");
        }

        [Test]
        public void PeopleWithAPendingCallbackAreNotPicked()
        {
            var crowd = new Crowd(4);
            Build(crowd, new PacingSettings(0, 1, 0, 0));
            Call wrong = calls.Add(crowd.Request(0, 1));
            calls.End(wrong.Id, CallEndReason.WrongConnection);
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));

            for (int i = 0; i < 50; i++)
            {
                Assert.That(spawner.TrySpawn(), Is.True);
                Call call = calls.Live[0];
                Assert.That(call.Request.Involves(crowd.People[0]), Is.False);
                Assert.That(call.Request.Involves(crowd.People[1]), Is.False);
                calls.End(call.Id, CallEndReason.Finished);
            }
        }

        [Test]
        public void CallersMissingFromTheDirectoryNeverCall()
        {
            var world = new TestWorld();
            var stranger = new FakeCaller(new FakePerson("Otis", "Bramble"), world.Regular, weight: 100);
            clock = new GameClock();
            calls = new CallService();
            var random = new SeededRandom(5);
            callbacks = new CallbackScheduler(calls, clock, random, 6);
            spawner = new CallSpawner(calls, new CallFactory(random), world.Directory, new ICaller[] { stranger, world.AdaCaller },
                random, new BusyTracker(calls, callbacks), new PacingSettings(0, 1, 0, 0), 6, () => 3);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(spawner.TrySpawn(), Is.True);
                Assert.That(calls.Live[0].Request.Caller, Is.SameAs(world.AdaCaller));
                calls.End(calls.Live[0].Id, CallEndReason.Finished);
            }
        }
    }
}
