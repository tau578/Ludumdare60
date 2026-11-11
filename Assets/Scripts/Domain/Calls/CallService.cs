using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>Owns live calls and timers. Accepted calls remain active independently of UI selection.</summary>
    public sealed class CallService
    {
        private readonly List<Call> live = new List<Call>();
        private readonly List<Call> expired = new List<Call>();
        private readonly List<Call> missed = new List<Call>();
        private readonly List<Call> hungUp = new List<Call>();
        private readonly double defaultPatienceSeconds;
        private int lastId;

        /// <param name="defaultPatienceSeconds">Economy.json's defaultPatienceSeconds (D63); 0 lets callers wait forever.</param>
        public CallService(double defaultPatienceSeconds = 0) =>
            this.defaultPatienceSeconds = defaultPatienceSeconds > 0 ? defaultPatienceSeconds : 0;

        public event Action<CallAddedArgs> CallAdded;
        public event Action<CallStateChangedArgs> CallStateChanged;
        public event Action<CallEndedArgs> CallEnded;

        /// <summary>Every call that hasn't ended, in arrival order.</summary>
        public IReadOnlyList<Call> Live => live;

        /// <summary>Most recently accepted waiting call, for compatibility; never used to judge circuits.</summary>
        public Call InHand { get; private set; }

        public Call Find(CallId id)
        {
            foreach (Call call in live)
                if (call.Id == id) return call;
            return null;
        }

        /// <summary>Whether a call that plays this script is still on a card (D52: no call shows twice).</summary>
        public bool IsLive(CallScript script)
        {
            if (script == null) return false;
            for (int i = 0; i < live.Count; i++)
                if (string.Equals(live[i].Request.Script.Id, script.Id, StringComparison.Ordinal)) return true;
            return false;
        }

        public Call Add(CallRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var call = new Call(new CallId(++lastId), request, defaultPatienceSeconds);
            live.Add(call);
            call.LineBusy = IsLineBusy(call);
            CallAdded?.Invoke(new CallAddedArgs(call));
            return call;
        }

        // Accept once. Opening another card's text does not change either call's lifecycle.
        public bool Take(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Incoming) return false;
            call.State = CallState.Taken;
            InHand = call;
            RaiseStateChanged(call, CallState.Incoming, CallState.Taken);
            return true;
        }

        public bool BeginConnecting(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Taken) return false;
            ChangeState(call, CallState.Connecting);
            return true;
        }

        public bool CancelConnecting(CallId id)
        {
            Call call = Find(id);
            if (call == null || call.State != CallState.Connecting) return false;
            ChangeState(call, CallState.Taken);
            return true;
        }

        // Plug order doesn't matter (D05), so a circuit may close from Taken as well as from Connecting.
        public bool Connect(CallId id)
        {
            Call call = Find(id);
            if (call == null || (call.State != CallState.Taken && call.State != CallState.Connecting)) return false;
            if (InHand == call) InHand = null;
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

        /// <summary>Whether a live call uses this jack as its caller's or its recipient's (D63: nobody calls while on a call).</summary>
        public bool IsJackInLiveCall(JackId jack)
        {
            for (int i = 0; i < live.Count; i++)
                if (live[i].Request.Involves(jack)) return true;
            return false;
        }

        /// <summary>Whether a call still waiting for its connection (incoming, taken, connecting) uses this jack (D83).</summary>
        public bool IsJackInWaitingCall(JackId jack)
        {
            for (int i = 0; i < live.Count; i++)
                if (IsAwaitingConnection(live[i].State) && live[i].Request.Involves(jack)) return true;
            return false;
        }

        /// <summary>
        /// Whether the call's recipient's jack carries another live call, connected or on hold (D63). A jack held only by
        /// a stale plug (D45) is no busy line: the player pulls the plug.
        /// </summary>
        public bool IsLineBusy(Call call)
        {
            if (call == null) return false;
            JackId to = call.Request.To;
            for (int i = 0; i < live.Count; i++)
            {
                Call other = live[i];
                if (other == call || (other.State != CallState.Connected && other.State != CallState.OnHold)) continue;
                if (other.Request.Involves(to)) return true;
            }
            return false;
        }

        /// <summary>Holds or releases a waiting call's response timer and patience; it never ends a call by itself.</summary>
        public bool HoldWaiting(CallId id, bool held)
        {
            Call call = Find(id);
            if (call == null) return false;
            call.WaitingHeld = held;
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
                    call.LineBusy = false;
                    call.SecondsLeft = Math.Max(0, call.SecondsLeft - dt);
                    if (call.SecondsLeft <= 0) expired.Add(call);
                }
                else if (IsAwaitingConnection(call.State))
                {
                    if (call.State != CallState.Incoming) call.SecondsInHand += dt;
                    call.LineBusy = IsLineBusy(call);
                    if (call.WaitingHeld) continue;
                    if (call.HasResponseTime)
                    {
                        call.ResponseSecondsLeft = Math.Max(0, call.ResponseSecondsLeft - dt);
                        if (call.ResponseSecondsLeft <= 0) missed.Add(call);
                    }
                    else if (call.HasPatience)
                    {
                        call.PatienceSecondsLeft = Math.Max(0, call.PatienceSecondsLeft - dt);
                        if (call.PatienceSecondsLeft > 0) continue;
                        call.HungUpOnBusyLine = call.LineBusy;
                        hungUp.Add(call);
                    }
                }
                else call.LineBusy = false;
            }
            // Ending raises events, so calls end only after the walk over the live list.
            foreach (Call call in expired) End(call, CallEndReason.Finished);
            foreach (Call call in missed) End(call, CallEndReason.Missed);
            foreach (Call call in hungUp) End(call, CallEndReason.HungUp);
            expired.Clear();
            missed.Clear();
            hungUp.Clear();
        }

        // The response timer and the patience run until a correct connection (D09, D63) and never restart after hold.
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
