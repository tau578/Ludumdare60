using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class EarningsServiceTests
    {
        private TestWorld world;
        private CallService calls;
        private Wallet wallet;
        private EarningsService earnings;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            wallet = new Wallet();
            earnings = new EarningsService(calls, wallet);
        }

        private void Tick(double dt)
        {
            earnings.Tick(dt);
            calls.Tick(dt);
        }

        [Test]
        public void TwentySecondCallAtFiveCentsPaysExactlyOneDollarWithUnevenFrames()
        {
            Call call = calls.Add(world.AdaWantsBasil(centsPerSecond: 5, durationSeconds: 20));
            calls.Take(call.Id);
            calls.Connect(call.Id);

            var random = new SeededRandom(42);
            for (int frame = 0; frame < 10000 && call.State != CallState.Ended; frame++)
                Tick(0.016 + random.NextDouble() * (0.05 - 0.016));

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(wallet.Cents, Is.EqualTo(100));
            Assert.That(call.CentsEarned, Is.EqualTo(100).Within(1e-9));

            for (int frame = 0; frame < 100; frame++) Tick(0.03);
            Assert.That(wallet.Cents, Is.EqualTo(100));
            Assert.That(call.CentsEarned, Is.EqualTo(100).Within(1e-9));
        }

        [Test]
        public void CallsThatAreNotConnectedEarnNothing()
        {
            calls.Add(world.AdaWantsBasil());
            Call taken = calls.Add(world.AdaWantsBasil());
            calls.Take(taken.Id);

            for (int frame = 0; frame < 100; frame++) Tick(0.05);

            Assert.That(wallet.Cents, Is.EqualTo(0));
            Assert.That(wallet.Fraction, Is.EqualTo(0));
            Assert.That(taken.CentsEarned, Is.EqualTo(0));
        }

        [Test]
        public void PaymentStopsWhenTheCallIsCutOff()
        {
            Call call = calls.Add(world.AdaWantsBasil(centsPerSecond: 4, durationSeconds: 20));
            calls.Take(call.Id);
            calls.Connect(call.Id);
            for (int frame = 0; frame < 50; frame++) Tick(0.05);

            calls.End(call.Id, CallEndReason.DisconnectedEarly);
            for (int frame = 0; frame < 50; frame++) Tick(0.05);

            Assert.That(call.CentsEarned, Is.EqualTo(10).Within(1e-9));
            Assert.That(wallet.Cents, Is.EqualTo(10));
        }
    }
}
