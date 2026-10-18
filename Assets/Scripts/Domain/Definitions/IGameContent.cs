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
        IPacingProfile Pacing { get; }
        IReadOnlyList<ICallerType> CallerTypes { get; }
        IReadOnlyList<IPerson> People { get; }
        IReadOnlyList<ICaller> Callers { get; }
        IReadOnlyList<IDirectoryEntry> Directory { get; }

        /// <summary>Economy.json's upgrades in file order (D39, D40).</summary>
        IReadOnlyList<IUpgrade> Upgrades { get; }
    }
}
