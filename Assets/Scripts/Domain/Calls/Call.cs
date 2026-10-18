namespace Switchboard.Domain
{
    public sealed class Call
    {
        internal Call(CallId id, CallRequest request)
        {
            Id = id;
            Request = request;
            SecondsLeft = request.DurationSeconds;
            // Every call, a callback included, starts with the type's full response time.
            ResponseSecondsLeft = request.Type != null ? System.Math.Max(0, request.Type.ResponseTimeSeconds) : 0;
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

        /// <summary>The caller's jack once the Notepad has noted it (D23); null until then.</summary>
        public JackId? CallerNote { get; internal set; }

        /// <summary>The wanted person's jack once the Notepad has noted it (D23); null until then.</summary>
        public JackId? RecipientNote { get; internal set; }
    }
}
