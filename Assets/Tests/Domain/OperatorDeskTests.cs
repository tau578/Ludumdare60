using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class OperatorDeskTests
    {
        private static readonly CordId Cord = new CordId(1);
        private static readonly CordId OtherCord = new CordId(2);

        private TestWorld world;
        private CallService calls;
        private FakeSwitchboard board;
        private OperatorDesk desk;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            board = new FakeSwitchboard();
            desk = new OperatorDesk(calls, new ConnectionRule(world.Directory), board);
        }

        private Call TakeAdaWantsBasil()
        {
            Call call = calls.Add(world.AdaWantsBasil(durationSeconds: 20));
            Assert.That(desk.TryTake(call.Id), Is.True);
            return call;
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void RightPairConnectsInEitherOrder(string first, string second)
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, new JackId(first), new JackId(second));

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(call.Cord, Is.EqualTo(Cord));
            Assert.That(desk.CallOn(Cord), Is.SameAs(call));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(board.Refused, Is.Empty);
        }

        [Test]
        public void WrongPairEndsTheCallAndLeavesTheCord()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);

            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(board.Refused, Is.Empty);
            Assert.That(calls.InHand, Is.Null);
        }

        [Test]
        public void CircuitClosedBeforeAcceptanceStaysOnTheBoard()
        {
            Call waiting = calls.Add(world.AdaWantsBasil());
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);

            Assert.That(board.Refused, Is.Empty);
            Assert.That(board.Returned, Is.Empty);
            Assert.That(waiting.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void FinishedCallLeavesItsCord()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            calls.Tick(20);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(desk.CallOn(Cord), Is.Null);
        }

        [Test]
        public void CordBackInTheTrayCutsItsCallOff()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCordReturned(Cord);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void ChangesToAnotherCordLeaveTheCallAlone()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnPlugInserted(OtherCord, TestWorld.C3);
            desk.OnCircuitOpened(OtherCord, TestWorld.C3, TestWorld.A1);
            desk.OnCordReturned(OtherCord);

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void FirstPlugPutsTheCallInHandIntoConnectingAndBackWhenTheCordReturns()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            Assert.That(desk.HalfPatched, Is.EqualTo(new[] { Cord }));

            desk.OnCordReturned(Cord);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            Assert.That(desk.HalfPatched, Is.Empty);
        }

        [Test]
        public void TakingACallWhileACordIsHalfPatchedGoesStraightToConnecting()
        {
            desk.OnPlugInserted(Cord, TestWorld.A1);
            Call call = TakeAdaWantsBasil();
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
        }

        [Test]
        public void AnotherAcceptedOrderDoesNotReactivateTheNewOrderLabel()
        {
            Call first = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.A1);
            Call second = calls.Add(new CallRequest(world.Callers[2], world.Ada, 5, 20));

            Assert.That(desk.TryTake(second.Id), Is.True);

            Assert.That(first.State, Is.EqualTo(CallState.Connecting));
            Assert.That(second.State, Is.EqualTo(CallState.Connecting));
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(second.State, Is.EqualTo(CallState.Connected));
            Assert.That(first.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void RightPairFromConnectingConnects()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.B2);
            desk.OnCircuitClosed(Cord, TestWorld.B2, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(desk.HalfPatched, Is.Empty);
        }

        [Test]
        public void WrongPairFromConnectingEndsTheCall()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.A1);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void CallInHandStaysConnectingWhileAnotherCordIsStillHalfPatched()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.A1);
            desk.OnPlugInserted(OtherCord, TestWorld.C3);
            desk.OnCordReturned(OtherCord);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
        }

        [Test]
        public void OpeningAnUnrelatedCircuitDoesNotChangeAnAcceptedOrder()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitOpened(OtherCord, TestWorld.C3, TestWorld.A1);
            Assert.That(desk.HalfPatched, Is.EqualTo(new[] { OtherCord }));
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void ReturningHalfPatchedCordsEmptiesTheBoard()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.A1);
            desk.OnPlugInserted(OtherCord, TestWorld.C3);

            desk.ReturnHalfPatchedCords();

            Assert.That(board.Returned, Is.EquivalentTo(new[] { Cord, OtherCord }));
            Assert.That(desk.HalfPatched, Is.Empty);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void SeveralCallsHoldTheirOwnCords()
        {
            Call first = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            Call second = calls.Add(new CallRequest(world.AdaCaller, world.Clara, 5, 40));
            desk.TryTake(second.Id);
            desk.OnCircuitClosed(OtherCord, TestWorld.C3, TestWorld.A1);

            Assert.That(desk.CallOn(Cord), Is.SameAs(first));
            Assert.That(desk.CallOn(OtherCord), Is.SameAs(second));
            calls.Tick(20);
            Assert.That(first.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(second.State, Is.EqualTo(CallState.Connected));
            Assert.That(board.Returned, Is.Empty);
        }
    }
}
