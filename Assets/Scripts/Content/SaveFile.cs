using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Turns saves into JSON and back, runs the migrations, and keeps a backup of the last good save (5.12).
    /// Broken data is never deleted on load; the next save simply writes over the main key.
    /// </summary>
    public sealed class SaveFile
    {
        // itch serves every HTML game from one shared domain, so the keys name the game (D55).
        public const string MainKey = "operator.save";
        public const string BackupKey = "operator.save.backup";

        private readonly ISaveStorage storage;
        private readonly IReadOnlyList<SaveMigration> migrations;

        public SaveFile(ISaveStorage storage, IReadOnlyList<SaveMigration> migrations = null)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.migrations = migrations ?? SaveMigrations.All;
        }

        /// <summary>Whether there is anything to continue: a main save or a backup.</summary>
        public bool HasSave => storage.Read(MainKey) != null || storage.Read(BackupKey) != null;

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data);

        /// <summary>Reads the main save, then the backup; with neither usable the game starts new.</summary>
        public SaveLoad Load()
        {
            var messages = new List<string>();
            string main = storage.Read(MainKey);
            string mainProblem = null;
            if (main != null)
            {
                if (TryParse(main, out SaveData data, out mainProblem)) return Loaded(data, SaveSource.Main, messages);
            }

            string backup = storage.Read(BackupKey);
            string backupProblem = null;
            if (backup != null && TryParse(backup, out SaveData older, out backupProblem))
            {
                messages.Add(main != null
                    ? $"Save: the saved game couldn't be read ({mainProblem}), so the backup of the save before it was loaded."
                    : "Save: there was no saved game, only its backup, so the backup was loaded.");
                return Loaded(older, SaveSource.Backup, messages);
            }

            if (main != null)
                messages.Add(backup != null
                    ? $"Save: the saved game couldn't be read ({mainProblem}), and neither could its backup ({backupProblem}), so a new game started."
                    : $"Save: the saved game couldn't be read ({mainProblem}) and there is no backup, so a new game started.");
            else if (backup != null)
                messages.Add($"Save: only a backup was found and it couldn't be read ({backupProblem}), so a new game started.");
            return new SaveLoad(null, SaveSource.New, messages);
        }

        /// <summary>Writes a save; the previous one moves to the backup key if it can still be read.</summary>
        public void Write(SaveData data) => Write(ToJson(data));

        public void Write(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            string previous = storage.Read(MainKey);
            if (previous == json) return;
            if (previous != null && TryParse(previous, out _, out _)) storage.Write(BackupKey, previous);
            storage.Write(MainKey, json);
        }

        /// <summary>New game (D55): the main save and its backup, career included.</summary>
        public void Delete()
        {
            storage.Delete(MainKey);
            storage.Delete(BackupKey);
        }

        /// <summary>Parses a save; problem is a plain reason when it can't be used.</summary>
        public static bool TryParse(string json, out SaveData data, out string problem)
        {
            data = null;
            problem = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                problem = "it is empty";
                return false;
            }
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                data = null;
            }
            if (data == null)
            {
                problem = "it isn't valid JSON";
                return false;
            }
            if (data.version < 1)
            {
                data = null;
                problem = "it has no version number";
                return false;
            }
            return true;
        }

        private SaveLoad Loaded(SaveData data, SaveSource source, List<string> messages)
        {
            if (data.version > SaveData.CurrentVersion)
                messages.Add($"Save: this save was made by a newer version of the game (save version {data.version}, " +
                    $"this one reads {SaveData.CurrentVersion}), so only what this version understands was loaded.");
            else
                SaveMigrations.Run(data, migrations);
            if (data.upgrades == null) data.upgrades = new List<string>();
            if (data.career == null) data.career = new CareerData();
            return new SaveLoad(data, source, messages);
        }
    }

    public enum SaveSource
    {
        New = 0,
        Main = 1,
        Backup = 2
    }

    /// <summary>What Load found: the save (null for a new game), where it came from, and plain messages to log.</summary>
    public sealed class SaveLoad
    {
        public SaveLoad(SaveData data, SaveSource source, IReadOnlyList<string> messages)
        {
            Data = data;
            Source = source;
            Messages = messages;
        }

        public SaveData Data { get; }
        public SaveSource Source { get; }
        public IReadOnlyList<string> Messages { get; }
    }
}
