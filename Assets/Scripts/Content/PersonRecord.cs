using System;
using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    public sealed class PersonRecord : IPerson
    {
        public PersonRecord(string id, string firstName, string lastName, string portrait, IReadOnlyList<string> tags = null)
        {
            Tags = tags ?? Array.Empty<string>();
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Portrait = portrait;
        }

        public string Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public IReadOnlyList<string> Tags { get; }

        // A sprite name from GameConfig's portrait list; presentation only, so it stays out of IPerson.
        public string Portrait { get; }
    }
}
