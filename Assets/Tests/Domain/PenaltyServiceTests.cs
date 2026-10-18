using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>Fines by end reason, the zero floor and the callback flags (Design 5.3, 5.5; D10, D37).</summary>
    public sealed class PenaltyServiceTests
    {
        private EmergencyWorld world;
        private CallService calls;
        private Wallet wallet;
        private PenaltyService penalties;
        private GameClock clock;
        private CallbackScheduler callbacks;
        private OperatorDesk desk;
        private List<FineAppliedArgs> fines;

        private void Build(long cents = 1000)
        {
            world = new EmergencyWorld();
            calls = new CallService();
            wallet = new Wallet(cents);
            penalties = new PenaltyService(calls, wallet);
            clock = new GameClock();
            callbacks = new CallbackScheduler(calls, clock, new SeededRandom(9), () => 6);
            desk = new OperatorDesk(calls, new ConnectionRule(world.Directory), new FakeSwitchboard());
            fines = new List<FineAppliedArgs>();
            penalties.FineApplied += fines.Add;
        }

        [SetUp]
        public void SetUp() => Build();

        [TestCase(CallEndReason.Missed, 200)]
        [TestCase(CallEndReason.WrongConnection, 200)]
        [TestCase(CallEndReason.DisconnectedEarly, 100)]
        [TestCase(CallEndReason.Finished, 0)]
        [TestCase(CallEndReason.Cleared, 0)]
        public void EmergencyFinesDependOnHowTheCallEnded(CallEndReason reason, long fine)
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

        [TestCase(CallEndReason.Missed)]
        [TestCase(CallEndReason.WrongConnection)]
        [TestCase(CallEndReason.DisconnectedEarly)]
        [TestCase(CallEndReason.Finished)]
        public void RegularCallsNeverFine(CallEndReason reason)
        {
            Call call = calls.Add(new CallRequest(world.Callers[0], world.People[1], 5, 20));

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
        public void AWrongPatchOnAnEmergencyFinesAndTheCallerRingsAgain()
        {
            Call call = calls.Add(world.EmergencyRequest(0, service: 0));
            desk.TryTake(call.Id);

            desk.OnCircuitClosed(new CordId(0), world.Directory.JackOf(call.Request.CallerPerson),
                world.Directory.JackOf(world.Services[1]), world.Directory.JackOf(call.Request.CallerPerson));

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(wallet.Cents, Is.EqualTo(800));
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));
            Assert.That(callbacks.Pending[0].Request.Recipient, Is.SameAs(world.Services[0]));
        }

        [Test]
        public void AMissCallsBackOnlyWhenTheTypeSaysSo()
        {
            calls.End(calls.Add(world.EmergencyRequest(0)).Id, CallEndReason.Missed);
            Assert.That(callbacks.Pending, Is.Empty);

            world.Emergency.CallbackAfterMiss = true;
            calls.End(calls.Add(world.EmergencyRequest(1)).Id, CallEndReason.Missed);
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnEarlyDisconnectCallsBackOnlyWhenTheTypeSaysSo()
        {
            calls.End(calls.Add(world.EmergencyRequest(0)).Id, CallEndReason.DisconnectedEarly);
            Assert.That(callbacks.Pending, Is.Empty);
            Assert.That(wallet.Cents, Is.EqualTo(900));

            world.Emergency.CallbackAfterDisconnect = true;
            calls.End(calls.Add(world.EmergencyRequest(1)).Id, CallEndReason.DisconnectedEarly);
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));
        }

        [Test]
        public void CuttingOffAConnectedEmergencyFinesTheEarlyDisconnectFine()
        {
            Call call = calls.Add(world.EmergencyRequest(0));
            desk.TryTake(call.Id);
            JackId caller = world.Directory.JackOf(call.Request.CallerPerson);
            JackId service = world.Directory.JackOf(call.Request.Recipient);
            desk.OnCircuitClosed(new CordId(0), caller, service);
            desk.OnCircuitOpened(new CordId(0), caller, service);

            desk.OnCordReturned(new CordId(0));

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(wallet.Cents, Is.EqualTo(900));
            Assert.That(fines[0].Reason, Is.EqualTo(CallEndReason.DisconnectedEarly));
        }
    }
}
