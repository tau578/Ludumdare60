using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>
    /// What a save holds (Design 5.12, D25, D55): the balance, the bought ids, the queue's place, the tutorial's shop hints
    /// already shown, and the career.
    /// Calls, cords, the shop shelf and the stats are not saved; they are rebuilt from the ids.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        // 2 (13a pass 4): the emergency hint joined the shop hints (SaveMigrations).
        public const int CurrentVersion = 2;

        // 0 means the file had no version; every save written has one.
        public int version;
        public long cents;
        public double fraction;
        public List<string> upgrades = new List<string>();
        public int queuePosition;

        /// <summary>A save from before the shop hints were saved has no such field: it counts as every hint done.</summary>
        public const int ShopHintsMissing = -1;

        // The ShopHints already shown (Step 10, fix 2). JsonUtility keeps this initializer when the field is missing.
        public int shopHintsDone = ShopHintsMissing;

        // Kept apart from the run data, so a later reset of the run can keep it (5.12).
        public CareerData career = new CareerData();
    }

    [Serializable]
    public sealed class CareerData
    {
        public long earnedCents;
        public int callsConnected;

        // Missing in saves from before 13a, which JsonUtility reads as 0.
        public int callsHungUp;
        public long finesPaidCents;
        public double secondsPlayed;
    }
}
