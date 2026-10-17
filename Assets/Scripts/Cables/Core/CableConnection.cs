using System.Collections.Generic;

namespace Switchboard.Cables
{
    public enum CablePhase { Ready, OneEndConnected, Connected }

    /// <summary>The connection rules, independent of UI, input and sound.</summary>
    public sealed class CableConnection<TSocket> where TSocket : class
    {
        public CablePhase Phase { get; private set; }
        public TSocket FirstSocket { get; private set; }
        public TSocket SecondSocket { get; private set; }
        public bool PreventSameSocketConnection { get; }

        public CableConnection(bool preventSameSocketConnection = true)
        {
            PreventSameSocketConnection = preventSameSocketConnection;
        }

        public bool Contains(TSocket socket) => socket != null &&
            (Same(FirstSocket, socket) || Same(SecondSocket, socket));

        public bool CanInsert(TSocket socket)
        {
            if (socket == null || Phase == CablePhase.Connected) return false;
            return !PreventSameSocketConnection || !Contains(socket);
        }

        public bool TryInsert(TSocket socket)
        {
            if (!CanInsert(socket)) return false;
            // Fill only the loose end, preserving the identity of the attached plug.
            if (FirstSocket == null) FirstSocket = socket;
            else SecondSocket = socket;
            Phase = FirstSocket != null && SecondSocket != null ? CablePhase.Connected : CablePhase.OneEndConnected;
            return true;
        }

        public bool TryRemove(TSocket socket)
        {
            if (socket == null) return false;
            if (Same(FirstSocket, socket)) FirstSocket = null;
            else if (Same(SecondSocket, socket)) SecondSocket = null;
            else return false;

            Phase = FirstSocket == null && SecondSocket == null ? CablePhase.Ready : CablePhase.OneEndConnected;
            return true;
        }

        public void Reset()
        {
            FirstSocket = SecondSocket = null;
            Phase = CablePhase.Ready;
        }

        private static bool Same(TSocket a, TSocket b) => EqualityComparer<TSocket>.Default.Equals(a, b);
    }
}
