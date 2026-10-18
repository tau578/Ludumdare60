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

        private static string Wires(string id, long cost, int wires) =>
            $"{{ 'id': '{id}', 'type': 'extra_wires', 'name': 'Extra cord', 'description': '{wires} cords', 'costCents': {cost}, 'wires': {wires} }}";

        // A valid start, so each test sees only its upgrades' messages. Without a calls file the pools aren't checked.
        private const string Head =
            "'startingCords': {0}, 'startingBoard': { 'cols': 3, 'rows': 3 }, 'startingClientSlots': 3, ";

        private static string Json(params string[] entries) => JsonStartingWith(StatSheet.DefaultStartingCords, entries);

        private static string JsonStartingWith(int startingCords, params string[] entries) =>
            ("{ " + Head.Replace("{0}", startingCords.ToString()) + $"'upgrades': [ {string.Join(", ", entries)} ] }}").Replace('\'', '"');

        private static EconomyCatalog Load(params string[] entries) => EconomyFileLoader.Load(Json(entries));

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        [Test]
        public void TheRealFileLoadsTenEntriesWithNoProblems()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            Assert.That(file, Is.Not.Null, EconomyFilePath);
            EconomyCatalog catalog = EconomyFileLoader.Load(file.text);
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(10));
            Assert.That(catalog.Upgrades[6].Id, Is.EqualTo("income_1"));
            Assert.That(catalog.Upgrades[6].Kind, Is.EqualTo(UpgradeKind.ExtraIncome));
            Assert.That(catalog.Upgrades[6].CostCents, Is.EqualTo(800));
            Assert.That(catalog.Upgrades[6].IncomeMultiplier, Is.EqualTo(1.5));
            int[] wires = { 4, 5, 6 };
            long[] costs = { 500, 800, 1300 };
            for (int i = 0; i < 3; i++)
            {
                Assert.That(catalog.Upgrades[i].Id, Is.EqualTo("wires_" + (i + 1)));
                Assert.That(catalog.Upgrades[i].Kind, Is.EqualTo(UpgradeKind.ExtraWires));
                Assert.That(catalog.Upgrades[i].Wires, Is.EqualTo(wires[i]));
                Assert.That(catalog.Upgrades[i].CostCents, Is.EqualTo(costs[i]));
            }
            for (int i = 0; i < 3; i++)
                Assert.That(catalog.Upgrades[3 + i].Kind, Is.EqualTo(UpgradeKind.ExtraClients), "the board entries follow the cords");
            Assert.That(catalog.Upgrades[9].Kind, Is.EqualTo(UpgradeKind.Secret));
            Assert.That(catalog.Upgrades[9].CostCents, Is.EqualTo(25000));
            foreach (IUpgrade upgrade in catalog.Upgrades) Assert.That(upgrade.Kind, Is.Not.EqualTo((UpgradeKind)3), "no Notepad (D54)");
        }

        [Test]
        public void GameConfigReferencesTheRealFile()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config.LoadEconomy().Upgrades.Count, Is.EqualTo(10));
            Assert.That(config.LoadEconomy().StartingCords, Is.EqualTo(2));
            Assert.That(config.ExtraCordColors.Count, Is.EqualTo(3));
            Assert.That(config.Validate(), Is.Empty);
        }

        [Test]
        public void TheRealShelfMatchesTheArtworkAndReplacesCompletedChains()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            var upgrades = new UpgradeService(EconomyFileLoader.Load(file.text).Upgrades, new Wallet(100000));
            var shelf = new ShopShelf(upgrades);
            Assert.That(new[] { shelf.OfferAt(0).Id, shelf.OfferAt(1).Id, shelf.OfferAt(2).Id },
                Is.EqualTo(new[] { "wires_1", "board_1", "income_1" }));
            Assert.That(upgrades.TryBuy("board_1"), Is.True);
            Assert.That(upgrades.TryBuy("board_2"), Is.True);
            Assert.That(shelf.OfferAt(1).Id, Is.EqualTo("board_3"));
            Assert.That(upgrades.TryBuy("board_3"), Is.True);
            Assert.That(shelf.OfferAt(1), Is.Null, "no fourth chain since the Notepad went (D54)");
            Assert.That(shelf.OfferAt(0).Id, Is.EqualTo("wires_1"));
            Assert.That(shelf.OfferAt(2).Id, Is.EqualTo("income_1"));
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
            AssertSingleError(catalog, "\"vip\"", "extra_income, extra_wires, extra_clients, secret");
        }

        [Test]
        public void LoadsWires()
        {
            EconomyCatalog catalog = Load(Wires("wires_1", 500, 4), Wires("wires_2", 800, 6), Secret);
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Upgrades[1].Wires, Is.EqualTo(6));
        }

        [Test]
        public void ReportsWiresThatDoNotGrow() =>
            AssertSingleError(Load(Wires("wires_1", 500, 4), Wires("wires_2", 800, 4), Secret),
                "Upgrade 2 \"wires_2\": wires 4 is not higher than wires_1's 4. Values in a chain must grow.");

        [Test]
        public void ReportsWiresNotAboveTheStartingCords()
        {
            AssertSingleError(Load(Wires("wires_1", 500, 2), Secret),
                "Upgrade 1 \"wires_1\"", "wires 2 is not above the 2 cords the game starts with", "3 or more");
            AssertSingleError(EconomyFileLoader.Load(JsonStartingWith(4, Wires("wires_1", 500, 4), Secret)), "wires 4", "5 or more");
        }

        [Test]
        public void ReportsMoreWiresThanTheTrayHolds() =>
            AssertSingleError(Load(Wires("wires_1", 500, 7), Secret), "Upgrade 1 \"wires_1\"", "wires 7", "Use 6 or less");

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
