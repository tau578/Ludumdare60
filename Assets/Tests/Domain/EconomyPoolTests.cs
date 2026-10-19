using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    /// <summary>Economy.json's starting values, call queues (D52) and the sections Step 7 removed.</summary>
    public sealed class EconomyPoolTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        // Calls on a 3x3 start (S1-S3), on 4x4 (M1-M3) and one on 5x5 (L1).
        private static readonly CallsCatalog Calls = new CallsCatalog(new[]
        {
            Scripts.Call("s1", "A1", "B2"), Scripts.Call("s2", "C3", "A2"), Scripts.Call("s3", "B1", "C2"),
            Scripts.Call("m1", "D4", "A1"), Scripts.Call("m2", "A4", "B4"), Scripts.Emergency("m3", "B3", "D4"),
            Scripts.Call("l1", "E5", "A1")
        }, new[] { "broken" }, null);

        private const string Start = "'startingCords': 2, 'startingBoard': { 'cols': 3, 'rows': 3 }, 'startingClientSlots': 2";

        private const string Secret = "{ 'id': 'secret', 'type': 'secret', 'name': '???', 'costCents': 25000 }";

        private static string Board(string id, int side, int slots, string pool) =>
            $"{{ 'id': '{id}', 'type': 'extra_clients', 'name': 'Bigger board', 'costCents': 1000, " +
            $"'boardSize': {{ 'cols': {side}, 'rows': {side} }}, 'clientSlots': {slots}{(pool != null ? ", 'stagePool': " + pool : "")} }}";

        private static string File(string start, string startPool, params string[] upgrades) =>
            ("{ " + start + (startPool != null ? ", 'startPool': " + startPool : "") +
                $", 'upgrades': [ {string.Join(", ", upgrades)}{(upgrades.Length > 0 ? ", " : "")}{Secret} ] }}").Replace('\'', '"');

        private static EconomyCatalog Load(string startPool, params string[] upgrades) =>
            EconomyFileLoader.Load(File(Start, startPool, upgrades), Calls);

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        private static List<string> Ids(IReadOnlyList<CallScript> pool)
        {
            var ids = new List<string>();
            foreach (CallScript call in pool) ids.Add(call.Id);
            return ids;
        }

        [Test]
        public void PoolsResolveToTheCallsInOrder()
        {
            EconomyCatalog catalog = Load("['s2', 's1', 's3']", Board("board_1", 4, 3, "['m1', 'm2', 'm3', 's1']"));
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(Ids(catalog.StartPool), Is.EqualTo(new[] { "s2", "s1", "s3" }));
            Assert.That(Ids(catalog.Upgrades[0].StagePool), Is.EqualTo(new[] { "m1", "m2", "m3", "s1" }));
            Assert.That(catalog.Upgrades[1].StagePool, Is.Empty, "only extra_clients entries have a pool");
        }

        [Test]
        public void ReportsAMissingOrEmptyStartPool()
        {
            AssertSingleError(Load(null), "startPool is missing", "\"startPool\": [");
            AssertSingleError(Load("[]"), "startPool is empty");
        }

        [Test]
        public void ReportsAnUnknownCallAndLeavesItOut()
        {
            EconomyCatalog catalog = Load("['s1', 'nobody', 's2']");
            AssertSingleError(catalog, "startPool: nobody is not in Calls.json");
            Assert.That(Ids(catalog.StartPool), Is.EqualTo(new[] { "s1", "s2" }));
        }

        [Test]
        public void ACallWithAProblemInCallsJsonIsNamedAsSuch() =>
            AssertSingleError(Load("['s1', 'broken', 's2']"), "startPool: broken has a problem in Calls.json");

        [Test]
        public void ACallInTheStartPoolMustFitTheStartingBoard()
        {
            EconomyCatalog catalog = Load("['s1', 'm1', 's2']");
            AssertSingleError(catalog, "startPool: m1 uses D4, but the startingBoard opens only 3x3");
            Assert.That(Ids(catalog.StartPool), Is.EqualTo(new[] { "s1", "s2" }), "it could never be connected, so it is left out");
        }

        [Test]
        public void ACallInAStagePoolMustFitThatEntrysBoard()
        {
            EconomyCatalog catalog = Load("['s1', 's2']", Board("board_1", 4, 3, "['m1', 'l1', 'm2', 'm3']"));
            AssertSingleError(catalog, "Upgrade \"board_1\": stagePool: l1 uses E5, but board_1 opens only 4x4");
            Assert.That(Ids(catalog.Upgrades[0].StagePool), Is.EqualTo(new[] { "m1", "m2", "m3" }));
        }

        [Test]
        public void EveryExtraClientsNeedsAStagePoolButTheEntryStays()
        {
            EconomyCatalog catalog = Load("['s1', 's2']", Board("board_1", 4, 3, null));
            AssertSingleError(catalog, "Upgrade \"board_1\": stagePool is missing");
            Assert.That(catalog.Upgrades[0].Id, Is.EqualTo("board_1"), "the board can still be bought; the queue stays as it was");
            Assert.That(catalog.Upgrades[0].StagePool, Is.Empty);
            AssertSingleError(Load("['s1', 's2']", Board("board_1", 4, 3, "[]")), "stagePool is empty");
        }

        [Test]
        public void APoolWithFewerCallsThanSlotsGetsANote()
        {
            AssertSingleError(Load("['s1']"), "startPool has 1 different call for 2 caller slots", "1 slot would stay empty");
            AssertSingleError(Load("['s1', 's2']", Board("board_1", 4, 3, "['m1', 'm1']")),
                "stagePool has 1 different call for 3 caller slots (board_1's clientSlots), so 2 slots would stay empty");
        }

        [Test]
        public void WithoutACallsFileThePoolsAreNotChecked()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(File(Start, null));
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.StartPool, Is.Empty);
        }

        [Test]
        public void TheRemovedSectionsAndTheNotepadAreNamedAsNoLongerUsed()
        {
            string json = File(Start + ", 'pacing': { 'firstCallDelaySeconds': 2, 'baseIntervalSeconds': 8 }, " +
                "'calls': [ { 'id': 'type.regular' } ]", "['s1', 's2']",
                "{ 'id': 'notepad', 'type': 'notepad', 'name': 'Notepad', 'costCents': 300 }");
            EconomyCatalog catalog = EconomyFileLoader.Load(json, Calls);

            Assert.That(catalog.Errors.Count, Is.EqualTo(3), string.Join("\n", catalog.Errors));
            Assert.That(catalog.Errors[0], Does.Contain("pacing section is no longer used"));
            Assert.That(catalog.Errors[1], Does.Contain("calls section (call types) is no longer used").And.Contain("Calls.json"));
            Assert.That(catalog.Errors[2], Does.Contain("\"notepad\": the notepad upgrade is no longer used"));
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(1), "only the secret");
        }

        [Test]
        public void StartingValuesLeftOutAreZeroCentsAndTwoCordsWithNoMessage()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(File("'startingBoard': { 'cols': 3, 'rows': 3 }, 'startingClientSlots': 2", null));
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.StartingCents, Is.EqualTo(0));
            Assert.That(catalog.StartingCords, Is.EqualTo(2));
        }

        [Test]
        public void StartingCentsAndCordsComeFromTheFile()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(File(Start.Replace("'startingCords': 2", "'startingCents': 500, 'startingCords': 6"), null));
            Assert.That(catalog.StartingCents, Is.EqualTo(500));
            Assert.That(catalog.StartingCords, Is.EqualTo(6));
        }

        [Test]
        public void OneStartingCordAllowsTheTwoCordUpgrade()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(File(Start.Replace("'startingCords': 2", "'startingCords': 1"), null,
                "{ 'id': 'wires_1', 'type': 'extra_wires', 'name': 'Extra cord', 'costCents': 150, 'wires': 2 }"));
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.StartingCords, Is.EqualTo(1));
            Assert.That(catalog.Upgrades, Has.Some.Matches<IUpgrade>(upgrade => upgrade.Id == "wires_1" && upgrade.Wires == 2));
        }

        [TestCase(0, "below the minimum of 1 cord")]
        [TestCase(-2, "below the minimum of 1 cord")]
        [TestCase(7, "more than the tray holds")]
        public void StartingCordsOutsideOneToSixFallBackToTwo(int cords, string message)
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(File(Start.Replace("'startingCords': 2", "'startingCords': " + cords), null));
            Assert.That(catalog.StartingCords, Is.EqualTo(2));
            AssertSingleError(catalog, "startingCords " + cords, message, "Use 1 to 6");
        }

        [Test]
        public void ReportsNegativeStartingCents()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(File(Start + ", 'startingCents': -5", null));
            AssertSingleError(catalog, "startingCents -5 is negative");
            Assert.That(catalog.StartingCents, Is.EqualTo(0));
        }

        [Test]
        public void WiresChecksUseTheFilesStartingCords() =>
            AssertSingleError(EconomyFileLoader.Load(File(Start.Replace("'startingCords': 2", "'startingCords': 4"), null,
                    "{ 'id': 'wires_1', 'type': 'extra_wires', 'name': 'Extra cord', 'costCents': 500, 'wires': 4 }")),
                "Upgrade 1 \"wires_1\": wires 4 is not above the 4 cords the game starts with", "5 or more");

        [Test]
        public void TheRealFilesHaveTheShippedQueues()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            CallsCatalog calls = config.LoadCalls();
            EconomyCatalog economy = config.LoadEconomy(calls);
            Assert.That(economy.Errors, Is.Empty);
            Assert.That(economy.StartingClientSlots, Is.EqualTo(2));
            Assert.That(economy.StartPool.Count, Is.EqualTo(8));
            Assert.That(economy.StartPool[0].Id, Is.EqualTo("call_001"), "a simple first call for the guide");
            foreach (CallScript call in economy.StartPool) Assert.That(call.IsEmergency, Is.False, call.Id);

            var boards = new List<IUpgrade>();
            foreach (IUpgrade upgrade in economy.Upgrades)
                if (upgrade.Kind == UpgradeKind.ExtraClients) boards.Add(upgrade);
            int[] sizes = { 10, 12, 14 };
            string[] services = { "D4", "E5", "B6" };
            for (int i = 0; i < 3; i++)
            {
                IReadOnlyList<CallScript> pool = boards[i].StagePool;
                Assert.That(pool.Count, Is.EqualTo(sizes[i]), boards[i].Id);
                int emergencies = 0;
                bool newService = false;
                foreach (CallScript call in pool)
                {
                    if (!call.IsEmergency) continue;
                    emergencies++;
                    if (call.To.Label == services[i]) newService = true;
                }
                Assert.That(emergencies, Is.GreaterThanOrEqualTo(2), boards[i].Id);
                Assert.That(newService, Is.True, $"{boards[i].Id} adds calls for the service on {services[i]}");
            }
        }

        [Test]
        public void TheRealFilesPassValidateContent()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config.Validate(), Is.Empty);
        }

        [Test]
        public void ValidateContentNamesACallOutsideTheStartingBoard()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            CallsCatalog calls = config.LoadCalls();
            string economy = config.EconomyFileText.Replace("\"startPool\": [ \"call_001\"", "\"startPool\": [ \"call_201\"");
            Assert.That(economy, Is.Not.EqualTo(config.EconomyFileText), "setup");
            IReadOnlyList<string> errors = ContentValidator.ValidateEconomy(economy, calls);
            Assert.That(errors, Has.Some.Contains("startPool: call_201 uses A5, but the startingBoard opens only 3x3"));
        }
    }
}
