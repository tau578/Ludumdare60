using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>Hold and resume (D07): a pulled plug holds the call, the same pair resumes it, anything else cuts it off.</summary>
    public sealed class HoldTests
    {
        private static readonly CordId Cord = new CordId(0);
        private static readonly CordId OtherCord = new CordId(1);

        private TestWorld world;
        private CallService calls;
        private Wallet wallet;
        private EarningsService earnings;
        private FakeSwitchboard board;
        private OperatorDesk desk;
        private List<ConnectionFeePaidArgs> feesPaid;
        private List<CallEndedArgs> ended;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            wallet = new Wallet();
            earnings = new EarningsService(calls, wallet);
            board = new FakeSwitchboard();
            desk = new OperatorDesk(calls, new ConnectionRule(world.Directory), board);
            feesPaid = new List<ConnectionFeePaidArgs>();
            earnings.ConnectionFeePaid += e => feesPaid.Add(e);
            ended = new List<CallEndedArgs>();
            calls.CallEnded += e => ended.Add(e);
        }

        private void Advance(double seconds)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += 0.05)
            {
                earnings.Tick(0.05);
                calls.Tick(0.05);
            }
        }

        // Ada (A1) wants Basil (B2), 5 c/s plus a 10 c fee, for 20 s, connected on Cord.
        private Call ConnectAdaToBasil()
        {
            Call call = calls.Add(world.AdaWantsBasil(centsPerSecond: 5, durationSeconds: 20, connectionFeeCents: 10));
            Assert.That(desk.TryTake(call.Id), Is.True);
            desk.OnPlugInserted(Cord, TestWorld.A1);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "setup");
            return call;
        }

        private Call TakeClaraCall(IPerson recipient)
        {
            Call call = calls.Add(new CallRequest(world.Callers[2], recipient, 4, 30));
            Assert.That(desk.TryTake(call.Id), Is.True);
            return call;
        }

        [Test]
        public void PulledPlugPutsTheCallOnHoldAndKeepsItsCord()
        {
            Call call = ConnectAdaToBasil();
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);

            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            Assert.That(call.Cord, Is.EqualTo(Cord));
            Assert.That(desk.CallOn(Cord), Is.SameAs(call));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(desk.HalfPatched, Is.Empty, "the remaining plug belongs to the on-hold call");
            Assert.That(calls.InHand, Is.Null);
        }

        [Test]
        public void HoldPausesPayAndTheDuration()
        {
            Call call = ConnectAdaToBasil();
            Advance(4);
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            long cents = wallet.Cents;
            double earned = call.CentsEarned;
            double secondsLeft = call.SecondsLeft;
            Assert.That(cents, Is.EqualTo(30));

            Advance(60);

            Assert.That(call.State, Is.EqualTo(CallState.OnHold), "the duration doesn't run out while on hold");
            Assert.That(wallet.Cents, Is.EqualTo(cents));
            Assert.That(call.CentsEarned, Is.EqualTo(earned));
            Assert.That(call.SecondsLeft, Is.EqualTo(secondsLeft));
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void RestoringTheSamePairResumesWithoutASecondFee(string first, string second)
        {
            Call call = ConnectAdaToBasil();
            Advance(4);
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            Advance(10);

            desk.OnCircuitClosed(Cord, new JackId(first), new JackId(second));

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(call.Cord, Is.EqualTo(Cord));
            Assert.That(feesPaid.Count, Is.EqualTo(1));
            Assert.That(wallet.Cents, Is.EqualTo(30), "no fee on resume");
            Assert.That(board.Returned, Is.Empty);
            Assert.That(board.Refused, Is.Empty);
            Advance(2);
            Assert.That(wallet.Cents, Is.EqualTo(40), "pay resumes");
        }

        [Test]
        public void AHeldAndResumedCallPaysTheFeePlusItsFullDurationOnce()
        {
            Call call = ConnectAdaToBasil();
            Advance(5);
            desk.OnCircuitOpened(Cord, TestWorld.B2, TestWorld.A1);
            Advance(7);
            desk.OnCircuitClosed(Cord, TestWorld.B2, TestWorld.A1);
            Advance(20);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(wallet.Cents, Is.EqualTo(10 + 5 * 20));
            Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void HoldingAgainAfterAResumeWorks()
        {
            Call call = ConnectAdaToBasil();
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCircuitOpened(Cord, TestWorld.B2, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            desk.OnCircuitClosed(Cord, TestWorld.B2, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(feesPaid.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReplugElsewhereCutsTheCallOffAndKeepsTheNewCircuit()
        {
            Call call = ConnectAdaToBasil();
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);

            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(board.Refused, Is.Empty);
            Assert.That(board.Returned, Is.Empty, "the player owns the physical wiring");
            Assert.That(desk.CallOn(Cord), Is.Null);
        }

        [Test]
        public void ReplugElsewhereIsJudgedAsAFreshCircuitForTheCallInHand()
        {
            Call held = ConnectAdaToBasil();
            Advance(3);
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            // Clara (C3) wants Ada (A1): the moved plug from B2 into C3 makes exactly that pair.
            Call fresh = TakeClaraCall(world.Ada);
            Assert.That(fresh.State, Is.EqualTo(CallState.Taken), "the on-hold cord isn't half-patched for the call in hand");

            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);

            Assert.That(held.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(fresh.State, Is.EqualTo(CallState.Connected));
            Assert.That(fresh.Cord, Is.EqualTo(Cord));
            Assert.That(desk.CallOn(Cord), Is.SameAs(fresh));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(board.Refused, Is.Empty);
            Assert.That(feesPaid.Count, Is.EqualTo(1), "the fresh call has no fee; the old one paid its own");
        }

        [Test]
        public void ReplugOneEndCutsOffHeldCallButDoesNotFailANewerOrder()
        {
            Call held = ConnectAdaToBasil();
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            Call fresh = TakeClaraCall(world.Basil);

            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);

            Assert.That(held.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(fresh.State, Is.EqualTo(CallState.Taken));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(ended.Count, Is.EqualTo(1));
            Assert.That(ended[0].Call, Is.SameAs(held));
        }

        [Test]
        public void CordBackInTheTrayCutsTheOnHoldCallOff()
        {
            Call call = ConnectAdaToBasil();
            Advance(2);
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            long cents = wallet.Cents;

            desk.OnCordReturned(Cord);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(desk.CallOn(Cord), Is.Null);
            Assert.That(desk.HalfPatched, Is.Empty);
            Advance(5);
            Assert.That(wallet.Cents, Is.EqualTo(cents));
        }

        [Test]
        public void ACordReusedAfterACutOffStartsClean()
        {
            ConnectAdaToBasil();
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCordReturned(Cord);

            Call next = TakeClaraCall(world.Ada);
            desk.OnPlugInserted(Cord, TestWorld.A1);
            Assert.That(next.State, Is.EqualTo(CallState.Connecting));
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2, TestWorld.A1);
            Assert.That(next.EndReason, Is.EqualTo(CallEndReason.WrongConnection), "judged, not mistaken for a restore");
        }

        [Test]
        public void SeveralCallsWithOneOnHold()
        {
            Call held = ConnectAdaToBasil();
            Call other = calls.Add(new CallRequest(world.Callers[2], world.Ada, 4, 6));
            desk.TryTake(other.Id);
            desk.OnCircuitClosed(OtherCord, TestWorld.C3, TestWorld.A1);
            Advance(1);
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            double heldEarned = held.CentsEarned;

            Advance(10);

            Assert.That(other.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(other.CentsEarned, Is.EqualTo(24).Within(1e-9));
            Assert.That(held.State, Is.EqualTo(CallState.OnHold));
            Assert.That(held.CentsEarned, Is.EqualTo(heldEarned));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(desk.CallOn(Cord), Is.SameAs(held));
        }

        [Test]
        public void OnHoldCallUsesACordAndIsNotWaitingForTheSpawnerLoad()
        {
            var crowd = new Crowd(12);
            var clock = new GameClock();
            var random = new SeededRandom(5);
            var crowdCalls = new CallService();
            var callbacks = new CallbackScheduler(crowdCalls, clock, random, 6);
            var spawner = new CallSpawner(crowdCalls, new CallFactory(random), crowd.Directory, crowd.Callers, random,
                new BusyTracker(crowdCalls, callbacks), new PacingSettings(2, 8, 0, 0.5), 6, () => 1);
            Call onHold = crowdCalls.Add(crowd.Request(0, 1));
            crowdCalls.Take(onHold.Id);
            crowdCalls.Connect(onHold.Id);
            crowdCalls.Hold(onHold.Id);
            crowdCalls.Add(crowd.Request(2, 3));

            // One cord, held by the on-hold call: one waiting call and no free cord.
            Assert.That(spawner.Load(), Is.EqualTo(1.5));
        }
    }
}
