using Switchboard.Domain;

namespace Switchboard.Content
{
    public sealed class DirectoryRecord : IDirectoryEntry
    {
        public DirectoryRecord(IPerson person, string jackLabel)
        {
            Person = person;
            JackLabel = jackLabel;
        }

        public IPerson Person { get; }
        public string JackLabel { get; }
    }
}
