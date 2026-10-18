using System;

namespace Switchboard.Content
{
    /// <summary>
    /// The shared people file (D31). The team's phone book reads contactsPerPage, name and socket; the validator also
    /// reads the tags. Other fields still in the file (portrait, type, rate, fee) are no longer read: calls carry
    /// their own since Step 7 (D51), and Step 8 replaces the file.
    /// </summary>
    [Serializable]
    public sealed class PeopleFileData
    {
        public int contactsPerPage = 6;
        public PeopleFileContact[] contacts = Array.Empty<PeopleFileContact>();
    }

    [Serializable]
    public sealed class PeopleFileContact
    {
        public string name;
        public string socket;

        // Missing in the file means none. Entries tagged emergency-service are services, not people (D36).
        public string[] tags;
    }
}
