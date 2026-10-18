using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>What the domain sees of the content: the economy file's money, pacing and call types, the people file, and the board's start.</summary>
    public sealed class GameContent : IGameContent
    {
        public GameContent(PeopleCatalog people, EconomyCatalog economy)
        {
            StartingCents = economy.StartingCents;
            StartingBoard = economy.StartingBoard;
            StartingClientSlots = economy.StartingClientSlots;
            StartingCords = economy.StartingCords;
            Pacing = economy.Pacing;
            CallerTypes = economy.CallTypes;
            People = people.People;
            Callers = people.Callers;
            Directory = people.Directory;
            Upgrades = economy.Upgrades;
        }

        public long StartingCents { get; }
        public BoardSize StartingBoard { get; }
        public int StartingClientSlots { get; }
        public int StartingCords { get; }
        public IPacingProfile Pacing { get; }
        public IReadOnlyList<ICallerType> CallerTypes { get; }
        public IReadOnlyList<IPerson> People { get; }
        public IReadOnlyList<ICaller> Callers { get; }
        public IReadOnlyList<IDirectoryEntry> Directory { get; }
        public IReadOnlyList<IUpgrade> Upgrades { get; }
    }
}
