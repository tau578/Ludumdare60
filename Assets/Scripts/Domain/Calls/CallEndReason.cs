namespace Switchboard.Domain
{
    public enum CallEndReason
    {
        None = 0,
        Finished = 1,
        WrongConnection = 2,
        DisconnectedEarly = 3,
        Missed = 4
    }
}
