using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// Which jacks are open now (D42): the board size the last bought extra_clients sets. Exactly the people on
    /// open jacks are in the game.
    /// </summary>
    public sealed class OpenBoard
    {
        private readonly StatSheet stats;
        private readonly PhoneDirectory directory;

        public OpenBoard(StatSheet stats, PhoneDirectory directory)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public BoardSize Size => stats.Board;

        public bool IsOpen(JackId jack) => stats.Board.Contains(jack);

        public bool IsOpen(IDirectoryEntry entry) => entry != null && stats.Board.Contains(entry.JackLabel);

        public bool IsInGame(IPerson person) => directory.TryGetJack(person, out JackId jack) && IsOpen(jack);
    }
}
