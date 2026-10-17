using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>Step 1 pacing: one live call at a time, the next one a fixed delay after the board goes quiet.</summary>
    public sealed class CallSpawner
    {
        public const double DefaultDelaySeconds = 2;

        private readonly CallService calls;
        private readonly CallFactory factory;
        private readonly PhoneDirectory directory;
        private readonly IReadOnlyList<ICaller> callers;
        private readonly IRandom random;
        private readonly double delaySeconds;
        private bool waiting;
        private double secondsUntilSpawn;

        public CallSpawner(CallService calls, CallFactory factory, PhoneDirectory directory,
            IReadOnlyList<ICaller> callers, IRandom random, double delaySeconds = DefaultDelaySeconds)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.callers = callers ?? Array.Empty<ICaller>();
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.delaySeconds = delaySeconds;
        }

        public void Tick(double dt)
        {
            if (calls.Live.Count > 0)
            {
                waiting = false;
                return;
            }
            if (!waiting)
            {
                waiting = true;
                secondsUntilSpawn = delaySeconds;
            }
            else secondsUntilSpawn -= dt;
            if (secondsUntilSpawn > 0) return;
            waiting = false;
            Spawn();
        }

        private void Spawn()
        {
            ICaller caller = PickCaller();
            IPerson recipient = caller != null ? PickRecipient(caller.Person) : null;
            if (recipient != null) calls.Add(factory.Create(caller, recipient));
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
            caller != null && caller.Weight > 0 && caller.Type != null && directory.Lists(caller.Person);

        private IPerson PickRecipient(IPerson caller)
        {
            IReadOnlyList<IDirectoryEntry> entries = directory.ListedEntries;
            int others = 0;
            for (int i = 0; i < entries.Count; i++)
                if (!entries[i].Person.IsSamePerson(caller)) others++;
            if (others == 0) return null;

            int pick = random.Next(0, others);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Person.IsSamePerson(caller)) continue;
                if (pick-- == 0) return entries[i].Person;
            }
            return null;
        }
    }
}
