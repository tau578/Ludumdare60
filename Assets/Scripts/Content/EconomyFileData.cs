using System;

namespace Switchboard.Content
{
    /// <summary>
    /// The designer's Assets/Configs/Economy.json (D39, D47). JsonUtility gives 0, false or null for a field the
    /// file leaves out, so values that must not silently become 0 start as a marker the loader can tell apart.
    /// </summary>
    [Serializable]
    public sealed class EconomyFileData
    {
        public const int Unset = int.MinValue;

        public long startingCents;
        public int startingCords = Unset;
        public PacingEntry pacing = new PacingEntry();
        public CallEntry[] calls;
        public UpgradeEntry[] upgrades = Array.Empty<UpgradeEntry>();
    }

    /// <summary>When new calls ring (Design 5.2). NaN marks a value the file leaves out.</summary>
    [Serializable]
    public sealed class PacingEntry
    {
        public float firstCallDelaySeconds = float.NaN;
        public float baseIntervalSeconds = float.NaN;
        public float intervalJitter = float.NaN;
        public float loadFactor = float.NaN;
        public float idleTimeoutSeconds = float.NaN;
    }

    /// <summary>One call type's money and timing. Fields left out mean "off", except the required ones.</summary>
    [Serializable]
    public sealed class CallEntry
    {
        public string id;
        public string name;
        public float spawnWeight = float.NaN;
        public RangeEntry rateCentsPerSecond = new RangeEntry();
        public RangeEntry feeCents = new RangeEntry();
        public RangeEntry durationSeconds = new RangeEntry();
        public RangeEntry callbackDelaySeconds = new RangeEntry();
        public float responseTimeSeconds;
        public long fineMissedCents;
        public long fineEarlyDisconnectCents;
        public bool callbackAfterMiss;
        public bool callbackAfterDisconnect;
        public int maxSimultaneous;
        public bool everyoneCalls;
        public string recipientTag;
    }

    /// <summary>A range written as { "min": 3, "max": 8 }.</summary>
    [Serializable]
    public sealed class RangeEntry
    {
        public float min = float.NaN;
        public float max = float.NaN;

        public bool IsMissing => float.IsNaN(min) && float.IsNaN(max);
        public bool IsPartial => float.IsNaN(min) != float.IsNaN(max);
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
        public int wires;
    }
}
