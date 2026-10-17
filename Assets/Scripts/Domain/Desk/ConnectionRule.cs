namespace Switchboard.Domain
{
    public sealed class ConnectionRule
    {
        private readonly PhoneDirectory directory;

        public ConnectionRule(PhoneDirectory directory) => this.directory = directory;

        // Plug order doesn't matter (D05).
        public bool IsCorrect(CallRequest request, JackId a, JackId b)
        {
            if (!directory.TryGetJack(request.CallerPerson, out JackId caller) ||
                !directory.TryGetJack(request.Recipient, out JackId recipient)) return false;
            return (a == caller && b == recipient) || (a == recipient && b == caller);
        }
    }
}
