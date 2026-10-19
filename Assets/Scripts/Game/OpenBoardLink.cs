using System;
using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// Pushes the open part of the board (D42, D49) to the team's sockets and the caller panel whenever the stats
    /// change. The lamps follow their sockets by themselves. The book always lists everyone (D53).
    /// </summary>
    internal sealed class OpenBoardLink : IDisposable
    {
        private readonly IReadOnlyList<SocketView> sockets;
        private readonly CallQueueView callQueue;
        private readonly StatSheet stats;
        private readonly OpenBoard board;

        public OpenBoardLink(IReadOnlyList<SocketView> sockets, CallQueueView callQueue, StatSheet stats, OpenBoard board)
        {
            this.sockets = sockets ?? Array.Empty<SocketView>();
            this.callQueue = callQueue;
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            stats.Changed += Apply;
            Apply();
        }

        public void Dispose() => stats.Changed -= Apply;

        private void Apply()
        {
            foreach (SocketView socket in sockets)
                if (socket != null) socket.SetOpen(board.IsOpen(new JackId(socket.Address)));
            if (callQueue != null) callQueue.SetOpenSlots(stats.ClientSlots);
        }
    }
}
