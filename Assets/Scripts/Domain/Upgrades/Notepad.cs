using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// The Notepad upgrade (D23): while it is owned, a tapped book entry notes that person's jack on the call in
    /// hand, if they are its caller or the person wanted. Anyone else, or no call in hand, changes nothing.
    /// Notes live on the call, so they survive a swap and go when the call ends.
    /// </summary>
    public sealed class Notepad
    {
        private readonly CallService calls;
        private readonly PhoneDirectory directory;
        private readonly StatSheet stats;

        public Notepad(CallService calls, PhoneDirectory directory, StatSheet stats)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>Raised once for every new note, with the call it was written on.</summary>
        public event Action<CallId> Noted;

        public bool Note(JackId jack)
        {
            Call call = calls.InHand;
            if (!stats.HasNotepad || call == null) return false;
            if (call.CallerNote == null && Is(call.Request.CallerPerson, jack))
            {
                call.CallerNote = jack;
                Noted?.Invoke(call.Id);
                return true;
            }
            if (call.RecipientNote == null && Is(call.Request.Recipient, jack))
            {
                call.RecipientNote = jack;
                Noted?.Invoke(call.Id);
                return true;
            }
            return false;
        }

        private bool Is(IPerson person, JackId jack) => directory.TryGetJack(person, out JackId listed) && listed == jack;
    }
}
