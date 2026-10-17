using System;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CallSpawnerTests
    {
        private TestWorld world;
        private CallService calls;
        private CallSpawner spawner;
        private int spawned;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            calls.CallAdded += _ => spawned++;
            var random = new SeededRandom(11);
            spawner = new CallSpawner(calls, new CallFactory(random), world.Directory, world.Callers, random, delaySeconds: 2);
            spawned = 0;
        }

        private void Advance(double seconds, double step = 0.1)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += step) spawner.Tick(step);
        }

        [Test]
        public void OneCallRingsAfterTheDelay()
        {
            Advance(1.9);
            Assert.That(calls.Live, Is.Empty);
            Advance(0.3);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(calls.Live[0].State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void NextCallRingsTheDelayAfterThePreviousEnds()
        {
            Advance(2.2);
            calls.End(calls.Live[0].Id, CallEndReason.Finished);
            Advance(1.9);
            Assert.That(calls.Live, Is.Empty);
            Advance(0.3);
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(spawned, Is.EqualTo(2));
        }

        [Test]
        public void NeverRingsASecondCallWhileOneIsLive()
        {
            int most = 0;
            for (int frame = 0; frame < 3000; frame++)
            {
                spawner.Tick(0.1);
                most = Math.Max(most, calls.Live.Count);
                if (frame % 250 == 249 && calls.Live.Count > 0) calls.End(calls.Live[0].Id, CallEndReason.WrongConnection);
            }
            Assert.That(most, Is.EqualTo(1));
            Assert.That(spawned, Is.GreaterThan(5));
        }

        [Test]
        public void CallerAndRecipientAreDifferentPeopleInTheDirectory()
        {
            for (int i = 0; i < 200; i++)
            {
                Advance(2.2);
                Call call = calls.Live[0];
                Assert.That(world.Directory.Lists(call.Request.CallerPerson), Is.True);
                Assert.That(world.Directory.Lists(call.Request.Recipient), Is.True);
                Assert.That(call.Request.Recipient.IsSamePerson(call.Request.CallerPerson), Is.False);
                calls.End(call.Id, CallEndReason.Finished);
            }
        }

        [Test]
        public void CallersMissingFromTheDirectoryNeverCall()
        {
            var stranger = new FakeCaller(new FakePerson("Otis", "Bramble"), world.Regular, weight: 100);
            var random = new SeededRandom(5);
            spawner = new CallSpawner(calls, new CallFactory(random), world.Directory,
                new ICaller[] { stranger, world.AdaCaller }, random, delaySeconds: 2);
            for (int i = 0; i < 50; i++)
            {
                Advance(2.2);
                Assert.That(calls.Live[0].Request.Caller, Is.SameAs(world.AdaCaller));
                calls.End(calls.Live[0].Id, CallEndReason.Finished);
            }
        }
    }
}
