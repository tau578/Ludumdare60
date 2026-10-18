using NUnit.Framework;
using System.Reflection;
using Switchboard.Content;
using Switchboard.Domain;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    public sealed class CallAudioTests
    {
        [TestCase(0)]
        [TestCase(15)]
        public void RegisterPlaysOnFirstConnectionWithoutDependingOnFeeOrRelayDebounce(int fee)
        {
            var root = new GameObject("Cash register test");
            try
            {
                var audio = root.AddComponent<CallAudio>();
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CallAudio).GetField("feedbackSource", flags).SetValue(audio, source);
                typeof(CallAudio).GetField("cashRegister", flags).SetValue(audio,
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx/CashRegister.wav"));
                typeof(CallAudio).GetField("nextFeedback", flags).SetValue(audio, float.MaxValue);
                var calls = new CallService();
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
                var people = config.LoadPeople(config.LoadEconomy(), null);
                audio.Bind(calls);
                var call = calls.Add(new CallRequest(people.Callers[0], people.People[1], 10, 30, fee));
                calls.Take(call.Id);
                Assert.That(source.isPlaying, Is.False);
                calls.Connect(call.Id);
                Assert.That(source.isPlaying, Is.True, "The actual register clip must start even without EarningsService");
                source.Stop();
                calls.Connect(call.Id);
                Assert.That(source.isPlaying, Is.False, "Repeated Connect must not replay the register");
                calls.Hold(call.Id);
                calls.Resume(call.Id);
                Assert.That(source.isPlaying, Is.False, "Resume keeps its separate relay cue, without another register");
                typeof(CallAudio).GetMethod("OnDisable", flags).Invoke(audio, null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ConnectionCueOnlyFiresForSuccessfulConnectionsAndUnsubscribesOnDisable()
        {
            var root = new GameObject("Call audio test");
            try
            {
                var audio = root.AddComponent<CallAudio>();
                var calls = new CallService();
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
                var people = config.LoadPeople(config.LoadEconomy(), null);
                var call = calls.Add(new CallRequest(people.Callers[0], people.People[1], 10, 30));
                int cues = 0;
                audio.ConnectionSucceeded += () => cues++;
                audio.Bind(calls);
                calls.Take(call.Id);
                calls.BeginConnecting(call.Id);
                calls.CancelConnecting(call.Id);
                Assert.That(cues, Is.Zero, "Reading or starting an order must stay quiet.");
                calls.Connect(call.Id);
                Assert.That(cues, Is.EqualTo(1));
                calls.Connect(call.Id);
                Assert.That(cues, Is.EqualTo(1), "Already connected calls must not replay the cue.");
                calls.Hold(call.Id);
                Assert.That(cues, Is.EqualTo(1));
                calls.Resume(call.Id);
                Assert.That(cues, Is.EqualTo(2));
                // Non-ExecuteAlways behaviours don't receive lifecycle callbacks in Edit Mode.
                typeof(CallAudio).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(audio, null);
                calls.Hold(call.Id);
                calls.Resume(call.Id);
                Assert.That(cues, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
