using System;
using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// Pushes the open part of the board (D42, D49) to the team's sockets, the phone book and the caller panel
    /// whenever the stats change. The lamps follow their sockets by themselves.
    /// </summary>
    internal sealed class OpenBoardLink : IDisposable
    {
        private readonly IReadOnlyList<SocketView> sockets;
        private readonly PhoneBookView book;
        private readonly CallQueueView callQueue;
        private readonly StatSheet stats;
        private readonly OpenBoard board;
        private readonly Predicate<string> isOpenSocket;

        public OpenBoardLink(IReadOnlyList<SocketView> sockets, PhoneBookView book, CallQueueView callQueue, StatSheet stats,
            OpenBoard board)
        {
            this.sockets = sockets ?? Array.Empty<SocketView>();
            this.book = book;
            this.callQueue = callQueue;
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            isOpenSocket = socket => this.board.IsOpen(new JackId(socket));
            stats.Changed += Apply;
            Apply();
        }

        public void Dispose() => stats.Changed -= Apply;

        private void Apply()
        {
            foreach (SocketView socket in sockets)
                if (socket != null) socket.SetOpen(board.IsOpen(new JackId(socket.Address)));
            if (book != null) book.SetVisibleSockets(isOpenSocket);
            if (callQueue != null) callQueue.SetOpenSlots(stats.ClientSlots);
        }
    }
}
