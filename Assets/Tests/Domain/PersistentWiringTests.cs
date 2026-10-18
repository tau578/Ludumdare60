using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class PersistentWiringTests
    {
        private TestWorld world;
        private CallService calls;
        private FakeSwitchboard board;
        private OperatorDesk desk;
        private static readonly CordId Wire = new CordId(0);

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld(); calls = new CallService(); board = new FakeSwitchboard();
            desk = new OperatorDesk(calls, new ConnectionRule(world.Directory), board);
        }
        private Call Add() => calls.Add(world.AdaWantsBasil(connectionFeeCents: 10));

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void PrewiredCorrectPairStartsOnlyWhenAcceptedAndPaysOnce(string a, string b)
        {
            var wallet = new Wallet(); var earnings = new EarningsService(calls, wallet);
            desk.OnCircuitClosed(Wire, new JackId(a), new JackId(b));
            Call call = Add();
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
            Assert.That(wallet.Cents, Is.Zero);
            Assert.That(desk.TryTake(call.Id), Is.True);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(wallet.Cents, Is.EqualTo(10));
            Assert.That(desk.TryTake(call.Id), Is.False);
            desk.OnCircuitClosed(Wire, new JackId(a), new JackId(b));
            Assert.That(wallet.Cents, Is.EqualTo(10));
            Assert.That(board.Returned, Is.Empty); Assert.That(board.Refused, Is.Empty);
        }

        [Test]
        public void PrewiredWrongPairIsQuietUntilTheLooseEndIsReinsertedAgainstCorrectAnchor()
        {
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3);
            Call call = Add(); desk.TryTake(call.Id);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            desk.OnCircuitClosed(Wire, TestWorld.C3, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            desk.OnCircuitOpened(Wire, TestWorld.A1, TestWorld.C3);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void CorrectingPrewiredWrongPairStartsTheAcceptedCall()
        {
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3);
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnCircuitOpened(Wire, TestWorld.A1, TestWorld.C3);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
        }

        [Test]
        public void CorrectingByMovingTheFirstPlugAlsoWorks()
        {
            desk.OnCircuitClosed(Wire, TestWorld.C3, TestWorld.B2);
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnCircuitOpened(Wire, TestWorld.B2, TestWorld.C3);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
        }

        [Test]
        public void OlderAcceptedOrderConnectsEvenAfterAnotherWasOpened()
        {
            Call older = Add(); desk.TryTake(older.Id);
            Call newer = calls.Add(new CallRequest(world.Callers[2], world.Basil, 5, 20)); desk.TryTake(newer.Id);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.B2);
            Assert.That(older.State, Is.EqualTo(CallState.Connected));
            Assert.That(newer.State, Is.EqualTo(CallState.Taken));
            Assert.That(calls.InHand, Is.SameAs(newer));
        }

        [Test]
        public void UnrelatedFreeWiringDoesNotFailAnyAcceptedOrder()
        {
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnPlugInserted(Wire, new JackId("D4"));
            desk.OnCircuitClosed(Wire, new JackId("D4"), new JackId("E5"));
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            Assert.That(board.Returned, Is.Empty); Assert.That(board.Refused, Is.Empty);
        }

        [Test]
        public void WrongPatchTargetsTheAnchoredSubscribersOrderRatherThanLastOpened()
        {
            Call ada = Add(); desk.TryTake(ada.Id);
            Call clara = calls.Add(new CallRequest(world.Callers[2], world.Basil, 5, 20)); desk.TryTake(clara.Id);
            desk.OnPlugInserted(Wire, TestWorld.A1);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(ada.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(clara.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void CorrectPairWinsOverAnotherOrdersSharedRecipient()
        {
            Call ada = Add(); desk.TryTake(ada.Id);
            Call clara = calls.Add(new CallRequest(world.Callers[2], world.Basil, 5, 20)); desk.TryTake(clara.Id);
            desk.OnPlugInserted(Wire, TestWorld.B2);
            desk.OnCircuitClosed(Wire, TestWorld.B2, TestWorld.C3);
            Assert.That(clara.State, Is.EqualTo(CallState.Connected));
            Assert.That(ada.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void FinishedWireCanServeAnotherOrderWithoutBeingMoved()
        {
            Call first = Add(); desk.TryTake(first.Id); desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.B2);
            calls.End(first.Id, CallEndReason.Finished);
            Call next = Add(); desk.TryTake(next.Id);
            Assert.That(next.State, Is.EqualTo(CallState.Connected));
            Assert.That(next.Cord, Is.EqualTo(Wire)); Assert.That(board.Returned, Is.Empty);
        }

        [Test]
        public void RemovedPrewiredCircuitCannotConnectALaterOrder()
        {
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.B2); desk.OnCordReturned(Wire);
            Call call = Add(); desk.TryTake(call.Id);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void PrewiredCordAlreadyServingACallCannotBeStolen()
        {
            Call first = Add(); desk.TryTake(first.Id); desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.B2);
            Call next = Add(); desk.TryTake(next.Id);
            Assert.That(next.State, Is.EqualTo(CallState.Taken));
            Assert.That(desk.CallOn(Wire), Is.SameAs(first));
        }

        [TestCase("A1")]
        [TestCase("B2")]
        public void EstablishingCorrectEndAgainstWrongOldEndIsQuietThenOtherEndIsChecked(string correct)
        {
            var anchor = new JackId(correct); var wrong = new JackId("D4");
            desk.OnCircuitClosed(Wire, TestWorld.C3, wrong);
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnCircuitOpened(Wire, wrong, TestWorld.C3);
            desk.OnCircuitClosed(Wire, anchor, wrong, wrong);
            Assert.That(call.State, Is.EqualTo(CallState.Taken), "New correct plug must establish an anchor without a penalty");
            desk.OnCircuitOpened(Wire, anchor, wrong);
            desk.OnCircuitClosed(Wire, anchor, TestWorld.C3, anchor);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
        }

        [TestCase("A1")]
        [TestCase("B2")]
        public void FreshWireWithCorrectFirstEndChecksItsSecondEnd(string correct)
        {
            Call call = Add(); desk.TryTake(call.Id);
            var anchor = new JackId(correct);
            desk.OnPlugInserted(Wire, anchor);
            desk.OnCircuitClosed(Wire, anchor, TestWorld.C3, anchor);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
        }

        [Test]
        public void RemovingCorrectAnchorForgetsItsFormerCorrectness()
        {
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3);
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnCircuitOpened(Wire, TestWorld.C3, TestWorld.A1);
            desk.OnCircuitClosed(Wire, new JackId("D4"), TestWorld.C3, TestWorld.C3);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            desk.OnCircuitOpened(Wire, TestWorld.C3, new JackId("D4"));
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.C3);
            Assert.That(call.State, Is.EqualTo(CallState.Taken), "Returning correct end establishes a new anchor");
        }

        [Test]
        public void AutomaticRestorationOfWrongPairDoesNotFailOrder()
        {
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3);
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnCircuitOpened(Wire, TestWorld.A1, TestWorld.C3);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3); // No manual anchor on cancel/restore.
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void WrongAnchorCannotFailOrderAtTheNewlyInsertedEnd()
        {
            Call call = Add(); desk.TryTake(call.Id);
            desk.OnPlugInserted(Wire, TestWorld.C3);
            desk.OnCircuitClosed(Wire, TestWorld.C3, TestWorld.A1, TestWorld.C3);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void NewerAcceptanceDoesNotChangeAnchoredOrder()
        {
            Call older = Add(); desk.TryTake(older.Id);
            desk.OnPlugInserted(Wire, TestWorld.A1);
            Call newer = calls.Add(new CallRequest(world.Callers[2], world.Basil, 5, 20)); desk.TryTake(newer.Id);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(older.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(newer.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void SuccessWinsEvenWhenAnchorAlsoBelongsToAnotherOrder()
        {
            Call older = Add(); desk.TryTake(older.Id);
            Call other = calls.Add(new CallRequest(world.Callers[2], world.Basil, 5, 20)); desk.TryTake(other.Id);
            desk.OnCircuitClosed(Wire, TestWorld.B2, TestWorld.C3, TestWorld.B2);
            Assert.That(older.State, Is.EqualTo(CallState.Taken));
            Assert.That(other.State, Is.EqualTo(CallState.Connected));
        }

        [Test]
        public void UnacceptedOrderAtAnchorCannotBeFailed()
        {
            Call call = Add();
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
            desk.TryTake(call.Id);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void NewCallbackNeedsAFreshManualInsertionNotJustAcceptance()
        {
            Call first = Add(); desk.TryTake(first.Id);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Call next = Add(); desk.TryTake(next.Id);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3);
            Assert.That(next.State, Is.EqualTo(CallState.Taken));
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(next.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
        }

        [Test]
        public void ExpiredAnchoredOrderCannotFailUnrelatedAcceptedOrder()
        {
            Call expired = Add(); desk.TryTake(expired.Id);
            desk.OnPlugInserted(Wire, TestWorld.A1);
            calls.End(expired.Id, CallEndReason.Missed);
            Call newer = calls.Add(new CallRequest(world.Callers[2], world.Basil, 5, 20)); desk.TryTake(newer.Id);
            desk.OnCircuitClosed(Wire, TestWorld.A1, TestWorld.C3, TestWorld.A1);
            Assert.That(newer.State, Is.EqualTo(CallState.Taken));
            Assert.That(expired.EndReason, Is.EqualTo(CallEndReason.Missed));
        }
    }
}
