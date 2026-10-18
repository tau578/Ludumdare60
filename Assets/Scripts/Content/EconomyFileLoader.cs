using System;
using System.Collections.Generic;
using System.Globalization;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Parses and checks the designer's economy file (D39, D40, D47, D49; Design 6.4): starting money, cords, board
    /// and caller slots, pacing, call types and upgrades. An entry with a problem is left out, and a bad starting or pacing value falls back to
    /// its default; each is reported in plain words naming the entry and the fix, and the game still starts.
    /// </summary>
    public static class EconomyFileLoader
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly TypeInfo[] Types =
        {
            new TypeInfo("extra_income", UpgradeKind.ExtraIncome),
            new TypeInfo("extra_wires", UpgradeKind.ExtraWires),
            new TypeInfo("notepad", UpgradeKind.Notepad),
            new TypeInfo("extra_clients", UpgradeKind.ExtraClients),
            new TypeInfo("secret", UpgradeKind.Secret)
        };

        private static readonly FloatRange DefaultCallbackDelay = new FloatRange(20, 40);

        public static EconomyCatalog Load(string json)
        {
            var errors = new List<string>();
            var upgrades = new List<IUpgrade>();
            var callTypes = new List<ICallerType>();
            long startingCents = 0;
            int startingCords = StatSheet.DefaultStartingCords;
            BoardSize startingBoard = BoardSize.Full;
            int startingClientSlots = StatSheet.MaxClientSlots;
            IPacingProfile pacing = PacingSettings.Default;
            EconomyFileData data = Parse(json, errors);
            if (data != null)
            {
                startingCents = CheckStartingCents(data.startingCents, errors);
                startingCords = CheckStartingCords(data.startingCords, errors);
                startingBoard = CheckStartingBoard(data.startingBoard, errors);
                startingClientSlots = CheckStartingClientSlots(data.startingClientSlots, errors);
                pacing = CheckPacing(data.pacing, errors);
                CheckCalls(data.calls, callTypes, errors);

                var ids = new Dictionary<string, int>(StringComparer.Ordinal);
                var lastInChain = new Dictionary<UpgradeKind, UpgradeRecord>();
                var rules = new ChainRules(startingCords, startingBoard, startingClientSlots);
                for (int i = 0; i < data.upgrades.Length; i++)
                {
                    UpgradeRecord record = Check(i, data.upgrades[i], ids, lastInChain, rules, errors);
                    if (record == null) continue;
                    upgrades.Add(record);
                    lastInChain[record.Kind] = record;
                }
                if (rules.SecretAt < 0)
                    errors.Add("There is no secret upgrade. Add exactly one entry with \"type\": \"secret\": " +
                        "the most expensive one, which the player saves for.");
            }
            return new EconomyCatalog(startingCents, startingCords, startingBoard, startingClientSlots, pacing, callTypes,
                upgrades, errors);
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

        private static long CheckStartingCents(long cents, List<string> errors)
        {
            if (cents >= 0) return cents;
            errors.Add($"startingCents {cents} is negative. Use 0 or more, in cents (500 is $5.00). The game starts with 0 for now.");
            return 0;
        }

        // A session starts with 2 to 6 cords; the input inventory is trimmed before binding.
        private static int CheckStartingCords(int cords, List<string> errors)
        {
            const int fallback = StatSheet.DefaultStartingCords;
            if (cords == EconomyFileData.Unset) return fallback;
            if (cords < fallback)
                errors.Add($"startingCords {cords} is below the {fallback} cords the tray is built with. " +
                    $"Use {fallback} to {StatSheet.MaxCords}. The game starts with {fallback} for now.");
            else if (cords > StatSheet.MaxCords)
                errors.Add($"startingCords {cords} is more than the tray holds. " +
                    $"Use {fallback} to {StatSheet.MaxCords}. The game starts with {fallback} for now.");
            else return cords;
            return fallback;
        }

        // Without a valid start the game still plays, on the whole board with every caller slot open.
        private static BoardSize CheckStartingBoard(BoardSizeEntry entry, List<string> errors)
        {
            BoardSize full = BoardSize.Full;
            string instead = $"The game starts on the full {full} board for now.";
            if (entry == null || entry.IsMissing)
                errors.Add($"startingBoard is missing. Add the part of the board a new game opens, such as " +
                    $"\"startingBoard\": {{ \"cols\": 3, \"rows\": 3 }}. {instead}");
            else if (entry.IsPartial)
                errors.Add($"startingBoard needs both cols and rows, such as {{ \"cols\": 3, \"rows\": 3 }}. {instead}");
            else
            {
                var size = new BoardSize(entry.cols, entry.rows);
                if (size.IsValid) return size;
                errors.Add($"startingBoard {size} is outside the board. cols and rows must each be 1 to {BoardSize.MaxSide}. {instead}");
            }
            return full;
        }

        private static int CheckStartingClientSlots(int slots, List<string> errors)
        {
            const int all = StatSheet.MaxClientSlots;
            if (slots == EconomyFileData.Unset)
                errors.Add($"startingClientSlots is missing. Add how many caller slots a new game opens, such as " +
                    $"\"startingClientSlots\": 3. The game opens all {all} for now.");
            else if (slots < 1 || slots > all)
                errors.Add($"startingClientSlots {slots} is outside 1 to {all}, the caller panel's slots. The game opens all {all} for now.");
            else return slots;
            return all;
        }

        private static IPacingProfile CheckPacing(PacingEntry entry, List<string> errors)
        {
            PacingSettings fallback = PacingSettings.Default;
            if (entry == null || float.IsNaN(entry.firstCallDelaySeconds) && float.IsNaN(entry.baseIntervalSeconds) &&
                float.IsNaN(entry.intervalJitter) && float.IsNaN(entry.loadFactor) && float.IsNaN(entry.idleTimeoutSeconds))
            {
                errors.Add("There is no pacing section, so the game uses the default pacing: " +
                    $"firstCallDelaySeconds {Number(fallback.FirstCallDelaySeconds)}, baseIntervalSeconds {Number(fallback.BaseIntervalSeconds)}, " +
                    $"intervalJitter {Number(fallback.IntervalJitter)}, loadFactor {Number(fallback.LoadFactor)}, " +
                    $"idleTimeoutSeconds {Number(fallback.IdleTimeoutSeconds)}. Add the section; see Docs/EconomyFile.md.");
                return fallback;
            }
            return new PacingSettings(
                PacingValue(entry.firstCallDelaySeconds, "firstCallDelaySeconds", fallback.FirstCallDelaySeconds, 0, null, errors),
                PacingValue(entry.baseIntervalSeconds, "baseIntervalSeconds", fallback.BaseIntervalSeconds, 0.1, null, errors),
                PacingValue(entry.intervalJitter, "intervalJitter", fallback.IntervalJitter, 0, 0.95, errors),
                PacingValue(entry.loadFactor, "loadFactor", fallback.LoadFactor, 0, null, errors),
                PacingValue(entry.idleTimeoutSeconds, "idleTimeoutSeconds", fallback.IdleTimeoutSeconds, 0, null, errors));
        }

        // A float widens to double exactly as the old asset did, so the same file plays the same.
        private static double PacingValue(float value, string field, double fallback, double lowest, double? highest, List<string> errors)
        {
            string instead = $"The game uses {Number(fallback)} for now.";
            if (float.IsNaN(value))
                errors.Add($"pacing: {field} is missing. Add \"{field}\": {Number(fallback)}. {instead}");
            else if (highest.HasValue && (value < lowest || value > highest.Value))
                errors.Add($"pacing: {field} {Number(value)} is outside {Number(lowest)} to {Number(highest.Value)}. " +
                    $"Use a value such as {Number(fallback)}. {instead}");
            else if (value < lowest)
                errors.Add(lowest == 0
                    ? $"pacing: {field} {Number(value)} is negative. Use 0 or more. {instead}"
                    : $"pacing: {field} {Number(value)} is below {Number(lowest)}. Use {Number(lowest)} or more. {instead}");
            else return value;
            return fallback;
        }

        private static void CheckCalls(CallEntry[] entries, List<ICallerType> callTypes, List<string> errors)
        {
            if (entries == null || entries.Length == 0)
            {
                errors.Add("The calls section is missing or empty, so no calls would ring. Add \"calls\": [ ... ] with at least " +
                    "one call type, such as type.regular; see Docs/EconomyFile.md.");
                return;
            }
            var ids = new Dictionary<string, int>(StringComparer.Ordinal);
            bool anySpawns = false;
            for (int i = 0; i < entries.Length; i++)
            {
                CallTypeRecord record = CheckCall(i, entries[i], ids, errors);
                if (record == null) continue;
                callTypes.Add(record);
                if (record.SpawnWeight > 0) anySpawns = true;
            }
            if (!anySpawns)
                errors.Add("No call type has a spawnWeight above 0, so no calls would ever ring. Give one a weight, such as 1.");
        }

        private static CallTypeRecord CheckCall(int index, CallEntry entry, Dictionary<string, int> ids, List<string> errors)
        {
            string id = entry?.id?.Trim() ?? string.Empty;
            string where = id.Length == 0 ? $"Call type {index + 1}" : $"Call type {index + 1} \"{id}\"";
            if (entry == null)
            {
                errors.Add($"{where} is empty. Fill it in or remove it.");
                return null;
            }
            int errorsBefore = errors.Count;

            if (id.Length == 0)
                errors.Add($"{where}: the id is empty. Give it an id such as type.vip; the people file's \"type\" uses it.");
            else if (ids.TryGetValue(id, out int first))
                errors.Add($"{where}: this id is already used by call type {first + 1}. Every id must be different.");
            else ids.Add(id, index);

            if (string.IsNullOrWhiteSpace(entry.name))
                errors.Add($"{where}: the name is empty. Type a short name, such as \"Regular\".");
            if (float.IsNaN(entry.spawnWeight))
                errors.Add($"{where}: spawnWeight is missing. Add how often this type rings compared with the others, " +
                    "such as \"spawnWeight\": 1.");
            else if (entry.spawnWeight < 0)
                errors.Add($"{where}: spawnWeight {Number(entry.spawnWeight)} is negative. Use 0 or more; 0 means it never rings.");

            FloatRange rate = Range(where, "rateCentsPerSecond", entry.rateCentsPerSecond, null, false, "{ \"min\": 3, \"max\": 8 }", errors);
            FloatRange fee = Range(where, "feeCents", entry.feeCents, null, false, "{ \"min\": 5, \"max\": 15 }", errors);
            FloatRange duration = Range(where, "durationSeconds", entry.durationSeconds, null, true, "{ \"min\": 15, \"max\": 30 }", errors);
            FloatRange callback = Range(where, "callbackDelaySeconds", entry.callbackDelaySeconds, DefaultCallbackDelay, false,
                "{ \"min\": 20, \"max\": 40 }", errors);
            NotNegative(where, "responseTimeSeconds", entry.responseTimeSeconds, errors);
            NotNegative(where, "fineMissedCents", entry.fineMissedCents, errors);
            NotNegative(where, "fineEarlyDisconnectCents", entry.fineEarlyDisconnectCents, errors);
            NotNegative(where, "maxSimultaneous", entry.maxSimultaneous, errors);
            if (errors.Count > errorsBefore) return null;

            return new CallTypeRecord(id, entry.name.Trim(), entry.spawnWeight, rate, fee, duration, callback)
            {
                ResponseTimeSeconds = entry.responseTimeSeconds,
                FineMissedCents = entry.fineMissedCents,
                FineEarlyDisconnectCents = entry.fineEarlyDisconnectCents,
                CallbackAfterMiss = entry.callbackAfterMiss,
                CallbackAfterDisconnect = entry.callbackAfterDisconnect,
                MaxSimultaneous = entry.maxSimultaneous,
                EveryoneCalls = entry.everyoneCalls,
                RecipientTag = entry.recipientTag?.Trim() ?? string.Empty
            };
        }

        /// <param name="fallback">Used when the file leaves the range out; null makes the range required.</param>
        private static FloatRange Range(string where, string field, RangeEntry range, FloatRange? fallback, bool aboveZero,
            string example, List<string> errors)
        {
            if (range == null || range.IsMissing)
            {
                if (fallback.HasValue) return fallback.Value;
                errors.Add($"{where}: {field} is missing. Add it as \"{field}\": {example}.");
                return default;
            }
            if (range.IsPartial)
                errors.Add($"{where}: {field} needs both a min and a max, such as {example}.");
            else if (range.min > range.max)
                errors.Add($"{where}: {field} min {Number(range.min)} is above its max {Number(range.max)}. Swap them.");
            else if (aboveZero && range.min <= 0)
                errors.Add($"{where}: {field} min {Number(range.min)} must be above 0. Raise it.");
            else if (range.min < 0)
                errors.Add($"{where}: {field} min {Number(range.min)} is negative. Use 0 or more.");
            return new FloatRange(range.min, range.max);
        }

        private static void NotNegative(string where, string field, double value, List<string> errors)
        {
            if (value < 0) errors.Add($"{where}: {field} {Number(value)} is negative. Use 0 or more; 0 means off.");
        }

        private static UpgradeRecord Check(int index, UpgradeEntry entry, Dictionary<string, int> ids,
            Dictionary<UpgradeKind, UpgradeRecord> lastInChain, ChainRules rules, List<string> errors)
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
            int wires = 0;
            if (type.Kind == UpgradeKind.ExtraWires)
            {
                wires = entry.wires;
                lastInChain.TryGetValue(type.Kind, out UpgradeRecord previous);
                if (wires <= rules.StartingCords)
                    errors.Add($"{where}: wires {wires} is not above the {rules.StartingCords} cords the game starts with. " +
                        $"wires is the total number of cords, so use {rules.StartingCords + 1} or more.");
                else if (wires > StatSheet.MaxCords)
                    errors.Add($"{where}: wires {wires} is more than the tray holds. Use {StatSheet.MaxCords} or less.");
                else if (previous != null && wires <= previous.Wires)
                    errors.Add($"{where}: wires {wires} is not higher than {previous.Id}'s {previous.Wires}. Values in a chain must grow.");
            }
            BoardSize boardSize = default;
            int clientSlots = 0;
            if (type.Kind == UpgradeKind.ExtraClients)
            {
                lastInChain.TryGetValue(type.Kind, out UpgradeRecord previous);
                boardSize = CheckBoardSize(where, entry.boardSize, previous, rules, errors);
                clientSlots = CheckClientSlots(where, entry.clientSlots, previous, rules, errors);
            }
            if (type.Kind == UpgradeKind.Secret && errors.Count == errorsBefore)
            {
                if (rules.SecretAt >= 0)
                    errors.Add($"{where}: there is already a secret upgrade (upgrade {rules.SecretAt + 1}). Keep exactly one secret entry.");
                else rules.SecretAt = index;
            }
            if (type.Kind == UpgradeKind.Notepad && errors.Count == errorsBefore)
            {
                if (rules.NotepadAt >= 0)
                    errors.Add($"{where}: there is already a notepad (upgrade {rules.NotepadAt + 1}). Keep at most one notepad entry.");
                else rules.NotepadAt = index;
            }
            if (errors.Count > errorsBefore) return null;
            return new UpgradeRecord(id, type.Kind, entry.name.Trim(), entry.description?.Trim() ?? string.Empty,
                entry.costCents, multiplier, wires, boardSize, clientSlots);
        }

        // Each entry sets the open board; along the chain, starting from startingBoard, it only grows.
        private static BoardSize CheckBoardSize(string where, BoardSizeEntry entry, UpgradeRecord previous, ChainRules rules,
            List<string> errors)
        {
            const string example = "{ \"cols\": 4, \"rows\": 4 }";
            if (entry == null || entry.IsMissing)
            {
                errors.Add($"{where}: boardSize is missing. Add the part of the board this upgrade opens, such as \"boardSize\": {example}.");
                return default;
            }
            if (entry.IsPartial)
            {
                errors.Add($"{where}: boardSize needs both cols and rows, such as {example}.");
                return default;
            }
            var size = new BoardSize(entry.cols, entry.rows);
            if (!size.IsValid)
            {
                errors.Add($"{where}: boardSize {size} is outside the board. cols and rows must each be 1 to {BoardSize.MaxSide}.");
                return size;
            }
            BoardSize before = previous != null ? previous.BoardSize : rules.StartingBoard;
            string beforeName = previous != null ? $"{previous.Id}'s" : "the startingBoard";
            if (size.Cols < before.Cols || size.Rows < before.Rows)
                errors.Add($"{where}: boardSize {size} is smaller than {beforeName} {before}. The board can only grow along the chain, " +
                    "so neither cols nor rows may go down.");
            else if (size.Equals(before))
                errors.Add($"{where}: boardSize {size} is the same as {beforeName} {before}. The board must grow: raise cols, rows or both.");
            return size;
        }

        private static int CheckClientSlots(string where, int slots, UpgradeRecord previous, ChainRules rules, List<string> errors)
        {
            if (slots == EconomyFileData.Unset)
            {
                errors.Add($"{where}: clientSlots is missing. Add how many caller slots are open after this upgrade, such as \"clientSlots\": 4.");
                return 0;
            }
            if (slots < 1 || slots > StatSheet.MaxClientSlots)
            {
                errors.Add($"{where}: clientSlots {slots} is outside 1 to {StatSheet.MaxClientSlots}, the caller panel's slots.");
                return slots;
            }
            int before = previous != null ? previous.ClientSlots : rules.StartingClientSlots;
            string beforeName = previous != null ? $"{previous.Id}'s" : "startingClientSlots";
            if (slots < before)
                errors.Add($"{where}: clientSlots {slots} is fewer than {beforeName} {before}. Caller slots can only grow along the chain.");
            return slots;
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

        // What the checks remember from one entry to the next.
        private sealed class ChainRules
        {
            public ChainRules(int startingCords, BoardSize startingBoard, int startingClientSlots)
            {
                StartingCords = startingCords > 0 ? startingCords : StatSheet.DefaultStartingCords;
                StartingBoard = startingBoard;
                StartingClientSlots = startingClientSlots;
            }

            public int StartingCords { get; }
            public BoardSize StartingBoard { get; }
            public int StartingClientSlots { get; }
            public int SecretAt { get; set; } = -1;
            public int NotepadAt { get; set; } = -1;
        }

        private sealed class TypeInfo
        {
            public TypeInfo(string name, UpgradeKind kind)
            {
                Name = name;
                Kind = kind;
            }

            public string Name { get; }
            public UpgradeKind Kind { get; }
        }
    }
}
