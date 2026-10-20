using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>D58: which caller jacks light up, and how.</summary>
    public sealed class CallerLightsTests
    {
        private static readonly CordId Cord = new CordId(1);
        private static readonly JackId D4 = new JackId("D4");

        private TestWorld world;
        private CallService calls;
        private OperatorDesk desk;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
            desk = new OperatorDesk(calls, new ConnectionRule(), new FakeSwitchboard());
        }

        private JackLight At(JackId jack, Call inHand) => CallerLights.AtJack(jack, calls.Live, inHand, desk);

        [Test]
        public void AWaitingCallBlinksAtItsCallersJackOnly()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            Assert.That(CallerLights.Of(call, null), Is.EqualTo(JackLight.Waiting));
            Assert.That(At(TestWorld.A1, null), Is.EqualTo(JackLight.Waiting));
            Assert.That(At(TestWorld.B2, null), Is.EqualTo(JackLight.Off), "the recipient is looked up, not lit");
            Assert.That(At(TestWorld.C3, null), Is.EqualTo(JackLight.Off));
        }

        [Test]
        public void TheCallInHandStandsOutAndAnotherAcceptedCallStillBlinks()
        {
            Call first = calls.Add(world.AdaWantsBasil());
            Call second = calls.Add(TestWorld.Request(TestWorld.C3, D4));
            desk.TryTake(first.Id);
            desk.TryTake(second.Id);
            Assert.That(At(TestWorld.C3, second), Is.EqualTo(JackLight.InHand));
            Assert.That(At(TestWorld.A1, second), Is.EqualTo(JackLight.Waiting), "accepted, but not in hand: it still waits");
            Assert.That(At(TestWorld.A1, first), Is.EqualTo(JackLight.InHand));
            Assert.That(At(TestWorld.C3, first), Is.EqualTo(JackLight.Waiting));
        }

        [Test]
        public void ALoosePlugInTheCallersJackPutsItOutAndPullingItLightsItAgain()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            desk.TryTake(call.Id);
            desk.OnPlugInserted(Cord, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            Assert.That(At(TestWorld.A1, call), Is.EqualTo(JackLight.Off), "the operator is patching from it");
            desk.OnCordReturned(Cord);
            Assert.That(At(TestWorld.A1, call), Is.EqualTo(JackLight.InHand));
        }

        [Test]
        public void APlugInTheRecipientsJackKeepsTheCallersLight()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            desk.TryTake(call.Id);
            desk.OnPlugInserted(Cord, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            Assert.That(At(TestWorld.A1, call), Is.EqualTo(JackLight.InHand), "the caller's end is still to plug");
        }

        [Test]
        public void AnOldCircuitInTheJackDoesNotHideAWaitingCaller()
        {
            // A finished call leaves its cord plugged (D45): the next caller from that jack must still show.
            Call old = calls.Add(TestWorld.Request(D4, TestWorld.A1));
            desk.TryTake(old.Id);
            desk.OnCircuitClosed(Cord, D4, TestWorld.A1);
            calls.Tick(old.SecondsLeft + 1);
            Assert.That(old.State, Is.EqualTo(CallState.Ended));
            Call next = calls.Add(world.AdaWantsBasil());
            Assert.That(At(TestWorld.A1, null), Is.EqualTo(JackLight.Waiting));
            desk.TryTake(next.Id);
            Assert.That(At(TestWorld.A1, next), Is.EqualTo(JackLight.InHand));
        }

        [Test]
        public void ConnectingEndingOrHoldingPutsTheLightOut()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            desk.TryTake(call.Id);
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(CallerLights.Of(call, call), Is.EqualTo(JackLight.Off));
            Assert.That(At(TestWorld.A1, null), Is.EqualTo(JackLight.Off));
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            Assert.That(CallerLights.Of(call, null), Is.EqualTo(JackLight.Off), "on hold is no new call");

            Call waiting = calls.Add(TestWorld.Request(TestWorld.C3, D4));
            calls.End(waiting.Id, CallEndReason.Cleared);
            Assert.That(CallerLights.Of(waiting, null), Is.EqualTo(JackLight.Off));
            Assert.That(At(TestWorld.C3, null), Is.EqualTo(JackLight.Off));
        }

        [Test]
        public void ASharedCallersJackLightsWhileAnyOfItsCallsWaits()
        {
            Call first = calls.Add(world.AdaWantsBasil());
            Call second = calls.Add(TestWorld.Request(TestWorld.A1, TestWorld.C3));
            Assert.That(At(TestWorld.A1, null), Is.EqualTo(JackLight.Waiting));
            desk.TryTake(second.Id);
            Assert.That(At(TestWorld.A1, second), Is.EqualTo(JackLight.InHand), "the brightest wins");
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3);
            Assert.That(second.State, Is.EqualTo(CallState.Connected));
            Assert.That(At(TestWorld.A1, null), Is.EqualTo(JackLight.Waiting), "the other call still waits");
            calls.End(first.Id, CallEndReason.Cleared);
            Assert.That(At(TestWorld.A1, null), Is.EqualTo(JackLight.Off));
        }

        [Test]
        public void OnlyTheGivenCallsAreConsidered()
        {
            calls.Add(world.AdaWantsBasil());
            Assert.That(CallerLights.AtJack(TestWorld.A1, new Call[0], null, desk), Is.EqualTo(JackLight.Off),
                "a call whose card hasn't shown yet doesn't light");
            Assert.That(CallerLights.AtJack(TestWorld.A1, null, null, desk), Is.EqualTo(JackLight.Off));
            Assert.That(CallerLights.AtJack(TestWorld.A1, calls.Live, null, null), Is.EqualTo(JackLight.Waiting));
        }
    }
}
