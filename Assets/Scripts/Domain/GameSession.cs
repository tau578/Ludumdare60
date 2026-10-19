using System;

namespace Switchboard.Domain
{
    /// <summary>Builds every domain service in dependency order and owns the tick order (Design 4.4).</summary>
    public sealed class GameSession
    {
        private readonly GameClock clock = new GameClock();

        public GameSession(IGameContent content, ISwitchboard switchboard, bool tutorial = false)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (switchboard == null) throw new ArgumentNullException(nameof(switchboard));
            Wallet = new Wallet(content.StartingCents);
            Calls = new CallService();
            Upgrades = new UpgradeService(content.Upgrades, Wallet);
            Stats = new StatSheet(Upgrades, content.StartingCords, content.StartingBoard, content.StartingClientSlots,
                content.StartPool);
            Board = new OpenBoard(Stats);
            Queue = new CallQueue(Calls, Stats, tutorial);
            Shelf = new ShopShelf(Upgrades);
            Goal = new GoalProgress(Upgrades, Wallet);
            Earnings = new EarningsService(Calls, Wallet, () => Stats.IncomeMultiplier);
            Penalties = new PenaltyService(Calls, Wallet);
            Desk = new OperatorDesk(Calls, new ConnectionRule(), switchboard);
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

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            clock.Advance(dt);
            Earnings.Tick(dt);
            Calls.Tick(dt);
            Queue.Tick();
            Goal.Tick();
        }

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
