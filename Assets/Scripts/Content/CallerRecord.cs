using Switchboard.Domain;

namespace Switchboard.Content
{
    public sealed class CallerRecord : ICaller
    {
        public CallerRecord(string id, IPerson person, ICallerType type, float centsPerSecondOverride, float connectionFeeCentsOverride)
        {
            Id = id;
            Person = person;
            Type = type;
            CentsPerSecondOverride = centsPerSecondOverride;
            ConnectionFeeCentsOverride = connectionFeeCentsOverride;
        }

        public string Id { get; }
        public IPerson Person { get; }
        public ICallerType Type { get; }
        public float Weight => 1;
        public float CentsPerSecondOverride { get; }
        public float ConnectionFeeCentsOverride { get; }
    }
}
