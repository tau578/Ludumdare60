using System;

namespace Switchboard.UI
{
    // The wire is the channel and its socket's column the volume; the row is only where it sits (lead, 2026-10-20).
    // No jack moves on its own: a socket holding another wire refuses the held one. Pickup reserves the old connection.
    // Like a real board, the wire picked up last lies on top of the others (lead, 2026-10-20).
    public sealed class VolumePatchState
    {
        private static readonly int[] DefaultStack = { 0, 1, 2 };
        private readonly int[] rows = { 0, 1, 2 };
        private readonly int[] sockets = { 1, 1, 1 };
        private readonly int[] stack = { 0, 1, 2 };
        public int HeldCable { get; private set; } = -1;
        public int RowOf(int cable) => rows[cable];
        public int SocketOf(int cable) => sockets[cable];
        /// <summary>The cable drawn at this layer; layer 0 is the bottom one.</summary>
        public int CableInLayer(int layer) => stack[layer];
        public int LayerOf(int cable) => Array.IndexOf(stack, cable);
        /// <summary>A missing or broken stack order falls back to the default without rejecting the connections.</summary>
        public bool Restore(int[] savedRows, int[] savedSockets, int[] savedStack = null)
        {
            if (savedRows == null || savedSockets == null || savedRows.Length != 3 || savedSockets.Length != 3) return false;
            int occupied = 0;
            for (int i = 0; i < 3; i++)
            {
                if (savedRows[i] < 0 || savedRows[i] >= 3 || savedSockets[i] < 0 || savedSockets[i] >= 5) return false;
                int bit = 1 << (savedRows[i] * 5 + savedSockets[i]);
                if ((occupied & bit) != 0) return false;
                occupied |= bit;
            }
            Array.Copy(savedRows, rows, 3);
            Array.Copy(savedSockets, sockets, 3);
            Array.Copy(IsStack(savedStack) ? savedStack : DefaultStack, stack, 3);
            HeldCable = -1;
            return true;
        }
        private static bool IsStack(int[] order)
        {
            if (order == null || order.Length != 3) return false;
            int seen = 0;
            foreach (int cable in order)
            {
                if (cable < 0 || cable >= 3 || (seen & (1 << cable)) != 0) return false;
                seen |= 1 << cable;
            }
            return true;
        }
        public bool PickUp(int cable)
        {
            if (cable < 0 || cable >= 3 || HeldCable >= 0) return false;
            HeldCable = cable;
            for (int layer = LayerOf(cable); layer < 2; layer++) stack[layer] = stack[layer + 1];
            stack[2] = cable;
            return true;
        }
        public int CableAt(int row, int socket)
        {
            for (int i = 0; i < 3; i++)
                if (i != HeldCable && rows[i] == row && sockets[i] == socket) return i;
            return -1;
        }
        public bool IsFree(int row, int socket) => CableAt(row, socket) < 0;
        public bool Connect(int row, int socket)
        {
            if (HeldCable < 0 || row < 0 || row >= 3 || socket < 0 || socket >= 5 || !IsFree(row, socket)) return false;
            rows[HeldCable] = row;
            sockets[HeldCable] = socket;
            HeldCable = -1;
            return true;
        }
        public void Cancel() => HeldCable = -1;
    }
}
