using System;

namespace Switchboard.Domain
{
    /// <summary>Whole cents in a long, plus a carried fraction so nothing is lost to rounding (D13).</summary>
    public sealed class Wallet
    {
        // Floating-point noise this close below a whole cent must not hold that cent back.
        private const double WholeCentTolerance = 1e-9;

        public Wallet(long cents = 0, double fraction = 0)
        {
            Cents = Math.Max(0, cents);
            Fraction = fraction >= 0 && fraction < 1 ? fraction : 0;
        }

        public long Cents { get; private set; }
        public double Fraction { get; private set; }

        public void Earn(double cents)
        {
            if (!(cents > 0)) return;
            double total = Fraction + cents;
            double whole = Math.Floor(total + WholeCentTolerance);
            Cents += (long)whole;
            Fraction = Math.Max(0, total - whole);
        }

        public long ApplyFine(long cents)
        {
            if (cents <= 0) return 0;
            long taken = Math.Min(cents, Cents);
            Cents -= taken;
            return taken;
        }
    }
}
