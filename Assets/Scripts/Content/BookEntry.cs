namespace Switchboard.Content
{
    /// <summary>One listing in the phone book (D53). Several entries may share a jack, like a household line.</summary>
    public sealed class BookEntry
    {
        public BookEntry(string name, string profession, string address, bool isEmergencyService, string jack)
        {
            Name = name;
            Profession = profession ?? string.Empty;
            Address = address ?? string.Empty;
            IsEmergencyService = isEmergencyService;
            Jack = jack;
        }

        /// <summary>As written in the file, "SURNAME, First"; a service by its whole name.</summary>
        public string Name { get; }
        public string Profession { get; }
        public string Address { get; }
        public bool IsEmergencyService { get; }

        /// <summary>A jack label on the board, such as "C3".</summary>
        public string Jack { get; }
    }
}
