using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// Rings new calls on a jittered interval that stretches while waiting calls outnumber free cords (Design 5.2).
    /// An attempt with no room, or with no free caller and recipient, is skipped; the next one comes an interval later.
    /// </summary>
    public sealed class CallSpawner
    {
        private const double MinimumIntervalSeconds = 0.1;

        private readonly CallService calls;
        private readonly CallFactory factory;
        private readonly PhoneDirectory directory;
        private readonly IReadOnlyList<ICaller> callers;
        private readonly IRandom random;
        private readonly BusyTracker busy;
        private readonly IPacingProfile pacing;
        private readonly int maxLiveCalls;
        private readonly Func<int> cordCount;
        private double secondsUntilSpawn;

        public CallSpawner(CallService calls, CallFactory factory, PhoneDirectory directory, IReadOnlyList<ICaller> callers,
            IRandom random, BusyTracker busy, IPacingProfile pacing, int maxLiveCalls, Func<int> cordCount)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.callers = callers ?? Array.Empty<ICaller>();
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.busy = busy ?? throw new ArgumentNullException(nameof(busy));
            this.pacing = pacing ?? PacingSettings.Default;
            this.maxLiveCalls = maxLiveCalls;
            this.cordCount = cordCount ?? (() => 0);
            secondsUntilSpawn = this.pacing.FirstCallDelaySeconds;
        }

        public double SecondsUntilSpawn => secondsUntilSpawn;

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            // Dividing time by the load stretches the interval live, so it speeds back up as the player catches up.
            secondsUntilSpawn -= dt / Load();
            if (secondsUntilSpawn > 0) return;
            TrySpawn();
            secondsUntilSpawn = NextInterval();
        }

        /// <summary>Rings one call now if there's room and a free caller and recipient exist.</summary>
        public bool TrySpawn()
        {
            if (calls.Live.Count >= maxLiveCalls) return false;
            ICaller caller = PickCaller();
            IPerson recipient = caller != null ? PickRecipient(caller.Person) : null;
            if (recipient == null) return false;
            calls.Add(factory.Create(caller, recipient));
            return true;
        }

        /// <summary>1 while every waiting call has a free cord; each waiting call beyond that adds the load factor.</summary>
        public double Load()
        {
            int waiting = 0, usingCord = 0;
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
            {
                CallState state = live[i].State;
                // An on-hold call still has its cord in the board, so it uses a cord and isn't waiting.
                if (state == CallState.Connected || state == CallState.OnHold) usingCord++;
                else waiting++;
            }
            int freeCords = Math.Max(0, cordCount() - usingCord);
            return 1 + Math.Max(0, pacing.LoadFactor) * Math.Max(0, waiting - freeCords);
        }

        public double NextInterval()
        {
            double jitter = Math.Min(Math.Max(pacing.IntervalJitter, 0), 0.95);
            double interval = pacing.BaseIntervalSeconds * random.Between(1 - jitter, 1 + jitter);
            return Math.Max(MinimumIntervalSeconds, interval);
        }

        private ICaller PickCaller()
        {
            double totalWeight = 0;
            for (int i = 0; i < callers.Count; i++)
                if (CanCall(callers[i])) totalWeight += callers[i].Weight;
            if (totalWeight <= 0) return null;

            double roll = random.NextDouble() * totalWeight;
            ICaller picked = null;
            for (int i = 0; i < callers.Count; i++)
            {
                if (!CanCall(callers[i])) continue;
                picked = callers[i];
                roll -= picked.Weight;
                if (roll < 0) break;
            }
            return picked;
        }

        // A caller who isn't in the directory could never be connected.
        private bool CanCall(ICaller caller) =>
            caller != null && caller.Weight > 0 && caller.Type != null && directory.Lists(caller.Person) &&
            !busy.IsBusy(caller.Person);

        private bool CanReceive(IPerson person, IPerson caller) => !person.IsSamePerson(caller) && !busy.IsBusy(person);

        private IPerson PickRecipient(IPerson caller)
        {
            IReadOnlyList<IDirectoryEntry> entries = directory.ListedEntries;
            int candidates = 0;
            for (int i = 0; i < entries.Count; i++)
                if (CanReceive(entries[i].Person, caller)) candidates++;
            if (candidates == 0) return null;

            int pick = random.Next(0, candidates);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!CanReceive(entries[i].Person, caller)) continue;
                if (pick-- == 0) return entries[i].Person;
            }
            return null;
        }
    }
}
