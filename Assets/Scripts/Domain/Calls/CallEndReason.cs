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
        Cleared = 5,

        // The caller's patience ran out before a connection (D63): no pay, no fine; the queue brings the call back later.
        HungUp = 6
    }
}
