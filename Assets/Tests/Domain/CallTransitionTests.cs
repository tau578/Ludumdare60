using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>The whole Step 2a transition matrix of Design 5.1: every verb from every state.</summary>
    public sealed class CallTransitionTests
    {
        public enum Verb
        {
            Take,
            BeginConnecting,
            CancelConnecting,
            Connect,
            EndWrongConnection,
            EndFinished
        }

        private static readonly CallState Incoming = CallState.Incoming;
        private static readonly CallState Taken = CallState.Taken;
        private static readonly CallState Connecting = CallState.Connecting;
        private static readonly CallState Connected = CallState.Connected;
        private static readonly CallState Ended = CallState.Ended;

        private static IEnumerable<TestCaseData> Matrix()
        {
            yield return Case(Incoming, Verb.Take, true, Taken);
            yield return Case(Incoming, Verb.BeginConnecting, false, Incoming);
            yield return Case(Incoming, Verb.CancelConnecting, false, Incoming);
            yield return Case(Incoming, Verb.Connect, false, Incoming);
            yield return Case(Incoming, Verb.EndWrongConnection, true, Ended);

            yield return Case(Taken, Verb.Take, false, Taken);
            yield return Case(Taken, Verb.BeginConnecting, true, Connecting);
            yield return Case(Taken, Verb.CancelConnecting, false, Taken);
            yield return Case(Taken, Verb.Connect, true, Connected);
            yield return Case(Taken, Verb.EndWrongConnection, true, Ended);

            yield return Case(Connecting, Verb.Take, false, Connecting);
            yield return Case(Connecting, Verb.BeginConnecting, false, Connecting);
            yield return Case(Connecting, Verb.CancelConnecting, true, Taken);
            yield return Case(Connecting, Verb.Connect, true, Connected);
            yield return Case(Connecting, Verb.EndWrongConnection, true, Ended);

            yield return Case(Connected, Verb.Take, false, Connected);
            yield return Case(Connected, Verb.BeginConnecting, false, Connected);
            yield return Case(Connected, Verb.CancelConnecting, false, Connected);
            yield return Case(Connected, Verb.Connect, false, Connected);
            yield return Case(Connected, Verb.EndFinished, true, Ended);

            yield return Case(Ended, Verb.Take, false, Ended);
            yield return Case(Ended, Verb.BeginConnecting, false, Ended);
            yield return Case(Ended, Verb.CancelConnecting, false, Ended);
            yield return Case(Ended, Verb.Connect, false, Ended);
            yield return Case(Ended, Verb.EndFinished, false, Ended);
        }

        private static TestCaseData Case(CallState from, Verb verb, bool allowed, CallState to) =>
            new TestCaseData(from, verb, allowed, to).SetName($"{from} {verb} -> {(allowed ? to.ToString() : "refused")}");

        [TestCaseSource(nameof(Matrix))]
        public void Transition(CallState from, Verb verb, bool allowed, CallState to)
        {
            var world = new TestWorld();
            var calls = new CallService();
            Call call = InState(calls, world, from);

            Assert.That(Apply(calls, call, verb), Is.EqualTo(allowed));
            Assert.That(call.State, Is.EqualTo(to));
            bool inHand = to == Taken || to == Connecting;
            Assert.That(calls.InHand, inHand ? Is.SameAs(call) : Is.Null);
        }

        [TestCase(CallState.Taken)]
        [TestCase(CallState.Connecting)]
        public void TakingAnotherCardPutsTheCallInHandBackToRinging(CallState inHandState)
        {
            var world = new TestWorld();
            var calls = new CallService();
            Call old = InState(calls, world, inHandState);
            Call other = calls.Add(world.AdaWantsBasil());

            Assert.That(calls.Take(other.Id), Is.True);

            Assert.That(old.State, Is.EqualTo(CallState.Incoming));
            Assert.That(other.State, Is.EqualTo(CallState.Taken));
            Assert.That(calls.InHand, Is.SameAs(other));
            Assert.That(calls.Take(old.Id), Is.True, "the swapped-out card can be taken again");
            Assert.That(other.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void TakingACardLeavesConnectedCallsAlone()
        {
            var world = new TestWorld();
            var calls = new CallService();
            Call connected = InState(calls, world, CallState.Connected);
            Call other = calls.Add(world.AdaWantsBasil());

            Assert.That(calls.Take(other.Id), Is.True);
            Assert.That(connected.State, Is.EqualTo(CallState.Connected));
        }

        private static Call InState(CallService calls, TestWorld world, CallState state)
        {
            Call call = calls.Add(world.AdaWantsBasil());
            if (state == Ended) calls.End(call.Id, CallEndReason.WrongConnection);
            if (state == Taken || state == Connecting || state == Connected) calls.Take(call.Id);
            if (state == Connecting) calls.BeginConnecting(call.Id);
            if (state == Connected) calls.Connect(call.Id);
            Assert.That(call.State, Is.EqualTo(state), "setup");
            return call;
        }

        private static bool Apply(CallService calls, Call call, Verb verb)
        {
            switch (verb)
            {
                case Verb.Take: return calls.Take(call.Id);
                case Verb.BeginConnecting: return calls.BeginConnecting(call.Id);
                case Verb.CancelConnecting: return calls.CancelConnecting(call.Id);
                case Verb.Connect: return calls.Connect(call.Id);
                case Verb.EndWrongConnection: return calls.End(call.Id, CallEndReason.WrongConnection);
                default: return calls.End(call.Id, CallEndReason.Finished);
            }
        }
    }
}
