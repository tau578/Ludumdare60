using System;

namespace Switchboard.Domain
{
    public readonly struct JackId : IEquatable<JackId>
    {
        private readonly string label;

        public JackId(string label) => this.label = label;

        public string Label => label ?? string.Empty;

        public bool Equals(JackId other) => string.Equals(Label, other.Label, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is JackId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Label);
        public override string ToString() => Label;

        public static bool operator ==(JackId a, JackId b) => a.Equals(b);
        public static bool operator !=(JackId a, JackId b) => !a.Equals(b);
    }
}
