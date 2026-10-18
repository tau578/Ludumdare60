using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>The valid people from the people file, plus a message for every entry that was left out.</summary>
    public sealed class PeopleCatalog
    {
        public PeopleCatalog(IReadOnlyList<IPerson> people, IReadOnlyList<IDirectoryEntry> directory, IReadOnlyList<string> errors)
        {
            People = people;
            Directory = directory;
            Errors = errors;
        }

        public IReadOnlyList<IPerson> People { get; }
        public IReadOnlyList<IDirectoryEntry> Directory { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}
