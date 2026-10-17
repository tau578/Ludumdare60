namespace Switchboard.Domain
{
    public interface IDirectoryEntry
    {
        IPerson Person { get; }
        string JackLabel { get; }
    }
}
