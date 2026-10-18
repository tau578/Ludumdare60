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

        private const string Notepad = "{ 'id': 'notepad', 'type': 'notepad', 'name': 'Notepad', 'description': 'Tap a name', 'costCents': 300 }";

        private static string Wires(string id, long cost, int wires) =>
            $"{{ 'id': '{id}', 'type': 'extra_wires', 'name': 'Extra cord', 'description': '{wires} cords', 'costCents': {cost}, 'wires': {wires} }}";

        private static string Json(params string[] entries) =>
            $"{{ 'upgrades': [ {string.Join(", ", entries)} ] }}".Replace('\'', '"');

        private static EconomyCatalog Load(params string[] entries) => EconomyFileLoader.Load(Json(entries));

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        [Test]
        public void TheRealFileLoadsEightEntriesWithNoProblems()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            Assert.That(file, Is.Not.Null, EconomyFilePath);
            EconomyCatalog catalog = EconomyFileLoader.Load(file.text);
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(8));
            Assert.That(catalog.Upgrades[0].Id, Is.EqualTo("income_1"));
            Assert.That(catalog.Upgrades[0].Kind, Is.EqualTo(UpgradeKind.ExtraIncome));
            Assert.That(catalog.Upgrades[0].CostCents, Is.EqualTo(800));
            Assert.That(catalog.Upgrades[0].IncomeMultiplier, Is.EqualTo(1.5));
            int[] wires = { 4, 5, 6 };
            long[] costs = { 500, 800, 1300 };
            for (int i = 0; i < 3; i++)
            {
                Assert.That(catalog.Upgrades[3 + i].Id, Is.EqualTo("wires_" + (i + 1)));
                Assert.That(catalog.Upgrades[3 + i].Kind, Is.EqualTo(UpgradeKind.ExtraWires));
                Assert.That(catalog.Upgrades[3 + i].Wires, Is.EqualTo(wires[i]));
                Assert.That(catalog.Upgrades[3 + i].CostCents, Is.EqualTo(costs[i]));
            }
            Assert.That(catalog.Upgrades[6].Id, Is.EqualTo("notepad"));
            Assert.That(catalog.Upgrades[6].Kind, Is.EqualTo(UpgradeKind.Notepad));
            Assert.That(catalog.Upgrades[6].CostCents, Is.EqualTo(300));
            Assert.That(catalog.Upgrades[7].Kind, Is.EqualTo(UpgradeKind.Secret));
            Assert.That(catalog.Upgrades[7].CostCents, Is.EqualTo(25000));
        }

        [Test]
        public void GameConfigReferencesTheRealFile()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config.LoadEconomy().Upgrades.Count, Is.EqualTo(8));
            Assert.That(config.StartingCords, Is.EqualTo(3));
            Assert.That(config.ExtraCordColors.Count, Is.EqualTo(3));
            Assert.That(config.Validate(), Is.Empty);
        }

        [Test]
        public void TheRealShelfOffersIncomeWiresAndTheNotepad()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            var upgrades = new UpgradeService(EconomyFileLoader.Load(file.text).Upgrades, new Wallet(100000));
            var shelf = new ShopShelf(upgrades);
            Assert.That(new[] { shelf.OfferAt(0).Id, shelf.OfferAt(1).Id, shelf.OfferAt(2).Id },
                Is.EqualTo(new[] { "income_1", "wires_1", "notepad" }));

            Assert.That(upgrades.TryBuy("notepad"), Is.True);
            Assert.That(shelf.OfferAt(2), Is.Null, "nothing is left for slot 3");
            Assert.That(shelf.OfferAt(0).Id, Is.EqualTo("income_1"));
            Assert.That(shelf.OfferAt(1).Id, Is.EqualTo("wires_1"));
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
            AssertSingleError(Load("{ 'id': 'clients_1', 'type': 'extra_clients', 'name': 'More lines', 'costCents': 1500 }", Secret),
                "\"clients_1\"", "extra_clients", "Step 7", "Remove the entry");

        [Test]
        public void LoadsWiresAndTheNotepad()
        {
            EconomyCatalog catalog = Load(Wires("wires_1", 500, 4), Wires("wires_2", 800, 6), Notepad, Secret);
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Upgrades[1].Wires, Is.EqualTo(6));
            Assert.That(catalog.Upgrades[2].Kind, Is.EqualTo(UpgradeKind.Notepad));
        }

        [Test]
        public void ReportsWiresThatDoNotGrow() =>
            AssertSingleError(Load(Wires("wires_1", 500, 4), Wires("wires_2", 800, 4), Secret),
                "Upgrade 2 \"wires_2\": wires 4 is not higher than wires_1's 4. Values in a chain must grow.");

        [Test]
        public void ReportsWiresNotAboveTheStartingCords()
        {
            AssertSingleError(Load(Wires("wires_1", 500, 3), Secret),
                "Upgrade 1 \"wires_1\"", "wires 3 is not above the 3 cords the game starts with", "4 or more");
            AssertSingleError(EconomyFileLoader.Load(Json(Wires("wires_1", 500, 4), Secret), 4), "wires 4", "5 or more");
        }

        [Test]
        public void ReportsMoreWiresThanTheTrayHolds() =>
            AssertSingleError(Load(Wires("wires_1", 500, 7), Secret), "Upgrade 1 \"wires_1\"", "wires 7", "Use 6 or less");

        [Test]
        public void ReportsASecondNotepad() =>
            AssertSingleError(Load(Notepad, "{ 'id': 'notepad_2', 'type': 'notepad', 'name': 'Notebook', 'costCents': 900 }", Secret),
                "Upgrade 2 \"notepad_2\"", "already a notepad (upgrade 1)", "at most one");

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
