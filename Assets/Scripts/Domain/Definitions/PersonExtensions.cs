using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    public static class PersonExtensions
    {
        // A one-word name, such as a service, has no first name.
        public static string FullName(this IPerson person) =>
            string.IsNullOrEmpty(person.FirstName) ? person.LastName : $"{person.FirstName} {person.LastName}";

        public static string ListingName(this IPerson person) =>
            string.IsNullOrEmpty(person.FirstName) ? person.LastName : $"{person.LastName}, {person.FirstName}";

        // Ids are the identity of content, so two definitions with one id are one person.
        public static bool IsSamePerson(this IPerson person, IPerson other) =>
            person != null && other != null && string.Equals(person.Id, other.Id, StringComparison.Ordinal);

        public static bool HasTag(this IPerson person, string tag)
        {
            IReadOnlyList<string> tags = person?.Tags;
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < tags.Count; i++)
                if (string.Equals(tags[i], tag, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
