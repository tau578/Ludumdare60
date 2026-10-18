using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// The call queue (D52). Calls play in pool order and loop; every free caller slot takes the next call at once.
    /// The pool follows StatSheet (the last bought extra_clients's stagePool, or the startPool), and a new pool
    /// starts at its first call. A call still on a card is skipped, so no call shows twice.
    /// </summary>
    public sealed class CallQueue
    {
        private readonly CallService calls;
        private readonly StatSheet stats;
        private IReadOnlyList<CallScript> pool;

        public CallQueue(CallService calls, StatSheet stats)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            pool = stats.CallPool;
            stats.Changed += OnStatsChanged;
        }

        public IReadOnlyList<CallScript> Pool => pool;

        /// <summary>The index in the pool that the queue tries next.</summary>
        public int Position { get; private set; }

        /// <summary>The pool's first call comes next (a new pool, or the dev reset).</summary>
        public void Restart()
        {
            pool = stats.CallPool;
            Position = 0;
        }

        /// <summary>Fills every free open slot, in order, at once.</summary>
        public void Tick()
        {
            while (calls.Live.Count < stats.ClientSlots)
            {
                CallScript next = TakeNext();
                if (next == null) return;
                calls.Add(new CallRequest(next));
            }
        }

        private CallScript TakeNext()
        {
            int count = pool?.Count ?? 0;
            for (int step = 0; step < count; step++)
            {
                int index = (Position + step) % count;
                CallScript script = pool[index];
                if (script == null || calls.IsLive(script)) continue;
                Position = (index + 1) % count;
                return script;
            }
            return null;
        }

        private void OnStatsChanged()
        {
            if (!ReferenceEquals(stats.CallPool, pool)) Restart();
        }
    }
}
