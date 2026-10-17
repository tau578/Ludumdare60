namespace Switchboard.Domain
{
    /// <summary>The physical board as the rules see it. CordBridge implements it in the game; a fake does in tests.</summary>
    public interface ISwitchboard
    {
        int CordCount { get; }
        void ReturnToTray(CordId cord);
        void Refuse(CordId cord);
    }
}
