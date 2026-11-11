using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// D80 (13a pass 2) on the real cards in the Final scene: the newest card is the top one, the cards rest on the lowest
    /// open slots, and a gap closes from above; an ended card keeps its slot while its stamp shows (D52).
    /// </summary>
    public sealed class CardOrderTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
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
            queue = All<CallQueueView>().Single();
            // Snap instead of sliding, so one frame puts every card in its place.
            queue.GetType().GetField("slideSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(queue, 0f);
            Assert.That(session.Calls.Live.Count, Is.Zero, "setup: the queue hasn't ticked");
            Assert.That(session.Stats.ClientSlots, Is.EqualTo(3), "setup: the start board's three windows");
            callNumber = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private Call AddCall(string from, string to, double? patience = null) =>
            session.Calls.Add(new CallRequest(new CallScript("test." + ++callNumber, "Test Caller " + callNumber, "",
                "Connect me to <b>Someone</b>.", new JackId(from), new JackId(to), 20, 10, 5, false, 0, patience)));

        private CallCardView CardOf(Call call) => All<CallCardView>().SingleOrDefault(c => c.Call == call);

        private void Frame()
        {
            Canvas.ForceUpdateCanvases();
            Invoke(queue, "LateUpdate");
        }

        private Vector2 Slot(int index) => Field<Vector2[]>(queue, "slotCentres")[index];

        private List<Call> ShownCalls()
        {
            var calls = new List<Call>();
            queue.CollectShownCalls(calls);
            return calls;
        }

        private void AssertRestsOn(Call call, int slot, string why) =>
            Assert.That(Vector2.Distance(CardOf(call).RestPosition, Slot(slot)), Is.LessThan(.5f), why);

        [Test]
        public void TheNewestCardIsTheTopOneAndTheCardsRestOnTheLowestSlots()
        {
            Call first = AddCall("A1", "A2");
            Frame();
            Assert.That(Slot(0).y, Is.GreaterThan(Slot(1).y + 10), "setup: the slots are laid out top to bottom");
            Assert.That(Slot(1).y, Is.GreaterThan(Slot(2).y + 10), "setup: the slots are laid out top to bottom");
            AssertRestsOn(first, 2, "a lone card rests on the lowest open slot");

            Call second = AddCall("B1", "B2");
            Call third = AddCall("C1", "C2");
            Frame();
            Assert.That(ShownCalls(), Is.EqualTo(new[] { third, second, first }), "newest at the top");
            AssertRestsOn(third, 0, "the newest on top");
            AssertRestsOn(second, 1, "the middle one");
            AssertRestsOn(first, 2, "the oldest stays at the bottom");
            Assert.That(CardOf(third).transform.GetSiblingIndex(), Is.GreaterThan(CardOf(first).transform.GetSiblingIndex()),
                "an upper card draws over the one below it, so its name plate stays readable");
        }

        [Test]
        public void AGapClosesFromAbove()
        {
            Call bottom = AddCall("A1", "A2");
            Call middle = AddCall("B1", "B2");
            Call top = AddCall("C1", "C2");
            Frame();

            session.Calls.End(middle.Id, CallEndReason.Cleared);
            Invoke(CardOf(middle), "Update");
            Frame();
            Assert.That(ShownCalls(), Is.EqualTo(new[] { top, bottom }));
            AssertRestsOn(bottom, 2, "the card below the gap stays put");
            AssertRestsOn(top, 1, "the card above the gap slides down into it");
        }

        [Test]
        public void AStampKeepsItsSlotAndTheNextCardArrivesOnTopAfterIt()
        {
            Call bottom = AddCall("A1", "A2");
            Call middle = AddCall("B1", "B2", 10);
            Call top = AddCall("C1", "C2");
            Frame();

            session.Calls.Tick(10.1);
            Assert.That(middle.EndReason, Is.EqualTo(CallEndReason.HungUp));
            CallCardView stamped = CardOf(middle);
            Assert.That(stamped.IsStampShowing, Is.True);
            Call next = AddCall("A3", "B3");
            Frame();
            Assert.That(CardOf(next), Is.Null, "the new card waits for the stamp (D52)");
            AssertRestsOn(middle, 1, "the stamp keeps its slot");
            AssertRestsOn(top, 0, "nothing moves while the stamp shows");

            Invoke(queue, "OnCardFinished", stamped);
            Frame();
            Assert.That(ShownCalls(), Is.EqualTo(new[] { next, top, bottom }));
            AssertRestsOn(next, 0, "the waiting call arrives on top");
            AssertRestsOn(top, 1, "the card above the gap slid down");
            AssertRestsOn(bottom, 2, "the card below stayed");
        }
    }
}
