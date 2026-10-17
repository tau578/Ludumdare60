using System;

namespace Switchboard.Domain
{
    public sealed class CallRequest
    {
        public CallRequest(ICaller caller, IPerson recipient, double centsPerSecond, double durationSeconds,
            double connectionFeeCents = 0, int attempt = 1)
        {
            Caller = caller ?? throw new ArgumentNullException(nameof(caller));
            Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            Type = caller.Type;
            CentsPerSecond = centsPerSecond;
            DurationSeconds = durationSeconds;
            ConnectionFeeCents = connectionFeeCents;
            Attempt = attempt;
        }

        public ICaller Caller { get; }
        public IPerson Recipient { get; }
        public ICallerType Type { get; }
        public double CentsPerSecond { get; }
        public double DurationSeconds { get; }
        public double ConnectionFeeCents { get; }
        public int Attempt { get; }
        public IPerson CallerPerson => Caller.Person;
    }
}
