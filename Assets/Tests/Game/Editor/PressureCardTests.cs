using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Domain;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// Step 13a (D63) on the real cards in the Final scene: the amber patience ring, no "Line busy" label (D83), and the
    /// "Hung up" stamp. A loaded game, so no tutorial; the tests add their own calls.
    /// </summary>
    public sealed class PressureCardTests
    {
        private Scene scene;
        private GameSession session;
        private GameStyle style;
        private int callNumber;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static object Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static T Field<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            foreach (var transform in All<Transform>().Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject))
                    PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var canvas in All<Canvas>().Where(c => c.transform.parent == null))
            {
                canvas.renderMode = RenderMode.WorldSpace;
                var rect = (RectTransform)canvas.transform;
                rect.sizeDelta = new Vector2(1920, 1080);
                rect.localScale = Vector3.one;
                rect.position = new Vector3(960, 540, 0);
            }
            Canvas.ForceUpdateCanvases();
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Install", null, new SaveData { version = SaveData.CurrentVersion, cents = 1000 });
            session = installer.Session;
            style = AssetDatabase.LoadAssetAtPath<GameStyle>("Assets/Content/GameStyle.asset");
            Assert.That(session.Calls.Live.Count, Is.Zero, "setup: the queue hasn't ticked");
            callNumber = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private Call AddCall(string from, string to, double? patience = null, bool emergency = false) =>
            session.Calls.Add(new CallRequest(new CallScript("test." + ++callNumber, "Test Caller " + callNumber, "",
                "Put me through, please.", new JackId(from), new JackId(to), 20, 10, 5, emergency, emergency ? 30 : 0, patience)));

        private CallCardView CardOf(Call call) => All<CallCardView>().SingleOrDefault(c => c.Call == call);

        private static void Frame(CallCardView card)
        {
            card.TickMotion(0, 0);
            Invoke(card, "LateUpdate");
        }

        private static bool SameRgb(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < .01f && Mathf.Abs(a.g - b.g) < .01f && Mathf.Abs(a.b - b.b) < .01f;

        [Test]
        public void TheRealFileGivesCallersPatience() =>
            Assert.That(AddCall("A1", "B2").HasPatience, Is.True, "Economy.json's defaultPatienceSeconds reaches the calls");

        [Test]
        public void PatienceShowsOnTheRingInAttentionAmberNeverAlarm()
        {
            Call call = AddCall("A1", "B2", 20);
            CallCardView card = CardOf(call);
            Frame(card);
            Assert.That(card.IsRingShown, Is.True);
            Assert.That(card.IsPatienceRing, Is.True);
            Assert.That(SameRgb(card.RingColor, style.AttentionHover), Is.True, $"amber, was {card.RingColor}");
            Assert.That(SameRgb(card.RingColor, style.AlarmGlow), Is.False);

            session.Calls.Tick(17);
            Frame(card);
            Assert.That(SameRgb(card.RingColor, style.AttentionHover), Is.True, "still amber in the last seconds");
            float first = card.RingColor.a;
            card.TickMotion(.15f, 0);
            Invoke(card, "LateUpdate");
            Assert.That(card.RingColor.a, Is.Not.EqualTo(first), "a gentle pulse in the last seconds");
        }

        [Test]
        public void AnEmergencyKeepsItsOwnRing()
        {
            Call call = AddCall("A1", "B2", emergency: true);
            CallCardView card = CardOf(call);
            Frame(card);
            Assert.That(card.IsRingShown, Is.True);
            Assert.That(card.IsPatienceRing, Is.False);
        }

        [Test]
        public void AHeldCallerShowsNoRingAndACallerWhoWaitsForeverNeither()
        {
            Call held = AddCall("A1", "B2", 20);
            session.Calls.HoldWaiting(held.Id, true);
            Frame(CardOf(held));
            Assert.That(CardOf(held).IsRingShown, Is.False, "the tutorial's caller can't hang up");
            session.Calls.HoldWaiting(held.Id, false);
            Frame(CardOf(held));
            Assert.That(CardOf(held).IsRingShown, Is.True, "released, the ring shows");

            Call forever = AddCall("C1", "C2", 0);
            Frame(CardOf(forever));
            Assert.That(CardOf(forever).IsRingShown, Is.False);
        }

        [Test]
        public void TheRingGoesOnceTheCallConnects()
        {
            Call call = AddCall("A1", "B2", 20);
            session.Calls.Take(call.Id);
            session.Calls.Connect(call.Id);
            CallCardView card = CardOf(call);
            card.Refresh();
            Frame(card);
            Assert.That(card.IsPatienceRing, Is.False, "the patience ring goes");
            // D83: the card is a tab now, and its ring shows the talk's time left instead.
            Assert.That(card.IsTab, Is.True);
        }

        [Test]
        public void ACallToABusyLineKeepsItsPayAndHasNoLineBusyLabel()
        {
            // D83: no "Line busy" label; the choice is to cut the talking call off or let the caller wait.
            Call talk = AddCall("A1", "B2");
            session.Calls.Take(talk.Id);
            session.Calls.Connect(talk.Id);
            Call waiting = AddCall("C1", "B2");
            CallCardView card = CardOf(waiting);
            Frame(card);
            Assert.That(waiting.LineBusy, Is.True, "the Domain still knows (13e reads it)");
            TMP_Text fee = card.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Connection reward");
            Assert.That(fee.gameObject.activeSelf, Is.True, "the pay badge stays");
            Assert.That(card.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "Line busy"), Is.False);
        }

        [Test]
        public void AHungUpCallerLeavesAHungUpStampInNeutralInk()
        {
            Call call = AddCall("A1", "B2", 10);
            CallCardView card = CardOf(call);
            session.Calls.Tick(10.1);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.HungUp));
            var stamp = Field<TMP_Text>(card, "stampLabel");
            Assert.That(card.IsStampShowing, Is.True);
            Assert.That(stamp.text, Is.EqualTo("Hung up"));
            Assert.That(SameRgb(stamp.color, style.CutOffInk), Is.True);
        }
    }
}
