using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class MoneyFormatTests
    {
        [TestCase(0, "$0.00")]
        [TestCase(5, "$0.05")]
        [TestCase(120, "$1.20")]
        [TestCase(1234, "$12.34")]
        [TestCase(10000000, "$100,000.00")]
        public void FormatsDollarsAndCents(long cents, string expected) =>
            Assert.That(MoneyFormat.Dollars(cents), Is.EqualTo(expected));

        [TestCase(5, "5¢/s")]
        [TestCase(2.5, "2.5¢/s")]
        public void FormatsRates(double centsPerSecond, string expected) =>
            Assert.That(MoneyFormat.Rate(centsPerSecond), Is.EqualTo(expected));
    }
}
