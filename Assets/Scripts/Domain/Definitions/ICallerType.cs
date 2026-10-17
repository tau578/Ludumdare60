namespace Switchboard.Domain
{
    public interface ICallerType
    {
        string Id { get; }
        string DisplayName { get; }
        FloatRange CentsPerSecond { get; }
        FloatRange ConnectionFeeCents { get; }
        FloatRange DurationSeconds { get; }
    }
}
