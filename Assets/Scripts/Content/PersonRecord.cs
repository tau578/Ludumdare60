using System;
using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    public sealed class PersonRecord : IPerson
    {
        public PersonRecord(string id, string firstName, string lastName, IReadOnlyList<string> tags = null)
        {
            Tags = tags ?? Array.Empty<string>();
            Id = id;
            FirstName = firstName;
            LastName = lastName;
        }

        public string Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public IReadOnlyList<string> Tags { get; }
    }
}
