using System;
using System.Collections.Generic;

namespace Switchboard.Content
{
    /// <summary>The book's entries in the order it shows them, plus a plain message for every problem found.</summary>
    public sealed class BookCatalog
    {
        public BookCatalog(int contactsPerPage, IReadOnlyList<BookEntry> entries, IReadOnlyList<string> errors)
        {
            ContactsPerPage = contactsPerPage;
            Entries = entries ?? Array.Empty<BookEntry>();
            Errors = errors ?? Array.Empty<string>();
        }

        public static BookCatalog Empty { get; } = new BookCatalog(BookFileLoader.DefaultContactsPerPage, null, null);

        public int ContactsPerPage { get; }

        /// <summary>Services first, in file order; then everyone else by name, ignoring case.</summary>
        public IReadOnlyList<BookEntry> Entries { get; }

        public IReadOnlyList<string> Errors { get; }
    }
}
