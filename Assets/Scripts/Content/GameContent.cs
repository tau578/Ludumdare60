using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>What the domain sees of the content: the economy file's money, pacing and call types, the people file, and GameConfig's slot count.</summary>
    public sealed class GameContent : IGameContent
    {
        public GameContent(GameConfig config, PeopleCatalog people, EconomyCatalog economy)
        {
            StartingCents = economy.StartingCents;
            MaxWaitingCalls = config.MaxWaitingCalls;
            StartingCords = economy.StartingCords;
            Pacing = economy.Pacing;
            CallerTypes = economy.CallTypes;
            People = people.People;
            Callers = people.Callers;
            Directory = people.Directory;
            Upgrades = economy.Upgrades;
        }

        public long StartingCents { get; }
        public int MaxWaitingCalls { get; }
        public int StartingCords { get; }
        public IPacingProfile Pacing { get; }
        public IReadOnlyList<ICallerType> CallerTypes { get; }
        public IReadOnlyList<IPerson> People { get; }
        public IReadOnlyList<ICaller> Callers { get; }
        public IReadOnlyList<IDirectoryEntry> Directory { get; }
        public IReadOnlyList<IUpgrade> Upgrades { get; }
    }
}
