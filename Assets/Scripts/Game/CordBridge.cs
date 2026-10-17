using System.Collections.Generic;
using Switchboard.Cables;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Game
{
    /// <summary>
    /// Mirrors the team's cords into the domain through their public members only (Design 3 and 4.5).
    /// The cord input runs in Update, so LateUpdate sees its result in the same frame.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class CordBridge : MonoBehaviour, ISwitchboard
    {
        private CableController[] cables = new CableController[0];
        private CordState[] observed = new CordState[0];
        private OperatorDesk desk;

        public int CordCount => cables.Length;

        public void Attach(Scene level)
        {
            List<CableController> found = LevelScene.FindAll<CableController>(level);
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            cables = found.ToArray();
            observed = new CordState[cables.Length];
            for (int i = 0; i < cables.Length; i++) observed[i] = CordState.Of(cables[i]);
        }

        public void Bind(OperatorDesk operatorDesk) => desk = operatorDesk;

        public void ReturnToTray(CordId cord)
        {
            CableController cable = CableAt(cord);
            if (cable == null) return;
            SocketView first = cable.FirstSocket, second = cable.SecondSocket;
            if (first != null) cable.TryRemove(first);
            if (second != null) cable.TryRemove(second);
        }

        public void Refuse(CordId cord)
        {
            CableController cable = CableAt(cord);
            if (cable == null) return;
            cable.PlayBlockedFeedback();
            ReturnToTray(cord);
        }

        private CableController CableAt(CordId cord) =>
            cord.Index >= 0 && cord.Index < cables.Length ? cables[cord.Index] : null;

        private void LateUpdate()
        {
            if (desk == null) return;
            for (int i = 0; i < cables.Length; i++)
            {
                if (cables[i] == null) continue;
                CordState before = observed[i];
                CordState now = CordState.Of(cables[i]);
                if (now.Same(before)) continue;
                observed[i] = now;
                Report(i, before, now);
            }
        }

        // Reports in a fixed order: opened, returned, first plug, closed. The desk's own commands show up
        // as changes next frame; if one lands mid-report, the rest of this report is stale and skipped.
        private void Report(int index, CordState before, CordState now)
        {
            var cord = new CordId(index);
            bool returned = false;
            if (before.IsClosed && !now.IsSameCircuit(before))
            {
                bool firstStays = now.Holds(before.First);
                bool secondStays = now.Holds(before.Second);
                if (firstStays != secondStays)
                {
                    SocketView stillIn = firstStays ? before.First : before.Second;
                    SocketView pulled = firstStays ? before.Second : before.First;
                    desk.OnCircuitOpened(cord, Jack(stillIn), Jack(pulled));
                }
                else
                {
                    desk.OnCordReturned(cord);
                    returned = true;
                }
                if (Moved(index, now)) return;
            }
            if (!returned && now.Phase == CablePhase.Ready && before.Phase != CablePhase.Ready)
            {
                desk.OnCordReturned(cord);
                if (Moved(index, now)) return;
            }
            if (before.PlugCount == 0 && now.PlugCount > 0)
            {
                desk.OnPlugInserted(cord, Jack(now.First != null ? now.First : now.Second));
                if (Moved(index, now)) return;
            }
            if (now.IsClosed && !before.IsSameCircuit(now))
                desk.OnCircuitClosed(cord, Jack(now.First), Jack(now.Second));
        }

        private bool Moved(int index, CordState expected) => !CordState.Of(cables[index]).Same(expected);

        private static JackId Jack(SocketView socket) => new JackId(socket.Address);

        private readonly struct CordState
        {
            private CordState(CablePhase phase, SocketView first, SocketView second)
            {
                Phase = phase;
                First = first;
                Second = second;
            }

            public CablePhase Phase { get; }
            public SocketView First { get; }
            public SocketView Second { get; }
            public int PlugCount => (First != null ? 1 : 0) + (Second != null ? 1 : 0);
            public bool IsClosed => Phase == CablePhase.Connected && First != null && Second != null;

            public static CordState Of(CableController cable) =>
                new CordState(cable.Phase, cable.FirstSocket, cable.SecondSocket);

            public bool Holds(SocketView socket) => socket != null && (First == socket || Second == socket);
            public bool IsSameCircuit(CordState other) => IsClosed && other.IsClosed && Holds(other.First) && Holds(other.Second);
            public bool Same(CordState other) => Phase == other.Phase && First == other.First && Second == other.Second;
        }
    }
}
