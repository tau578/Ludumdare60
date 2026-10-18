namespace Switchboard.Domain
{
    /// <summary>When new calls ring (Design 5.2). Progress curves come in Step 7.</summary>
    public interface IPacingProfile
    {
        double FirstCallDelaySeconds { get; }
        double BaseIntervalSeconds { get; }

        // 0.3 means each interval is rolled within ±30 % of the base.
        double IntervalJitter { get; }

        // How much slower calls ring per waiting card beyond the free cords.
        double LoadFactor { get; }

        // Seconds without input before types that carry fines stop ringing (D16); 0 turns the guard off.
        double IdleTimeoutSeconds { get; }
    }
}
