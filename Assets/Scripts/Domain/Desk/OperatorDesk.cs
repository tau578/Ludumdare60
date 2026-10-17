using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>The one place that coordinates calls with cords: it judges circuits, binds cords to calls and returns them.</summary>
    public sealed class OperatorDesk
    {
        private readonly CallService calls;
        private readonly ConnectionRule rule;
        private readonly ISwitchboard switchboard;
        private readonly List<CordId> halfPatched = new List<CordId>();
        private readonly List<CordId> returning = new List<CordId>();

        public OperatorDesk(CallService calls, ConnectionRule rule, ISwitchboard switchboard)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.rule = rule ?? throw new ArgumentNullException(nameof(rule));
            this.switchboard = switchboard ?? throw new ArgumentNullException(nameof(switchboard));
            calls.CallEnded += OnCallEnded;
        }

        /// <summary>Cords with exactly one plug in, as reported by the board.</summary>
        public IReadOnlyList<CordId> HalfPatched => halfPatched;

        // A call taken while a cord is already half-patched goes straight to Connecting (Design 5.1).
        public bool TryTake(CallId id)
        {
            if (!calls.Take(id)) return false;
            SyncCallInHand();
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
            SetHalfPatched(cord, true);
            SyncCallInHand();
        }

        public void OnCircuitClosed(CordId cord, JackId a, JackId b)
        {
            SetHalfPatched(cord, false);
            Judge(cord, a, b);
            SyncCallInHand();
        }

        // Until hold arrives in Step 2b (D07), a pulled plug cuts the call off and its cord goes back to the tray.
        public void OnCircuitOpened(CordId cord, JackId stillIn, JackId pulled)
        {
            Call call = CallOn(cord);
            if (call != null)
            {
                calls.End(call.Id, CallEndReason.DisconnectedEarly);
                return;
            }
            SetHalfPatched(cord, true);
            SyncCallInHand();
        }

        public void OnCordReturned(CordId cord)
        {
            SetHalfPatched(cord, false);
            Call call = CallOn(cord);
            if (call != null) calls.End(call.Id, CallEndReason.DisconnectedEarly);
            SyncCallInHand();
        }

        /// <summary>Sends every half-patched cord back to the tray (the dev panel's "clear calls").</summary>
        public void ReturnHalfPatchedCords()
        {
            returning.Clear();
            returning.AddRange(halfPatched);
            halfPatched.Clear();
            foreach (CordId cord in returning) switchboard.ReturnToTray(cord);
            returning.Clear();
            SyncCallInHand();
        }

        private void Judge(CordId cord, JackId a, JackId b)
        {
            Call call = calls.InHand;
            if (call == null)
            {
                switchboard.Refuse(cord);
                return;
            }
            if (rule.IsCorrect(call.Request, a, b))
            {
                call.Cord = cord;
                calls.Connect(call.Id);
                return;
            }
            calls.End(call.Id, CallEndReason.WrongConnection);
            switchboard.ReturnToTray(cord);
        }

        // The call in hand is Connecting exactly while some cord is half-patched.
        private void SyncCallInHand()
        {
            Call call = calls.InHand;
            if (call == null) return;
            if (halfPatched.Count > 0 && call.State == CallState.Taken) calls.BeginConnecting(call.Id);
            else if (halfPatched.Count == 0 && call.State == CallState.Connecting) calls.CancelConnecting(call.Id);
        }

        private void SetHalfPatched(CordId cord, bool value)
        {
            int index = halfPatched.IndexOf(cord);
            if (value && index < 0) halfPatched.Add(cord);
            else if (!value && index >= 0) halfPatched.RemoveAt(index);
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            if (e.Call.Cord is CordId cord) switchboard.ReturnToTray(cord);
        }
    }
}
