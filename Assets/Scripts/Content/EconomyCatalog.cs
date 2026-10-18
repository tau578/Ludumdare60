using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>The usable values of the economy file, plus a plain message for every entry left out or replaced.</summary>
    public sealed class EconomyCatalog
    {
        public EconomyCatalog(long startingCents, int startingCords, IPacingProfile pacing, IReadOnlyList<ICallerType> callTypes,
            IReadOnlyList<IUpgrade> upgrades, IReadOnlyList<string> errors)
        {
            StartingCents = startingCents;
            StartingCords = startingCords;
            Pacing = pacing;
            CallTypes = callTypes;
            Upgrades = upgrades;
            Errors = errors;
        }

        public long StartingCents { get; }
        public int StartingCords { get; }
        public IPacingProfile Pacing { get; }
        public IReadOnlyList<ICallerType> CallTypes { get; }
        public IReadOnlyList<IUpgrade> Upgrades { get; }
        public IReadOnlyList<string> Errors { get; }

        public bool HasCallType(string id)
        {
            foreach (ICallerType type in CallTypes)
                if (type.Id == id) return true;
            return false;
        }
    }
}
