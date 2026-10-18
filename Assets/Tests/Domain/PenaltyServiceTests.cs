using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>Only a missed emergency is fined, its own penaltyCents, never below zero (Design 5.5; D37, D51).</summary>
    public sealed class PenaltyServiceTests
    {
        private EmergencyWorld world;
        private CallService calls;
        private Wallet wallet;
        private PenaltyService penalties;
        private OperatorDesk desk;
        private List<FineAppliedArgs> fines;

        private void Build(long cents = 1000)
        {
            world = new EmergencyWorld();
            calls = new CallService();
            wallet = new Wallet(cents);
            penalties = new PenaltyService(calls, wallet);
            desk = new OperatorDesk(calls, new ConnectionRule(), new FakeSwitchboard());
            fines = new List<FineAppliedArgs>();
            penalties.FineApplied += fines.Add;
        }

        [SetUp]
        public void SetUp() => Build();

        [TestCase(CallEndReason.Missed, 200)]
        [TestCase(CallEndReason.WrongConnection, 0)]
        [TestCase(CallEndReason.DisconnectedEarly, 0)]
        [TestCase(CallEndReason.Finished, 0)]
        [TestCase(CallEndReason.Cleared, 0)]
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
        public void EachEmergencyFinesItsOwnPenalty()
        {
            Call call = calls.Add(new CallRequest(Scripts.Emergency("fire", "A1", "F5", penaltyCents: 350)));
            calls.Tick(31);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(wallet.Cents, Is.EqualTo(650));
            Assert.That(fines[0].RequestedCents, Is.EqualTo(350));
        }

        [Test]
        public void AnEmergencyWithNoPenaltyIsNotFined()
        {
            Call call = calls.Add(new CallRequest(Scripts.Emergency("free", "A1", "F4", penaltyCents: 0)));
            calls.End(call.Id, CallEndReason.Missed);
            Assert.That(wallet.Cents, Is.EqualTo(1000));
            Assert.That(fines, Is.Empty);
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
            Assert.That(fines[0].RequestedCents, Is.EqualTo(200));
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
