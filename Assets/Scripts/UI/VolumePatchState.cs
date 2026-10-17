using System;

namespace Switchboard.UI
{
    // Pickup reserves the old connection. A cross-row move swaps the rows' cables atomically.
    public sealed class VolumePatchState
    {
        private readonly int[] rows = { 0, 1, 2 };
        private readonly int[] sockets = { 1, 1, 1 };
        public int HeldCable { get; private set; } = -1;
        public int RowOf(int cable) => rows[cable];
        public int SocketOf(int cable) => sockets[cable];
        public bool Restore(int[] savedRows, int[] savedSockets)
        {
            if (savedRows == null || savedSockets == null || savedRows.Length != 3 || savedSockets.Length != 3) return false;
            int occupiedRows = 0;
            for (int i = 0; i < 3; i++)
            {
                if (savedRows[i] < 0 || savedRows[i] >= 3 || savedSockets[i] < 0 || savedSockets[i] >= 5) return false;
                int bit = 1 << savedRows[i];
                if ((occupiedRows & bit) != 0) return false;
                occupiedRows |= bit;
            }
            Array.Copy(savedRows, rows, 3);
            Array.Copy(savedSockets, sockets, 3);
            HeldCable = -1;
            return true;
        }
        public bool PickUp(int cable)
        {
            if (cable < 0 || cable >= 3 || HeldCable >= 0) return false;
            HeldCable = cable;
            return true;
        }
        public int CableAt(int row, int socket)
        {
            for (int i = 0; i < 3; i++)
                if (i != HeldCable && rows[i] == row && sockets[i] == socket) return i;
            return -1;
        }
        public bool Connect(int row, int socket)
        {
            if (HeldCable < 0 || row < 0 || row >= 3 || socket < 0 || socket >= 5) return false;
            int cable = HeldCable;
            if (row != rows[cable])
            {
                int displaced = Array.IndexOf(rows, row);
                rows[displaced] = rows[cable];
                sockets[displaced] = sockets[cable];
            }
            rows[cable] = row;
            sockets[cable] = socket;
            HeldCable = -1;
            return true;
        }
        public void Cancel() => HeldCable = -1;
    }
}
