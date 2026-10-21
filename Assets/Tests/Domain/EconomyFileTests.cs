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
            "'startingCords': {0}, 'startingBoard': { 'cols': 3, 'rows': 3 }, 'startingClientSlots': 3, " +
            "'quickConnectSeconds': 5, 'quickConnectTipCents': 5, ";

        private static string Json(params string[] entries) => JsonStartingWith(StatSheet.DefaultStartingCords, entries);

        private static string JsonStartingWith(int startingCords, params string[] entries) =>
            ("{ " + Head.Replace("{0}", startingCords.ToString()) + $"'upgrades': [ {string.Join(", ", entries)} ] }}").Replace('\'', '"');

        private static EconomyCatalog Load(params string[] entries) => EconomyFileLoader.Load(Json(entries));

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        // The designer balances Economy.json at any time (lead, 2026-10-19): these check the rules, not the entries.
        [Test]
        public void TheRealFileLoadsEveryEntryWithNoProblems()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            Assert.That(file, Is.Not.Null, EconomyFilePath);
            EconomyCatalog catalog = EconomyFileLoader.Load(file.text);
            Assert.That(catalog.Errors, Is.Empty, string.Join("\n", catalog.Errors));
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(JsonUtility.FromJson<EconomyFileData>(file.text).upgrades.Length),
                "every entry loads");
            Assert.That(catalog.StartingCords, Is.InRange(1, StatSheet.MaxCords));
            int secrets = 0;
            int cords = catalog.StartingCords;
            double multiplier = 1;
            foreach (IUpgrade upgrade in catalog.Upgrades)
            {
                Assert.That(upgrade.Kind, Is.Not.EqualTo((UpgradeKind)3), "no Notepad (D54)");
                Assert.That(upgrade.CostCents, Is.GreaterThan(0), upgrade.Id);
                if (upgrade.Kind == UpgradeKind.Secret) secrets++;
                else if (upgrade.Kind == UpgradeKind.ExtraWires)
                {
                    Assert.That(upgrade.Wires, Is.InRange(cords + 1, StatSheet.MaxCords), $"{upgrade.Id}: each cord step adds cords");
                    cords = upgrade.Wires;
                }
                else if (upgrade.Kind == UpgradeKind.ExtraIncome)
                {
                    Assert.That(upgrade.IncomeMultiplier, Is.GreaterThan(multiplier), $"{upgrade.Id}: each rate step earns more");
                    multiplier = upgrade.IncomeMultiplier;
                }
            }
            Assert.That(secrets, Is.EqualTo(1), "exactly one secret goal");
        }

        [Test]
        public void GameConfigReferencesTheRealFile()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config.EconomyFileText, Is.EqualTo(AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath).text));
            EconomyCatalog fromFile = EconomyFileLoader.Load(config.EconomyFileText);
            Assert.That(config.LoadEconomy().Upgrades.Count, Is.EqualTo(fromFile.Upgrades.Count));
            Assert.That(config.LoadEconomy().StartingCords, Is.EqualTo(fromFile.StartingCords));
            Assert.That(config.ExtraCordColors.Count, Is.EqualTo(3));
            Assert.That(config.Validate(), Is.Empty);
        }

        [Test]
        public void TheRealShelfShowsOneEntryPerChainAndReplacesCompletedChains()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            IReadOnlyList<IUpgrade> all = EconomyFileLoader.Load(file.text).Upgrades;
            long total = 0;
            foreach (IUpgrade upgrade in all) total += upgrade.CostCents;
            var upgrades = new UpgradeService(all, new Wallet(total));
            var shelf = new ShopShelf(upgrades);

            // The chains in file order, without the secret, which has its own button.
            var chains = new List<UpgradeKind>();
            foreach (UpgradeKind kind in upgrades.Kinds)
                if (kind != UpgradeKind.Secret) chains.Add(kind);
            for (int slot = 0; slot < ShopShelf.SlotCount; slot++)
            {
                IUpgrade expected = slot < chains.Count ? upgrades.ChainOf(chains[slot])[0] : null;
                Assert.That(shelf.OfferAt(slot), Is.SameAs(expected), $"slot {slot + 1} shows the first entry of chain {slot + 1}");
            }

            // Buying out the chain in the middle slot brings the next chain not on the shelf; the other slots stay.
            if (chains.Count < 2) return;
            IUpgrade first = shelf.OfferAt(0), last = shelf.OfferAt(2);
            foreach (IUpgrade upgrade in upgrades.ChainOf(chains[1]))
            {
                Assert.That(shelf.OfferAt(1), Is.SameAs(upgrade), "a chain is sold in order");
                Assert.That(upgrades.TryBuy(upgrade.Id), Is.True, upgrade.Id);
            }
            IUpgrade next = chains.Count > ShopShelf.SlotCount ? upgrades.ChainOf(chains[ShopShelf.SlotCount])[0] : null;
            Assert.That(shelf.OfferAt(1), Is.SameAs(next), "a bought-out chain makes room for the next one, or leaves the slot empty");
            Assert.That(shelf.OfferAt(0), Is.SameAs(first));
            Assert.That(shelf.OfferAt(2), Is.SameAs(last));
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

        // The quick-connect tip (D59): two top-level fields.
        private static EconomyCatalog LoadWithQuickConnect(string fields) =>
            EconomyFileLoader.Load(("{ 'startingCords': 2, 'startingBoard': { 'cols': 3, 'rows': 3 }, 'startingClientSlots': 3, " +
                fields + $"'upgrades': [ {Secret} ] }}").Replace('\'', '"'));

        [Test]
        public void TheRealFileSetsAQuickConnectTip()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath).text);
            var data = JsonUtility.FromJson<EconomyFileData>(AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath).text);
            Assert.That(double.IsNaN(data.quickConnectSeconds), Is.False, "quickConnectSeconds is in the file");
            Assert.That(double.IsNaN(data.quickConnectTipCents), Is.False, "quickConnectTipCents is in the file");
            Assert.That(catalog.QuickTip.Seconds, Is.EqualTo(data.quickConnectSeconds));
            Assert.That(catalog.QuickTip.Cents, Is.EqualTo((long)data.quickConnectTipCents));
        }

        [Test]
        public void QuickConnectValuesComeFromTheFile()
        {
            EconomyCatalog catalog = LoadWithQuickConnect("'quickConnectSeconds': 3.5, 'quickConnectTipCents': 12, ");
            Assert.That(catalog.Errors, Is.Empty, string.Join("\n", catalog.Errors));
            Assert.That(catalog.QuickTip.Seconds, Is.EqualTo(3.5));
            Assert.That(catalog.QuickTip.Cents, Is.EqualTo(12));
            Assert.That(new GameContent(catalog).QuickConnectTipCents, Is.EqualTo(12));
            Assert.That(new GameContent(catalog).QuickConnectSeconds, Is.EqualTo(3.5));
        }

        [Test]
        public void ZeroTurnsTheTipOffWithNoMessage()
        {
            EconomyCatalog catalog = LoadWithQuickConnect("'quickConnectSeconds': 5, 'quickConnectTipCents': 0, ");
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.QuickTip.IsOn, Is.False);
        }

        [Test]
        public void MissingQuickConnectFieldsGetTheirDefaultsWithANote()
        {
            EconomyCatalog catalog = LoadWithQuickConnect("");
            Assert.That(catalog.Errors.Count, Is.EqualTo(2), string.Join("\n", catalog.Errors));
            Assert.That(catalog.Errors[0], Does.Contain("quickConnectSeconds is missing").And.Contain("A note only")
                .And.Contain("\"quickConnectSeconds\": 5"));
            Assert.That(catalog.Errors[1], Does.Contain("quickConnectTipCents is missing").And.Contain("A note only")
                .And.Contain("\"quickConnectTipCents\": 5"));
            Assert.That(catalog.QuickTip.Seconds, Is.EqualTo(QuickConnectTip.DefaultSeconds));
            Assert.That(catalog.QuickTip.Cents, Is.EqualTo(QuickConnectTip.DefaultCents));
        }

        [TestCase("'quickConnectSeconds': -2, 'quickConnectTipCents': 5, ", "quickConnectSeconds -2 is negative")]
        [TestCase("'quickConnectSeconds': 5, 'quickConnectTipCents': -1, ", "quickConnectTipCents -1 is negative")]
        [TestCase("'quickConnectSeconds': 5, 'quickConnectTipCents': 2.5, ", "quickConnectTipCents 2.5 must be whole cents")]
        public void AWrongQuickConnectValueIsReportedAndFallsBack(string fields, string message)
        {
            EconomyCatalog catalog = LoadWithQuickConnect(fields);
            AssertSingleError(catalog, message, "The game uses 5 for now");
            Assert.That(catalog.QuickTip.Seconds, Is.EqualTo(QuickConnectTip.DefaultSeconds));
            Assert.That(catalog.QuickTip.Cents, Is.EqualTo(QuickConnectTip.DefaultCents));
        }
    }
}
