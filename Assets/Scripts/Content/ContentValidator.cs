using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Checks the people file and the caller types together (Design 6.3). Per-contact checks come from
    /// <see cref="PeopleFileLoader"/>; this adds what needs the whole file or the type catalog.
    /// </summary>
    public static class ContentValidator
    {
        public const int MaxContactsPerPage = 12;

        /// <summary>The team's board in Level.unity: rows A to F, columns 1 to 6.</summary>
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
            CheckPageSize(peopleJson, errors);
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
                errors.Add("There are no caller types.");
                return;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < types.Count; i++)
            {
                ICallerType type = types[i];
                if (type == null)
                {
                    errors.Add($"Caller type {i + 1} is missing.");
                    continue;
                }
                string where = $"Caller type '{type.Id}'";
                if (string.IsNullOrWhiteSpace(type.Id)) errors.Add($"Caller type {i + 1} has no id.");
                else if (!ids.Add(type.Id)) errors.Add($"{where} is listed twice.");
                CheckRange(where, "rate", type.CentsPerSecond, 0, errors);
                CheckRange(where, "connection fee", type.ConnectionFeeCents, 0, errors);
                CheckRange(where, "duration", type.DurationSeconds, float.Epsilon, errors);
                CheckRange(where, "callback delay", type.CallbackDelaySeconds, 0, errors);
            }
        }

        private static void CheckRange(string where, string what, FloatRange range, float lowest, List<string> errors)
        {
            if (range.min > range.max) errors.Add($"{where}: the {what} range has min {range.min} above max {range.max}.");
            if (range.min < lowest) errors.Add($"{where}: the {what} range starts at {range.min}, below {lowest}.");
        }

        private static void CheckPageSize(string json, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                PeopleFileData data = JsonUtility.FromJson<PeopleFileData>(json);
                if (data != null && (data.contactsPerPage < 1 || data.contactsPerPage > MaxContactsPerPage))
                    errors.Add($"contactsPerPage is {data.contactsPerPage}; the book allows 1 to {MaxContactsPerPage}.");
            }
            catch (ArgumentException)
            {
                // The loader reports invalid JSON.
            }
        }

        // The book shows contacts in file order, and its letter tabs jump to the first matching surname.
        private static void CheckSurnames(IReadOnlyList<IDirectoryEntry> entries, List<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                string surname = entries[i].Person.LastName;
                if (!seen.Add(surname)) errors.Add($"The surname {surname} is used twice.");
                if (i > 0 && string.Compare(entries[i - 1].Person.LastName, surname, StringComparison.OrdinalIgnoreCase) > 0)
                    errors.Add($"{entries[i - 1].Person.LastName} is listed before {surname}; sort the file by surname.");
            }
        }

        // Any subscriber can be a recipient for now, so a type needs someone besides the caller.
        private static void CheckRecipients(IReadOnlyList<ICallerType> types, PeopleCatalog catalog, List<string> errors)
        {
            if (types == null) return;
            foreach (ICallerType type in types)
            {
                if (type == null || !HasCaller(catalog.Callers, type)) continue;
                if (catalog.People.Count < 2) errors.Add($"Caller type '{type.Id}' has nobody to call.");
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
