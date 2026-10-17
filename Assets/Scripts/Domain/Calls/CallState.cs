namespace Switchboard.Domain
{
    public enum CallState
    {
        Incoming = 0,
        Taken = 1,
        Connecting = 2,
        Connected = 3,
        OnHold = 4,
        Ended = 5
    }
}
