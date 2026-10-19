using System;
using System.Collections.Generic;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Parses, checks and sorts the designer's Book.json (D53), once, for both the phone book and Validate Content.
    /// An entry with a problem is left out with a plain message naming it and the fix; the rest of the book still shows.
    /// </summary>
    public static class BookFileLoader
    {
        public const int DefaultContactsPerPage = 6;
        public const int MaxContactsPerPage = 12;

        /// <param name="boardJacks">The jack labels on the board; null means the full 6x6 board.</param>
        public static BookCatalog Load(string json, IEnumerable<string> boardJacks = null)
        {
            var errors = new List<string>();
            BookFileData data = Parse(json, errors);
            if (data == null) return new BookCatalog(DefaultContactsPerPage, null, errors);

            int perPage = PageSize(data.contactsPerPage, errors);
            var board = new HashSet<string>(boardJacks ?? ContentValidator.BoardJacks(), StringComparer.Ordinal);
            var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var services = new List<BookEntry>();
            var people = new List<BookEntry>();
            for (int i = 0; i < data.entries.Length; i++)
            {
                BookEntry entry = Check(i, data.entries[i], board, names, errors);
                if (entry == null) continue;
                (entry.IsEmergencyService ? services : people).Add(entry);
            }
            services.AddRange(SortedByName(people));
            return new BookCatalog(perPage, services, errors);
        }

        private static BookFileData Parse(string json, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add("The book file is missing or empty, so the phone book is empty. It belongs at Assets/Configs/Book.json " +
                    "and in GameConfig's Book File slot.");
                return null;
            }
            BookFileData data;
            try
            {
                data = JsonUtility.FromJson<BookFileData>(json);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"The book file is not valid JSON (check for a missing comma, quote or bracket): {ex.Message}");
                return null;
            }
            if (data == null)
            {
                errors.Add("The book file must contain a JSON object, starting with { and ending with }.");
                return null;
            }
            if ((data.entries == null || data.entries.Length == 0) && data.contacts != null && data.contacts.Length > 0)
            {
                errors.Add("The book file uses the old PhoneBook.json format: rename \"contacts\" to \"entries\", and each " +
                    "entry's \"socket\" to \"jack\". See Docs/BookFile.md.");
                return null;
            }
            if (data.entries == null || data.entries.Length == 0)
            {
                errors.Add("The book file has no entries. Add \"entries\": [ ... ] with one entry per line; see Docs/BookFile.md.");
                data.entries = Array.Empty<BookFileEntry>();
            }
            return data;
        }

        private static int PageSize(int value, List<string> errors)
        {
            if (value == BookFileData.Unset) return DefaultContactsPerPage;
            if (value >= 1 && value <= MaxContactsPerPage) return value;
            errors.Add($"contactsPerPage is {value}; the book shows 1 to {MaxContactsPerPage} entries per page. " +
                $"Using {DefaultContactsPerPage}.");
            return DefaultContactsPerPage;
        }

        private static BookEntry Check(int index, BookFileEntry entry, HashSet<string> board, Dictionary<string, int> names,
            List<string> errors)
        {
            string name = Tidy(entry?.name);
            string where = name.Length == 0 ? $"Entry {index + 1}" : $"Entry {index + 1} \"{name}\"";
            if (entry == null)
            {
                errors.Add($"{where} is empty. Fill it in or remove it.");
                return null;
            }
            int errorsBefore = errors.Count;

            if (name.Length == 0)
                errors.Add($"{where}: the name is missing. Write it \"SURNAME, First\", such as \"MOSS, Ada\".");
            else if (names.TryGetValue(name, out int first))
                errors.Add($"{where}: this name is already listed as entry {first + 1}. Every name must be different; " +
                    "give one of them another first name.");
            else names.Add(name, index);

            string jack = entry.jack?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(jack))
                errors.Add(string.IsNullOrWhiteSpace(entry.socket)
                    ? $"{where}: the jack is missing. Add the jack the player patches, such as \"jack\": \"C3\"."
                    : $"{where}: \"socket\" is the old name. Rename it to \"jack\", such as \"jack\": \"{entry.socket.Trim()}\".");
            else if (!board.Contains(jack))
                errors.Add($"{where}: jack {jack} is not on the board. Use a jack from A1 to F6, such as C3.");

            if (errors.Count > errorsBefore) return null;
            return new BookEntry(name, Tidy(entry.profession), Tidy(entry.address), entry.emergencyService, jack);
        }

        // Ordinal comparison gives the same order on every platform, Web included. The file index breaks ties,
        // because List.Sort is not stable.
        private static List<BookEntry> SortedByName(List<BookEntry> entries)
        {
            var order = new List<KeyValuePair<int, BookEntry>>(entries.Count);
            for (int i = 0; i < entries.Count; i++) order.Add(new KeyValuePair<int, BookEntry>(i, entries[i]));
            order.Sort((a, b) =>
            {
                int byName = StringComparer.OrdinalIgnoreCase.Compare(a.Value.Name, b.Value.Name);
                return byName != 0 ? byName : a.Key.CompareTo(b.Key);
            });
            var sorted = new List<BookEntry>(order.Count);
            foreach (var pair in order) sorted.Add(pair.Value);
            return sorted;
        }

        // Trimmed, with runs of spaces made single, so "MOSS,  Ada " and "MOSS, Ada" are one name.
        private static string Tidy(string text) =>
            string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
    }
}
