using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Parses and validates the people file (D31). Since Step 7 only the book's fields matter (name, socket, tags);
    /// calls carry their own portrait, jacks and pay (D51). Invalid entries are left out and reported in plain words,
    /// because the designer edits the file by hand.
    /// </summary>
    public static class PeopleFileLoader
    {
        /// <summary>Entries with this tag are emergency services (D36): listed and reachable, but they never call.</summary>
        public const string ServiceTag = "emergency-service";

        /// <param name="boardJacks">The jack labels on the board; null skips the on-board check.</param>
        public static PeopleCatalog Load(string json, IEnumerable<string> boardJacks)
        {
            var errors = new List<string>();
            var people = new List<IPerson>();
            var directory = new List<IDirectoryEntry>();

            PeopleFileData data = Parse(json, errors);
            if (data != null)
            {
                var board = boardJacks != null ? new HashSet<string>(boardJacks, StringComparer.Ordinal) : null;
                var jackOwners = new Dictionary<string, string>(StringComparer.Ordinal);
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < data.contacts.Length; i++)
                    Add(i, data.contacts[i], board, jackOwners, ids, errors, people, directory);
            }
            return new PeopleCatalog(people, directory, errors);
        }

        public static string PersonId(string name)
        {
            SplitName(name, out string first, out string last);
            return "person." + string.Join("-", Words(first + " " + last)).ToLowerInvariant();
        }

        public static bool IsService(PeopleFileContact contact) => HasTag(contact?.tags, ServiceTag);

        /// <summary>How messages name a contact: its place in the file and its name.</summary>
        public static string Describe(int index, PeopleFileContact contact)
        {
            string[] words = Words(contact?.name);
            return words.Length == 0 ? $"Contact {index + 1}" : $"Contact {index + 1} \"{string.Join(" ", words)}\"";
        }

        private static PeopleFileData Parse(string json, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add("The people file is missing or empty.");
                return null;
            }
            PeopleFileData data;
            try
            {
                data = JsonUtility.FromJson<PeopleFileData>(json);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"The people file is not valid JSON (check for a missing comma, quote or bracket): {ex.Message}");
                return null;
            }
            if (data == null)
            {
                errors.Add("The people file must contain a JSON object.");
                return null;
            }
            if (data.contacts == null) data.contacts = Array.Empty<PeopleFileContact>();
            return data;
        }

        private static void Add(int index, PeopleFileContact contact, HashSet<string> board, Dictionary<string, string> jackOwners,
            HashSet<string> ids, List<string> errors, List<IPerson> people, List<IDirectoryEntry> directory)
        {
            string[] words = Words(contact?.name);
            if (words.Length == 0)
            {
                errors.Add($"Contact {index + 1}: the name is empty. Type a name, such as \"Ada Moss\".");
                return;
            }
            string name = string.Join(" ", words);
            string where = Describe(index, contact);
            bool service = IsService(contact);
            int errorsBefore = errors.Count;

            string jack = contact.socket?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(jack)) errors.Add($"{where}: the socket is empty. Give it a free jack on the board, such as C4.");
            else if (board != null && !board.Contains(jack))
                errors.Add($"{where}: jack {jack} is not on the board. Use a jack from the board, such as C4.");
            else if (jackOwners.TryGetValue(jack, out string owner))
                errors.Add($"{where}: jack {jack} is already used by {owner}. Give one of them a free jack.");
            else jackOwners.Add(jack, name);

            string id = PersonId(name);
            if (!ids.Add(id)) errors.Add($"{where}: this name is listed twice. Every name must be different.");

            if (errors.Count > errorsBefore) return;

            // A service's whole name is its listing, so "Fire Brigade" is not filed under Brigade.
            SplitName(name, out string firstName, out string lastName);
            if (service) { firstName = string.Empty; lastName = name; }
            var person = new PersonRecord(id, firstName, lastName, Tags(contact.tags));
            people.Add(person);
            directory.Add(new DirectoryRecord(person, jack));
        }

        private static IReadOnlyList<string> Tags(string[] tags)
        {
            if (tags == null || tags.Length == 0) return Array.Empty<string>();
            var clean = new List<string>(tags.Length);
            foreach (string tag in tags)
                if (!string.IsNullOrWhiteSpace(tag)) clean.Add(tag.Trim());
            return clean;
        }

        private static bool HasTag(string[] tags, string tag)
        {
            if (tags == null) return false;
            foreach (string each in tags)
                if (each != null && string.Equals(each.Trim(), tag, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void SplitName(string name, out string firstName, out string lastName)
        {
            int comma = name?.IndexOf(',') ?? -1;
            if (comma >= 0)
            {
                lastName = string.Join(" ", Words(name.Substring(0, comma)));
                firstName = string.Join(" ", Words(name.Substring(comma + 1)));
                return;
            }
            string[] words = Words(name);
            firstName = words.Length > 1 ? string.Join(" ", words, 0, words.Length - 1) : string.Empty;
            lastName = words.Length > 0 ? words[words.Length - 1] : string.Empty;
        }

        private static string[] Words(string name) =>
            string.IsNullOrWhiteSpace(name)
                ? Array.Empty<string>()
                : name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
    }
}
