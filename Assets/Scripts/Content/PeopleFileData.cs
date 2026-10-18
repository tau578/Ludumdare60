using System;

namespace Switchboard.Content
{
    /// <summary>
    /// The shared people file (D31). The team's phone book reads only contactsPerPage, name and socket,
    /// so those keep its format; the game also reads the rest.
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
        public string portrait;
        public string type;
        public float rate;
        public float fee;

        // Missing in the file means none. Entries tagged emergency-service are services, not people (D36).
        public string[] tags;
    }
}
