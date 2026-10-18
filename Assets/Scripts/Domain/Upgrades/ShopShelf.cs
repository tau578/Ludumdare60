using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// What the shop's rotating buttons offer (Design 5.9, D41). Each slot holds one chain until it is bought out,
    /// then takes the next chain not on the shelf, in file order; the other slots never move. The secret has its
    /// own pinned button and is never shelved.
    /// </summary>
    public sealed class ShopShelf
    {
        public const int SlotCount = 3;

        private readonly UpgradeService upgrades;
        private readonly UpgradeKind[] slots = new UpgradeKind[SlotCount];
        private readonly bool[] filled = new bool[SlotCount];

        public ShopShelf(UpgradeService upgrades)
        {
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            upgrades.UpgradeBought += OnUpgradeBought;
            Fill();
        }

        public event Action Changed;

        public bool TryGetKind(int slot, out UpgradeKind kind)
        {
            kind = slots[slot];
            return filled[slot];
        }

        /// <summary>The entry the slot sells now; null for an empty slot.</summary>
        public IUpgrade OfferAt(int slot) => filled[slot] ? upgrades.NextInChain(slots[slot]) : null;

        public void Rebuild()
        {
            Fill();
            Changed?.Invoke();
        }

        private void Fill()
        {
            for (int i = 0; i < SlotCount; i++) filled[i] = false;
            for (int i = 0; i < SlotCount; i++) TakeNextChain(i);
        }

        private void OnUpgradeBought(UpgradeBought e)
        {
            for (int i = 0; i < SlotCount; i++)
                if (filled[i] && slots[i] == e.Upgrade.Kind && upgrades.IsBoughtOut(slots[i]))
                {
                    filled[i] = false;
                    TakeNextChain(i);
                }
            Changed?.Invoke();
        }

        private void TakeNextChain(int slot)
        {
            IReadOnlyList<UpgradeKind> kinds = upgrades.Kinds;
            for (int i = 0; i < kinds.Count; i++)
            {
                UpgradeKind kind = kinds[i];
                if (kind == UpgradeKind.Secret || IsShelved(kind) || upgrades.IsBoughtOut(kind)) continue;
                slots[slot] = kind;
                filled[slot] = true;
                return;
            }
        }

        private bool IsShelved(UpgradeKind kind)
        {
            for (int i = 0; i < SlotCount; i++)
                if (filled[i] && slots[i] == kind) return true;
            return false;
        }
    }
}
