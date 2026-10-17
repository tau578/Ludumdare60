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

        public OperatorDesk(CallService calls, ConnectionRule rule, ISwitchboard switchboard)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.rule = rule ?? throw new ArgumentNullException(nameof(rule));
            this.switchboard = switchboard ?? throw new ArgumentNullException(nameof(switchboard));
            calls.CallEnded += OnCallEnded;
        }

        public bool TryTake(CallId id) => calls.Take(id);

        public Call CallOn(CordId cord)
        {
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
                if (live[i].Cord == cord) return live[i];
            return null;
        }

        // Half-patched cords only matter once calls can be Connecting (Step 2).
        public void OnPlugInserted(CordId cord, JackId jack) { }

        public void OnCircuitClosed(CordId cord, JackId a, JackId b)
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

        // Until hold arrives in Step 2 (D07), any pulled plug cuts the call off.
        public void OnCircuitOpened(CordId cord, JackId stillIn, JackId pulled) => CutOff(cord);

        public void OnCordReturned(CordId cord) => CutOff(cord);

        private void CutOff(CordId cord)
        {
            Call call = CallOn(cord);
            if (call != null) calls.End(call.Id, CallEndReason.DisconnectedEarly);
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            if (e.Call.Cord is CordId cord) switchboard.ReturnToTray(cord);
        }
    }
}
