using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// Every stat is the value of the last bought entry of its chain, or the base value (Design 5.9). Values set
    /// the result, so recomputing from the bought ids can never apply anything twice.
    /// </summary>
    public sealed class StatSheet
    {
        public const double BaseIncomeMultiplier = 1;

        private readonly UpgradeService upgrades;

        public StatSheet(UpgradeService upgrades)
        {
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            upgrades.UpgradeBought += _ => Recompute();
            Recompute();
        }

        public double IncomeMultiplier { get; private set; } = BaseIncomeMultiplier;

        public void Recompute()
        {
            IncomeMultiplier = BaseIncomeMultiplier;
            IReadOnlyList<IUpgrade> chain = upgrades.ChainOf(UpgradeKind.ExtraIncome);
            for (int i = 0; i < chain.Count; i++)
                if (upgrades.IsOwned(chain[i].Id)) IncomeMultiplier = chain[i].IncomeMultiplier;
        }
    }
}
