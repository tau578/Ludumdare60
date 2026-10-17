using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>Links people and jacks. Content links them only through the directory table.</summary>
    public sealed class PhoneDirectory
    {
        private readonly Dictionary<string, JackId> jackByPerson = new Dictionary<string, JackId>(StringComparer.Ordinal);
        private readonly Dictionary<JackId, IPerson> personByJack = new Dictionary<JackId, IPerson>();
        private readonly List<IDirectoryEntry> listed = new List<IDirectoryEntry>();

        public PhoneDirectory(IEnumerable<IDirectoryEntry> entries)
        {
            if (entries != null)
                foreach (IDirectoryEntry entry in entries) Add(entry);
            listed.Sort(BySurname);
        }

        public IReadOnlyList<IDirectoryEntry> ListedEntries => listed;

        public bool Lists(IPerson person) => person != null && person.Id != null && jackByPerson.ContainsKey(person.Id);

        public bool TryGetJack(IPerson person, out JackId jack)
        {
            jack = default;
            return person != null && person.Id != null && jackByPerson.TryGetValue(person.Id, out jack);
        }

        public JackId JackOf(IPerson person) => TryGetJack(person, out JackId jack)
            ? jack
            : throw new KeyNotFoundException($"{person?.Id ?? "null"} is not in the directory.");

        public IPerson PersonAt(JackId jack) => personByJack.TryGetValue(jack, out IPerson person) ? person : null;

        // Duplicates are skipped here; reporting them is the content validator's job.
        private void Add(IDirectoryEntry entry)
        {
            if (entry?.Person?.Id == null || string.IsNullOrEmpty(entry.JackLabel)) return;
            var jack = new JackId(entry.JackLabel);
            if (jackByPerson.ContainsKey(entry.Person.Id) || personByJack.ContainsKey(jack)) return;
            jackByPerson.Add(entry.Person.Id, jack);
            personByJack.Add(jack, entry.Person);
            listed.Add(entry);
        }

        private static int BySurname(IDirectoryEntry a, IDirectoryEntry b)
        {
            int order = string.Compare(a.Person.LastName, b.Person.LastName, StringComparison.OrdinalIgnoreCase);
            if (order == 0) order = string.Compare(a.Person.FirstName, b.Person.FirstName, StringComparison.OrdinalIgnoreCase);
            return order != 0 ? order : string.CompareOrdinal(a.JackLabel, b.JackLabel);
        }
    }
}
