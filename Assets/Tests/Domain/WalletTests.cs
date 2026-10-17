using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class WalletTests
    {
        [Test]
        public void MillionTinyEarningsAddExactlyOneHundredCents()
        {
            var wallet = new Wallet();
            for (int i = 0; i < 1000000; i++) wallet.Earn(0.0001);
            Assert.That(wallet.Cents, Is.EqualTo(100));
        }

        [Test]
        public void FractionStaysWithinZeroAndOneAndNothingIsLost()
        {
            var wallet = new Wallet();
            var random = new SeededRandom(7);
            double total = 0;
            bool fractionInRange = true;
            for (int i = 0; i < 20000; i++)
            {
                double cents = random.NextDouble() * 3;
                total += cents;
                wallet.Earn(cents);
                fractionInRange &= wallet.Fraction >= 0 && wallet.Fraction < 1;
            }
            Assert.That(fractionInRange, Is.True);
            Assert.That(wallet.Cents + wallet.Fraction, Is.EqualTo(total).Within(1e-6));
        }

        [Test]
        public void FineFloorsTheBalanceAtZeroAndReturnsWhatWasTaken()
        {
            var wallet = new Wallet(250);
            Assert.That(wallet.ApplyFine(100), Is.EqualTo(100));
            Assert.That(wallet.Cents, Is.EqualTo(150));
            Assert.That(wallet.ApplyFine(500), Is.EqualTo(150));
            Assert.That(wallet.Cents, Is.EqualTo(0));
            Assert.That(wallet.ApplyFine(10), Is.EqualTo(0));
            Assert.That(wallet.Cents, Is.EqualTo(0));
        }

        [Test]
        public void NonPositiveAmountsChangeNothing()
        {
            var wallet = new Wallet(10);
            wallet.Earn(0);
            wallet.Earn(-5);
            wallet.Earn(double.NaN);
            Assert.That(wallet.ApplyFine(-3), Is.EqualTo(0));
            Assert.That(wallet.Cents, Is.EqualTo(10));
            Assert.That(wallet.Fraction, Is.EqualTo(0));
        }
    }
}
