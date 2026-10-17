using System.Collections.Generic;

namespace Switchboard.Domain
{
    public interface IGameContent
    {
        long StartingCents { get; }
        int MaxWaitingCalls { get; }
        IPacingProfile Pacing { get; }
        IReadOnlyList<ICallerType> CallerTypes { get; }
        IReadOnlyList<IPerson> People { get; }
        IReadOnlyList<ICaller> Callers { get; }
        IReadOnlyList<IDirectoryEntry> Directory { get; }
    }
}
