using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// D83 (13a pass 2b) on the real cards in the Final scene: a talking call's card shrinks into a tab in a 2x2 grid in the
    /// 4th window's space and frees its window; the tab shows the time left, the money so far, "On hold", then its stamp;
    /// tabs take no clicks and fit their place at every board stage.
    /// </summary>
    public sealed class TalkingTabTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
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
            foreach (var canvas in All<Canvas>().Where(c => c.transform.parent == null))
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Install", null, new SaveData { version = SaveData.CurrentVersion, cents = 1000 });
            session = installer.Session;
            style = Field<GameStyle>(installer, "style");
            queue = All<CallQueueView>().Single();
            queue.GetType().GetField("slideSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(queue, 0f);
            Assert.That(session.Calls.Live.Count, Is.Zero, "setup: the queue hasn't ticked");
            callNumber = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private Call AddCall(string from, string to) =>
            session.Calls.Add(new CallRequest(new CallScript("test." + ++callNumber, "Test Caller " + callNumber, "",
                "Connect me to <b>Someone</b>.", new JackId(from), new JackId(to), 20, 10, 5, false, 0, null)));

        private CallCardView CardOf(Call call) => All<CallCardView>().SingleOrDefault(c => c.Call == call);

        private void Connect(Call call)
        {
            session.Calls.Take(call.Id);
            session.Calls.Connect(call.Id);
        }

        // Each frame: the queue places and shrinks the cards (snapping, as slideSpeed is 0), then the cards poll their rings.
        private void Frame()
        {
            Canvas.ForceUpdateCanvases();
            Invoke(queue, "LateUpdate");
            foreach (CallCardView card in All<CallCardView>().Where(c => c.Call != null)) Invoke(card, "LateUpdate");
        }

        private List<Call> WaitingCards()
        {
            var calls = new List<Call>();
            queue.CollectShownCalls(calls);
            return calls;
        }

        private static Rect WorldRect(RectTransform rect, float shrink = 1)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var box = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            return new Rect(box.center - box.size * shrink * .5f, box.size * shrink);
        }

        [Test]
        public void AConnectedCardShrinksIntoATabAndItsWindowTakesTheNextCaller()
        {
            Call bottom = AddCall("A1", "A2");
            Call middle = AddCall("B1", "B2");
            Call top = AddCall("C1", "C2");
            Frame();

            Connect(middle);
            Frame();
            CallCardView tab = CardOf(middle);
            Assert.That(tab.IsTab, Is.True);
            Assert.That(queue.Tabs, Is.EqualTo(new[] { tab }));
            Assert.That(WaitingCards(), Is.EqualTo(new[] { top, bottom }), "the talking call left its window; the one above slid down");
            Assert.That(tab.Scale, Is.EqualTo(queue.TabScaleOf(tab)).Within(.001f), "shrunk to the tab size");
            Assert.That(tab.Scale, Is.LessThan(.75f));
            Assert.That(Vector2.Distance(tab.RestPosition, queue.TabCentre(0)), Is.LessThan(.5f), "in the grid's first cell");
            Assert.That(tab.GetComponent<CanvasGroup>().blocksRaycasts, Is.False, "a tab takes no clicks");

            Call next = session.Calls.Add(new CallRequest(new CallScript("test.next", "Next Caller", "", "Connect me to <b>X</b>.",
                new JackId("D1"), new JackId("D2"), 20, 10, 5, false, 0, null)));
            Frame();
            Assert.That(CardOf(next), Is.Not.Null, "the freed window shows the next caller at once");
            Assert.That(WaitingCards(), Is.EqualTo(new[] { next, top, bottom }));
            Assert.That(CardOf(next).IsTab, Is.False);
        }

        [Test]
        public void ATabShowsTheTimeLeftTheMoneySoFarOnHoldAndItsStamp()
        {
            Call call = AddCall("A1", "A2");
            Frame();
            Connect(call);
            session.Calls.Tick(5);
            Frame();
            CallCardView tab = CardOf(call);

            Assert.That(tab.IsRingShown, Is.True, "the time-left ring");
            Assert.That(tab.RingColor, Is.EqualTo(style.GoodRing), "in Good green, never the alarm");
            var ring = Field<Image>(tab, "responseRing");
            Assert.That(ring.fillAmount, Is.EqualTo((float)(call.SecondsLeft / call.Request.DurationSeconds)).Within(.01f));
            Assert.That(tab.EarningsText, Is.EqualTo(MoneyFormat.Compact((long)System.Math.Floor(call.CentsEarned + 1e-9))));
            Assert.That(tab.IsTabPlateShown, Is.True, "the money sits on its plate");
            Assert.That(tab.NamePlateText, Is.Null, "no name on a talking tab");
            Assert.That(tab.IsSpeechBubbleVisible, Is.False, "a tab has no bubble");

            session.Calls.Hold(call.Id);
            string frozen = tab.EarningsText;
            session.Calls.Tick(3);
            Frame();
            Assert.That(tab.NamePlateText, Is.EqualTo("On hold"));
            Assert.That(tab.EarningsText, Is.EqualTo(frozen), "the money is frozen while held");
            Assert.That(tab.RingColor.a, Is.LessThan(style.GoodRing.a), "the ring dims while held");

            session.Calls.Resume(call.Id);
            Frame();
            Assert.That(tab.NamePlateText, Is.Null, "resumed: a talking tab again");
            Assert.That(tab.RingColor, Is.EqualTo(style.GoodRing));

            session.Calls.Tick(call.SecondsLeft + .1);
            Frame();
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(tab.IsTab && tab.IsStampShowing, Is.True, "the stamp shows on the tab");
            Assert.That(Field<TMPro.TMP_Text>(tab, "stampLabel").text, Does.StartWith("Paid "));
            Assert.That(tab.IsRingShown, Is.False);
            Assert.That(queue.Tabs, Does.Contain(tab), "the tab keeps its cell for the stamp hold (D52)");

            Invoke(queue, "OnCardFinished", tab);
            Assert.That(queue.Tabs, Is.Empty, "then it leaves");
            Assert.That(tab.Call, Is.Null);
            Assert.That(tab.IsTab, Is.False, "the pooled card is a full card again");
            Assert.That(tab.Scale, Is.EqualTo(1));
            Assert.That(tab.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
        }

        [Test]
        public void ATabsStampAndPopupsKeepAReadableSize()
        {
            Call call = AddCall("A1", "A2");
            Frame();
            Connect(call);
            Frame();
            CallCardView tab = CardOf(call);
            var fee = Field<FloatingTextView>(tab, "feePopup");
            Assert.That(fee.transform.lossyScale.x, Is.EqualTo(1).Within(.01f), "the fee popup keeps its full size on a tab");

            session.Calls.End(call.Id, CallEndReason.DisconnectedEarly);
            // Ends the stamp's pop at once (it runs on real time, which an EditMode test doesn't advance).
            typeof(CallCardView).GetField("stampPopTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tab, 10f);
            Invoke(tab, "AnimateStamp");
            Assert.That(tab.transform.localScale.x, Is.EqualTo(tab.Scale).Within(.001f), "showing the stamp keeps the tab's size");
            var stamp = Field<GameObject>(tab, "stamp");
            float share = Field<float>(tab, "tabStampScale");
            Vector3 full = Field<Vector3>(tab, "stampScale");
            Assert.That(stamp.transform.lossyScale.x, Is.EqualTo(full.x * share).Within(.01f), "the stamp is a set share of a card's stamp");
            Assert.That(Field<TMPro.TMP_Text>(tab, "stampLabel").text, Is.EqualTo("Cut off"));
        }

        [Test]
        public void FourTabsFillTheGridLeftToRightTopToBottomAndCloseUp()
        {
            var calls = new List<Call>();
            string[] from = { "A1", "B1", "C1", "D1" }, to = { "A2", "B2", "C2", "D2" };
            for (int i = 0; i < 4; i++)
            {
                Call call = AddCall(from[i], to[i]);
                Frame();
                Connect(call);
                Frame();
                calls.Add(call);
            }
            Assert.That(queue.Tabs.Select(t => t.Call), Is.EqualTo(calls));
            Vector2 c0 = queue.TabCentre(0), c1 = queue.TabCentre(1), c2 = queue.TabCentre(2), c3 = queue.TabCentre(3);
            Assert.That(c1.x, Is.GreaterThan(c0.x + 10)); Assert.That(c1.y, Is.EqualTo(c0.y).Within(.01f));
            Assert.That(c2.y, Is.LessThan(c0.y - 10)); Assert.That(c2.x, Is.EqualTo(c0.x).Within(.01f));
            Assert.That(c3.x, Is.EqualTo(c1.x).Within(.01f)); Assert.That(c3.y, Is.EqualTo(c2.y).Within(.01f));

            session.Calls.End(calls[0].Id, CallEndReason.Cleared);
            Invoke(CardOf(calls[0]), "Update");
            Frame();
            Assert.That(queue.Tabs.Select(t => t.Call), Is.EqualTo(calls.Skip(1)), "a cleared tab leaves at once");
            Assert.That(Vector2.Distance(CardOf(calls[1]).RestPosition, c0), Is.LessThan(.5f), "the others close up");
        }

        [Test]
        public void AFullGridCutsTheOldestStampShort()
        {
            var calls = new List<Call>();
            string[] from = { "A1", "B1", "C1", "D1", "E1" }, to = { "A2", "B2", "C2", "D2", "E2" };
            for (int i = 0; i < 4; i++)
            {
                Call call = AddCall(from[i], to[i]);
                Frame();
                Connect(call);
                calls.Add(call);
            }
            session.Calls.End(calls[1].Id, CallEndReason.Finished);
            Frame();
            Assert.That(CardOf(calls[1]).IsStampShowing, Is.True, "setup: the stamp holds its cell");
            Call fifth = AddCall(from[4], to[4]);
            Frame();
            Connect(fifth);
            Frame();
            Assert.That(queue.Tabs.Count, Is.EqualTo(CallQueueView.MaxTabs));
            Assert.That(queue.Tabs.Select(t => t.Call), Is.EqualTo(new[] { calls[0], calls[2], calls[3], fifth }));
        }

        [Test]
        public void TheTabsFitTheFourthWindowsSpaceAtEveryBoardStage()
        {
            CallCardView[] tabs = null;
            Call waitingTop = null;
            string[] from = { "A1", "A3", "B1", "B3" }, to = { "A2", "A4", "B2", "B4" };
            var stages = new List<string> { "start" };
            do
            {
                Frame();
                Assert.That(session.Stats.ClientSlots, Is.EqualTo(3), $"{stages.Last()}: three waiting windows at every stage");
                if (tabs == null)
                {
                    var calls = new List<Call>();
                    for (int i = 0; i < 4; i++)
                    {
                        Call call = AddCall(from[i], to[i]);
                        Frame();
                        Connect(call);
                        calls.Add(call);
                    }
                    AddCall("C1", "C2");
                    AddCall("C3", "C4");
                    waitingTop = AddCall("D1", "D2");
                    Frame();
                    tabs = calls.Select(CardOf).ToArray();
                    Assert.That(tabs.All(t => t.IsTab), Is.True);
                    Assert.That(WaitingCards().Count, Is.EqualTo(3), "four tabs beside three waiting cards");
                }
                Frame();
                var sockets = All<SocketView>().Where(s => s.gameObject.activeInHierarchy).ToArray();
                Transform shop = All<Transform>().Single(t => t.name == "UpgradeShopPanel");
                PhoneBookView book = All<PhoneBookView>().Single();
                var lowestWaiting = WorldRect(CardOf(WaitingCards().Last()).PortraitRect);
                for (int i = 0; i < tabs.Length; i++)
                {
                    string where = $"{stages.Last()}, tab {i + 1}";
                    Rect art = WorldRect(tabs[i].PortraitRect);
                    Rect plate = WorldRect(tabs[i].TabPlateRect);
                    Rect whole = Rect.MinMaxRect(Mathf.Min(art.xMin, plate.xMin), Mathf.Min(art.yMin, plate.yMin),
                        Mathf.Max(art.xMax, plate.xMax), Mathf.Max(art.yMax, plate.yMax));
                    Assert.That(art.width, Is.EqualTo(Field<float>(queue, "tabSize")).Within(1f), where + ": the tab size");
                    foreach (SocketView socket in sockets)
                        Assert.That(whole.Overlaps(WorldRect((RectTransform)socket.transform)), Is.False, $"{where} covers jack {socket.Address}");
                    Assert.That(whole.Overlaps(WorldRect((RectTransform)shop)), Is.False, where + " covers the shop");
                    if (book.ClosedBookGraphic != null)
                        Assert.That(whole.Overlaps(WorldRect(book.ClosedBookGraphic.rectTransform)), Is.False, where + " covers the book");
                    Assert.That(whole.yMax, Is.LessThan(lowestWaiting.yMin + lowestWaiting.height * .2f),
                        where + " stays below the third window's card (its name plate may hang just past it)");
                    for (int j = 0; j < i; j++)
                        Assert.That(WorldRect(tabs[j].PortraitRect).Overlaps(art), Is.False, $"{where} overlaps tab {j + 1}");
                }
                Assert.That(CardOf(waitingTop), Is.Not.Null);
                stages.Add("stage " + stages.Count);
            } while (session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients));
            Assert.That(stages.Count, Is.GreaterThan(2), "setup: the real file opens the board at least once");
        }

        [Test]
        public void TheTabsSettingsAreInspectorFields()
        {
            foreach (string name in new[] { "tabSize", "tabSpacing", "tabGridOffset" })
            {
                FieldInfo field = typeof(CallQueueView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, name);
                Assert.That(field.GetCustomAttribute<SerializeField>(), Is.Not.Null, name + " is serialized");
            }
        }
    }
}
