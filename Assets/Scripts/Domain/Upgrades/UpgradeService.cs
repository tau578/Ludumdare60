using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// Owns the bought upgrade ids (Design 5.9). Entries of one kind form a chain in file order, and only the
    /// next unbought entry of a chain can be bought.
    /// </summary>
    public sealed class UpgradeService
    {
        private static readonly IReadOnlyList<IUpgrade> NoChain = Array.Empty<IUpgrade>();

        private readonly Wallet wallet;
        private readonly List<IUpgrade> all = new List<IUpgrade>();
        private readonly Dictionary<string, IUpgrade> byId = new Dictionary<string, IUpgrade>(StringComparer.Ordinal);
        private readonly Dictionary<UpgradeKind, List<IUpgrade>> chains = new Dictionary<UpgradeKind, List<IUpgrade>>();
        private readonly List<UpgradeKind> kinds = new List<UpgradeKind>();
        private readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);

        public UpgradeService(IReadOnlyList<IUpgrade> upgrades, Wallet wallet)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            if (upgrades == null) return;
            foreach (IUpgrade upgrade in upgrades)
            {
                // The loader already leaves out broken entries; this only guards against a second copy of an id.
                if (upgrade == null || string.IsNullOrEmpty(upgrade.Id) || byId.ContainsKey(upgrade.Id)) continue;
                all.Add(upgrade);
                byId.Add(upgrade.Id, upgrade);
                if (!chains.TryGetValue(upgrade.Kind, out List<IUpgrade> chain))
                {
                    chain = new List<IUpgrade>();
                    chains.Add(upgrade.Kind, chain);
                    kinds.Add(upgrade.Kind);
                }
                chain.Add(upgrade);
            }
            Goal = FirstOf(UpgradeKind.Secret);
        }

        public event Action<UpgradeBought> UpgradeBought;

        public IReadOnlyList<IUpgrade> All => all;

        /// <summary>Each kind once, in order of its first appearance in the file.</summary>
        public IReadOnlyList<UpgradeKind> Kinds => kinds;

        /// <summary>The one secret entry, the goal (D14); null when the file has none.</summary>
        public IUpgrade Goal { get; }

        public IUpgrade Find(string id) => id != null && byId.TryGetValue(id, out IUpgrade upgrade) ? upgrade : null;

        public bool IsOwned(string id) => id != null && owned.Contains(id);

        public IReadOnlyList<IUpgrade> ChainOf(UpgradeKind kind) => chains.TryGetValue(kind, out List<IUpgrade> chain) ? chain : NoChain;

        /// <summary>The next entry of the kind's chain that isn't bought yet; null when the chain is bought out.</summary>
        public IUpgrade NextInChain(UpgradeKind kind)
        {
            if (!chains.TryGetValue(kind, out List<IUpgrade> chain)) return null;
            for (int i = 0; i < chain.Count; i++)
                if (!owned.Contains(chain[i].Id)) return chain[i];
            return null;
        }

        public bool IsBoughtOut(UpgradeKind kind) => NextInChain(kind) == null;

        public PurchaseCheck CanBuy(string id)
        {
            IUpgrade upgrade = Find(id);
            if (upgrade == null) return PurchaseCheck.Unknown;
            if (owned.Contains(upgrade.Id)) return PurchaseCheck.Owned;
            if (NextInChain(upgrade.Kind) != upgrade) return PurchaseCheck.NotNext;
            return wallet.Cents >= upgrade.CostCents ? PurchaseCheck.Ok : PurchaseCheck.TooExpensive;
        }

        public bool TryBuy(string id)
        {
            if (CanBuy(id) != PurchaseCheck.Ok) return false;
            IUpgrade upgrade = byId[id];
            if (!wallet.TrySpend(upgrade.CostCents)) return false;
            owned.Add(upgrade.Id);
            UpgradeBought?.Invoke(new UpgradeBought(upgrade, upgrade.CostCents));
            return true;
        }

        /// <summary>A dev tool: gives the next entry of the kind's chain for free, as if bought for 0.</summary>
        public bool TryGrantNext(UpgradeKind kind)
        {
            IUpgrade next = NextInChain(kind);
            if (next == null) return false;
            owned.Add(next.Id);
            UpgradeBought?.Invoke(new UpgradeBought(next, 0));
            return true;
        }

        /// <summary>
        /// Puts back the bought ids of a save (5.12) before the stats are built: nothing is spent and no
        /// UpgradeBought is raised. Ids the file no longer has are ignored.
        /// </summary>
        public void Restore(IEnumerable<string> ids)
        {
            if (ids == null) return;
            foreach (string id in ids)
                if (id != null && byId.ContainsKey(id)) owned.Add(id);
        }

        /// <summary>The bought ids in file order, for a save.</summary>
        public List<string> OwnedIds()
        {
            var ids = new List<string>(owned.Count);
            foreach (IUpgrade upgrade in all)
                if (owned.Contains(upgrade.Id)) ids.Add(upgrade.Id);
            return ids;
        }

        public bool OwnsAny => owned.Count > 0;

        /// <summary>A dev tool: forgets every purchase without refunding it.</summary>
        public void ClearOwned() => owned.Clear();

        private IUpgrade FirstOf(UpgradeKind kind) => chains.TryGetValue(kind, out List<IUpgrade> chain) ? chain[0] : null;
    }

    public readonly struct UpgradeBought
    {
        public UpgradeBought(IUpgrade upgrade, long spentCents)
        {
            Upgrade = upgrade;
            SpentCents = spentCents;
        }

        public IUpgrade Upgrade { get; }
        public long SpentCents { get; }
    }
}
