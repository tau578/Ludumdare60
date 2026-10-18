using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class WalletSpendTests
    {
        [Test]
        public void SpendsWhenTheBalanceCoversIt()
        {
            var wallet = new Wallet(1000);
            Assert.That(wallet.TrySpend(800), Is.True);
            Assert.That(wallet.Cents, Is.EqualTo(200));
            Assert.That(wallet.TrySpend(200), Is.True);
            Assert.That(wallet.Cents, Is.EqualTo(0));
        }

        [TestCase(801)]
        [TestCase(0)]
        [TestCase(-5)]
        public void RefusesWhenShortOrNotPositiveAndChangesNothing(long cents)
        {
            var wallet = new Wallet(800, 0.25);
            Assert.That(wallet.TrySpend(cents), Is.False);
            Assert.That(wallet.Cents, Is.EqualTo(800));
            Assert.That(wallet.Fraction, Is.EqualTo(0.25));
        }

        [Test]
        public void KeepsTheFraction()
        {
            var wallet = new Wallet(1000, 0.75);
            wallet.TrySpend(999);
            Assert.That(wallet.Cents, Is.EqualTo(1));
            Assert.That(wallet.Fraction, Is.EqualTo(0.75));
        }
    }

    public sealed class UpgradeServiceTests
    {
        private Wallet wallet;
        private UpgradeService upgrades;
        private List<UpgradeBought> bought;

        [SetUp]
        public void SetUp()
        {
            wallet = new Wallet(100000);
            upgrades = new UpgradeService(FakeUpgrade.PlaceholderShop(), wallet);
            bought = new List<UpgradeBought>();
            upgrades.UpgradeBought += bought.Add;
        }

        private void AssertRefused(string id, PurchaseCheck reason)
        {
            long cents = wallet.Cents;
            int owned = CountOwned();
            Assert.That(upgrades.CanBuy(id), Is.EqualTo(reason));
            Assert.That(upgrades.TryBuy(id), Is.False);
            Assert.That(wallet.Cents, Is.EqualTo(cents));
            Assert.That(CountOwned(), Is.EqualTo(owned));
        }

        private int CountOwned()
        {
            int count = 0;
            foreach (IUpgrade upgrade in upgrades.All)
                if (upgrades.IsOwned(upgrade.Id)) count++;
            return count;
        }

        [Test]
        public void BuysTheNextEntryOfAChain()
        {
            Assert.That(upgrades.NextInChain(UpgradeKind.ExtraIncome).Id, Is.EqualTo("income_1"));
            Assert.That(upgrades.TryBuy("income_1"), Is.True);
            Assert.That(upgrades.IsOwned("income_1"), Is.True);
            Assert.That(wallet.Cents, Is.EqualTo(100000 - 800));
            Assert.That(upgrades.NextInChain(UpgradeKind.ExtraIncome).Id, Is.EqualTo("income_2"));
        }

        [Test]
        public void RefusesOutOfOrder() => AssertRefused("income_2", PurchaseCheck.NotNext);

        [Test]
        public void RefusesWhatIsAlreadyOwned()
        {
            upgrades.TryBuy("income_1");
            AssertRefused("income_1", PurchaseCheck.Owned);
        }

        [Test]
        public void RefusesAnUnknownId()
        {
            AssertRefused("income_9", PurchaseCheck.Unknown);
            AssertRefused(null, PurchaseCheck.Unknown);
        }

        [Test]
        public void RefusesWhatIsTooExpensive()
        {
            var poor = new UpgradeService(FakeUpgrade.PlaceholderShop(), wallet = new Wallet(799));
            upgrades = poor;
            AssertRefused("income_1", PurchaseCheck.TooExpensive);
        }

        [Test]
        public void RaisesUpgradeBoughtOncePerPurchase()
        {
            upgrades.TryBuy("income_1");
            upgrades.TryBuy("income_1");
            upgrades.TryBuy("income_3");
            upgrades.TryBuy("income_2");
            Assert.That(bought.Count, Is.EqualTo(2));
            Assert.That(bought[0].Upgrade.Id, Is.EqualTo("income_1"));
            Assert.That(bought[0].SpentCents, Is.EqualTo(800));
            Assert.That(bought[1].Upgrade.Id, Is.EqualTo("income_2"));
        }

        [Test]
        public void TheSecretCanBeBoughtRegardlessOfTheOtherChains()
        {
            Assert.That(upgrades.Goal.Id, Is.EqualTo("secret"));
            Assert.That(upgrades.TryBuy("secret"), Is.True);
            Assert.That(upgrades.IsOwned("secret"), Is.True);
            Assert.That(upgrades.IsOwned("income_1"), Is.False);
        }

        [Test]
        public void ChainsKeepFileOrderAndKindsFollowFirstAppearance()
        {
            var service = new UpgradeService(new IUpgrade[]
            {
                new FakeUpgrade("wires_1", UpgradeKind.ExtraWires, 500),
                FakeUpgrade.Income("income_1", 800, 1.5),
                new FakeUpgrade("wires_2", UpgradeKind.ExtraWires, 800),
                new FakeUpgrade("secret", UpgradeKind.Secret, 25000)
            }, wallet);
            Assert.That(service.Kinds, Is.EqualTo(new[] { UpgradeKind.ExtraWires, UpgradeKind.ExtraIncome, UpgradeKind.Secret }));
            Assert.That(service.ChainOf(UpgradeKind.ExtraWires).Count, Is.EqualTo(2));
            Assert.That(service.ChainOf(UpgradeKind.ExtraWires)[1].Id, Is.EqualTo("wires_2"));
            Assert.That(service.ChainOf(UpgradeKind.ExtraClients), Is.Empty);
        }
    }

    public sealed class StatSheetTests
    {
        private Wallet wallet;
        private UpgradeService upgrades;
        private StatSheet stats;

        [SetUp]
        public void SetUp()
        {
            wallet = new Wallet(100000);
            upgrades = new UpgradeService(FakeUpgrade.PlaceholderShop(), wallet);
            stats = new StatSheet(upgrades);
        }

        [Test]
        public void MultiplierIsOneWithNothingBought() => Assert.That(stats.IncomeMultiplier, Is.EqualTo(1));

        [Test]
        public void MultiplierIsTheLastBoughtValueSetNotStacked()
        {
            upgrades.TryBuy("income_1");
            Assert.That(stats.IncomeMultiplier, Is.EqualTo(1.5));
            upgrades.TryBuy("income_2");
            Assert.That(stats.IncomeMultiplier, Is.EqualTo(2.0));
        }

        [Test]
        public void RecomputingTwiceGivesTheSameResult()
        {
            upgrades.TryBuy("income_1");
            upgrades.TryBuy("income_2");
            stats.Recompute();
            stats.Recompute();
            Assert.That(stats.IncomeMultiplier, Is.EqualTo(2.0));
        }

        [Test]
        public void TheSecretLeavesTheMultiplierAlone()
        {
            upgrades.TryBuy("secret");
            Assert.That(stats.IncomeMultiplier, Is.EqualTo(1));
        }
    }

    public sealed class UpgradeEarningsTests
    {
        private TestWorld world;
        private CallService calls;
        private Wallet wallet;
        private UpgradeService upgrades;
        private StatSheet stats;
        private EarningsService earnings;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            wallet = new Wallet(800);
            upgrades = new UpgradeService(FakeUpgrade.PlaceholderShop(), wallet);
            stats = new StatSheet(upgrades);
            earnings = new EarningsService(calls, wallet, () => stats.IncomeMultiplier);
        }

        private void Advance(int frames)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                earnings.Tick(0.05);
                calls.Tick(0.05);
            }
        }

        private Call Connect(CallRequest request)
        {
            Call call = calls.Add(request);
            calls.Take(call.Id);
            calls.Connect(call.Id);
            return call;
        }

        [Test]
        public void AMultiplierBoughtMidCallAppliesToTheRestOfThatCall()
        {
            Call call = Connect(world.AdaWantsBasil(centsPerSecond: 5, durationSeconds: 20));
            Advance(200);
            Assert.That(wallet.Cents, Is.EqualTo(850));
            Assert.That(upgrades.TryBuy("income_1"), Is.True);
            Advance(240);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(call.CentsEarned, Is.EqualTo(50 + 75).Within(1e-6));
            Assert.That(wallet.Cents, Is.EqualTo(50 + 75));
        }

        [Test]
        public void TheFeeOfACallConnectedAfterThePurchaseIsMultiplied()
        {
            upgrades.TryBuy("income_1");
            double fee = 0;
            earnings.ConnectionFeePaid += e => fee = e.Cents;
            Connect(world.AdaWantsBasil(centsPerSecond: 5, durationSeconds: 20, connectionFeeCents: 10));
            Assert.That(fee, Is.EqualTo(15).Within(1e-9));
            Assert.That(wallet.Cents, Is.EqualTo(15));
        }

        [Test]
        public void FinesAreNotMultiplied()
        {
            var emergency = new EmergencyWorld();
            var penalties = new PenaltyService(calls, wallet);
            wallet.Earn(9200);
            upgrades.TryBuy("income_1");
            upgrades.TryBuy("income_2");
            upgrades.TryBuy("income_3");
            Assert.That(stats.IncomeMultiplier, Is.EqualTo(3));
            long taken = 0;
            penalties.FineApplied += e => taken = e.TakenCents;
            Call call = calls.Add(emergency.EmergencyRequest(0));
            calls.End(call.Id, CallEndReason.Missed);
            Assert.That(taken, Is.EqualTo(200));
        }
    }

    public sealed class ShopShelfTests
    {
        // Since the Notepad went (D54) the file has three chains; a fourth, later type keeps the rotation covered.
        private const UpgradeKind LaterKind = (UpgradeKind)99;

        private static readonly IUpgrade[] FourChains =
        {
            FakeUpgrade.Income("income_1", 800, 1.5),
            new FakeUpgrade("wires_1", UpgradeKind.ExtraWires, 500),
            FakeUpgrade.Income("income_2", 2000, 2),
            new FakeUpgrade("later_1", LaterKind, 300),
            new FakeUpgrade("secret", UpgradeKind.Secret, 25000),
            new FakeUpgrade("clients_1", UpgradeKind.ExtraClients, 1000),
            new FakeUpgrade("wires_2", UpgradeKind.ExtraWires, 800)
        };

        private UpgradeService upgrades;
        private ShopShelf shelf;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            upgrades = new UpgradeService(FourChains, new Wallet(1000000));
            shelf = new ShopShelf(upgrades);
            shelf.Changed += () => changes++;
        }

        private string[] Offers()
        {
            var offers = new string[ShopShelf.SlotCount];
            for (int i = 0; i < offers.Length; i++) offers[i] = shelf.OfferAt(i)?.Id;
            return offers;
        }

        [Test]
        public void ChainsArePlacedInFileOrderAndTheSecretIsNeverShelved() =>
            Assert.That(Offers(), Is.EqualTo(new[] { "income_1", "wires_1", "later_1" }));

        [Test]
        public void ABoughtOutSlotTakesTheNextChainWhileTheOthersStay()
        {
            upgrades.TryBuy("wires_1");
            Assert.That(Offers(), Is.EqualTo(new[] { "income_1", "wires_2", "later_1" }));
            upgrades.TryBuy("later_1");
            Assert.That(Offers(), Is.EqualTo(new[] { "income_1", "wires_2", "clients_1" }));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void SlotsGoEmptyWhenNothingIsLeft()
        {
            foreach (string id in new[] { "income_1", "income_2", "wires_1", "wires_2", "later_1", "clients_1", "secret" })
                Assert.That(upgrades.TryBuy(id), Is.True, id);
            Assert.That(Offers(), Is.EqualTo(new string[] { null, null, null }));
            Assert.That(shelf.TryGetKind(0, out _), Is.False);
        }

        [Test]
        public void BuyingTheSecretMovesNoSlot()
        {
            upgrades.TryBuy("secret");
            Assert.That(Offers(), Is.EqualTo(new[] { "income_1", "wires_1", "later_1" }));
        }

        [Test]
        public void RebuildPutsTheShelfBackAfterTheBoughtIdsAreCleared()
        {
            upgrades.TryBuy("later_1");
            upgrades.ClearOwned();
            shelf.Rebuild();
            Assert.That(Offers(), Is.EqualTo(new[] { "income_1", "wires_1", "later_1" }));
        }

        [Test]
        public void AShopWithOnlyTheSecretHasAnEmptyShelf()
        {
            var lonely = new ShopShelf(new UpgradeService(new IUpgrade[] { new FakeUpgrade("secret", UpgradeKind.Secret, 100) }, new Wallet()));
            for (int i = 0; i < ShopShelf.SlotCount; i++) Assert.That(lonely.OfferAt(i), Is.Null);
        }
    }

    public sealed class GoalProgressTests
    {
        private Wallet wallet;
        private UpgradeService upgrades;
        private GoalProgress goal;
        private int affordable;
        private int purchased;

        [SetUp]
        public void SetUp()
        {
            wallet = new Wallet();
            upgrades = new UpgradeService(FakeUpgrade.PlaceholderShop(), wallet);
            goal = new GoalProgress(upgrades, wallet);
            goal.GoalAffordable += () => affordable++;
            goal.GoalPurchased += () => purchased++;
        }

        [Test]
        public void ProgressIsTheClampedRatio()
        {
            Assert.That(goal.Goal.Id, Is.EqualTo("secret"));
            Assert.That(goal.Progress, Is.EqualTo(0));
            wallet.Earn(10000);
            Assert.That(goal.Progress, Is.EqualTo(0.4).Within(1e-9));
            wallet.Earn(90000);
            Assert.That(goal.Progress, Is.EqualTo(1));
        }

        [Test]
        public void GoalAffordableFiresOnce()
        {
            wallet.Earn(24999);
            goal.Tick();
            Assert.That(affordable, Is.EqualTo(0));
            wallet.Earn(1);
            goal.Tick();
            goal.Tick();
            wallet.ApplyFine(5000);
            goal.Tick();
            wallet.Earn(5000);
            goal.Tick();
            Assert.That(affordable, Is.EqualTo(1));
        }

        [Test]
        public void GoalPurchasedFiresOnBuyingTheSecretAndProgressStaysFull()
        {
            wallet.Earn(30000);
            upgrades.TryBuy("income_1");
            Assert.That(purchased, Is.EqualTo(0));
            Assert.That(upgrades.TryBuy("secret"), Is.True);
            Assert.That(purchased, Is.EqualTo(1));
            Assert.That(goal.IsPurchased, Is.True);
            Assert.That(wallet.Cents, Is.EqualTo(30000 - 800 - 25000));
            Assert.That(goal.Progress, Is.EqualTo(1));
        }

        [Test]
        public void NoSecretMeansNoGoal()
        {
            var none = new GoalProgress(new UpgradeService(new IUpgrade[] { FakeUpgrade.Income("income_1", 800, 1.5) }, wallet), wallet);
            int fired = 0;
            none.GoalAffordable += () => fired++;
            wallet.Earn(100000);
            none.Tick();
            Assert.That(none.Goal, Is.Null);
            Assert.That(none.Progress, Is.EqualTo(0));
            Assert.That(fired, Is.EqualTo(0));
        }
    }

    public sealed class UpgradeSessionTests
    {
        [Test]
        public void ASessionWithNoUpgradesStillTicks()
        {
            var world = new TestWorld();
            var session = new GameSession(world.Content, new FakeSwitchboard());
            for (int frame = 0; frame < 200; frame++) session.Tick(0.05);
            Assert.That(session.Calls.Live.Count, Is.GreaterThan(0));
            Assert.That(session.Stats.IncomeMultiplier, Is.EqualTo(1));
            for (int i = 0; i < ShopShelf.SlotCount; i++) Assert.That(session.Shelf.OfferAt(i), Is.Null);
            Assert.That(session.Goal.Goal, Is.Null);
            Assert.That(session.Goal.Progress, Is.EqualTo(0));
        }

        [Test]
        public void TheSessionPaysWithTheBoughtMultiplierAndResetsTheShop()
        {
            var world = new TestWorld();
            world.Content.StartingCents = 30000;
            world.Content.Upgrades = FakeUpgrade.PlaceholderShop();
            var session = new GameSession(world.Content, new FakeSwitchboard());
            int affordable = 0;
            session.Goal.GoalAffordable += () => affordable++;
            session.Tick(0.05);
            Assert.That(affordable, Is.EqualTo(1));

            Assert.That(session.Upgrades.TryBuy("income_1"), Is.True);
            Assert.That(session.Upgrades.TryBuy("income_2"), Is.True);
            Assert.That(session.Stats.IncomeMultiplier, Is.EqualTo(2));
            Assert.That(session.Shelf.OfferAt(0).Id, Is.EqualTo("income_3"));

            session.ResetUpgrades();
            Assert.That(session.Upgrades.IsOwned("income_1"), Is.False);
            Assert.That(session.Stats.IncomeMultiplier, Is.EqualTo(1));
            Assert.That(session.Shelf.OfferAt(0).Id, Is.EqualTo("income_1"));
            Assert.That(session.Wallet.Cents, Is.EqualTo(30000 - 800 - 2000), "a reset refunds nothing");
            session.Wallet.Earn(100000);
            session.Tick(0.05);
            Assert.That(affordable, Is.EqualTo(2), "the affordable flag is reset too");
        }
    }
}
