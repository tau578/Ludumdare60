using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    public sealed class EconomyFileTests
    {
        private const string EconomyFilePath = "Assets/Configs/Economy.json";
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private const string Secret = "{ 'id': 'secret', 'type': 'secret', 'name': '???', 'description': 'Something big', 'costCents': 25000 }";

        private static string Income(string id, long cost, float multiplier, string name = "Rates") =>
            $"{{ 'id': '{id}', 'type': 'extra_income', 'name': '{name}', 'description': 'Earn more', 'costCents': {cost}, 'incomeMultiplier': {multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)} }}";

        private static EconomyCatalog Load(params string[] entries) =>
            EconomyFileLoader.Load($"{{ 'upgrades': [ {string.Join(", ", entries)} ] }}".Replace('\'', '"'));

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        [Test]
        public void TheRealFileLoadsFourEntriesWithNoProblems()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            Assert.That(file, Is.Not.Null, EconomyFilePath);
            EconomyCatalog catalog = EconomyFileLoader.Load(file.text);
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(4));
            Assert.That(catalog.Upgrades[0].Id, Is.EqualTo("income_1"));
            Assert.That(catalog.Upgrades[0].Kind, Is.EqualTo(UpgradeKind.ExtraIncome));
            Assert.That(catalog.Upgrades[0].CostCents, Is.EqualTo(800));
            Assert.That(catalog.Upgrades[0].IncomeMultiplier, Is.EqualTo(1.5));
            Assert.That(catalog.Upgrades[3].Kind, Is.EqualTo(UpgradeKind.Secret));
            Assert.That(catalog.Upgrades[3].CostCents, Is.EqualTo(25000));
        }

        [Test]
        public void GameConfigReferencesTheRealFile()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config.LoadEconomy().Upgrades.Count, Is.EqualTo(4));
        }

        [Test]
        public void AValidFilePasses() => Assert.That(Load(Income("income_1", 800, 1.5f), Secret).Errors, Is.Empty);

        [Test]
        public void ReportsAMissingFile()
        {
            AssertSingleError(EconomyFileLoader.Load(null), "missing");
            Assert.That(ContentValidator.ValidateEconomy("  ").Count, Is.EqualTo(1));
        }

        [Test]
        public void ReportsAnUnreadableFile() => AssertSingleError(EconomyFileLoader.Load("{ \"upgrades\": [ { \"id\": } ]"), "not valid JSON");

        [Test]
        public void ReportsAnEmptyId() => AssertSingleError(Load(Income("", 800, 1.5f), Secret), "Upgrade 1", "id is empty");

        [Test]
        public void ReportsABadlyFormedId() => AssertSingleError(Load(Income("Income-1", 800, 1.5f), Secret), "\"Income-1\"", "income_1");

        [Test]
        public void ReportsADuplicateId() =>
            AssertSingleError(Load(Income("income_1", 800, 1.5f), Income("income_1", 2000, 2), Secret), "Upgrade 2", "already used by upgrade 1");

        [Test]
        public void ReportsAnUnknownTypeAndListsTheKnownOnes()
        {
            EconomyCatalog catalog = Load("{ 'id': 'vip_1', 'type': 'vip', 'name': 'VIP', 'costCents': 100 }", Secret);
            AssertSingleError(catalog, "\"vip\"", "extra_income, extra_wires, notepad, extra_clients, secret");
        }

        [Test]
        public void ReportsATypeNotBuiltYet() =>
            AssertSingleError(Load("{ 'id': 'wires_1', 'type': 'extra_wires', 'name': 'Wire', 'costCents': 500, 'wires': 4 }", Secret),
                "extra_wires", "Step 5b");

        [Test]
        public void ReportsAMissingName() => AssertSingleError(Load(Income("income_1", 800, 1.5f, name: " "), Secret), "name is empty");

        [Test]
        public void ReportsACostOfZeroOrLess()
        {
            AssertSingleError(Load(Income("income_1", 0, 1.5f), Secret), "costCents is 0");
            AssertSingleError(Load(Income("income_1", -5, 1.5f), Secret), "costCents is -5");
        }

        [Test]
        public void ReportsAMultiplierOfOneOrLess() => AssertSingleError(Load(Income("income_1", 800, 1f), Secret), "incomeMultiplier 1 ", "above 1");

        [Test]
        public void ReportsAChainThatDoesNotGrow() =>
            AssertSingleError(Load(Income("income_1", 800, 1.5f), Income("income_2", 2000, 1.2f), Secret),
                "Upgrade 2 \"income_2\": incomeMultiplier 1.2 is not higher than income_1's 1.5. Values in a chain must grow.");

        [Test]
        public void ReportsNoSecret() => AssertSingleError(Load(Income("income_1", 800, 1.5f)), "no secret");

        [Test]
        public void ReportsASecondSecret() =>
            AssertSingleError(Load(Secret, "{ 'id': 'secret_2', 'type': 'secret', 'name': 'More', 'costCents': 50000 }"),
                "\"secret_2\"", "exactly one secret");

        [Test]
        public void ABrokenEntryIsLeftOutWhileTheOthersLoad()
        {
            EconomyCatalog catalog = Load(Income("income_1", 800, 1.5f), Income("income_2", 0, 2), Income("income_3", 5000, 3), Secret);
            Assert.That(catalog.Errors.Count, Is.EqualTo(1));
            var ids = new List<string>();
            foreach (IUpgrade upgrade in catalog.Upgrades) ids.Add(upgrade.Id);
            Assert.That(ids, Is.EqualTo(new[] { "income_1", "income_3", "secret" }));
        }
    }
}
