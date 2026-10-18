using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Checks the people file as a whole (Design 6.3), and the calls against it. Per-contact checks come from
    /// <see cref="PeopleFileLoader"/>, per-call checks from <see cref="CallsFileLoader"/> and the economy file's
    /// rules from <see cref="EconomyFileLoader"/>.
    /// Messages are for the designer who edits the files by hand: each names the entry and the fix.
    /// </summary>
    public static class ContentValidator
    {
        public const int MaxContactsPerPage = 12;

        /// <summary>The team's board in Final.unity: rows A to F, columns 1 to 6.</summary>
        public static List<string> BoardJacks()
        {
            BoardSize board = BoardSize.Full;
            var jacks = new List<string>(board.Cols * board.Rows);
            for (char row = 'A'; row <= board.LastRow; row++)
                for (int column = 1; column <= board.Cols; column++)
                    jacks.Add($"{row}{column}");
            return jacks;
        }

        public static List<string> Validate(string peopleJson, IEnumerable<string> boardJacks)
        {
            var errors = new List<string>();
            PeopleFileData data = TryParse(peopleJson);
            CheckPageSize(data, errors);
            CheckServicesFirst(data, errors);
            PeopleCatalog catalog = PeopleFileLoader.Load(peopleJson, boardJacks);
            errors.AddRange(catalog.Errors);
            CheckSurnames(catalog.Directory, errors);
            return errors;
        }

        /// <summary>
        /// The player finds a call's jacks in the book, so every jack a call uses needs someone listed on it. Until
        /// Step 8 the book is PhoneBook.json.
        /// </summary>
        public static List<string> ValidateCallJacks(IReadOnlyList<CallScript> calls, IReadOnlyList<IDirectoryEntry> book)
        {
            var errors = new List<string>();
            if (calls == null) return errors;
            var listed = new HashSet<string>(StringComparer.Ordinal);
            if (book != null)
                foreach (IDirectoryEntry entry in book)
                    if (entry != null && !string.IsNullOrEmpty(entry.JackLabel)) listed.Add(entry.JackLabel);
            foreach (CallScript call in calls)
            {
                if (call == null) continue;
                if (!listed.Contains(call.From.Label)) errors.Add(Unlisted(call, call.From));
                if (call.To != call.From && !listed.Contains(call.To.Label)) errors.Add(Unlisted(call, call.To));
            }
            return errors;
        }

        private static string Unlisted(CallScript call, JackId jack) =>
            $"{call.Id} uses {jack}, but nobody is listed on {jack} in PhoneBook.json, so the player can't look it up. " +
            "Put someone on that jack in the book, or give the call another jack.";

        /// <summary>Checks Economy.json (Design 6.4). Every rule lives in the loader, which the game uses too.</summary>
        public static IReadOnlyList<string> ValidateEconomy(string economyJson, CallsCatalog calls = null) =>
            EconomyFileLoader.Load(economyJson, calls).Errors;

        private static PeopleFileData TryParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonUtility.FromJson<PeopleFileData>(json);
            }
            catch (ArgumentException)
            {
                return null; // The loader reports invalid JSON.
            }
        }

        private static void CheckPageSize(PeopleFileData data, List<string> errors)
        {
            if (data != null && (data.contactsPerPage < 1 || data.contactsPerPage > MaxContactsPerPage))
                errors.Add($"contactsPerPage is {data.contactsPerPage}; the book allows 1 to {MaxContactsPerPage}. Use 6.");
        }

        // The book shows the file in order, and the services belong at the top of page 1 (D36).
        private static void CheckServicesFirst(PeopleFileData data, List<string> errors)
        {
            if (data?.contacts == null) return;
            bool personSeen = false;
            for (int i = 0; i < data.contacts.Length; i++)
            {
                PeopleFileContact contact = data.contacts[i];
                if (!PeopleFileLoader.IsService(contact))
                {
                    personSeen = true;
                    continue;
                }
                if (personSeen)
                    errors.Add($"{PeopleFileLoader.Describe(i, contact)}: emergency services must be listed at the top of the file, " +
                        "before the people. Move this line up to the other services.");
            }
        }

        // The book shows contacts in file order, and its letter tabs jump to the first matching surname.
        // Services are a block of their own at the top, so only people follow these rules.
        private static void CheckSurnames(IReadOnlyList<IDirectoryEntry> entries, List<string> errors)
        {
            var seen = new Dictionary<string, IPerson>(StringComparer.OrdinalIgnoreCase);
            IPerson previous = null;
            for (int i = 0; i < entries.Count; i++)
            {
                IPerson person = entries[i].Person;
                if (person.HasTag(PeopleFileLoader.ServiceTag)) continue;
                string surname = person.LastName;
                if (seen.TryGetValue(surname, out IPerson first))
                    errors.Add($"\"{person.FullName()}\": the surname {surname} is already used by \"{first.FullName()}\". " +
                        "Every surname must be different, so give one of them another surname.");
                else seen.Add(surname, person);
                if (previous != null && string.Compare(previous.LastName, surname, StringComparison.OrdinalIgnoreCase) > 0)
                    errors.Add($"\"{person.FullName()}\" is listed after \"{previous.FullName()}\", but {surname} comes before " +
                        $"{previous.LastName}. Move the line up; sort the file by surname.");
                previous = person;
            }
        }
    }
}
