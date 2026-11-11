using System;
using System.Collections.Generic;

namespace Switchboard.Domain.Tests
{
    /// <summary>Hand-written calls for tests (D51).</summary>
    internal static class Scripts
    {
        public static CallScript Call(string id, string from, string to, double centsPerSecond = 5, double durationSeconds = 20,
            double connectCents = 0, double? patienceSeconds = null) =>
            new CallScript(id, "Caller " + id, "caller-01", "Connect me, please.", new JackId(from), new JackId(to),
                durationSeconds, connectCents, centsPerSecond, patienceSeconds: patienceSeconds);

        /// <summary>30 s to connect (D64); it pays 30¢ + 7¢/s for 15 s, 135¢ in all, so a 1.5 factor fines 203¢ (D82).</summary>
        public static CallScript Emergency(string id, string from, string to, double centsPerSecond = 7, double durationSeconds = 15,
            double connectCents = 30, double responseTimeSeconds = 30) =>
            new CallScript(id, "Caller " + id, "caller-02", "Get me the Doctor!", new JackId(from), new JackId(to),
                durationSeconds, connectCents, centsPerSecond, true, responseTimeSeconds);

        /// <summary>
        /// Count different calls on the board's open part. call_1 joins the first open jack and the one half the open
        /// jacks further on, call_2 the second and the next, and so on, so nearby calls share no jack and the queue's
        /// busy-caller rule (D63) skips none of them.
        /// </summary>
        /// <summary>
        /// Up to four calls for a 4x4 stage, on jacks a 3x3 pool never uses (D1 to A4, D2 to B4, D3 to C4, D4 to C3),
        /// so a bought board's calls never wait for an old card's caller (D63).
        /// </summary>
        public static CallScript[] StagePool(int count, string prefix = "stage_")
        {
            string[,] pairs = { { "D1", "A4" }, { "D2", "B4" }, { "D3", "C4" }, { "D4", "C3" } };
            var pool = new CallScript[count];
            for (int i = 0; i < count; i++) pool[i] = Call(prefix + (i + 1), pairs[i, 0], pairs[i, 1], 5, 20, 10);
            return pool;
        }

        public static CallScript[] Pool(int count, BoardSize board, string prefix = "call_")
        {
            var jacks = new List<string>();
            for (int row = 0; row < board.Rows; row++)
                for (int column = 1; column <= board.Cols; column++)
                    jacks.Add($"{(char)('A' + row)}{column}");
            var pool = new CallScript[count];
            int offset = Math.Max(1, jacks.Count / 2);
            for (int i = 0; i < count; i++)
                pool[i] = Call(prefix + (i + 1), jacks[i % jacks.Count], jacks[(i + offset) % jacks.Count], 5, 20, 10);
            return pool;
        }
    }

    internal sealed class FakeContent : IGameContent
    {
        public long StartingCents { get; set; }
        public BoardSize StartingBoard { get; set; } = BoardSize.Full;
        public int StartingClientSlots { get; set; } = 6;
        public int StartingCords { get; set; } = 3;
        public IReadOnlyList<CallScript> StartPool { get; set; } = Array.Empty<CallScript>();
        // Off by default, so tests that count money exactly don't see a tip.
        public double QuickConnectSeconds { get; set; }
        public long QuickConnectTipCents { get; set; }
        // Callers wait forever by default, so tests that tick long don't see hang-ups (D63).
        public double DefaultPatienceSeconds { get; set; }
        public double EmergencyFineFactor { get; set; } = 1.5;
        public int MaxBusyLineCards { get; set; } = 1;
        public IReadOnlyList<IUpgrade> Upgrades { get; set; } = Array.Empty<IUpgrade>();
    }

    internal sealed class FakeUpgrade : IUpgrade
    {
        public FakeUpgrade(string id, UpgradeKind kind, long costCents, double incomeMultiplier = 0, int wires = 0,
            BoardSize boardSize = default, int clientSlots = 0, IReadOnlyList<CallScript> stagePool = null)
        {
            Id = id;
            Kind = kind;
            CostCents = costCents;
            IncomeMultiplier = incomeMultiplier;
            Wires = wires;
            BoardSize = boardSize;
            ClientSlots = clientSlots;
            StagePool = stagePool ?? Array.Empty<CallScript>();
        }

        public string Id { get; }
        public UpgradeKind Kind { get; }
        public string Name => Id;
        public string Description => string.Empty;
        public long CostCents { get; }
        public double IncomeMultiplier { get; }
        public int Wires { get; }
        public BoardSize BoardSize { get; }
        public int ClientSlots { get; }
        public IReadOnlyList<CallScript> StagePool { get; }

        /// <summary>An extra_clients entry: a square board of the given side and as many caller slots (D49).</summary>
        public static FakeUpgrade BoardUpgrade(string id, long costCents, int side, IReadOnlyList<CallScript> stagePool = null,
            int clientSlots = 0) =>
            new FakeUpgrade(id, UpgradeKind.ExtraClients, costCents, boardSize: new BoardSize(side, side),
                clientSlots: clientSlots > 0 ? clientSlots : side, stagePool: stagePool);

        public static FakeUpgrade WireUpgrade(string id, long costCents, int wires) =>
            new FakeUpgrade(id, UpgradeKind.ExtraWires, costCents, wires: wires);

        /// <summary>The 5b shop of D43 without the Notepad (D54): income, then 4, 5 and 6 cords, and the secret.</summary>
        public static IUpgrade[] FullShop() => new IUpgrade[]
        {
            Income("income_1", 800, 1.5),
            Income("income_2", 2000, 2.0),
            Income("income_3", 5000, 3.0),
            WireUpgrade("wires_1", 500, 4),
            WireUpgrade("wires_2", 800, 5),
            WireUpgrade("wires_3", 1300, 6),
            new FakeUpgrade("secret", UpgradeKind.Secret, 25000)
        };

        public static FakeUpgrade Income(string id, long costCents, double multiplier) =>
            new FakeUpgrade(id, UpgradeKind.ExtraIncome, costCents, multiplier);

        /// <summary>The placeholder shop of D43: income x1.5, x2 and x3 for $8, $20 and $50, and the $250 secret.</summary>
        public static IUpgrade[] PlaceholderShop() => new IUpgrade[]
        {
            Income("income_1", 800, 1.5),
            Income("income_2", 2000, 2.0),
            Income("income_3", 5000, 3.0),
            new FakeUpgrade("secret", UpgradeKind.Secret, 25000)
        };
    }

    internal sealed class FakeSwitchboard : ISwitchboard
    {
        public List<CordId> Returned { get; } = new List<CordId>();
        public List<CordId> Refused { get; } = new List<CordId>();
        public int CordCount { get; set; } = 3;

        public void ReturnToTray(CordId cord) => Returned.Add(cord);
        public void Refuse(CordId cord) => Refused.Add(cord);
    }

    /// <summary>Saves kept in a dictionary instead of PlayerPrefs or the browser (9a).</summary>
    internal sealed class MemoryStorage : ISaveStorage
    {
        public Dictionary<string, string> Keys { get; } = new Dictionary<string, string>();
        public int Writes { get; private set; }

        public string Read(string key) => Keys.TryGetValue(key, out string value) ? value : null;

        public void Write(string key, string value)
        {
            Keys[key] = value;
            Writes++;
        }

        public void Delete(string key) => Keys.Remove(key);
    }

    /// <summary>Three jacks the calls use: A1, B2 and C3. "Ada wants Basil" joins A1 and B2.</summary>
    internal sealed class TestWorld
    {
        public static readonly JackId A1 = new JackId("A1");
        public static readonly JackId B2 = new JackId("B2");
        public static readonly JackId C3 = new JackId("C3");

        private static int nextId;

        public TestWorld()
        {
            AdaCallsBasil = Scripts.Call("ada_basil", "A1", "B2", 5, 20, 10);
            Content = new FakeContent { StartPool = new[] { AdaCallsBasil } };
        }

        /// <summary>The one call in the start pool: A1 to B2, 10¢ + 5¢/s for 20 s.</summary>
        public CallScript AdaCallsBasil { get; }
        public FakeContent Content { get; }

        public CallRequest AdaWantsBasil(double centsPerSecond = 5, double durationSeconds = 20, double connectionFeeCents = 0) =>
            Request(A1, B2, centsPerSecond, durationSeconds, connectionFeeCents);

        public static CallRequest Request(JackId from, JackId to, double centsPerSecond = 5, double durationSeconds = 20,
            double connectionFeeCents = 0) =>
            new CallRequest(new CallScript("test_" + (++nextId), "Caller", "caller-01", "Connect me, please.", from, to,
                durationSeconds, connectionFeeCents, centsPerSecond));
    }

    /// <summary>
    /// Emergencies (D51): people's calls from jacks A1 onwards (Grid.Jack) to Police on F6, Fire Brigade on F5 or
    /// Doctor on F4, with 30 s to connect.
    /// </summary>
    internal sealed class EmergencyWorld
    {
        private static int nextId;

        public static JackId CallerJack(int person) => new JackId(Grid.Jack(person));

        public static JackId ServiceJack(int service) => new JackId(Grid.Jack(35 - service));

        public CallRequest EmergencyRequest(int caller, int service = 0) =>
            new CallRequest(Scripts.Emergency("emergency_" + (++nextId), Grid.Jack(caller), Grid.Jack(35 - service)));

        public CallRequest RegularRequest(int caller, int recipient, double centsPerSecond = 5, double durationSeconds = 20) =>
            new CallRequest(Scripts.Call("regular_" + (++nextId), Grid.Jack(caller), Grid.Jack(recipient), centsPerSecond, durationSeconds));
    }

    internal static class Grid
    {
        /// <summary>The board's jacks row by row: 0 is A1, 5 is A6, 6 is B1, up to 35 for F6.</summary>
        public static string Jack(int index) => $"{(char)('A' + index / 6)}{index % 6 + 1}";
    }
}
