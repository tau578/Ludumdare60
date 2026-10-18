using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Checks the people file against the economy file's call types (Design 6.3). Per-contact checks come from
    /// <see cref="PeopleFileLoader"/>; this adds what needs the whole file or the type catalog. The call types'
    /// own rules live in <see cref="EconomyFileLoader"/>.
    /// Messages are for the designer who edits the file by hand: each names the contact and the fix.
    /// </summary>
    public static class ContentValidator
    {
        public const int MaxContactsPerPage = 12;

        /// <summary>The team's board in Final.unity: rows A to F, columns 1 to 6.</summary>
        public static List<string> BoardJacks()
        {
            var jacks = new List<string>(36);
            for (char row = 'A'; row <= 'F'; row++)
                for (int column = 1; column <= 6; column++)
                    jacks.Add($"{row}{column}");
            return jacks;
        }

        public static List<string> Validate(string peopleJson, IReadOnlyList<ICallerType> callerTypes,
            Func<string, bool> hasPortrait, IEnumerable<string> boardJacks)
        {
            var errors = new List<string>();
            PeopleFileData data = TryParse(peopleJson);
            CheckPageSize(data, errors);
            CheckServicesFirst(data, errors);
            PeopleCatalog catalog = PeopleFileLoader.Load(peopleJson, callerTypes, hasPortrait, boardJacks);
            errors.AddRange(catalog.Errors);
            CheckSurnames(catalog.Directory, errors);
            CheckRecipients(callerTypes, catalog, errors);
            return errors;
        }

        /// <summary>Checks Economy.json (Design 6.4). Every rule lives in the loader, which the game uses too.</summary>
        public static IReadOnlyList<string> ValidateEconomy(string economyJson) => EconomyFileLoader.Load(economyJson).Errors;

        /// <summary>
        /// Checks GameConfig's card styles against the economy file's call types. A call type with no style gets
        /// plain cards, so only styles that match no call type are reported.
        /// </summary>
        /// <param name="styleIds">The id of each style asset in GameConfig's list; null for an empty slot.</param>
        public static List<string> ValidateCardStyles(IReadOnlyList<string> styleIds, IReadOnlyList<ICallerType> callTypes)
        {
            var errors = new List<string>();
            if (styleIds == null) return errors;
            for (int i = 0; i < styleIds.Count; i++)
            {
                string id = styleIds[i];
                if (id == null)
                    errors.Add($"GameConfig's card style {i + 1} is empty. Pick a card style asset or remove the slot.");
                else if (!HasType(callTypes, id))
                    errors.Add($"GameConfig's card style {i + 1} is for \"{id}\", but Economy.json has no call type with that id. " +
                        "Add the call type to the file's calls, or correct the style asset's id.");
            }
            return errors;
        }

        private static bool HasType(IReadOnlyList<ICallerType> types, string id)
        {
            if (types == null) return false;
            foreach (ICallerType type in types)
                if (type != null && string.Equals(type.Id, id, StringComparison.Ordinal)) return true;
            return false;
        }

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

        private static void CheckRecipients(IReadOnlyList<ICallerType> types, PeopleCatalog catalog, List<string> errors)
        {
            if (types == null) return;
            foreach (ICallerType type in types)
            {
                if (type == null) continue;
                int recipients = 0;
                foreach (IDirectoryEntry entry in catalog.Directory)
                    if (type.Accepts(entry.Person)) recipients++;
                if (!string.IsNullOrEmpty(type.RecipientTag))
                {
                    if (recipients == 0)
                        errors.Add($"Call type '{type.Id}' sends its calls to contacts tagged \"{type.RecipientTag}\", but no contact " +
                            "has that tag. Add the tag to a contact, or clear the type's recipientTag in Economy.json.");
                }
                // A caller can't call themselves, so people calling people need at least two.
                else if (HasCaller(catalog.Callers, type) && recipients < 2)
                    errors.Add($"Call type '{type.Id}' has nobody to call. Add at least two people without tags.");
            }
        }

        private static bool HasCaller(IReadOnlyList<ICaller> callers, ICallerType type)
        {
            foreach (ICaller caller in callers)
                if (caller.Type == type) return true;
            return false;
        }
    }
}
