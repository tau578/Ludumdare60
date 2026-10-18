using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CallServiceTests
    {
        private TestWorld world;
        private CallService calls;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            calls = new CallService();
        }

        [Test]
        public void CallGoesFromIncomingThroughTakenAndConnectedToFinished()
        {
            var changes = new List<(CallState from, CallState to)>();
            var endings = new List<CallEndReason>();
            calls.CallStateChanged += e => changes.Add((e.From, e.To));
            calls.CallEnded += e => endings.Add(e.Reason);

            Call call = calls.Add(world.AdaWantsBasil(durationSeconds: 1));
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
            Assert.That(calls.Take(call.Id), Is.True);
            Assert.That(calls.InHand, Is.SameAs(call));
            Assert.That(calls.Connect(call.Id), Is.True);
            Assert.That(calls.InHand, Is.Null);

            calls.Tick(0.6);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            calls.Tick(0.6);

            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(calls.Live, Is.Empty);
            Assert.That(changes, Is.EqualTo(new[]
            {
                (CallState.Incoming, CallState.Taken),
                (CallState.Taken, CallState.Connected),
                (CallState.Connected, CallState.Ended)
            }));
            Assert.That(endings, Is.EqualTo(new[] { CallEndReason.Finished }));
        }

        [Test]
        public void ConnectingACallThatWasNotTakenIsRejected()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            Assert.That(calls.Connect(call.Id), Is.False);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void TakingACallTwiceIsRejected()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            Assert.That(calls.Take(call.Id), Is.True);
            Assert.That(calls.Take(call.Id), Is.False);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
        }

        [Test]
        public void TakingAnEndedCallIsRejected()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            calls.End(call.Id, CallEndReason.WrongConnection);
            Assert.That(calls.Take(call.Id), Is.False);
            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(calls.InHand, Is.Null);
        }

        [Test]
        public void TakingAnotherCardLeavesBothOrdersAccepted()
        {
            var changes = new List<(Call call, CallState from, CallState to)>();
            Call first = calls.Add(world.AdaWantsBasil());
            Call second = calls.Add(world.AdaWantsBasil());
            calls.Take(first.Id);
            calls.CallStateChanged += e => changes.Add((e.Call, e.From, e.To));

            Assert.That(calls.Take(second.Id), Is.True);

            Assert.That(first.State, Is.EqualTo(CallState.Taken));
            Assert.That(second.State, Is.EqualTo(CallState.Taken));
            Assert.That(calls.InHand, Is.SameAs(second));
            Assert.That(changes, Is.EqualTo(new[]
            {
                (second, CallState.Incoming, CallState.Taken)
            }));
        }

        [Test]
        public void EndingNeedsAReasonAndALiveCall()
        {
            Call call = calls.Add(world.AdaWantsBasil());
            calls.Take(call.Id);
            Assert.That(calls.End(call.Id, CallEndReason.None), Is.False);
            Assert.That(calls.End(call.Id, CallEndReason.WrongConnection), Is.True);
            Assert.That(calls.End(call.Id, CallEndReason.Finished), Is.False);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(calls.InHand, Is.Null);
        }

        [Test]
        public void OnlyConnectedCallsRunTheirDuration()
        {
            Call call = calls.Add(world.AdaWantsBasil(durationSeconds: 20));
            calls.Tick(30);
            calls.Take(call.Id);
            calls.Tick(30);
            Assert.That(call.State, Is.EqualTo(CallState.Taken));
            Assert.That(call.SecondsLeft, Is.EqualTo(20));
        }

        [Test]
        public void NewCallsGetDistinctIdsAndArriveInOrder()
        {
            Call first = calls.Add(world.AdaWantsBasil());
            Call second = calls.Add(world.AdaWantsBasil());
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(calls.Live, Is.EqualTo(new[] { first, second }));
            Assert.That(calls.Find(second.Id), Is.SameAs(second));
        }
    }
}
