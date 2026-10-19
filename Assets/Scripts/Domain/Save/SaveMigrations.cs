using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>One step that brings a save from FromVersion to FromVersion + 1.</summary>
    public sealed class SaveMigration
    {
        public SaveMigration(int fromVersion, Action<SaveData> apply)
        {
            FromVersion = fromVersion;
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public int FromVersion { get; }
        public Action<SaveData> Apply { get; }
    }

    /// <summary>Runs the migration steps one version at a time, in order, up to the version this build writes.</summary>
    public static class SaveMigrations
    {
        /// <summary>The game's own steps. Version 1 is the first format, so there are none yet.</summary>
        public static readonly IReadOnlyList<SaveMigration> All = Array.Empty<SaveMigration>();

        /// <summary>Returns how many steps ran. A save newer than the target is left as it is.</summary>
        public static int Run(SaveData data, IReadOnlyList<SaveMigration> steps, int targetVersion = SaveData.CurrentVersion)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int ran = 0;
            while (data.version < targetVersion)
            {
                int from = data.version;
                if (steps != null)
                    for (int i = 0; i < steps.Count; i++)
                    {
                        if (steps[i] == null || steps[i].FromVersion != from) continue;
                        steps[i].Apply(data);
                        ran++;
                    }
                data.version = from + 1;
            }
            return ran;
        }
    }
}
