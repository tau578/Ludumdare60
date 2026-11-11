namespace Switchboard.Domain
{
    public sealed class Call
    {
        internal Call(CallId id, CallRequest request, double defaultPatienceSeconds = 0)
        {
            Id = id;
            Request = request;
            SecondsLeft = request.DurationSeconds;
            // Only emergencies are timed (D51); the timer starts with the call.
            ResponseSecondsLeft = request.Script.ResponseTimeSeconds;
            HasResponseTime = ResponseSecondsLeft > 0;
            // An emergency uses its response time only (D64); 0 patience waits forever (D63).
            double patience = request.Script.PatienceSeconds ?? defaultPatienceSeconds;
            PatienceSeconds = HasResponseTime || !(patience > 0) ? 0 : patience;
            PatienceSecondsLeft = PatienceSeconds;
        }

        public CallId Id { get; }
        public CallRequest Request { get; }
        public CallState State { get; internal set; }

        /// <summary>Still waiting for its connection (Incoming, Taken, Connecting): it holds a caller window (D83).</summary>
        public bool IsWaiting => State == CallState.Incoming || State == CallState.Taken || State == CallState.Connecting;

        /// <summary>Connected or on hold: its card is a tab, and it holds no caller window (D83).</summary>
        public bool IsTalking => State == CallState.Connected || State == CallState.OnHold;
        public CallEndReason EndReason { get; internal set; }
        public double SecondsLeft { get; internal set; }
        public double CentsEarned { get; internal set; }
        public bool ConnectionFeePaid { get; internal set; }

        /// <summary>Game seconds since the card was taken, counted only while it waits for its connection (D59).</summary>
        public double SecondsInHand { get; internal set; }

        /// <summary>The quick-connect tip this call paid (D59); 0 if it connected too late or tips are off.</summary>
        public double TipCents { get; internal set; }
        public CordId? Cord { get; internal set; }

        /// <summary>Whether the call must be connected in time (D09).</summary>
        public bool HasResponseTime { get; }

        /// <summary>Counts down while the call waits, is in hand or is connecting; stops for good once connected.</summary>
        public double ResponseSecondsLeft { get; internal set; }

        /// <summary>The caller's whole patience (D63); 0 when the call waits forever or is an emergency.</summary>
        public double PatienceSeconds { get; }

        /// <summary>Whether the caller hangs up when their patience runs out (D63).</summary>
        public bool HasPatience => PatienceSeconds > 0;

        /// <summary>Counts down like the response timer, held with it; stops for good once connected.</summary>
        public double PatienceSecondsLeft { get; internal set; }

        /// <summary>
        /// The response timer and the patience are held: the tutorial's guided call can't run out while the guide runs (D57).
        /// </summary>
        public bool WaitingHeld { get; internal set; }

        /// <summary>
        /// While the call waits: its recipient's jack carries another live call (connected or on hold), so the card reads
        /// "Line busy" (D63). Updated by <see cref="CallService"/> when the call arrives and every tick.
        /// </summary>
        public bool LineBusy { get; internal set; }

        /// <summary>The caller hung up while the recipient's line was busy: not the operator's fault, so no complaint (13e).</summary>
        public bool HungUpOnBusyLine { get; internal set; }

        /// <summary>The fine actually taken when the call ended; 0 if none.</summary>
        public long FineCents { get; internal set; }
    }
}
