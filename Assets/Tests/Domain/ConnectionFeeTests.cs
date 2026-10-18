using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class ConnectionFeeTests
    {
        private static readonly CordId Cord = new CordId(0);

        private TestWorld world;
        private CallService calls;
        private Wallet wallet;
        private EarningsService earnings;
        private OperatorDesk desk;
        private List<ConnectionFeePaidArgs> feesPaid;
        private double multiplier;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            wallet = new Wallet();
            multiplier = 1;
            earnings = new EarningsService(calls, wallet, () => multiplier);
            desk = new OperatorDesk(calls, new ConnectionRule(), new FakeSwitchboard());
            feesPaid = new List<ConnectionFeePaidArgs>();
            earnings.ConnectionFeePaid += e => feesPaid.Add(e);
        }

        private Call TakeAdaWantsBasil(double fee, double centsPerSecond = 5, double durationSeconds = 20)
        {
            Call call = calls.Add(world.AdaWantsBasil(centsPerSecond, durationSeconds, fee));
            Assert.That(desk.TryTake(call.Id), Is.True);
            return call;
        }

        private void Tick(double dt)
        {
            earnings.Tick(dt);
            calls.Tick(dt);
        }

        private void Advance(double seconds)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += 0.05) Tick(0.05);
        }

        [Test]
        public void FeeIsPaidOnceTheMomentTheRightPairConnects()
        {
            Call call = TakeAdaWantsBasil(fee: 10);
            desk.OnCircuitClosed(Cord, TestWorld.B2, TestWorld.A1);

            Assert.That(wallet.Cents, Is.EqualTo(10), "paid before any tick");
            Assert.That(call.CentsEarned, Is.EqualTo(10));
            Assert.That(call.ConnectionFeePaid, Is.True);
            Assert.That(feesPaid.Count, Is.EqualTo(1));
            Assert.That(feesPaid[0].Call, Is.SameAs(call));
            Assert.That(feesPaid[0].Cents, Is.EqualTo(10));

            Advance(1);
            Assert.That(wallet.Cents, Is.EqualTo(15));
            Assert.That(feesPaid.Count, Is.EqualTo(1));
        }

        [Test]
        public void WrongConnectionPaysNoFee()
        {
            Call call = TakeAdaWantsBasil(fee: 10);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Advance(1);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(wallet.Cents, Is.EqualTo(0));
            Assert.That(call.CentsEarned, Is.EqualTo(0));
            Assert.That(call.ConnectionFeePaid, Is.False);
            Assert.That(feesPaid, Is.Empty);
        }

        [Test]
        public void MultiplierAppliesToTheFee()
        {
            multiplier = 2;
            TakeAdaWantsBasil(fee: 10);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);

            Assert.That(wallet.Cents, Is.EqualTo(20));
            Assert.That(feesPaid[0].Cents, Is.EqualTo(20));

            Advance(1);
            Assert.That(wallet.Cents, Is.EqualTo(30));
        }

        [Test]
        public void FinishedCallEarnsTheFeePlusDurationTimesRate()
        {
            Call call = TakeAdaWantsBasil(fee: 12, centsPerSecond: 5, durationSeconds: 20);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);

            var random = new SeededRandom(8);
            for (int frame = 0; frame < 10000 && call.State != CallState.Ended; frame++)
                Tick(0.016 + random.NextDouble() * (0.05 - 0.016));
            Advance(1);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(call.CentsEarned, Is.EqualTo(112).Within(1e-9));
            Assert.That(wallet.Cents, Is.EqualTo(112));
            Assert.That(feesPaid.Count, Is.EqualTo(1));
        }

        [Test]
        public void ZeroFeeRaisesNoFeeEvent()
        {
            TakeAdaWantsBasil(fee: 0);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);

            Assert.That(wallet.Cents, Is.EqualTo(0));
            Assert.That(feesPaid, Is.Empty);
        }
    }
}
