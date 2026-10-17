using System;

namespace Switchboard.Domain
{
    public readonly struct CordId : IEquatable<CordId>
    {
        public CordId(int index) => Index = index;

        public int Index { get; }

        public bool Equals(CordId other) => Index == other.Index;
        public override bool Equals(object obj) => obj is CordId other && Equals(other);
        public override int GetHashCode() => Index;
        public override string ToString() => $"Cord {Index}";

        public static bool operator ==(CordId a, CordId b) => a.Equals(b);
        public static bool operator !=(CordId a, CordId b) => !a.Equals(b);
    }
}
