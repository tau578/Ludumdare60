using System;

namespace Switchboard.Domain
{
    /// <summary>Which jacks are open now (D42): the board size the last bought extra_clients sets.</summary>
    public sealed class OpenBoard
    {
        private readonly StatSheet stats;

        public OpenBoard(StatSheet stats) => this.stats = stats ?? throw new ArgumentNullException(nameof(stats));

        public BoardSize Size => stats.Board;

        public bool IsOpen(JackId jack) => stats.Board.Contains(jack);
    }
}
