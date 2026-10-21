namespace Switchboard.Domain
{
    /// <summary>
    /// The quick-connect tip (D59): a call connected within <see cref="Seconds"/> of its card being taken pays
    /// <see cref="Cents"/> once, on top of its fee. A window or a tip of 0 turns it off.
    /// </summary>
    public readonly struct QuickConnectTip
    {
        /// <summary>The window and the tip a file without the two fields gets.</summary>
        public const double DefaultSeconds = 5;
        public const long DefaultCents = 5;

        public QuickConnectTip(double seconds, long cents)
        {
            Seconds = seconds > 0 ? seconds : 0;
            Cents = cents > 0 ? cents : 0;
        }

        public double Seconds { get; }
        public long Cents { get; }
        public bool IsOn => Seconds > 0 && Cents > 0;

        public bool Qualifies(double secondsInHand) => IsOn && secondsInHand <= Seconds;
    }
}
