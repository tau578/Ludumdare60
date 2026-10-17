namespace Switchboard.Domain
{
    /// <summary>Plain pacing values: the fallback when no profile is configured, and handy in tests.</summary>
    public sealed class PacingSettings : IPacingProfile
    {
        public static readonly PacingSettings Default = new PacingSettings(2, 8, 0.3, 0.5);

        public PacingSettings(double firstCallDelaySeconds, double baseIntervalSeconds, double intervalJitter, double loadFactor)
        {
            FirstCallDelaySeconds = firstCallDelaySeconds;
            BaseIntervalSeconds = baseIntervalSeconds;
            IntervalJitter = intervalJitter;
            LoadFactor = loadFactor;
        }

        public double FirstCallDelaySeconds { get; }
        public double BaseIntervalSeconds { get; }
        public double IntervalJitter { get; }
        public double LoadFactor { get; }
    }
}
