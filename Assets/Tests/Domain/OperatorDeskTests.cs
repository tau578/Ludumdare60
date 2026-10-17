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
        public void WrongPairEndsTheCallAndReturnsTheCord()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3);

            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(board.Returned, Is.EqualTo(new[] { Cord }));
            Assert.That(board.Refused, Is.Empty);
            Assert.That(calls.InHand, Is.Null);
        }

        [Test]
        public void CircuitClosedWithNoCallInHandIsRefused()
        {
            Call waiting = calls.Add(world.AdaWantsBasil());
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);

            Assert.That(board.Refused, Is.EqualTo(new[] { Cord }));
            Assert.That(board.Returned, Is.Empty);
            Assert.That(waiting.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void FinishedCallReturnsItsCord()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            calls.Tick(20);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(board.Returned, Is.EqualTo(new[] { Cord }));
            Assert.That(desk.CallOn(Cord), Is.Null);
        }

        [Test]
        public void PulledPlugCutsTheCallOffAndReturnsTheCord()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCircuitOpened(Cord, TestWorld.B2, TestWorld.A1);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(board.Returned, Is.EqualTo(new[] { Cord }));
        }

        [Test]
        public void CordBackInTheTrayCutsItsCallOff()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            desk.OnCordReturned(Cord);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.DisconnectedEarly));
            Assert.That(board.Returned, Is.EqualTo(new[] { Cord }));
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
    }
}
