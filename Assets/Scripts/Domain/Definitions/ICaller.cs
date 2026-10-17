namespace Switchboard.Domain
{
    public interface ICaller
    {
        string Id { get; }
        IPerson Person { get; }
        ICallerType Type { get; }
        float Weight { get; }

        // 0 means the rate is rolled from the caller type's range.
        float CentsPerSecondOverride { get; }

        // 0 means the connection fee is rolled from the caller type's range.
        float ConnectionFeeCentsOverride { get; }
    }
}
