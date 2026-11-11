using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>
    /// Only a missed emergency is fined, never below zero (Design 5.5; D51). The fine follows pay (D82): 1.5 times the
    /// emergency's fee plus its rate for its whole length, times the income multiplier, rounded to the nearest cent.
    /// The default emergency pays 30¢ + 7¢/s for 15 s = 135¢, so its fine is 202.5¢, rounded to 203¢.
    /// </summary>
    public sealed class PenaltyServiceTests
    {
        private EmergencyWorld world;
        private CallService calls;
        private Wallet wallet;
        private PenaltyService penalties;
        private OperatorDesk desk;
        private List<FineAppliedArgs> fines;
        private double multiplier;

        private void Build(long cents = 1000, double factor = 1.5)
        {
            world = new EmergencyWorld();
            calls = new CallService();
            wallet = new Wallet(cents);
            multiplier = 1;
            penalties = new PenaltyService(calls, wallet, factor, () => multiplier);
            desk = new OperatorDesk(calls, new ConnectionRule(), new FakeSwitchboard());
            fines = new List<FineAppliedArgs>();
            penalties.FineApplied += fines.Add;
        }

        [SetUp]
        public void SetUp() => Build();

        [TestCase(CallEndReason.Missed, 203)]
        [TestCase(CallEndReason.WrongConnection, 0)]
        [TestCase(CallEndReason.DisconnectedEarly, 0)]
        [TestCase(CallEndReason.Finished, 0)]
        [TestCase(CallEndReason.Cleared, 0)]
        [TestCase(CallEndReason.HungUp, 0)]
        public void OnlyAMissedEmergencyIsFined(CallEndReason reason, long fine)
        {
            Call call = calls.Add(world.EmergencyRequest(0));

            calls.End(call.Id, reason);

            Assert.That(wallet.Cents, Is.EqualTo(1000 - fine));
            Assert.That(call.FineCents, Is.EqualTo(fine));
            Assert.That(fines.Count, Is.EqualTo(fine > 0 ? 1 : 0));
            if (fine == 0) return;
            Assert.That(fines[0].Call, Is.SameAs(call));
            Assert.That(fines[0].Reason, Is.EqualTo(reason));
            Assert.That(fines[0].RequestedCents, Is.EqualTo(fine));
            Assert.That(fines[0].TakenCents, Is.EqualTo(fine));
        }

        [Test]
        public void TheFineIsOneAndAHalfTimesWhatTheEmergencyWouldHavePaid()
        {
            // 40¢ + 10¢/s for 20 s = 240¢; 1.5 times that is 360¢.
            Call call = calls.Add(new CallRequest(Scripts.Emergency("fire", "A1", "F5", centsPerSecond: 10, durationSeconds: 20,
                connectCents: 40)));
            calls.Tick(31);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(wallet.Cents, Is.EqualTo(640));
            Assert.That(fines[0].RequestedCents, Is.EqualTo(360));
            Assert.That(penalties.FineFor(call.Request.Script), Is.EqualTo(360));
        }

        [Test]
        public void TheFineFollowsTheIncomeMultiplierInForceWhenItIsMissed()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            multiplier = 2;
            calls.End(call.Id, CallEndReason.Missed);
            Assert.That(fines[0].RequestedCents, Is.EqualTo(405), "135¢ x 2 x 1.5");
            Assert.That(wallet.Cents, Is.EqualTo(595));
        }

        [TestCase(1.5, 7.5, 10, 0, 113)]  // 75¢ x 1.5 = 112.5¢, rounded up to 113¢
        [TestCase(1.5, 3.3, 10, 0, 50)]   // 33¢ x 1.5 = 49.5¢, rounded up to 50¢
        [TestCase(1.2, 1, 10, 1, 13)]     // 11¢ x 1.2 = 13.2¢, rounded down to 13¢
        [TestCase(0, 7, 15, 30, 0)]       // a factor of 0 fines nothing
        public void TheFineIsRoundedToTheNearestCent(double factor, double centsPerSecond, double seconds, double fee, long fine)
        {
            Build(1000, factor);
            CallScript script = Scripts.Emergency("sos", "A1", "F4", centsPerSecond, seconds, fee);
            Assert.That(penalties.FineFor(script), Is.EqualTo(fine));
            Call call = calls.Add(new CallRequest(script));
            calls.End(call.Id, CallEndReason.Missed);
            Assert.That(wallet.Cents, Is.EqualTo(1000 - fine));
            Assert.That(fines.Count, Is.EqualTo(fine > 0 ? 1 : 0));
        }

        [Test]
        public void TheQuickConnectTipIsNotPartOfTheFine()
        {
            var earnings = new EarningsService(calls, wallet, () => multiplier, new QuickConnectTip(5, 100));
            Call call = calls.Add(world.EmergencyRequest(0));
            calls.End(call.Id, CallEndReason.Missed);
            Assert.That(fines[0].RequestedCents, Is.EqualTo(203));
            Assert.That(earnings.TotalPaidCents, Is.Zero);
        }

        [TestCase(CallEndReason.Missed)]
        [TestCase(CallEndReason.WrongConnection)]
        [TestCase(CallEndReason.DisconnectedEarly)]
        [TestCase(CallEndReason.Finished)]
        public void RegularCallsNeverFine(CallEndReason reason)
        {
            Call call = calls.Add(world.RegularRequest(0, 1));

            calls.End(call.Id, reason);

            Assert.That(wallet.Cents, Is.EqualTo(1000));
            Assert.That(fines, Is.Empty);
        }

        [Test]
        public void MoneyNeverGoesBelowZero()
        {
            Build(150);
            Call call = calls.Add(world.EmergencyRequest(0));

            calls.End(call.Id, CallEndReason.Missed);

            Assert.That(wallet.Cents, Is.EqualTo(0));
            Assert.That(call.FineCents, Is.EqualTo(150));
            Assert.That(fines.Count, Is.EqualTo(1));
            Assert.That(fines[0].RequestedCents, Is.EqualTo(203));
            Assert.That(fines[0].TakenCents, Is.EqualTo(150));
        }

        [Test]
        public void AnEmptyWalletReportsTheFineWithNothingTaken()
        {
            Build(0);
            Call call = calls.Add(world.EmergencyRequest(0));

            calls.End(call.Id, CallEndReason.Missed);

            Assert.That(wallet.Cents, Is.EqualTo(0));
            Assert.That(call.FineCents, Is.EqualTo(0));
            Assert.That(fines[0].TakenCents, Is.EqualTo(0));
        }

        [Test]
        public void AWrongPatchOnAnEmergencyEndsItWithNoFine()
        {
            Call call = calls.Add(world.EmergencyRequest(0, service: 0));
            desk.TryTake(call.Id);

            desk.OnCircuitClosed(new CordId(0), call.Request.From, EmergencyWorld.ServiceJack(1), call.Request.From);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(wallet.Cents, Is.EqualTo(1000));
            Assert.That(fines, Is.Empty);
        }

        [Test]
        public void CuttingOffAConnectedEmergencyIsNotFined()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(call.Id);
            desk.OnCircuitClosed(new CordId(0), call.Request.From, call.Request.To);
            desk.OnCircuitOpened(new CordId(0), call.Request.From, call.Request.To);

            desk.OnCordReturned(new CordId(0));

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(wallet.Cents, Is.EqualTo(1000));
            Assert.That(fines, Is.Empty);
        }
    }
}
