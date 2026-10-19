using System;
using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>
    /// The checks Tools/Switchboard/Validate Content runs. Each file's rules live in its loader, which the game uses
    /// too (<see cref="BookFileLoader"/>, <see cref="CallsFileLoader"/>, <see cref="EconomyFileLoader"/>); this class
    /// adds the checks across files. Messages are for the designer who edits the files by hand: each names the entry
    /// and the fix.
    /// </summary>
    public static class ContentValidator
    {
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

        /// <summary>Checks Book.json (D53). Every rule lives in the loader, which the phone book uses too.</summary>
        public static IReadOnlyList<string> ValidateBook(string bookJson, IEnumerable<string> boardJacks = null) =>
            BookFileLoader.Load(bookJson, boardJacks).Errors;

        /// <summary>The player finds a call's jacks in the book, so every jack a call uses needs an entry in Book.json.</summary>
        public static List<string> ValidateCallJacks(IReadOnlyList<CallScript> calls, IReadOnlyList<BookEntry> book)
        {
            var errors = new List<string>();
            if (calls == null) return errors;
            var listed = new HashSet<string>(StringComparer.Ordinal);
            if (book != null)
                foreach (BookEntry entry in book)
                    if (entry != null && !string.IsNullOrEmpty(entry.Jack)) listed.Add(entry.Jack);
            foreach (CallScript call in calls)
            {
                if (call == null) continue;
                if (!listed.Contains(call.From.Label)) errors.Add(Unlisted(call, call.From));
                if (call.To != call.From && !listed.Contains(call.To.Label)) errors.Add(Unlisted(call, call.To));
            }
            return errors;
        }

        private static string Unlisted(CallScript call, JackId jack) =>
            $"{call.Id} uses {jack}, but no entry in Book.json is on {jack}, so the player can't look it up. " +
            "Add an entry with that jack to the book, or give the call another jack.";

        /// <summary>Checks Economy.json (Design 6.4). Every rule lives in the loader, which the game uses too.</summary>
        public static IReadOnlyList<string> ValidateEconomy(string economyJson, CallsCatalog calls = null) =>
            EconomyFileLoader.Load(economyJson, calls).Errors;
    }
}
