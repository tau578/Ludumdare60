using System;

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
    }
}
