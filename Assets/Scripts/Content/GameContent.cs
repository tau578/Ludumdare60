using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>What the domain sees of the content: GameConfig's settings plus the people file and the economy file.</summary>
    public sealed class GameContent : IGameContent
    {
        public GameContent(GameConfig config, PeopleCatalog people, EconomyCatalog economy)
        {
            StartingCents = config.StartingCents;
            MaxWaitingCalls = config.MaxWaitingCalls;
            Pacing = config.Pacing;
            CallerTypes = config.CallerTypes;
            People = people.People;
            Callers = people.Callers;
            Directory = people.Directory;
            Upgrades = economy.Upgrades;
        }

        public long StartingCents { get; }
        public int MaxWaitingCalls { get; }
        public IPacingProfile Pacing { get; }
        public IReadOnlyList<ICallerType> CallerTypes { get; }
        public IReadOnlyList<IPerson> People { get; }
        public IReadOnlyList<ICaller> Callers { get; }
        public IReadOnlyList<IDirectoryEntry> Directory { get; }
        public IReadOnlyList<IUpgrade> Upgrades { get; }
    }
}
