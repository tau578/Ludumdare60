using System;

namespace Switchboard.Content
{
    /// <summary>
    /// The designer's Assets/Configs/Book.json (D53). JsonUtility keeps a field's initial value when the file leaves it
    /// out, so contactsPerPage starts as <see cref="Unset"/> to tell "missing" from a typed number.
    /// </summary>
    [Serializable]
    public sealed class BookFileData
    {
        public const int Unset = int.MinValue;

        public int contactsPerPage = Unset;
        public BookFileEntry[] entries;

        // Only read to point an old PhoneBook.json-style file at the new name.
        public BookFileEntry[] contacts;
    }

    [Serializable]
    public sealed class BookFileEntry
    {
        public string name;
        public string profession;
        public string address;
        public bool emergencyService;
        public string jack;

        // Only read to point an old-style entry at the new name.
        public string socket;
    }
}
