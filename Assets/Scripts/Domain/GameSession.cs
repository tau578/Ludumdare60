using System;

namespace Switchboard.Domain
{
    /// <summary>Builds every domain service in dependency order and owns the tick order (Design 4.4).</summary>
    public sealed class GameSession
    {
        private readonly GameClock clock = new GameClock();
        private readonly long startingCents;

        /// <param name="save">A loaded save, restored while the services are built so nothing binds to a new game
        /// first (5.12); null starts a new game with Economy.json's starting values.</param>
        public GameSession(IGameContent content, ISwitchboard switchboard, SaveData save = null)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (switchboard == null) throw new ArgumentNullException(nameof(switchboard));
            startingCents = Math.Max(0, content.StartingCents);
            Wallet = save != null ? new Wallet(save.cents, save.fraction) : new Wallet(content.StartingCents);
            Calls = new CallService();
            Upgrades = new UpgradeService(content.Upgrades, Wallet);
            // Before the stats, shelf and goal are built, so they start from the bought ids with no second rebuild.
            if (save != null) Upgrades.Restore(save.upgrades);
            Stats = new StatSheet(Upgrades, content.StartingCords, content.StartingBoard, content.StartingClientSlots,
                content.StartPool);
            Board = new OpenBoard(Stats);
            Queue = new CallQueue(Calls, Stats);
            if (save != null) Queue.Restore(save.queuePosition);
            Shelf = new ShopShelf(Upgrades);
            Goal = new GoalProgress(Upgrades, Wallet);
            Earnings = new EarningsService(Calls, Wallet, () => Stats.IncomeMultiplier);
            Penalties = new PenaltyService(Calls, Wallet);
            Desk = new OperatorDesk(Calls, new ConnectionRule(), switchboard);
            Career = new CareerStats(Calls, Earnings, Penalties, save?.career);
            IsRestored = save != null;
        }

        public IGameClock Clock => clock;
        public Wallet Wallet { get; }
        public CallService Calls { get; }
        public UpgradeService Upgrades { get; }
        public StatSheet Stats { get; }
        public OpenBoard Board { get; }
        public CallQueue Queue { get; }
        public ShopShelf Shelf { get; }
        public GoalProgress Goal { get; }
        public EarningsService Earnings { get; }
        public PenaltyService Penalties { get; }
        public OperatorDesk Desk { get; }
        public CareerStats Career { get; }

        /// <summary>Whether this session was loaded from a save.</summary>
        public bool IsRestored { get; }

        /// <summary>
        /// Whether a new game has anything worth saving yet: money moved, something bought, or a call connected.
        /// A loaded session always has.
        /// </summary>
        public bool HasProgress => IsRestored || Upgrades.OwnsAny || Career.CallsConnected > 0 ||
            Wallet.Cents != startingCents || Wallet.Fraction > 0;

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            clock.Advance(dt);
            Career.Tick(dt);
            Earnings.Tick(dt);
            Calls.Tick(dt);
            Queue.Tick();
            Goal.Tick();
        }

        /// <summary>A fresh snapshot for the save (5.12): never the live lists, so later play can't change it.</summary>
        public SaveData CaptureSave() => new SaveData
        {
            version = SaveData.CurrentVersion,
            cents = Wallet.Cents,
            fraction = Wallet.Fraction,
            upgrades = Upgrades.OwnedIds(),
            queuePosition = Queue.ResumePosition,
            career = Career.Capture()
        };

        /// <summary>
        /// A dev tool: forgets every purchase (no refund) and puts the shop back to its start. Cords already added
        /// stay, because a cord can't be taken away safely mid-call. The board closes again, so live calls are
        /// cleared first (they may use closing jacks), cords plugged into closed jacks go back to the tray, and the
        /// queue starts again at startPool's first call.
        /// </summary>
        public void ResetUpgrades()
        {
            EndAllCalls();
            Upgrades.ClearOwned();
            Stats.Recompute();
            Queue.Restart();
            Desk.ReturnCordsWhere(jack => !Board.IsOpen(jack));
            Goal.ResetAffordable();
            Shelf.Rebuild();
        }

        /// <summary>A dev tool: ends every live call without pay or fines and empties the board. The queue carries on.</summary>
        public void ClearCalls()
        {
            EndAllCalls();
            Desk.ReturnAllCords();
        }

        private void EndAllCalls()
        {
            while (Calls.Live.Count > 0) Calls.End(Calls.Live[Calls.Live.Count - 1].Id, CallEndReason.Cleared);
        }
    }
}
