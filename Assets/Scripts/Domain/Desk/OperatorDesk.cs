using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>Tracks persistent physical circuits and connects accepted calls by their subscribers.</summary>
    public sealed class OperatorDesk
    {
        private readonly CallService calls;
        private readonly ConnectionRule rule;
        private readonly ISwitchboard switchboard;
        private readonly List<CordId> halfPatched = new List<CordId>();
        private readonly List<CordId> returning = new List<CordId>();
        private readonly List<HeldCircuit> held = new List<HeldCircuit>();
        private readonly List<HeldCircuit> closed = new List<HeldCircuit>();
        private readonly Dictionary<CordId, JackId> remainingJacks = new Dictionary<CordId, JackId>();

        public OperatorDesk(CallService calls, ConnectionRule rule, ISwitchboard switchboard)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.rule = rule ?? throw new ArgumentNullException(nameof(rule));
            this.switchboard = switchboard ?? throw new ArgumentNullException(nameof(switchboard));
            calls.CallEnded += OnCallEnded;
        }

        /// <summary>Cords with exactly one plug in, as reported by the board.</summary>
        public IReadOnlyList<CordId> HalfPatched => halfPatched;

        // Accepting checks existing correct circuits only: a prewired wrong pair is not a new mistake.
        public bool TryTake(CallId id)
        {
            if (!calls.Take(id)) return false;
            Call call = calls.Find(id);
            foreach (HeldCircuit circuit in closed)
                if (CallOn(circuit.Cord) == null && rule.IsCorrect(call.Request, circuit.A, circuit.B))
                {
                    call.Cord = circuit.Cord;
                    calls.Connect(call.Id);
                    break;
                }
            SyncWaitingCalls();
            return true;
        }

        public Call CallOn(CordId cord)
        {
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
                if (live[i].Cord == cord) return live[i];
            return null;
        }

        public void OnPlugInserted(CordId cord, JackId jack)
        {
            RemoveClosed(cord);
            remainingJacks[cord] = jack;
            SetHalfPatched(cord, CallOn(cord) == null);
            SyncWaitingCalls();
        }

        // The same pair closing again is the team's restore and resumes an on-hold call without a second fee.
        // Any other pair cuts the on-hold call off, and the cord's new circuit is judged as a fresh one (D07).
        public void OnCircuitClosed(CordId cord, JackId a, JackId b, JackId? manualAnchor = null)
        {
            // The anchor is the opposite plug's socket immediately BEFORE a manual insertion.
            // Observations and automatic restoration may connect correctly but never cause Wrong number.
            int closedIndex = ClosedIndex(cord);
            if (!manualAnchor.HasValue && closedIndex >= 0 && closed[closedIndex].Joins(a, b)) return;
            RemoveClosed(cord);
            closed.Add(new HeldCircuit(cord, a, b));
            remainingJacks.Remove(cord);
            SetHalfPatched(cord, false);
            Call onHold = CallOn(cord);
            if (onHold != null && onHold.State == CallState.Connected) return;
            if (onHold != null && onHold.State == CallState.OnHold)
            {
                int index = HeldIndex(cord);
                bool samePair = index >= 0 && held[index].Joins(a, b);
                if (index >= 0) held.RemoveAt(index);
                if (samePair)
                {
                    calls.Resume(onHold.Id);
                    SyncWaitingCalls();
                    return;
                }
                // The cord now carries the new circuit, so ending the old call must not send it back to the tray.
                onHold.Cord = null;
                calls.End(onHold.Id, CallEndReason.DisconnectedEarly);
            }
            Judge(cord, a, b, manualAnchor);
            SyncWaitingCalls();
        }

        // A pulled plug puts the cord's call on hold (D07). The cord still belongs to that call, so its one
        // remaining plug doesn't count as a free half-cord for other accepted orders.
        public void OnCircuitOpened(CordId cord, JackId stillIn, JackId pulled)
        {
            RemoveClosed(cord);
            remainingJacks[cord] = stillIn;
            Call call = CallOn(cord);
            if (call != null)
            {
                if (calls.Hold(call.Id)) held.Add(new HeldCircuit(cord, stillIn, pulled));
                return;
            }
            SetHalfPatched(cord, true);
            SyncWaitingCalls();
        }

        public void OnCordReturned(CordId cord)
        {
            RemoveClosed(cord);
            remainingJacks.Remove(cord);
            SetHalfPatched(cord, false);
            Call call = CallOn(cord);
            if (call != null) calls.End(call.Id, CallEndReason.DisconnectedEarly);
            SyncWaitingCalls();
        }

        /// <summary>Sends every half-patched cord back to the tray (the dev panel's "clear calls").</summary>
        public void ReturnHalfPatchedCords()
        {
            returning.Clear();
            returning.AddRange(halfPatched);
            halfPatched.Clear();
            foreach (CordId cord in returning) switchboard.ReturnToTray(cord);
            foreach (CordId cord in returning) remainingJacks.Remove(cord);
            returning.Clear();
            SyncWaitingCalls();
        }

        /// <summary>Explicit developer reset only; ordinary call outcomes never move plugs.</summary>
        public void ReturnAllCords()
        {
            returning.Clear();
            foreach (HeldCircuit circuit in closed) returning.Add(circuit.Cord);
            foreach (CordId cord in remainingJacks.Keys)
                if (!returning.Contains(cord)) returning.Add(cord);
            closed.Clear(); remainingJacks.Clear(); halfPatched.Clear(); held.Clear();
            foreach (CordId cord in returning) switchboard.ReturnToTray(cord);
            returning.Clear();
            SyncWaitingCalls();
        }

        private static bool Waiting(Call call) => call.State == CallState.Taken || call.State == CallState.Connecting;

        private void Judge(CordId cord, JackId a, JackId b, JackId? manualAnchor)
        {
            // A correct active pair wins, regardless of which card the player last opened.
            foreach (Call call in calls.Live)
            {
                if (!Waiting(call) || !rule.IsCorrect(call.Request, a, b)) continue;
                call.Cord = cord;
                calls.Connect(call.Id);
                return;
            }
            // Only the end that was already in place can identify a mistaken attempt.
            // Newly placing a correct end against an unrelated old end merely establishes an anchor.
            if (!(manualAnchor is JackId anchor) || (anchor != a && anchor != b)) return;
            Call wrong = WaitingAt(anchor, true) ?? WaitingAt(anchor, false);
            if (wrong != null) calls.End(wrong.Id, CallEndReason.WrongConnection);
            // Unrelated circuits are free wiring. Even a wrong-number circuit stays physically plugged in.
        }

        private Call WaitingAt(JackId jack, bool callerOnly)
        {
            foreach (Call call in calls.Live)
                if (Waiting(call) &&
                    (callerOnly ? rule.IsCaller(call.Request, jack) : rule.Involves(call.Request, jack))) return call;
            return null;
        }

        // Connecting is presentation state for each accepted order with a related free half-cord.
        private void SyncWaitingCalls()
        {
            foreach (Call call in calls.Live)
            {
                if (!Waiting(call)) continue;
                bool connecting = false;
                foreach (CordId cord in halfPatched)
                    if (remainingJacks.TryGetValue(cord, out JackId jack) && rule.Involves(call.Request, jack)) { connecting = true; break; }
                if (connecting && call.State == CallState.Taken) calls.BeginConnecting(call.Id);
                else if (!connecting && call.State == CallState.Connecting) calls.CancelConnecting(call.Id);
            }
        }

        private void SetHalfPatched(CordId cord, bool value)
        {
            int index = halfPatched.IndexOf(cord);
            if (value && index < 0) halfPatched.Add(cord);
            else if (!value && index >= 0) halfPatched.RemoveAt(index);
        }

        private int HeldIndex(CordId cord)
        {
            for (int i = 0; i < held.Count; i++)
                if (held[i].Cord == cord) return i;
            return -1;
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            if (!(e.Call.Cord is CordId cord)) return;
            int index = HeldIndex(cord);
            if (index >= 0) held.RemoveAt(index);
            if (remainingJacks.ContainsKey(cord)) SetHalfPatched(cord, true);
            SyncWaitingCalls();
        }

        private int ClosedIndex(CordId cord)
        {
            for (int i = 0; i < closed.Count; i++) if (closed[i].Cord == cord) return i;
            return -1;
        }

        private void RemoveClosed(CordId cord)
        {
            int index = ClosedIndex(cord);
            if (index >= 0) closed.RemoveAt(index);
        }

        private readonly struct HeldCircuit
        {
            public HeldCircuit(CordId cord, JackId a, JackId b)
            {
                Cord = cord;
                A = a;
                B = b;
            }

            public CordId Cord { get; }
            public JackId A { get; }
            public JackId B { get; }

            public bool Joins(JackId a, JackId b) => (A == a && B == b) || (A == b && B == a);
        }
    }
}
