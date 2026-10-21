using System.Collections.Generic;

namespace Switchboard.Domain
{
    public interface IGameContent
    {
        long StartingCents { get; }

        /// <summary>The open part of the board in a new game, before any extra_clients upgrade (D49).</summary>
        BoardSize StartingBoard { get; }

        /// <summary>The open caller slots in a new game, before any extra_clients upgrade (D49).</summary>
        int StartingClientSlots { get; }

        /// <summary>The cords a new game starts with, before any extra_wires upgrade (5b).</summary>
        int StartingCords { get; }

        /// <summary>The queue of a new game, Economy.json's startPool (D52), in play order.</summary>
        IReadOnlyList<CallScript> StartPool { get; }

        /// <summary>The quick-connect tip's window in seconds (D59); 0 turns the tip off.</summary>
        double QuickConnectSeconds { get; }

        /// <summary>The quick-connect tip in cents, before the income multiplier (D59); 0 turns the tip off.</summary>
        long QuickConnectTipCents { get; }

        /// <summary>Economy.json's upgrades in file order (D39, D40).</summary>
        IReadOnlyList<IUpgrade> Upgrades { get; }
    }
}
