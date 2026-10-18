using System;

namespace Switchboard.Content
{
    /// <summary>
    /// The designer's Assets/Configs/Economy.json (D39, D47, D52). JsonUtility gives 0, false or null for a field the
    /// file leaves out, so values that must not silently become 0 start as a marker the loader can tell apart.
    /// </summary>
    [Serializable]
    public sealed class EconomyFileData
    {
        public const int Unset = int.MinValue;

        public long startingCents;
        public int startingCords = Unset;
        public BoardSizeEntry startingBoard = new BoardSizeEntry();
        public int startingClientSlots = Unset;

        /// <summary>Call ids from Calls.json for a new game (D52); null when the file leaves it out.</summary>
        public string[] startPool;

        public UpgradeEntry[] upgrades = Array.Empty<UpgradeEntry>();

        // Sections Step 7 removed (D51). Read only to tell the designer they are no longer used.
        public RemovedPacingEntry pacing = new RemovedPacingEntry();
        public RemovedCallTypeEntry[] calls;
    }

    /// <summary>The old pacing section; NaN everywhere means the file has none.</summary>
    [Serializable]
    public sealed class RemovedPacingEntry
    {
        public float firstCallDelaySeconds = float.NaN;
        public float baseIntervalSeconds = float.NaN;
        public float intervalJitter = float.NaN;
        public float loadFactor = float.NaN;
        public float idleTimeoutSeconds = float.NaN;

        public bool IsPresent => !float.IsNaN(firstCallDelaySeconds) || !float.IsNaN(baseIntervalSeconds) ||
            !float.IsNaN(intervalJitter) || !float.IsNaN(loadFactor) || !float.IsNaN(idleTimeoutSeconds);
    }

    /// <summary>An entry of the old calls section (call types).</summary>
    [Serializable]
    public sealed class RemovedCallTypeEntry
    {
        public string id;
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
        public BoardSizeEntry boardSize = new BoardSizeEntry();
        public int clientSlots = EconomyFileData.Unset;

        /// <summary>An extra_clients entry's call queue (D52); null when the entry leaves it out.</summary>
        public string[] stagePool;
    }

    /// <summary>The open part of the board, written as { "cols": 4, "rows": 4 } (D49).</summary>
    [Serializable]
    public sealed class BoardSizeEntry
    {
        public int cols = EconomyFileData.Unset;
        public int rows = EconomyFileData.Unset;

        public bool IsMissing => cols == EconomyFileData.Unset && rows == EconomyFileData.Unset;
        public bool IsPartial => (cols == EconomyFileData.Unset) != (rows == EconomyFileData.Unset);
    }
}
