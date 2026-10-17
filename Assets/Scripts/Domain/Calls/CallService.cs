using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>Owns the live calls, the call in hand and the call timers. It knows nothing about cords, money or UI.</summary>
    public sealed class CallService
    {
        private readonly List<Call> live = new List<Call>();
        private readonly List<Call> expired = new List<Call>();
        private int lastId;

        public event Action<CallAddedArgs> CallAdded;
        public event Action<CallStateChangedArgs> CallStateChanged;
        public event Action<CallEndedArgs> CallEnded;

        public IReadOnlyList<Call> Live => live;
        public Call InHand { get; private set; }

        public Call Find(CallId id)
        {
            foreach (Call call in live)
                if (call.Id == id) return call;
            return null;
        }

        public Call Add(CallRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var call = new Call(new CallId(++lastId), request);
            live.Add(call);
            CallAdded?.Invoke(new CallAddedArgs(call));
            return call;
        }

        // One call in hand at a time; swapping cards comes with the queue in Step 2 (D03).
        public bool Take(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Incoming || InHand != null) return false;
            InHand = call;
            ChangeState(call, CallState.Taken);
            return true;
        }

        public bool Connect(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Taken) return false;
            InHand = null;
            ChangeState(call, CallState.Connected);
            return true;
        }

        public bool End(CallId id, CallEndReason reason)
        {
            Call call = Find(id);
            if (call == null || reason == CallEndReason.None) return false;
            End(call, reason);
            return true;
        }

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            foreach (Call call in live)
            {
                if (call.State != CallState.Connected) continue;
                call.SecondsLeft = Math.Max(0, call.SecondsLeft - dt);
                if (call.SecondsLeft <= 0) expired.Add(call);
            }
            // Ending raises events, so calls end only after the walk over the live list.
            foreach (Call call in expired) End(call, CallEndReason.Finished);
            expired.Clear();
        }

        private void End(Call call, CallEndReason reason)
        {
            if (call.State == CallState.Ended) return;
            live.Remove(call);
            if (InHand == call) InHand = null;
            call.EndReason = reason;
            ChangeState(call, CallState.Ended);
            CallEnded?.Invoke(new CallEndedArgs(call, reason));
        }

        private void ChangeState(Call call, CallState to)
        {
            CallState from = call.State;
            call.State = to;
            CallStateChanged?.Invoke(new CallStateChangedArgs(call, from, to));
        }
    }
}
