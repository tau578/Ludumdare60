using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// One hand-written call from the designer's Calls.json (D51): who calls, what they say, which two jacks connect
    /// it, and what it pays. Money is in cents; an emergency also has a response time and a fine for missing it.
    /// </summary>
    public sealed class CallScript
    {
        public CallScript(string id, string name, string icon, string message, JackId from, JackId to,
            double durationSeconds, double connectCents, double centsPerSecond, bool emergency = false,
            double responseTimeSeconds = 0, long penaltyCents = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? string.Empty;
            Icon = icon ?? string.Empty;
            Message = message ?? string.Empty;
            From = from;
            To = to;
            DurationSeconds = durationSeconds;
            ConnectCents = connectCents;
            CentsPerSecond = centsPerSecond;
            IsEmergency = emergency;
            // Only emergencies are timed and fined (D51).
            ResponseTimeSeconds = emergency ? Math.Max(0, responseTimeSeconds) : 0;
            PenaltyCents = emergency ? Math.Max(0, penaltyCents) : 0;
        }

        public string Id { get; }

        /// <summary>The caller's name, shown under the portrait.</summary>
        public string Name { get; }

        /// <summary>A portrait sprite name, such as caller-01; presentation only.</summary>
        public string Icon { get; }

        /// <summary>What the caller says in the speech bubble.</summary>
        public string Message { get; }

        public JackId From { get; }
        public JackId To { get; }
        public double DurationSeconds { get; }
        public double ConnectCents { get; }
        public double CentsPerSecond { get; }
        public bool IsEmergency { get; }

        /// <summary>Seconds to connect an emergency before it is missed; 0 for regular calls.</summary>
        public double ResponseTimeSeconds { get; }

        /// <summary>The fine for a missed emergency, never multiplied (D37); 0 for regular calls.</summary>
        public long PenaltyCents { get; }

        public bool Uses(JackId jack) => From == jack || To == jack;
    }
}
