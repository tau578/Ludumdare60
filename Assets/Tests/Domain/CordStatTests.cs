using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CordStatTests
    {
        private UpgradeService upgrades;
        private StatSheet stats;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            upgrades = new UpgradeService(FakeUpgrade.FullShop(), new Wallet(100000));
            stats = new StatSheet(upgrades, 3);
            changes = 0;
            stats.Changed += () => changes++;
        }

        [Test]
        public void CordsAreTheStartingCordsWithNothingBought()
        {
            Assert.That(stats.Cords, Is.EqualTo(3));
            Assert.That(new StatSheet(new UpgradeService(null, new Wallet(0)), 5).Cords, Is.EqualTo(5));
        }

        [Test]
        public void CordsAreTheLastBoughtValueSetNotStacked()
        {
            upgrades.TryBuy("wires_1");
            Assert.That(stats.Cords, Is.EqualTo(4));
            upgrades.TryBuy("wires_2");
            Assert.That(stats.Cords, Is.EqualTo(5), "4 then 5 gives 5, not 3 + 1 + 2");
            stats.Recompute();
            Assert.That(stats.Cords, Is.EqualTo(5));
        }

        [Test]
        public void ChangedIsRaisedOnPurchaseAndOnReset()
        {
            upgrades.TryBuy("wires_1");
            Assert.That(changes, Is.EqualTo(1));
            upgrades.ClearOwned();
            stats.Recompute();
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(stats.Cords, Is.EqualTo(3));
        }

        [Test]
        public void SessionResetPutsTheCordsBackAndRaisesChanged()
        {
            var world = new TestWorld();
            world.Content.StartingCents = 100000;
            world.Content.Upgrades = FakeUpgrade.FullShop();
            var session = new GameSession(world.Content, new FakeSwitchboard());
            int sessionChanges = 0;
            session.Stats.Changed += () => sessionChanges++;
            session.Upgrades.TryBuy("wires_1");
            session.Upgrades.TryBuy("wires_2");
            Assert.That(session.Stats.Cords, Is.EqualTo(5));

            session.ResetUpgrades();
            Assert.That(session.Stats.Cords, Is.EqualTo(3));
            Assert.That(sessionChanges, Is.EqualTo(3));
        }
    }
}
