using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>Parses and validates the people file (D31). Invalid entries are left out and reported.</summary>
    public static class PeopleFileLoader
    {
        /// <param name="hasPortrait">Null skips the portrait check.</param>
        /// <param name="boardJacks">The jack labels on the board; null skips the on-board check.</param>
        public static PeopleCatalog Load(string json, IReadOnlyList<ICallerType> callerTypes,
            Func<string, bool> hasPortrait, IEnumerable<string> boardJacks)
        {
            var errors = new List<string>();
            var people = new List<IPerson>();
            var callers = new List<ICaller>();
            var directory = new List<IDirectoryEntry>();

            PeopleFileData data = Parse(json, errors);
            if (data != null)
            {
                var board = boardJacks != null ? new HashSet<string>(boardJacks, StringComparer.Ordinal) : null;
                var jackOwners = new Dictionary<string, string>(StringComparer.Ordinal);
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < data.contacts.Length; i++)
                    Add(i, data.contacts[i], callerTypes, hasPortrait, board, jackOwners, ids, errors, people, callers, directory);
            }
            return new PeopleCatalog(people, callers, directory, errors);
        }

        public static string PersonId(string name) => "person." + string.Join("-", Words(name)).ToLowerInvariant();

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
                errors.Add($"The people file is not valid JSON: {ex.Message}");
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

        private static void Add(int index, PeopleFileContact contact, IReadOnlyList<ICallerType> callerTypes,
            Func<string, bool> hasPortrait, HashSet<string> board, Dictionary<string, string> jackOwners,
            HashSet<string> ids, List<string> errors, List<IPerson> people, List<ICaller> callers, List<IDirectoryEntry> directory)
        {
            string[] words = Words(contact?.name);
            if (words.Length == 0)
            {
                errors.Add($"Contact {index + 1}: the name is empty.");
                return;
            }
            string name = string.Join(" ", words);
            string where = $"Contact {index + 1} ({name})";
            int errorsBefore = errors.Count;

            string jack = contact.socket?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(jack)) errors.Add($"{where}: the jack is empty.");
            else if (board != null && !board.Contains(jack)) errors.Add($"{where}: jack {jack} is not on the board.");
            else if (jackOwners.TryGetValue(jack, out string owner)) errors.Add($"{where}: jack {jack} is already used by {owner}.");
            else jackOwners.Add(jack, name);

            string id = PersonId(name);
            if (!ids.Add(id)) errors.Add($"{where}: this name is listed twice.");
            if (hasPortrait != null && !hasPortrait(contact.portrait))
                errors.Add($"{where}: portrait '{contact.portrait}' is not in GameConfig's portrait list.");
            ICallerType type = FindType(callerTypes, contact.type);
            if (type == null) errors.Add($"{where}: call type '{contact.type}' is unknown.");
            if (contact.rate < 0 || contact.fee < 0) errors.Add($"{where}: rate and fee must not be negative.");
            if (errors.Count > errorsBefore) return;

            string firstName = string.Join(" ", words, 0, words.Length - 1);
            var person = new PersonRecord(id, firstName, words[words.Length - 1], contact.portrait);
            people.Add(person);
            callers.Add(new CallerRecord("caller." + id.Substring("person.".Length), person, type, contact.rate, contact.fee));
            directory.Add(new DirectoryRecord(person, jack));
        }

        private static ICallerType FindType(IReadOnlyList<ICallerType> callerTypes, string id)
        {
            if (callerTypes == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < callerTypes.Count; i++)
                if (callerTypes[i] != null && string.Equals(callerTypes[i].Id, id, StringComparison.Ordinal)) return callerTypes[i];
            return null;
        }

        private static string[] Words(string name) =>
            string.IsNullOrWhiteSpace(name)
                ? Array.Empty<string>()
                : name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
    }
}
