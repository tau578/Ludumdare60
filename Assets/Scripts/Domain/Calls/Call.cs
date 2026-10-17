namespace Switchboard.Domain
{
    public sealed class Call
    {
        internal Call(CallId id, CallRequest request)
        {
            Id = id;
            Request = request;
            SecondsLeft = request.DurationSeconds;
        }

        public CallId Id { get; }
        public CallRequest Request { get; }
        public CallState State { get; internal set; }
        public CallEndReason EndReason { get; internal set; }
        public double SecondsLeft { get; internal set; }
        public double CentsEarned { get; internal set; }
        public bool ConnectionFeePaid { get; internal set; }
        public CordId? Cord { get; internal set; }
    }
}
