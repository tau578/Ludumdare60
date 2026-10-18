namespace Switchboard.Domain
{
    /// <summary>A call connects when its own two jacks are patched together (D51), in either order (D05).</summary>
    public sealed class ConnectionRule
    {
        public bool IsCaller(CallRequest request, JackId jack) => request.From == jack;

        public bool Involves(CallRequest request, JackId jack) => request.Involves(jack);

        public bool IsCorrect(CallRequest request, JackId a, JackId b) =>
            (a == request.From && b == request.To) || (a == request.To && b == request.From);
    }
}
