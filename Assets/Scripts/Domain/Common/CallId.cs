using System;

namespace Switchboard.Domain
{
    public readonly struct CallId : IEquatable<CallId>
    {
        public CallId(int value) => Value = value;

        public int Value { get; }

        public bool Equals(CallId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CallId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Call {Value}";

        public static bool operator ==(CallId a, CallId b) => a.Equals(b);
        public static bool operator !=(CallId a, CallId b) => !a.Equals(b);
    }
}
