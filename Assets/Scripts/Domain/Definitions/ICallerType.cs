namespace Switchboard.Domain
{
    public interface ICallerType
    {
        string Id { get; }
        string DisplayName { get; }
        FloatRange CentsPerSecond { get; }
        FloatRange ConnectionFeeCents { get; }
        FloatRange DurationSeconds { get; }
        FloatRange CallbackDelaySeconds { get; }

        // 0 means the caller waits as long as it takes.
        double ResponseTimeSeconds { get; }

        // Also the fine for a wrong connection, because the rules make the two equal (Design 5.5).
        long FineMissedCents { get; }
        long FineEarlyDisconnectCents { get; }
        bool CallbackAfterMiss { get; }
        bool CallbackAfterDisconnect { get; }

        // Empty means any person without tags; otherwise only entries with this tag receive the calls.
        string RecipientTag { get; }

        // 0 means no limit. Live calls and pending callbacks of the type both count.
        int MaxSimultaneous { get; }

        // Temporary until progress-driven weights (D37, Step 7).
        double SpawnWeight { get; }

        // Every untagged person can place calls of this type, besides their own type from the people file.
        bool EveryoneCalls { get; }
    }
}
