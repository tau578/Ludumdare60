using System;
using System.Collections.Generic;
using System.Globalization;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Parses and checks the designer's economy file (D39, D40; Design 6.4). An entry with a problem is left out
    /// and reported in plain words naming the entry and the fix, and the game still starts.
    /// </summary>
    public static class EconomyFileLoader
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly TypeInfo[] Types =
        {
            new TypeInfo("extra_income", UpgradeKind.ExtraIncome, null),
            new TypeInfo("extra_wires", UpgradeKind.ExtraWires, "Step 5b"),
            new TypeInfo("notepad", UpgradeKind.Notepad, "Step 5b"),
            new TypeInfo("extra_clients", UpgradeKind.ExtraClients, "Step 7"),
            new TypeInfo("secret", UpgradeKind.Secret, null)
        };

        public static EconomyCatalog Load(string json)
        {
            var errors = new List<string>();
            var upgrades = new List<IUpgrade>();
            EconomyFileData data = Parse(json, errors);
            if (data != null)
            {
                var ids = new Dictionary<string, int>(StringComparer.Ordinal);
                var lastInChain = new Dictionary<UpgradeKind, UpgradeRecord>();
                int secretAt = -1;
                for (int i = 0; i < data.upgrades.Length; i++)
                {
                    UpgradeRecord record = Check(i, data.upgrades[i], ids, lastInChain, ref secretAt, errors);
                    if (record == null) continue;
                    upgrades.Add(record);
                    lastInChain[record.Kind] = record;
                }
                if (secretAt < 0)
                    errors.Add("There is no secret upgrade. Add exactly one entry with \"type\": \"secret\": " +
                        "the most expensive one, which the player saves for.");
            }
            return new EconomyCatalog(upgrades, errors);
        }

        public static string KnownTypes()
        {
            var names = new string[Types.Length];
            for (int i = 0; i < Types.Length; i++) names[i] = Types[i].Name;
            return string.Join(", ", names);
        }

        private static EconomyFileData Parse(string json, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add("The economy file is missing or empty. It belongs at Assets/Configs/Economy.json and in " +
                    "GameConfig's Economy File slot.");
                return null;
            }
            EconomyFileData data;
            try
            {
                data = JsonUtility.FromJson<EconomyFileData>(json);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"The economy file is not valid JSON (check for a missing comma, quote or bracket): {ex.Message}");
                return null;
            }
            if (data == null)
            {
                errors.Add("The economy file must contain a JSON object, starting with { and ending with }.");
                return null;
            }
            if (data.upgrades == null) data.upgrades = Array.Empty<UpgradeEntry>();
            return data;
        }

        private static UpgradeRecord Check(int index, UpgradeEntry entry, Dictionary<string, int> ids,
            Dictionary<UpgradeKind, UpgradeRecord> lastInChain, ref int secretAt, List<string> errors)
        {
            string id = entry?.id?.Trim() ?? string.Empty;
            string where = id.Length == 0 ? $"Upgrade {index + 1}" : $"Upgrade {index + 1} \"{id}\"";
            if (entry == null)
            {
                errors.Add($"{where} is empty. Fill it in or remove it.");
                return null;
            }
            int errorsBefore = errors.Count;

            if (id.Length == 0)
                errors.Add($"{where}: the id is empty. Give it a short id of lowercase letters, digits and _, such as income_4.");
            else if (!IsWellFormed(id))
                errors.Add($"{where}: the id may only use lowercase letters a-z, digits 0-9 and _. Try {Suggest(id)}.");
            else if (ids.TryGetValue(id, out int first))
                errors.Add($"{where}: this id is already used by upgrade {first + 1}. Every id must be different.");
            else ids.Add(id, index);

            if (string.IsNullOrWhiteSpace(entry.name))
                errors.Add($"{where}: the name is empty. Type the name the shop button shows, such as \"Better rates\".");
            if (entry.costCents <= 0)
                errors.Add($"{where}: costCents is {entry.costCents}. Give a price in cents above 0, such as 800 for $8.00.");

            TypeInfo type = FindType(entry.type);
            if (type == null)
            {
                errors.Add(string.IsNullOrWhiteSpace(entry.type)
                    ? $"{where}: the type is empty. Use one of: {KnownTypes()}."
                    : $"{where}: type \"{entry.type.Trim()}\" is unknown. Use one of: {KnownTypes()}.");
                return null;
            }
            if (type.ArrivesIn != null)
            {
                errors.Add($"{where}: type \"{type.Name}\" arrives in {type.ArrivesIn} and can't be used yet. Remove the entry for now.");
                return null;
            }

            double multiplier = 0;
            if (type.Kind == UpgradeKind.ExtraIncome)
            {
                multiplier = Math.Round(entry.incomeMultiplier, 6);
                lastInChain.TryGetValue(type.Kind, out UpgradeRecord previous);
                if (multiplier <= 1)
                    errors.Add($"{where}: incomeMultiplier {Number(multiplier)} must be above 1 (1.5 means half as much again). Raise it.");
                else if (previous != null && multiplier <= previous.IncomeMultiplier)
                    errors.Add($"{where}: incomeMultiplier {Number(multiplier)} is not higher than {previous.Id}'s " +
                        $"{Number(previous.IncomeMultiplier)}. Values in a chain must grow.");
            }
            if (type.Kind == UpgradeKind.Secret && errors.Count == errorsBefore)
            {
                if (secretAt >= 0)
                    errors.Add($"{where}: there is already a secret upgrade (upgrade {secretAt + 1}). Keep exactly one secret entry.");
                else secretAt = index;
            }
            if (errors.Count > errorsBefore) return null;
            return new UpgradeRecord(id, type.Kind, entry.name.Trim(), entry.description?.Trim() ?? string.Empty,
                entry.costCents, multiplier);
        }

        private static TypeInfo FindType(string name)
        {
            string trimmed = name?.Trim();
            foreach (TypeInfo type in Types)
                if (string.Equals(type.Name, trimmed, StringComparison.Ordinal)) return type;
            return null;
        }

        private static bool IsWellFormed(string id)
        {
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_') return false;
            return true;
        }

        private static string Suggest(string id)
        {
            var chars = id.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!(chars[i] >= 'a' && chars[i] <= 'z') && !(chars[i] >= '0' && chars[i] <= '9')) chars[i] = '_';
            return new string(chars);
        }

        private static string Number(double value) => value.ToString("0.###", Invariant);

        private sealed class TypeInfo
        {
            public TypeInfo(string name, UpgradeKind kind, string arrivesIn)
            {
                Name = name;
                Kind = kind;
                ArrivesIn = arrivesIn;
            }

            public string Name { get; }
            public UpgradeKind Kind { get; }

            /// <summary>The step that builds the type; null once it is built.</summary>
            public string ArrivesIn { get; }
        }
    }
}
