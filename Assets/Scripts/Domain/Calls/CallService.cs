using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>Owns the live calls, the call in hand and the call timers. It knows nothing about cords, money or UI.</summary>
    public sealed class CallService
    {
        private readonly List<Call> live = new List<Call>();
        private readonly List<Call> expired = new List<Call>();
        private readonly List<Call> missed = new List<Call>();
        private int lastId;

        public event Action<CallAddedArgs> CallAdded;
        public event Action<CallStateChangedArgs> CallStateChanged;
        public event Action<CallEndedArgs> CallEnded;

        /// <summary>Every call that hasn't ended, in arrival order.</summary>
        public IReadOnlyList<Call> Live => live;

        /// <summary>The one Taken or Connecting call, if any (D03).</summary>
        public Call InHand { get; private set; }

        public Call Find(CallId id)
        {
            foreach (Call call in live)
                if (call.Id == id) return call;
            return null;
        }

        public int CountLive(ICallerType type)
        {
            int count = 0;
            for (int i = 0; i < live.Count; i++)
                if (live[i].Request.Type == type) count++;
            return count;
        }

        public bool IsInLiveCall(IPerson person)
        {
            for (int i = 0; i < live.Count; i++)
                if (live[i].Request.Involves(person)) return true;
            return false;
        }

        public Call Add(CallRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var call = new Call(new CallId(++lastId), request);
            live.Add(call);
            CallAdded?.Invoke(new CallAddedArgs(call));
            return call;
        }

        // Taking a card while another is in hand swaps them; the old one rings again with its timers running (D03).
        public bool Take(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Incoming) return false;
            Call previous = InHand;
            CallState previousFrom = previous?.State ?? CallState.Incoming;
            if (previous != null) previous.State = CallState.Incoming;
            call.State = CallState.Taken;
            InHand = call;
            if (previous != null) RaiseStateChanged(previous, previousFrom, CallState.Incoming);
            RaiseStateChanged(call, CallState.Incoming, CallState.Taken);
            return true;
        }

        public bool BeginConnecting(CallId id)
        {
            Call call = Find(id);
            if (call == null || call != InHand || call.State != CallState.Taken) return false;
            ChangeState(call, CallState.Connecting);
            return true;
        }

        public bool CancelConnecting(CallId id)
        {
            Call call = Find(id);
            if (call == null || call != InHand || call.State != CallState.Connecting) return false;
            ChangeState(call, CallState.Taken);
            return true;
        }

        // Plug order doesn't matter (D05), so a circuit may close from Taken as well as from Connecting.
        public bool Connect(CallId id)
        {
            Call call = Find(id);
            if (call == null || (call.State != CallState.Taken && call.State != CallState.Connecting)) return false;
            InHand = null;
            ChangeState(call, CallState.Connected);
            return true;
        }

        // A pulled plug pauses pay and the duration timer (D07); the fee is never paid again on resume.
        public bool Hold(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Connected) return false;
            ChangeState(call, CallState.OnHold);
            return true;
        }

        public bool Resume(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.OnHold) return false;
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
                if (call.State == CallState.Connected)
                {
                    call.SecondsLeft = Math.Max(0, call.SecondsLeft - dt);
                    if (call.SecondsLeft <= 0) expired.Add(call);
                }
                else if (call.HasResponseTime && IsAwaitingConnection(call.State))
                {
                    call.ResponseSecondsLeft = Math.Max(0, call.ResponseSecondsLeft - dt);
                    if (call.ResponseSecondsLeft <= 0) missed.Add(call);
                }
            }
            // Ending raises events, so calls end only after the walk over the live list.
            foreach (Call call in expired) End(call, CallEndReason.Finished);
            foreach (Call call in missed) End(call, CallEndReason.Missed);
            expired.Clear();
            missed.Clear();
        }

        // The response timer runs until a correct connection (D09) and never restarts after hold.
        private static bool IsAwaitingConnection(CallState state) =>
            state == CallState.Incoming || state == CallState.Taken || state == CallState.Connecting;

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
            RaiseStateChanged(call, from, to);
        }

        private void RaiseStateChanged(Call call, CallState from, CallState to) =>
            CallStateChanged?.Invoke(new CallStateChangedArgs(call, from, to));
    }
}
