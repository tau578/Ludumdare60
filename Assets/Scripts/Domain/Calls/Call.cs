namespace Switchboard.Domain
{
    public sealed class Call
    {
        internal Call(CallId id, CallRequest request)
        {
            Id = id;
            Request = request;
            SecondsLeft = request.DurationSeconds;
            // Only emergencies are timed (D51); the timer starts with the call.
            ResponseSecondsLeft = request.Script.ResponseTimeSeconds;
            HasResponseTime = ResponseSecondsLeft > 0;
        }

        public CallId Id { get; }
        public CallRequest Request { get; }
        public CallState State { get; internal set; }
        public CallEndReason EndReason { get; internal set; }
        public double SecondsLeft { get; internal set; }
        public double CentsEarned { get; internal set; }
        public bool ConnectionFeePaid { get; internal set; }
        public CordId? Cord { get; internal set; }

        /// <summary>Whether the call must be connected in time (D09).</summary>
        public bool HasResponseTime { get; }

        /// <summary>Counts down while the call waits, is in hand or is connecting; stops for good once connected.</summary>
        public double ResponseSecondsLeft { get; internal set; }

        /// <summary>The fine actually taken when the call ended; 0 if none.</summary>
        public long FineCents { get; internal set; }
    }
}
