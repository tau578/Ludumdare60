using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// The call queue (D52). Calls play in pool order and loop; every free caller window takes the next call at once.
    /// Only waiting calls (Incoming, Taken, Connecting) hold a window: a talking call shrinks to a tab and frees its
    /// window (D83), so the queue fills while the waiting calls are fewer than ClientSlots.
    /// The pool follows StatSheet (the last bought extra_clients's stagePool, or the startPool), and a new pool
    /// starts at its first call. Live calls and other requests from the same caller are skipped, and so is anyone
    /// already on a call, as caller or recipient (D63). A call to a busy recipient may be dealt, but only
    /// maxBusyLineCards of them at a time, and only to someone who is talking (connected or on hold, D83).
    /// </summary>
    public sealed class CallQueue
    {
        private readonly CallService calls;
        private readonly StatSheet stats;
        private readonly int maxBusyLineCards;

        // Live calls dealt from the current pool, with their pool index, so a save can replay them (D55).
        private readonly List<DealtCall> dealt = new List<DealtCall>();
        private IReadOnlyList<CallScript> pool;
        private bool waitingForFirstCall;
        private Call tutorialCall;
        private Call heldCall;

        /// <param name="maxBusyLineCards">Economy.json's maxBusyLineCards (D63): calls to a busy recipient allowed at once.</param>
        public CallQueue(CallService calls, StatSheet stats, bool tutorial = false, int maxBusyLineCards = 0)
        {
            this.maxBusyLineCards = Math.Max(0, maxBusyLineCards);
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            pool = stats.CallPool;
            waitingForFirstCall = tutorial;
            stats.Changed += OnStatsChanged;
            calls.CallEnded += OnCallEnded;
        }

        public IReadOnlyList<CallScript> Pool => pool;

        /// <summary>The index in the pool that the queue tries next.</summary>
        public int Position { get; private set; }

        /// <summary>
        /// Where a reloaded game starts (D55): the pool index of the oldest live call from the current pool, so
        /// calls still on cards ring again; with none, Position. Calls left over from an older pool don't count.
        /// </summary>
        public int ResumePosition
        {
            get
            {
                IReadOnlyList<Call> live = calls.Live;
                for (int i = 0; i < live.Count; i++)
                    for (int j = 0; j < dealt.Count; j++)
                        if (dealt[j].Id == live[i].Id) return dealt[j].Index;
                return Position;
            }
        }

        /// <summary>The tutorial is over: the guided call's response timer or patience runs again, if it is still waiting.</summary>
        public void ReleaseTutorialCall()
        {
            if (heldCall != null) calls.HoldWaiting(heldCall.Id, false);
            heldCall = null;
        }

        /// <summary>The pool's first call comes next (a new pool, or the dev reset).</summary>
        public void Restart()
        {
            pool = stats.CallPool;
            Position = 0;
            dealt.Clear();
        }

        /// <summary>Starts a loaded game at a saved position; one outside the pool (the file got shorter) starts at 0.</summary>
        public void Restore(int position)
        {
            int count = pool?.Count ?? 0;
            Position = position >= 0 && position < count ? position : 0;
        }

        /// <summary>Fills every free open slot, in order, at once.</summary>
        public void Tick()
        {
            if (waitingForFirstCall)
            {
                if (tutorialCall == null)
                {
                    int index = TakeNext();
                    if (index < 0) return;
                    tutorialCall = calls.Add(new CallRequest(pool[index]));
                    dealt.Add(new DealtCall(tutorialCall.Id, index));
                    // The guided call waits for the player while the tutorial runs (D57).
                    calls.HoldWaiting(tutorialCall.Id, true);
                    heldCall = tutorialCall;
                }
                if (!tutorialCall.ConnectionFeePaid && tutorialCall.State != CallState.Ended) return;
                waitingForFirstCall = false;
                tutorialCall = null;
            }
            while (WaitingCalls() < stats.ClientSlots)
            {
                int index = TakeNext();
                if (index < 0) return;
                Call call = calls.Add(new CallRequest(pool[index]));
                dealt.Add(new DealtCall(call.Id, index));
            }
        }

        /// <summary>Live calls that still wait for their connection and so hold a caller window (D83).</summary>
        public int WaitingCalls()
        {
            IReadOnlyList<Call> live = calls.Live;
            int count = 0;
            for (int i = 0; i < live.Count; i++)
                if (live[i].IsWaiting) count++;
            return count;
        }

        private int TakeNext()
        {
            int count = pool?.Count ?? 0;
            for (int step = 0; step < count; step++)
            {
                int index = (Position + step) % count;
                CallScript script = pool[index];
                if (script == null || calls.IsLive(script) || HasLiveCaller(script.Name)) continue;
                if (!stats.Board.Contains(script.From) || !stats.Board.Contains(script.To)) continue;
                // Nobody calls while on a call (D63). A busy recipient is allowed, but rarely, and only someone who is
                // talking: never someone whose own card still waits (D83).
                if (calls.IsJackInLiveCall(script.From)) continue;
                if (calls.IsJackInWaitingCall(script.To)) continue;
                if (calls.IsJackInLiveCall(script.To) && BusyLineCards() >= maxBusyLineCards) continue;
                Position = (index + 1) % count;
                return index;
            }
            return -1;
        }

        private void OnStatsChanged()
        {
            if (!ReferenceEquals(stats.CallPool, pool)) Restart();
        }

        /// <summary>
        /// Live calls dealt to a busy recipient: their recipient's jack is used by an earlier live call. Counted from the
        /// live list, so a card stops counting once the earlier call ends.
        /// </summary>
        public int BusyLineCards()
        {
            IReadOnlyList<Call> live = calls.Live;
            int count = 0;
            for (int i = 1; i < live.Count; i++)
            {
                JackId to = live[i].Request.To;
                for (int j = 0; j < i; j++)
                    if (live[j].Request.Involves(to))
                    {
                        count++;
                        break;
                    }
            }
            return count;
        }

        private bool HasLiveCaller(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (Call call in calls.Live)
                if (string.Equals(call.Request.Script.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            for (int i = 0; i < dealt.Count; i++)
                if (dealt[i].Id == e.Call.Id)
                {
                    dealt.RemoveAt(i);
                    return;
                }
        }

        private readonly struct DealtCall
        {
            public DealtCall(CallId id, int index)
            {
                Id = id;
                Index = index;
            }

            public CallId Id { get; }
            public int Index { get; }
        }
    }
}
