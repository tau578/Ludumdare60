using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// One hand-written call from the designer's Calls.json (D51): who calls, what they say, which two jacks connect
    /// it, and what it pays. Money is in cents; an emergency also has a response time, and a regular call may have
    /// its own patience (D63).
    /// </summary>
    public sealed class CallScript
    {
        /// <param name="patienceSeconds">Null uses Economy.json's defaultPatienceSeconds; 0 waits forever. Emergencies ignore it.</param>
        public CallScript(string id, string name, string icon, string message, JackId from, JackId to,
            double durationSeconds, double connectCents, double centsPerSecond, bool emergency = false,
            double responseTimeSeconds = 0, double? patienceSeconds = null)
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
            // Only emergencies are timed (D51), and they use their response time, never patience (D64).
            ResponseTimeSeconds = emergency ? Math.Max(0, responseTimeSeconds) : 0;
            PatienceSeconds = emergency || patienceSeconds == null ? (double?)null : Math.Max(0, patienceSeconds.Value);
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

        /// <summary>This call's own patience in seconds (D63); null uses the default, 0 waits forever, null for emergencies.</summary>
        public double? PatienceSeconds { get; }

        /// <summary>What connecting the call pays before any multiplier: its fee plus its rate for its whole length (D82).</summary>
        public double FullPayCents => ConnectCents + CentsPerSecond * DurationSeconds;

        public bool Uses(JackId jack) => From == jack || To == jack;
    }
}
