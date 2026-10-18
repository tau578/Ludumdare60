using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>The usable values of the economy file, plus a plain message for every entry left out or replaced.</summary>
    public sealed class EconomyCatalog
    {
        public EconomyCatalog(long startingCents, int startingCords, BoardSize startingBoard, int startingClientSlots,
            IPacingProfile pacing, IReadOnlyList<ICallerType> callTypes, IReadOnlyList<IUpgrade> upgrades, IReadOnlyList<string> errors)
        {
            StartingCents = startingCents;
            StartingCords = startingCords;
            StartingBoard = startingBoard;
            StartingClientSlots = startingClientSlots;
            Pacing = pacing;
            CallTypes = callTypes;
            Upgrades = upgrades;
            Errors = errors;
        }

        public long StartingCents { get; }
        public int StartingCords { get; }
        public BoardSize StartingBoard { get; }
        public int StartingClientSlots { get; }

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
