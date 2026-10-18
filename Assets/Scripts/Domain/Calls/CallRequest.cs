using System;

namespace Switchboard.Domain
{
    /// <summary>What one live call asks for: the hand-written call it plays (D51).</summary>
    public sealed class CallRequest
    {
        public CallRequest(CallScript script) => Script = script ?? throw new ArgumentNullException(nameof(script));

        public CallScript Script { get; }
        public JackId From => Script.From;
        public JackId To => Script.To;
        public double CentsPerSecond => Script.CentsPerSecond;
        public double DurationSeconds => Script.DurationSeconds;
        public double ConnectionFeeCents => Script.ConnectCents;

        public bool Involves(JackId jack) => Script.Uses(jack);
    }
}
