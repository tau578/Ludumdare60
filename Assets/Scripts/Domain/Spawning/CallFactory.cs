using System;

namespace Switchboard.Domain
{
    public sealed class CallFactory
    {
        private readonly IRandom random;

        public CallFactory(IRandom random) => this.random = random ?? throw new ArgumentNullException(nameof(random));

        public CallRequest Create(ICaller caller, IPerson recipient, int attempt = 1)
        {
            if (caller == null) throw new ArgumentNullException(nameof(caller));
            double centsPerSecond = caller.CentsPerSecondOverride > 0
                ? caller.CentsPerSecondOverride
                : Roll(caller.Type.CentsPerSecond);
            double durationSeconds = Roll(caller.Type.DurationSeconds);
            double connectionFeeCents = caller.ConnectionFeeCentsOverride > 0
                ? caller.ConnectionFeeCentsOverride
                : Roll(caller.Type.ConnectionFeeCents);
            return new CallRequest(caller, recipient, centsPerSecond, durationSeconds, connectionFeeCents, attempt);
        }

        // Whole numbers keep rates readable on cards and make a finished call pay whole cents.
        // A range too narrow to hold a whole number falls back to a continuous roll.
        private double Roll(FloatRange range)
        {
            int low = (int)Math.Ceiling(range.min);
            int high = (int)Math.Floor(range.max);
            if (low <= high) return random.Next(low, high + 1);
            return range.min + (range.max - range.min) * random.NextDouble();
        }
    }
}
