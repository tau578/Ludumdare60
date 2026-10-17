using System;

namespace Switchboard.Domain
{
    /// <summary>Builds every domain service in dependency order and owns the tick order (Design 4.4).</summary>
    public sealed class GameSession
    {
        // The caller panel has six slots (D17).
        public const int DefaultMaxWaitingCalls = 6;

        private readonly GameClock clock = new GameClock();

        public GameSession(IGameContent content, IRandom random, ISwitchboard switchboard)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (switchboard == null) throw new ArgumentNullException(nameof(switchboard));
            int maxLiveCalls = content.MaxWaitingCalls > 0 ? content.MaxWaitingCalls : DefaultMaxWaitingCalls;
            Directory = new PhoneDirectory(content.Directory);
            Wallet = new Wallet(content.StartingCents);
            Calls = new CallService();
            Earnings = new EarningsService(Calls, Wallet);
            Callbacks = new CallbackScheduler(Calls, clock, random, maxLiveCalls);
            Busy = new BusyTracker(Calls, Callbacks);
            Spawner = new CallSpawner(Calls, new CallFactory(random), Directory, content.Callers, random, Busy,
                content.Pacing, maxLiveCalls, () => switchboard.CordCount);
            Desk = new OperatorDesk(Calls, new ConnectionRule(Directory), switchboard);
        }

        public IGameClock Clock => clock;
        public PhoneDirectory Directory { get; }
        public Wallet Wallet { get; }
        public CallService Calls { get; }
        public EarningsService Earnings { get; }
        public CallbackScheduler Callbacks { get; }
        public BusyTracker Busy { get; }
        public CallSpawner Spawner { get; }
        public OperatorDesk Desk { get; }

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            clock.Advance(dt);
            Earnings.Tick(dt);
            Calls.Tick(dt);
            Callbacks.Tick();
            Spawner.Tick(dt);
        }

        /// <summary>A dev tool: ends every live call without pay, fines or callbacks, and empties the board.</summary>
        public void ClearCalls()
        {
            Callbacks.Clear();
            while (Calls.Live.Count > 0) Calls.End(Calls.Live[Calls.Live.Count - 1].Id, CallEndReason.Cleared);
            Desk.ReturnHalfPatchedCords();
        }
    }
}
