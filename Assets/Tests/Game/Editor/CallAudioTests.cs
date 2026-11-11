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
        // The real file's first call, with the test's own fee.
        private static CallRequest Order(double fee)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
            CallScript first = config.LoadEconomy().StartPool[0];
            return new CallRequest(new CallScript(first.Id, first.Name, first.Icon, first.Message, first.From, first.To, 30, fee, 10));
        }

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
                audio.Bind(calls);
                var call = calls.Add(Order(fee));
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
                var call = calls.Add(Order(0));
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

        // S1 and S2 (13a): the alarm bell and the clock tick, on game time.
        private static CallAudio AlarmAudio(GameObject root, out AudioSource alarm, out AudioSource feedback)
        {
            var audio = root.AddComponent<CallAudio>();
            alarm = root.AddComponent<AudioSource>();
            feedback = root.AddComponent<AudioSource>();
            alarm.playOnAwake = feedback.playOnAwake = false;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CallAudio).GetField("alarmSource", flags).SetValue(audio, alarm);
            typeof(CallAudio).GetField("feedbackSource", flags).SetValue(audio, feedback);
            typeof(CallAudio).GetField("emergencyBell", flags).SetValue(audio,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx/EmergencyBell.wav"));
            typeof(CallAudio).GetField("clockTick", flags).SetValue(audio,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx/ClockTick.wav"));
            return audio;
        }

        private static GameStyle Style => AssetDatabase.LoadAssetAtPath<GameStyle>("Assets/Content/GameStyle.asset");

        private static CallRequest Emergency(string from, double response = 30) => new CallRequest(new CallScript("sos_" + from,
            "Caller", "caller-01", "Help!", new JackId(from), new JackId("F4"), 20, 10, 5, true, response));

        [Test]
        public void TheGamePrefabWiresTheAlarmBellAndTheTick()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/Game.prefab");
            var audio = prefab.GetComponent<CallAudio>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var source = (AudioSource)typeof(CallAudio).GetField("alarmSource", flags).GetValue(audio);
            Assert.That(source, Is.Not.Null);
            Assert.That(source.GetComponent<Switchboard.UI.AudioChannelSource>(), Is.Not.Null, "the volume cords reach it");
            Assert.That(((AudioClip)typeof(CallAudio).GetField("emergencyBell", flags).GetValue(audio)).name, Is.EqualTo("EmergencyBell"));
            Assert.That(((AudioClip)typeof(CallAudio).GetField("clockTick", flags).GetValue(audio)).name, Is.EqualTo("ClockTick"));
        }

        [Test]
        public void AnEmergencyRingsTheBellUntilItIsTakenAndARegularCallDoesNot()
        {
            var root = new GameObject("Alarm test");
            try
            {
                CallAudio audio = AlarmAudio(root, out AudioSource alarm, out _);
                var calls = new CallService();
                audio.Bind(calls, Style);
                calls.Add(Order(0));
                Assert.That(audio.BellsRung, Is.Zero, "a regular call keeps its single ring");

                Call sos = calls.Add(Emergency("A1"));
                Assert.That(audio.BellsRung, Is.EqualTo(1), "the bell rings as it arrives");
                Assert.That(alarm.isPlaying, Is.True);
                audio.Advance(5.9f);
                Assert.That(audio.BellsRung, Is.EqualTo(1));
                audio.Advance(.2f);
                Assert.That(audio.BellsRung, Is.EqualTo(2), "again every few seconds while it waits");

                audio.SetPaused(true);
                audio.Advance(20);
                Assert.That(audio.BellsRung, Is.EqualTo(2), "the pause stops it");
                audio.SetPaused(false);

                calls.Take(sos.Id);
                audio.Advance(.1f);
                Assert.That(alarm.isPlaying, Is.False, "taken: the bell stops at once");
                audio.Advance(20);
                Assert.That(audio.BellsRung, Is.EqualTo(2), "and doesn't ring again");
                typeof(CallAudio).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(audio, null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // D81 amended (13a pass 4): a tutorial step holds the game's clock, the bell's repeats and the ticks with it.
        [Test]
        public void ATutorialStepHoldsTheBellsRepeatsAndTheTicks()
        {
            var root = new GameObject("Held alarm test");
            try
            {
                CallAudio audio = AlarmAudio(root, out _, out _);
                var calls = new CallService();
                bool held = false;
                audio.Bind(calls, Style, () => held);
                Call waiting = calls.Add(Emergency("A1"));
                Call urgent = calls.Add(Emergency("B2"));
                calls.Take(urgent.Id);
                calls.Tick(urgent.ResponseSecondsLeft - 1);
                Assert.That(audio.BellsRung, Is.EqualTo(2), "setup: one bell each as they arrive");

                held = true;
                audio.Advance(20);
                Assert.That(audio.BellsRung, Is.EqualTo(2), "no repeat while a step shows");
                Assert.That(audio.TicksPlayed, Is.Zero, "no ticks while a step shows");

                held = false;
                audio.Advance(.1f);
                Assert.That(audio.TicksPlayed, Is.EqualTo(1), "the ticks go on once it ends");
                audio.Advance(6.1f);
                Assert.That(audio.BellsRung, Is.EqualTo(3), "and so do the bell's repeats");
                Assert.That(waiting.State, Is.EqualTo(CallState.Incoming));
                typeof(CallAudio).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(audio, null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TheClockTicksOnlyInTheLastSecondsAndFasterNearZero()
        {
            var root = new GameObject("Tick test");
            try
            {
                CallAudio audio = AlarmAudio(root, out _, out _);
                var calls = new CallService();
                GameStyle style = Style;
                audio.Bind(calls, style);
                Call sos = calls.Add(Emergency("A1"));
                calls.Take(sos.Id);
                float urgent = style.UrgentSeconds;
                double step = .05;
                while (sos.ResponseSecondsLeft > urgent + .1)
                {
                    calls.Tick(step);
                    audio.Advance((float)step);
                }
                Assert.That(audio.TicksPlayed, Is.Zero, "no ticks before the urgent seconds");

                int ticksEarly = 0, ticksLate = 0;
                while (sos.State != CallState.Ended)
                {
                    int before = audio.TicksPlayed;
                    double left = sos.ResponseSecondsLeft;
                    calls.Tick(step);
                    audio.Advance((float)step);
                    if (audio.TicksPlayed == before) continue;
                    if (left > urgent * .6f) ticksEarly++;
                    else if (left <= urgent * .3f) ticksLate++;
                }
                Assert.That(ticksEarly, Is.GreaterThan(0));
                Assert.That(ticksLate / (urgent * .3f), Is.GreaterThan(ticksEarly / (urgent * .4f)), "faster near 0");
                int afterEnd = audio.TicksPlayed;
                audio.Advance(5);
                Assert.That(audio.TicksPlayed, Is.EqualTo(afterEnd), "nothing once it ended");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TicksKeepInStepWithTheRingsPulse()
        {
            Assert.That(CallAudio.TickInterval(9, 10, 3), Is.EqualTo(1f).Within(1e-5), "every third pulse at first");
            Assert.That(CallAudio.TickInterval(5, 10, 3), Is.EqualTo(2f / 3).Within(1e-5));
            Assert.That(CallAudio.TickInterval(1, 10, 3), Is.EqualTo(1f / 3).Within(1e-5), "every pulse at the end");
        }
    }
}
