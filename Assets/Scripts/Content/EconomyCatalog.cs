using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>The usable upgrades of the economy file, plus a plain message for every entry left out.</summary>
    public sealed class EconomyCatalog
    {
        public EconomyCatalog(IReadOnlyList<IUpgrade> upgrades, IReadOnlyList<string> errors)
        {
            Upgrades = upgrades;
            Errors = errors;
        }

        public IReadOnlyList<IUpgrade> Upgrades { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}
