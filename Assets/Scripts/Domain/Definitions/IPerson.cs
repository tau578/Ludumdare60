using System.Collections.Generic;

namespace Switchboard.Domain
{
    public interface IPerson
    {
        string Id { get; }
        string FirstName { get; }
        string LastName { get; }
        IReadOnlyList<string> Tags { get; }
    }
}
