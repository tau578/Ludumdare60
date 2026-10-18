using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Checks the people file and the caller types together (Design 6.3). Per-contact checks come from
    /// <see cref="PeopleFileLoader"/>; this adds what needs the whole file or the type catalog.
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
            CheckTypes(callerTypes, errors);
            PeopleFileData data = TryParse(peopleJson);
            CheckPageSize(data, errors);
            CheckServicesFirst(data, errors);
            PeopleCatalog catalog = PeopleFileLoader.Load(peopleJson, callerTypes, hasPortrait, boardJacks);
            errors.AddRange(catalog.Errors);
            CheckSurnames(catalog.Directory, errors);
            CheckRecipients(callerTypes, catalog, errors);
            return errors;
        }

        private static void CheckTypes(IReadOnlyList<ICallerType> types, List<string> errors)
        {
            if (types == null || types.Count == 0)
            {
                errors.Add("There are no caller types. Add at least one to GameConfig's caller types.");
                return;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool anySpawns = false;
            for (int i = 0; i < types.Count; i++)
            {
                ICallerType type = types[i];
                if (type == null)
                {
                    errors.Add($"Caller type {i + 1} in GameConfig is empty. Pick an asset or remove the slot.");
                    continue;
                }
                string where = $"Caller type '{type.Id}'";
                if (string.IsNullOrWhiteSpace(type.Id)) errors.Add($"Caller type {i + 1} has no id. Give it one, such as type.regular.");
                else if (!ids.Add(type.Id)) errors.Add($"{where} is listed twice in GameConfig. Remove one of them.");
                CheckRange(where, "rate", type.CentsPerSecond, 0, errors);
                CheckRange(where, "connection fee", type.ConnectionFeeCents, 0, errors);
                CheckRange(where, "duration", type.DurationSeconds, float.Epsilon, errors);
                CheckRange(where, "callback delay", type.CallbackDelaySeconds, 0, errors);
                CheckNotNegative(where, "response time", type.ResponseTimeSeconds, errors);
                CheckNotNegative(where, "missed fine", type.FineMissedCents, errors);
                CheckNotNegative(where, "early disconnect fine", type.FineEarlyDisconnectCents, errors);
                CheckNotNegative(where, "max simultaneous", type.MaxSimultaneous, errors);
                CheckNotNegative(where, "spawn weight", type.SpawnWeight, errors);
                if (type.SpawnWeight > 0) anySpawns = true;
            }
            if (!anySpawns) errors.Add("No caller type has a spawn weight above 0, so no calls would ever ring. Give one a weight, such as 1.");
        }

        private static void CheckRange(string where, string what, FloatRange range, float lowest, List<string> errors)
        {
            if (range.min > range.max)
                errors.Add($"{where}: the {what} range has min {range.min} above max {range.max}. Swap them.");
            if (range.min < lowest) errors.Add($"{where}: the {what} range starts at {range.min}, below {lowest}. Raise the min.");
        }

        private static void CheckNotNegative(string where, string what, double value, List<string> errors)
        {
            if (value < 0) errors.Add($"{where}: the {what} is {value}. Use 0 or more.");
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
                        errors.Add($"Caller type '{type.Id}' sends its calls to contacts tagged \"{type.RecipientTag}\", but no contact " +
                            "has that tag. Add the tag to a contact, or clear the type's recipient tag.");
                }
                // A caller can't call themselves, so people calling people need at least two.
                else if (HasCaller(catalog.Callers, type) && recipients < 2)
                    errors.Add($"Caller type '{type.Id}' has nobody to call. Add at least two people without tags.");
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
