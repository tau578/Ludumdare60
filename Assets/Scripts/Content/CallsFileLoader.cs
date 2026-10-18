using System;
using System.Collections.Generic;
using System.Globalization;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>
    /// Parses and checks the designer's hand-written calls (D51). A call with a problem is left out and reported in
    /// plain words naming the call and the fix; the game still starts, with whatever calls are fine.
    /// </summary>
    public static class CallsFileLoader
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <param name="hasPortrait">Null skips the icon check.</param>
        public static CallsCatalog Load(string json, Func<string, bool> hasPortrait)
        {
            var errors = new List<string>();
            var calls = new List<CallScript>();
            var leftOut = new List<string>();
            CallsFileData data = Parse(json, errors);
            if (data != null)
            {
                var ids = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < data.calls.Length; i++)
                {
                    CallScript call = Check(i, data.calls[i], ids, hasPortrait, errors);
                    if (call != null) calls.Add(call);
                    else if (!string.IsNullOrWhiteSpace(data.calls[i]?.id)) leftOut.Add(data.calls[i].id.Trim());
                }
            }
            return new CallsCatalog(calls, leftOut, errors);
        }

        /// <summary>A jack label on the 6x6 board, such as "C3": a row letter A-F, then a column 1-6. Null if it isn't one.</summary>
        public static string JackLabel(string text)
        {
            string label = text?.Trim().ToUpperInvariant();
            if (label == null || label.Length != 2) return null;
            int row = label[0] - 'A';
            int column = label[1] - '0';
            return row >= 0 && row < BoardSize.MaxSide && column >= 1 && column <= BoardSize.MaxSide ? label : null;
        }

        private static CallsFileData Parse(string json, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add("The calls file is missing or empty, so no calls will ring. It belongs at Assets/Configs/Calls.json " +
                    "and in GameConfig's Calls File slot.");
                return null;
            }
            CallsFileData data;
            try
            {
                data = JsonUtility.FromJson<CallsFileData>(json);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"The calls file is not valid JSON (check for a missing comma, quote or bracket): {ex.Message}");
                return null;
            }
            if (data == null)
            {
                errors.Add("The calls file must contain a JSON object, starting with { and ending with }.");
                return null;
            }
            if (data.calls == null || data.calls.Length == 0)
            {
                errors.Add("The calls file has no calls. Add \"calls\": [ ... ] with one call per line; see Docs/EconomyFile.md.");
                data.calls = Array.Empty<CallFileEntry>();
            }
            return data;
        }

        private static CallScript Check(int index, CallFileEntry entry, Dictionary<string, int> ids,
            Func<string, bool> hasPortrait, List<string> errors)
        {
            string id = entry?.id?.Trim() ?? string.Empty;
            string where = id.Length == 0 ? $"Call {index + 1}" : $"Call {index + 1} \"{id}\"";
            if (entry == null)
            {
                errors.Add($"{where} is empty. Fill it in or remove it.");
                return null;
            }
            int errorsBefore = errors.Count;

            if (id.Length == 0)
                errors.Add($"{where}: the id is empty. Give it an id such as call_001; Economy.json's pools use it.");
            else if (ids.TryGetValue(id, out int first))
                errors.Add($"{where}: this id is already used by call {first + 1}. Every id must be different.");
            else ids.Add(id, index);

            string name = Text(where, "name", entry.name, "the caller's name, such as \"Ada Moss\"", errors);
            string icon = Text(where, "icon", entry.icon, "a portrait name, such as \"caller-01\"", errors);
            if (icon != null && hasPortrait != null && !hasPortrait(icon))
                errors.Add($"{where}: icon \"{icon}\" is not in GameConfig's portrait list. Use a portrait name from that list, " +
                    "such as caller-01.");
            string message = Text(where, "message", entry.message, "what the caller says, such as \"Connect me with Ada Moss, please.\"", errors);
            string from = Jack(where, "from", entry.from, errors);
            string to = Jack(where, "to", entry.to, errors);
            if (from != null && from == to)
                errors.Add($"{where}: from and to are both {from}. A call joins two different jacks.");

            if (double.IsNaN(entry.durationSeconds))
                errors.Add($"{where}: durationSeconds is missing. Add how many seconds the call lasts once connected, such as \"durationSeconds\": 20.");
            else if (entry.durationSeconds <= 0)
                errors.Add($"{where}: durationSeconds {Number(entry.durationSeconds)} must be above 0. Use a number of seconds such as 20.");
            Cents(where, "connectCents", entry.connectCents, true, "the fee paid on connecting, such as \"connectCents\": 10", errors);
            Cents(where, "centsPerSecond", entry.centsPerSecond, false, "the pay per second, such as \"centsPerSecond\": 5", errors);

            double response = 0;
            long penalty = 0;
            if (entry.emergency)
            {
                if (double.IsNaN(entry.responseTimeSeconds))
                    errors.Add($"{where}: an emergency needs responseTimeSeconds, the seconds the player has to connect it, " +
                        "such as \"responseTimeSeconds\": 30.");
                else if (entry.responseTimeSeconds <= 0)
                    errors.Add($"{where}: responseTimeSeconds {Number(entry.responseTimeSeconds)} must be above 0. Use a number of seconds such as 30.");
                else response = entry.responseTimeSeconds;
                if (Cents(where, "penaltyCents", entry.penaltyCents, true,
                        "the fine for missing it, such as \"penaltyCents\": 200 (0 for no fine)", errors))
                    penalty = (long)entry.penaltyCents;
            }
            if (errors.Count > errorsBefore) return null;

            // A note only: the call still plays, as a regular one.
            if (!entry.emergency && (!double.IsNaN(entry.responseTimeSeconds) || !double.IsNaN(entry.penaltyCents)))
                errors.Add($"{where}: responseTimeSeconds and penaltyCents are only used for emergencies. Add \"emergency\": true, " +
                    "or remove them.");

            return new CallScript(id, name, icon, message, new JackId(from), new JackId(to), entry.durationSeconds,
                entry.connectCents, entry.centsPerSecond, entry.emergency, response, penalty);
        }

        private static string Text(string where, string field, string value, string example, List<string> errors)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            errors.Add($"{where}: {field} is empty. Add {example}.");
            return null;
        }

        private static string Jack(string where, string field, string value, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{where}: {field} is empty. Add a jack label such as \"C3\": a row letter A to F, then a column 1 to 6.");
                return null;
            }
            string label = JackLabel(value);
            if (label == null)
                errors.Add($"{where}: {field} \"{value.Trim()}\" is not a jack on the board. Use a row letter A to F, then a column 1 to 6, " +
                    "such as \"C3\".");
            return label;
        }

        // Returns true when the value is usable.
        private static bool Cents(string where, string field, double value, bool whole, string example, List<string> errors)
        {
            if (double.IsNaN(value))
                errors.Add($"{where}: {field} is missing. Add {example}.");
            else if (value < 0)
                errors.Add($"{where}: {field} {Number(value)} is negative. Use 0 or more, in cents.");
            else if (whole && Math.Abs(value - Math.Round(value)) > 1e-9)
                errors.Add($"{where}: {field} {Number(value)} must be whole cents, such as {Number(Math.Round(value))}.");
            else return true;
            return false;
        }

        private static string Number(double value) => value.ToString("0.###", Invariant);
    }
}
