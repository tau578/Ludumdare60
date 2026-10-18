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
        public const int DefaultStartingCords = 3;

        // All the team's cord tray holds (5b).
        public const int MaxCords = 6;

        private readonly UpgradeService upgrades;
        private readonly int startingCords;

        public StatSheet(UpgradeService upgrades, int startingCords = DefaultStartingCords)
        {
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.startingCords = startingCords > 0 ? startingCords : DefaultStartingCords;
            upgrades.UpgradeBought += _ => Recompute();
            Compute();
        }

        /// <summary>Raised after every recompute, so views can follow cords and the Notepad.</summary>
        public event Action Changed;

        public double IncomeMultiplier { get; private set; } = BaseIncomeMultiplier;

        /// <summary>The total number of cords (D40: wires sets the total).</summary>
        public int Cords { get; private set; }

        /// <summary>True while any notepad entry is owned (D23).</summary>
        public bool HasNotepad { get; private set; }

        public void Recompute()
        {
            Compute();
            Changed?.Invoke();
        }

        private void Compute()
        {
            IncomeMultiplier = BaseIncomeMultiplier;
            IReadOnlyList<IUpgrade> income = upgrades.ChainOf(UpgradeKind.ExtraIncome);
            for (int i = 0; i < income.Count; i++)
                if (upgrades.IsOwned(income[i].Id)) IncomeMultiplier = income[i].IncomeMultiplier;

            Cords = startingCords;
            IReadOnlyList<IUpgrade> wires = upgrades.ChainOf(UpgradeKind.ExtraWires);
            for (int i = 0; i < wires.Count; i++)
                if (upgrades.IsOwned(wires[i].Id)) Cords = wires[i].Wires;

            HasNotepad = false;
            IReadOnlyList<IUpgrade> notepad = upgrades.ChainOf(UpgradeKind.Notepad);
            for (int i = 0; i < notepad.Count; i++)
                if (upgrades.IsOwned(notepad[i].Id)) HasNotepad = true;
        }
    }
}
