using System.Collections.Generic;

namespace Switchboard.Domain
{
    public interface IGameContent
    {
        long StartingCents { get; }
        int MaxWaitingCalls { get; }

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
