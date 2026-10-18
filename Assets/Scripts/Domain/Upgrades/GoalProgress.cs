using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// Savings toward the secret upgrade (Design 5.10, D14). Code reacts to the secret's kind, never to its id.
    /// </summary>
    public sealed class GoalProgress
    {
        private readonly UpgradeService upgrades;
        private readonly Wallet wallet;
        private bool affordableRaised;

        public GoalProgress(UpgradeService upgrades, Wallet wallet)
        {
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            upgrades.UpgradeBought += OnUpgradeBought;
        }

        /// <summary>Raised once, the first time the balance covers the goal.</summary>
        public event Action GoalAffordable;

        public event Action GoalPurchased;

        /// <summary>The secret entry; null when the file has none, and then there is no goal at all.</summary>
        public IUpgrade Goal => upgrades.Goal;

        public bool IsPurchased => Goal != null && upgrades.IsOwned(Goal.Id);

        public double Progress
        {
            get
            {
                if (Goal == null) return 0;
                if (IsPurchased || Goal.CostCents <= 0) return 1;
                double ratio = (double)wallet.Cents / Goal.CostCents;
                return ratio < 0 ? 0 : ratio > 1 ? 1 : ratio;
            }
        }

        // Checked last in the tick, after the money of this tick has moved.
        public void Tick()
        {
            if (Goal == null || affordableRaised || Progress < 1) return;
            affordableRaised = true;
            GoalAffordable?.Invoke();
        }

        /// <summary>A dev tool: lets GoalAffordable fire again.</summary>
        public void ResetAffordable() => affordableRaised = false;

        private void OnUpgradeBought(UpgradeBought e)
        {
            if (e.Upgrade.Kind != UpgradeKind.Secret) return;
            affordableRaised = true;
            GoalPurchased?.Invoke();
        }
    }
}
