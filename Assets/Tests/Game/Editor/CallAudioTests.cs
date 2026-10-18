using NUnit.Framework;
using Switchboard.Content;
using Switchboard.Domain;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    public sealed class CallAudioTests
    {
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
