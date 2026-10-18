using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>One call type from the economy file's calls section (D47).</summary>
    public sealed class CallTypeRecord : ICallerType
    {
        public CallTypeRecord(string id, string displayName, double spawnWeight, FloatRange centsPerSecond,
            FloatRange connectionFeeCents, FloatRange durationSeconds, FloatRange callbackDelaySeconds)
        {
            Id = id;
            DisplayName = displayName;
            SpawnWeight = spawnWeight;
            CentsPerSecond = centsPerSecond;
            ConnectionFeeCents = connectionFeeCents;
            DurationSeconds = durationSeconds;
            CallbackDelaySeconds = callbackDelaySeconds;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public FloatRange CentsPerSecond { get; }
        public FloatRange ConnectionFeeCents { get; }
        public FloatRange DurationSeconds { get; }
        public FloatRange CallbackDelaySeconds { get; }
        public double SpawnWeight { get; }
        public double ResponseTimeSeconds { get; internal set; }
        public long FineMissedCents { get; internal set; }
        public long FineEarlyDisconnectCents { get; internal set; }
        public bool CallbackAfterMiss { get; internal set; }
        public bool CallbackAfterDisconnect { get; internal set; }
        public string RecipientTag { get; internal set; } = string.Empty;
        public int MaxSimultaneous { get; internal set; }
        public bool EveryoneCalls { get; internal set; }
    }
}
