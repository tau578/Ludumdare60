using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// The Notepad upgrade (D23): while it is owned, a tapped book entry notes that person's jack on every accepted
    /// call still waiting to connect (Taken or Connecting) where they are the caller or the person wanted.
    /// Ringing, connected, held and ended calls are never noted. Notes live on the call until it ends.
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

        /// <summary>Raised once for every call that got a new note.</summary>
        public event Action<CallId> Noted;

        /// <summary>Returns true if at least one call got a new note.</summary>
        public bool Note(JackId jack)
        {
            if (!stats.HasNotepad) return false;
            bool any = false;
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
            {
                Call call = live[i];
                if (!IsWaitingToConnect(call.State)) continue;
                if (!NoteOn(call, jack)) continue;
                any = true;
                Noted?.Invoke(call.Id);
            }
            return any;
        }

        private bool NoteOn(Call call, JackId jack)
        {
            bool changed = false;
            if (call.CallerNote == null && Is(call.Request.CallerPerson, jack))
            {
                call.CallerNote = jack;
                changed = true;
            }
            if (call.RecipientNote == null && Is(call.Request.Recipient, jack))
            {
                call.RecipientNote = jack;
                changed = true;
            }
            return changed;
        }

        private static bool IsWaitingToConnect(CallState state) =>
            state == CallState.Taken || state == CallState.Connecting;

        private bool Is(IPerson person, JackId jack) => directory.TryGetJack(person, out JackId listed) && listed == jack;
    }
}
