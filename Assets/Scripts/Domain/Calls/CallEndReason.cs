namespace Switchboard.Domain
{
    public enum CallEndReason
    {
        None = 0,
        Finished = 1,
        WrongConnection = 2,
        DisconnectedEarly = 3,
        Missed = 4,

        // Removed by the dev panel's "clear calls": no pay, no fine, no callback.
        Cleared = 5
    }
}
