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

        public static string Rate(double centsPerSecond) => centsPerSecond.ToString("0.##", Invariant) + "¢/s";
    }
}
