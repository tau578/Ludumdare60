using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// Callers who were put through wrongly ring again after their type's delay (Design 5.3); after a miss or an
    /// early disconnect only if their type says so (D10). It keeps the original request; the callback is a new
    /// call with attempt + 1.
    /// </summary>
    public sealed class CallbackScheduler
    {
        private readonly CallService calls;
        private readonly IGameClock clock;
        private readonly IRandom random;
        private readonly Func<int> openSlots;
        private readonly IdleGuard idle;
        private readonly List<PendingCallback> pending = new List<PendingCallback>();

        /// <param name="openSlots">The open caller slots, read live: live calls never outnumber them (D17, D49).</param>
        public CallbackScheduler(CallService calls, IGameClock clock, IRandom random, Func<int> openSlots, IdleGuard idle = null)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.openSlots = openSlots ?? (() => 0);
            this.idle = idle;
            calls.CallEnded += OnCallEnded;
        }

        /// <summary>Waiting callbacks, soonest first.</summary>
        public IReadOnlyList<PendingCallback> Pending => pending;

        public bool IsPending(IPerson person)
        {
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Request.Involves(person)) return true;
            return false;
        }

        public int CountPending(ICallerType type)
        {
            int count = 0;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Request.Type == type) count++;
            return count;
        }

        // Due callbacks ring in due order when there's room and neither party is on another call. They are always
        // for people already in the game, and the board never shrinks in play, so they stay on open jacks.
        // A callback also waits while its type is at its limit of live calls or held back by the idle guard.
        // They tick before the spawner, so they go ahead of new calls.
        public void Tick()
        {
            int i = 0;
            int slots = openSlots();
            while (i < pending.Count && calls.Live.Count < slots)
            {
                PendingCallback callback = pending[i];
                if (callback.DueAt > clock.Now) break;
                CallRequest request = callback.Request;
                if (calls.IsInLiveCall(request.CallerPerson) || calls.IsInLiveCall(request.Recipient) || IsHeldBack(request.Type))
                {
                    i++;
                    continue;
                }
                pending.RemoveAt(i);
                calls.Add(request.Again());
            }
        }

        public void Clear() => pending.Clear();

        private bool IsHeldBack(ICallerType type) =>
            (idle != null && idle.Blocks(type)) ||
            (type != null && type.MaxSimultaneous > 0 && calls.CountLive(type) >= type.MaxSimultaneous);

        private void OnCallEnded(CallEndedArgs e)
        {
            CallRequest request = e.Call.Request;
            if (!request.Type.CallsBackAfter(e.Reason)) return;
            FloatRange delay = request.Type.CallbackDelaySeconds;
            Schedule(new PendingCallback(request, clock.Now + random.Between(delay.min, delay.max)));
        }

        private void Schedule(PendingCallback callback)
        {
            int index = pending.Count;
            while (index > 0 && pending[index - 1].DueAt > callback.DueAt) index--;
            pending.Insert(index, callback);
        }
    }

    public readonly struct PendingCallback
    {
        public PendingCallback(CallRequest request, double dueAt)
        {
            Request = request;
            DueAt = dueAt;
        }

        /// <summary>The request of the call that went wrong; the callback rings with <see cref="CallRequest.Again"/>.</summary>
        public CallRequest Request { get; }
        public double DueAt { get; }
    }
}
