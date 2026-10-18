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
            IPacingProfile pacing = content.Pacing ?? PacingSettings.Default;
            Directory = new PhoneDirectory(content.Directory);
            Wallet = new Wallet(content.StartingCents);
            Idle = new IdleGuard(pacing.IdleTimeoutSeconds);
            Calls = new CallService();
            Upgrades = new UpgradeService(content.Upgrades, Wallet);
            Stats = new StatSheet(Upgrades, content.StartingCords);
            Shelf = new ShopShelf(Upgrades);
            Goal = new GoalProgress(Upgrades, Wallet);
            Earnings = new EarningsService(Calls, Wallet, () => Stats.IncomeMultiplier);
            Penalties = new PenaltyService(Calls, Wallet);
            Callbacks = new CallbackScheduler(Calls, clock, random, maxLiveCalls, Idle);
            Busy = new BusyTracker(Calls, Callbacks);
            Spawner = new CallSpawner(Calls, new CallFactory(random), Directory, content.Callers, random, Busy,
                pacing, maxLiveCalls, () => switchboard.CordCount, Callbacks, Idle);
            Desk = new OperatorDesk(Calls, new ConnectionRule(Directory), switchboard);
            Notepad = new Notepad(Calls, Directory, Stats);
        }

        public IGameClock Clock => clock;
        public PhoneDirectory Directory { get; }
        public Wallet Wallet { get; }
        public CallService Calls { get; }
        public UpgradeService Upgrades { get; }
        public StatSheet Stats { get; }
        public ShopShelf Shelf { get; }
        public GoalProgress Goal { get; }
        public EarningsService Earnings { get; }
        public PenaltyService Penalties { get; }
        public IdleGuard Idle { get; }
        public CallbackScheduler Callbacks { get; }
        public BusyTracker Busy { get; }
        public CallSpawner Spawner { get; }
        public OperatorDesk Desk { get; }
        public Notepad Notepad { get; }

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            clock.Advance(dt);
            Idle.Tick(dt);
            Earnings.Tick(dt);
            Calls.Tick(dt);
            Callbacks.Tick();
            Spawner.Tick(dt);
            Goal.Tick();
        }

        /// <summary>
        /// A dev tool: forgets every purchase (no refund) and puts the shop back to its start. Cords already added
        /// stay, because a cord can't be taken away safely mid-call.
        /// </summary>
        public void ResetUpgrades()
        {
            Upgrades.ClearOwned();
            Stats.Recompute();
            Goal.ResetAffordable();
            Shelf.Rebuild();
        }

        /// <summary>A dev tool: ends every live call without pay, fines or callbacks, and empties the board.</summary>
        public void ClearCalls()
        {
            Callbacks.Clear();
            while (Calls.Live.Count > 0) Calls.End(Calls.Live[Calls.Live.Count - 1].Id, CallEndReason.Cleared);
            Desk.ReturnAllCords();
        }
    }
}
