using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>The usable values of the economy file, plus a plain message for every entry left out or replaced.</summary>
    public sealed class EconomyCatalog
    {
        public EconomyCatalog(long startingCents, int startingCords, BoardSize startingBoard, int startingClientSlots,
            IReadOnlyList<CallScript> startPool, IReadOnlyList<IUpgrade> upgrades, IReadOnlyList<string> errors,
            QuickConnectTip quickTip = default)
        {
            QuickTip = quickTip;
            StartingCents = startingCents;
            StartingCords = startingCords;
            StartingBoard = startingBoard;
            StartingClientSlots = startingClientSlots;
            StartPool = startPool;
            Upgrades = upgrades;
            Errors = errors;
        }

        public long StartingCents { get; }
        public int StartingCords { get; }
        public BoardSize StartingBoard { get; }
        public int StartingClientSlots { get; }

        /// <summary>quickConnectSeconds and quickConnectTipCents (D59).</summary>
        public QuickConnectTip QuickTip { get; }

        /// <summary>The queue of a new game (D52), with unknown calls and calls off the starting board left out.</summary>
        public IReadOnlyList<CallScript> StartPool { get; }

        /// <summary>The most caller slots the file can ever open: the start or any extra_clients entry.</summary>
        public int MostClientSlots
        {
            get
            {
                int most = StartingClientSlots;
                foreach (IUpgrade upgrade in Upgrades)
                    if (upgrade.Kind == UpgradeKind.ExtraClients && upgrade.ClientSlots > most) most = upgrade.ClientSlots;
                return most;
            }
        }
        public IReadOnlyList<IUpgrade> Upgrades { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}
