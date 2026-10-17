using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CallFactoryTests
    {
        private TestWorld world;

        [SetUp]
        public void SetUp() => world = new TestWorld();

        [Test]
        public void RolledValuesStayWithinTheTypeRanges()
        {
            var factory = new CallFactory(new SeededRandom(3));
            var rates = new HashSet<double>();
            for (int i = 0; i < 500; i++)
            {
                CallRequest request = factory.Create(world.AdaCaller, world.Basil);
                Assert.That(world.Regular.CentsPerSecond.Contains(request.CentsPerSecond), Is.True);
                Assert.That(world.Regular.DurationSeconds.Contains(request.DurationSeconds), Is.True);
                rates.Add(request.CentsPerSecond);
            }
            Assert.That(rates, Is.EquivalentTo(new double[] { 3, 4, 5, 6, 7, 8 }));
        }

        [Test]
        public void RangesWithoutAWholeNumberStillStayWithinTheRange()
        {
            var narrow = new FakeCallerType(new FloatRange(2.2f, 2.8f), new FloatRange(15.1f, 15.9f));
            var factory = new CallFactory(new SeededRandom(9));
            for (int i = 0; i < 200; i++)
            {
                CallRequest request = factory.Create(new FakeCaller(world.Ada, narrow), world.Basil);
                Assert.That(narrow.CentsPerSecond.Contains(request.CentsPerSecond), Is.True);
                Assert.That(narrow.DurationSeconds.Contains(request.DurationSeconds), Is.True);
            }
        }

        [Test]
        public void CallerOverrideWinsOverTheTypeRange()
        {
            var factory = new CallFactory(new SeededRandom(4));
            var vip = new FakeCaller(world.Ada, world.Regular, centsPerSecondOverride: 25);
            for (int i = 0; i < 50; i++)
                Assert.That(factory.Create(vip, world.Basil).CentsPerSecond, Is.EqualTo(25));
        }

        [Test]
        public void RequestRecordsCallerRecipientTypeAndAttempt()
        {
            CallRequest request = new CallFactory(new SeededRandom(1)).Create(world.AdaCaller, world.Basil, attempt: 2);
            Assert.That(request.Caller, Is.SameAs(world.AdaCaller));
            Assert.That(request.CallerPerson, Is.SameAs(world.Ada));
            Assert.That(request.Recipient, Is.SameAs(world.Basil));
            Assert.That(request.Type, Is.SameAs(world.Regular));
            Assert.That(request.Attempt, Is.EqualTo(2));
        }
    }
}
