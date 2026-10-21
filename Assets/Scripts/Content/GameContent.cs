using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>What the domain sees of the content: the economy file's money, board start, call queue, quick-connect tip and upgrades.</summary>
    public sealed class GameContent : IGameContent
    {
        public GameContent(EconomyCatalog economy)
        {
            StartingCents = economy.StartingCents;
            StartingBoard = economy.StartingBoard;
            StartingClientSlots = economy.StartingClientSlots;
            StartingCords = economy.StartingCords;
            StartPool = economy.StartPool;
            QuickConnectSeconds = economy.QuickTip.Seconds;
            QuickConnectTipCents = economy.QuickTip.Cents;
            Upgrades = economy.Upgrades;
        }

        public long StartingCents { get; }
        public BoardSize StartingBoard { get; }
        public int StartingClientSlots { get; }
        public int StartingCords { get; }
        public IReadOnlyList<CallScript> StartPool { get; }
        public double QuickConnectSeconds { get; }
        public long QuickConnectTipCents { get; }
        public IReadOnlyList<IUpgrade> Upgrades { get; }
    }
}
