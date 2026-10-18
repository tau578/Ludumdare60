using System;
using Switchboard.Domain;
using Switchboard.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// Wires the team's phone book to the Notepad (D23): rows take taps only while the Notepad is owned, and a tap
    /// notes the row's jack on the call in hand.
    /// </summary>
    internal sealed class NotepadBookLink : IDisposable
    {
        private readonly PhoneBookView book;
        private readonly StatSheet stats;
        private readonly Notepad notepad;

        public NotepadBookLink(PhoneBookView book, StatSheet stats, Notepad notepad)
        {
            this.book = book ?? throw new ArgumentNullException(nameof(book));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.notepad = notepad ?? throw new ArgumentNullException(nameof(notepad));
            book.ContactTapped += OnContactTapped;
            stats.Changed += Apply;
            Apply();
        }

        public void Dispose()
        {
            stats.Changed -= Apply;
            if (book != null) book.ContactTapped -= OnContactTapped;
        }

        private void Apply()
        {
            if (book != null) book.ContactTapsEnabled = stats.HasNotepad;
        }

        private void OnContactTapped(string name, string socket) => notepad.Note(new JackId(socket));
    }
}
