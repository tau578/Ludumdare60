using System;

namespace Switchboard.Content
{
    /// <summary>The designer's Assets/Configs/Economy.json (D39). Step 5a reads only its upgrades.</summary>
    [Serializable]
    public sealed class EconomyFileData
    {
        public UpgradeEntry[] upgrades = Array.Empty<UpgradeEntry>();
    }

    /// <summary>One one-time upgrade (D40). Fields a type doesn't use stay at their defaults.</summary>
    [Serializable]
    public sealed class UpgradeEntry
    {
        public string id;
        public string type;
        public string name;
        public string description;
        public long costCents;
        public float incomeMultiplier;
    }
}
