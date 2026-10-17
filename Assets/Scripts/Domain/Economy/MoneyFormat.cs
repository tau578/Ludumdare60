using System.Globalization;

namespace Switchboard.Domain
{
    public static class MoneyFormat
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Dollars(long cents)
        {
            if (cents < 0) return "-" + Dollars(-cents);
            return "$" + (cents / 100).ToString("#,0", Invariant) + "." + (cents % 100).ToString("00", Invariant);
        }

        public static string Cents(double cents) => cents.ToString("0.##", Invariant) + "¢";

        public static string Rate(double centsPerSecond) => Cents(centsPerSecond) + "/s";

        // The card's badge: the one-off fee plus the per-second rate, e.g. "10¢ + 5¢/s" (D32).
        public static string CardRate(double connectionFeeCents, double centsPerSecond) =>
            connectionFeeCents > 0 ? Cents(connectionFeeCents) + " + " + Rate(centsPerSecond) : Rate(centsPerSecond);
    }
}
