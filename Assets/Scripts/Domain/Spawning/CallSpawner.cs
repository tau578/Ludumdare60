using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// Rings new calls on a jittered interval that stretches while waiting calls outnumber free cords (Design 5.2).
    /// An attempt with no room, or with no free caller and recipient, is skipped; the next one comes an interval later.
    /// Each call picks its type first, then a caller of that type, then a recipient by the type's recipient rule.
    /// Only people on open jacks call or are called (D42); a type with no open caller or recipient doesn't ring.
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
        private readonly Func<int> openSlots;
        private readonly Func<int> cordCount;
        private readonly CallbackScheduler callbacks;
        private readonly IdleGuard idle;
        private readonly OpenBoard board;
        private readonly List<ICallerType> types = new List<ICallerType>();
        private readonly double[] typeWeights;
        private double secondsUntilSpawn;

        public CallSpawner(CallService calls, CallFactory factory, PhoneDirectory directory, IReadOnlyList<ICaller> callers,
            IRandom random, BusyTracker busy, IPacingProfile pacing, Func<int> openSlots, Func<int> cordCount,
            CallbackScheduler callbacks = null, IdleGuard idle = null, OpenBoard board = null)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.callers = callers ?? Array.Empty<ICaller>();
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.busy = busy ?? throw new ArgumentNullException(nameof(busy));
            this.pacing = pacing ?? PacingSettings.Default;
            this.openSlots = openSlots ?? (() => 0);
            this.cordCount = cordCount ?? (() => 0);
            this.callbacks = callbacks;
            this.idle = idle;
            this.board = board;
            for (int i = 0; i < this.callers.Count; i++)
            {
                ICallerType type = this.callers[i]?.Type;
                if (type != null && !types.Contains(type)) types.Add(type);
            }
            typeWeights = new double[types.Count];
            secondsUntilSpawn = this.pacing.FirstCallDelaySeconds;
        }

        public double SecondsUntilSpawn => secondsUntilSpawn;

        /// <summary>Every type some caller can place, in the order the callers list them.</summary>
        public IReadOnlyList<ICallerType> Types => types;

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
            if (calls.Live.Count >= openSlots()) return false;
            ICallerType type = PickType();
            return type != null && Spawn(type);
        }

        /// <summary>Rings one call of this type now, whatever its weight, if the type's other rules allow it.</summary>
        public bool TrySpawn(ICallerType type)
        {
            if (type == null || calls.Live.Count >= openSlots() || !IsAllowed(type)) return false;
            return Spawn(type);
        }

        /// <summary>Whether anyone on the open board could ever receive this type's calls, busy or not.</summary>
        public bool HasOpenRecipient(ICallerType type)
        {
            if (type == null) return false;
            IReadOnlyList<IDirectoryEntry> entries = directory.ListedEntries;
            for (int i = 0; i < entries.Count; i++)
                if (IsOpen(entries[i]) && type.Accepts(entries[i].Person)) return true;
            return false;
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

        private ICallerType PickType()
        {
            double totalWeight = 0;
            int allowed = 0, last = -1;
            for (int i = 0; i < types.Count; i++)
            {
                ICallerType type = types[i];
                typeWeights[i] = type.SpawnWeight > 0 && IsAllowed(type) ? type.SpawnWeight : 0;
                if (typeWeights[i] <= 0) continue;
                totalWeight += typeWeights[i];
                allowed++;
                last = i;
            }
            if (allowed == 0) return null;
            // A single choice needs no roll, so content with one type keeps the random sequence it always had.
            if (allowed == 1) return types[last];

            double roll = random.NextDouble() * totalWeight;
            for (int i = 0; i < types.Count; i++)
            {
                if (typeWeights[i] <= 0) continue;
                roll -= typeWeights[i];
                if (roll < 0) return types[i];
            }
            return types[last];
        }

        private bool IsAllowed(ICallerType type)
        {
            if (idle != null && idle.Blocks(type)) return false;
            if (type.MaxSimultaneous > 0)
            {
                int taken = calls.CountLive(type) + (callbacks?.CountPending(type) ?? 0);
                if (taken >= type.MaxSimultaneous) return false;
            }
            int recipients = CountRecipients(type, out IPerson lone);
            for (int i = 0; i < callers.Count; i++)
                if (CanCall(callers[i], type, recipients, lone)) return true;
            return false;
        }

        private bool Spawn(ICallerType type)
        {
            int recipients = CountRecipients(type, out IPerson lone);
            ICaller caller = PickCaller(type, recipients, lone);
            IPerson recipient = caller != null ? PickRecipient(type, caller.Person) : null;
            if (recipient == null) return false;
            calls.Add(factory.Create(caller, recipient));
            return true;
        }

        private ICaller PickCaller(ICallerType type, int recipients, IPerson lone)
        {
            double totalWeight = 0;
            for (int i = 0; i < callers.Count; i++)
                if (CanCall(callers[i], type, recipients, lone)) totalWeight += callers[i].Weight;
            if (totalWeight <= 0) return null;

            double roll = random.NextDouble() * totalWeight;
            ICaller picked = null;
            for (int i = 0; i < callers.Count; i++)
            {
                if (!CanCall(callers[i], type, recipients, lone)) continue;
                picked = callers[i];
                roll -= picked.Weight;
                if (roll < 0) break;
            }
            return picked;
        }

        // A caller who isn't in the directory could never be connected, and one whose only free recipient is
        // themselves has nobody to call.
        private bool CanCall(ICaller caller, ICallerType type, int recipients, IPerson lone) =>
            caller != null && caller.Weight > 0 && caller.Type == type && directory.Lists(caller.Person) &&
            (board == null || board.IsInGame(caller.Person)) && !busy.IsBusy(caller.Person) && (recipients >= 2 || (recipients == 1 && !lone.IsSamePerson(caller.Person)));

        private bool CanReceive(ICallerType type, IDirectoryEntry entry) =>
            IsOpen(entry) && type.Accepts(entry.Person) && !busy.IsBusy(entry.Person);

        private bool IsOpen(IDirectoryEntry entry) => board == null || board.IsOpen(entry);

        private int CountRecipients(ICallerType type, out IPerson lone)
        {
            lone = null;
            int count = 0;
            IReadOnlyList<IDirectoryEntry> entries = directory.ListedEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!CanReceive(type, entries[i])) continue;
                lone = entries[i].Person;
                count++;
            }
            return count;
        }

        private IPerson PickRecipient(ICallerType type, IPerson caller)
        {
            IReadOnlyList<IDirectoryEntry> entries = directory.ListedEntries;
            int candidates = 0;
            for (int i = 0; i < entries.Count; i++)
                if (CanReceive(type, entries[i]) && !entries[i].Person.IsSamePerson(caller)) candidates++;
            if (candidates == 0) return null;

            int pick = random.Next(0, candidates);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!CanReceive(type, entries[i]) || entries[i].Person.IsSamePerson(caller)) continue;
                if (pick-- == 0) return entries[i].Person;
            }
            return null;
        }
    }
}
