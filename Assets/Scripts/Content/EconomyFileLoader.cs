using System;
using System.Collections.Generic;
using System.Globalization;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Parses and checks the designer's economy file (D39, D40, D47, D49, D52; Design 6.4): starting money, cords,
    /// board and caller slots, the call queues and upgrades. An entry with a problem is left out, and a bad starting
    /// value falls back to its default; each is reported in plain words naming the entry and the fix, and the game
    /// still starts.
    /// </summary>
    public static class EconomyFileLoader
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly TypeInfo[] Types =
        {
            new TypeInfo("extra_income", UpgradeKind.ExtraIncome),
            new TypeInfo("extra_wires", UpgradeKind.ExtraWires),
            new TypeInfo("extra_clients", UpgradeKind.ExtraClients),
            new TypeInfo("secret", UpgradeKind.Secret)
        };

        /// <param name="calls">Calls.json, which the pools refer to; null skips the pools and their checks.</param>
        public static EconomyCatalog Load(string json, CallsCatalog calls = null)
        {
            var errors = new List<string>();
            var upgrades = new List<IUpgrade>();
            IReadOnlyList<CallScript> startPool = Array.Empty<CallScript>();
            long startingCents = 0;
            int startingCords = StatSheet.DefaultStartingCords;
            BoardSize startingBoard = BoardSize.Full;
            int startingClientSlots = StatSheet.MaxClientSlots;
            var quickTip = new QuickConnectTip(QuickConnectTip.DefaultSeconds, QuickConnectTip.DefaultCents);
            EconomyFileData data = Parse(json, errors);
            if (data != null)
            {
                startingCents = CheckStartingCents(data.startingCents, errors);
                startingCords = CheckStartingCords(data.startingCords, errors);
                startingBoard = CheckStartingBoard(data.startingBoard, errors);
                startingClientSlots = CheckStartingClientSlots(data.startingClientSlots, errors);
                quickTip = CheckQuickConnect(data, errors);
                CheckRemovedSections(data, errors);

                var ids = new Dictionary<string, int>(StringComparer.Ordinal);
                var lastInChain = new Dictionary<UpgradeKind, UpgradeRecord>();
                var rules = new ChainRules(startingCords, startingBoard, startingClientSlots);
                var stageEntries = new List<KeyValuePair<UpgradeRecord, UpgradeEntry>>();
                for (int i = 0; i < data.upgrades.Length; i++)
                {
                    UpgradeRecord record = Check(i, data.upgrades[i], ids, lastInChain, rules, errors);
                    if (record == null) continue;
                    upgrades.Add(record);
                    lastInChain[record.Kind] = record;
                    if (record.Kind == UpgradeKind.ExtraClients)
                        stageEntries.Add(new KeyValuePair<UpgradeRecord, UpgradeEntry>(record, data.upgrades[i]));
                }
                if (rules.SecretAt < 0)
                    errors.Add("There is no secret upgrade. Add exactly one entry with \"type\": \"secret\": " +
                        "the most expensive one, which the player saves for.");
                if (calls != null)
                {
                    startPool = CheckPool("startPool", "startPool", data.startPool, startingBoard, "the startingBoard",
                        startingClientSlots, "startingClientSlots", calls, errors);
                    foreach (KeyValuePair<UpgradeRecord, UpgradeEntry> stage in stageEntries)
                    {
                        UpgradeRecord record = stage.Key;
                        string where = $"Upgrade \"{record.Id}\"";
                        record.StagePool = CheckPool(where + ": stagePool", "stagePool", stage.Value.stagePool, record.BoardSize,
                            record.Id, record.ClientSlots, record.Id + "'s clientSlots", calls, errors);
                    }
                }
            }
            return new EconomyCatalog(startingCents, startingCords, startingBoard, startingClientSlots, startPool, upgrades, errors,
                quickTip);
        }

        /// <summary>
        /// Resolves a pool's call ids. Unknown calls and calls with a jack outside the stage's board are left out, because
        /// the player could never connect them; the queue plays the rest in order.
        /// </summary>
        private static IReadOnlyList<CallScript> CheckPool(string where, string field, string[] ids, BoardSize board,
            string boardName, int slots, string slotsName, CallsCatalog calls, List<string> errors)
        {
            const string example = "[ \"call_001\", \"call_002\" ]";
            if (ids == null)
            {
                errors.Add($"{where} is missing. Add the call ids from Calls.json this stage plays in order, such as \"{field}\": {example}.");
                return Array.Empty<CallScript>();
            }
            if (ids.Length == 0)
            {
                errors.Add($"{where} is empty, so no calls would ring. Add call ids from Calls.json, such as {example}.");
                return Array.Empty<CallScript>();
            }
            var pool = new List<CallScript>(ids.Length);
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            foreach (string raw in ids)
            {
                string id = raw?.Trim() ?? string.Empty;
                CallScript call = calls.Find(id);
                if (id.Length == 0)
                    errors.Add($"{where} has an empty call id. Remove it or type the id of a call in Calls.json.");
                else if (call == null && calls.IsLeftOut(id))
                    errors.Add($"{where}: {id} has a problem in Calls.json (see the Calls file messages), so it is left out of the queue.");
                else if (call == null)
                    errors.Add($"{where}: {id} is not in Calls.json. Check the spelling, or add the call to Calls.json.");
                else if (!board.Contains(call.From) || !board.Contains(call.To))
                {
                    JackId outside = board.Contains(call.From) ? call.To : call.From;
                    errors.Add($"{where}: {id} uses {outside}, but {boardName} opens only {board}. The player could never connect it, " +
                        "so it is left out. Move it to the pool of a bigger board, or give it jacks on this one.");
                }
                else
                {
                    pool.Add(call);
                    distinct.Add(id);
                }
            }
            if (pool.Count > 0 && distinct.Count < slots)
            {
                int empty = slots - distinct.Count;
                errors.Add($"{where} has {distinct.Count} different call{(distinct.Count == 1 ? "" : "s")} for {slots} caller slots " +
                    $"({slotsName}), so {empty} slot{(empty == 1 ? "" : "s")} would stay empty. A note only: add more calls if you want every slot busy.");
            }
            else if (pool.Count == 0)
                errors.Add($"{where} has no usable calls, so no calls would ring at this stage. Fix the calls named above.");
            return pool;
        }

        private static void CheckRemovedSections(EconomyFileData data, List<string> errors)
        {
            if (data.pacing != null && data.pacing.IsPresent)
                errors.Add("The pacing section is no longer used: calls now come from the queues (startPool and each " +
                    "extra_clients's stagePool). Remove the section.");
            if (data.calls != null)
                errors.Add("The calls section (call types) is no longer used: every call is written in Calls.json. " +
                    "Remove the section.");
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

        // A session starts with 1 to 6 cords; the input inventory is trimmed before binding.
        private static int CheckStartingCords(int cords, List<string> errors)
        {
            const int fallback = StatSheet.DefaultStartingCords;
            if (cords == EconomyFileData.Unset) return fallback;
            if (cords < 1)
                errors.Add($"startingCords {cords} is below the minimum of 1 cord. " +
                    $"Use 1 to {StatSheet.MaxCords}. The game starts with {fallback} for now.");
            else if (cords > StatSheet.MaxCords)
                errors.Add($"startingCords {cords} is more than the tray holds. " +
                    $"Use 1 to {StatSheet.MaxCords}. The game starts with {fallback} for now.");
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

        // The tip (D59) is optional: a field left out gets its default with a note; a wrong value is an error.
        private static QuickConnectTip CheckQuickConnect(EconomyFileData data, List<string> errors)
        {
            const string off = "0 turns the tip off.";
            double seconds = QuickConnectTip.DefaultSeconds;
            double value = data.quickConnectSeconds;
            if (double.IsNaN(value))
                errors.Add($"quickConnectSeconds is missing, so the quick-connect tip uses {Number(seconds)} seconds. " +
                    $"A note only: add \"quickConnectSeconds\": {Number(seconds)}, the seconds the player has after taking a card " +
                    "to earn the tip, to choose your own.");
            else if (value < 0)
                errors.Add($"quickConnectSeconds {Number(value)} is negative. Use 0 or more seconds; {off} " +
                    $"The game uses {Number(seconds)} for now.");
            else seconds = value;

            long cents = QuickConnectTip.DefaultCents;
            value = data.quickConnectTipCents;
            if (double.IsNaN(value))
                errors.Add($"quickConnectTipCents is missing, so a quick connection pays a {cents}¢ tip. " +
                    $"A note only: add \"quickConnectTipCents\": {cents}, the tip in cents, to choose your own.");
            else if (value < 0)
                errors.Add($"quickConnectTipCents {Number(value)} is negative. Use 0 or more, in cents; {off} " +
                    $"The game uses {cents} for now.");
            else if (Math.Abs(value - Math.Round(value)) > 1e-9)
                errors.Add($"quickConnectTipCents {Number(value)} must be whole cents, such as {Number(Math.Round(value))}. " +
                    $"The game uses {cents} for now.");
            else cents = (long)Math.Round(value);
            return new QuickConnectTip(seconds, cents);
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

            if (string.Equals(entry.type?.Trim(), "notepad", StringComparison.Ordinal))
            {
                errors.Add($"{where}: the notepad upgrade is no longer used, so this entry is left out. Remove it from the file.");
                return null;
            }
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
