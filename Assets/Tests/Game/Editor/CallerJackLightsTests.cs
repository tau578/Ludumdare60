using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// Faster lookup (D58) in the real Final scene: callers' jacks light up on the board, and a row clicked in the open
    /// book marks its jack. A loaded game, so every slot rings at once and no tutorial blocks the clicks.
    /// </summary>
    public sealed class CallerJackLightsTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
        private CordBridge bridge;
        private PhoneBookView book;
        private CableInputController input;
        private CallerJackLights lights;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            foreach (var transform in All<Transform>().Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject))
                    PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Overlay canvases have no size in a preview scene; a 1920x1080 canvas at the screen's place stands in.
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
            book = All<PhoneBookView>().Single();
            book.ReloadContacts();
            book.SetOpen(false);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Install", null, new SaveData { version = SaveData.CurrentVersion, cents = 0 });
            session = installer.Session;
            queue = All<CallQueueView>().Single();
            bridge = All<CordBridge>().Single();
            input = All<CableInputController>().Single();
            lights = All<CallerJackLights>().Single();
            foreach (CableController cable in All<CableController>().Where(c => c.gameObject.activeInHierarchy && c.TrayView != null))
                cable.TrayView.Bind(cable);
            Assert.That(session.IsTutorial, Is.False, "setup: no tutorial in the way");
            session.Tick(3);
            lights.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private SocketView Socket(JackId jack) => All<SocketView>().Single(s => s.Address == jack.Label);
        private SocketView Socket(string jack) => All<SocketView>().Single(s => s.Address == jack);
        private CallCardView CardOf(Call call) => All<CallCardView>().Single(c => c.Call == call);
        private Call[] Shown => All<CallCardView>().Where(c => c.Call != null && c.Call.State != CallState.Ended)
            .Select(c => c.Call).ToArray();
        private static Vector2 Centre(Transform target)
        {
            var rect = (RectTransform)target;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private void Click(Transform target) => input.ProcessTarget(target.gameObject, null, Centre(target), true);

        // Takes a tray cord first when the hand is empty (pulling a cord's only plug sends it back to the tray).
        private void Plug(SocketView socket)
        {
            if (input.ActiveCable == null)
                Click(All<CableController>().First(c => c.gameObject.activeInHierarchy && c.IsInTray).TrayView.transform);
            Assert.That(input.ActiveCable, Is.Not.Null, "setup: a cord in hand");
            Click(socket.transform);
            bridge.Synchronize();
        }

        private void ClickRow(RectTransform row) => book.OnPointerClick(new PointerEventData(null)
            { position = Centre(row), button = PointerEventData.InputButton.Left });

        // Rows on the open spread, each with the jack it names; the real Book.json decides who is there.
        private RectTransform[] RowsOnSpread() => All<PhoneBookContactRow>()
            .Where(r => r.gameObject.activeInHierarchy && r.Jack.Length > 0).Select(r => (RectTransform)r.transform).ToArray();
        private static string JackOf(RectTransform row) => row.GetComponent<PhoneBookContactRow>().Jack;

        [Test]
        public void OnlyTheCallInHandsJackBlinksAndWaitingCallersStayDark()
        {
            Call[] calls = Shown;
            Assert.That(calls.Length, Is.GreaterThan(1), "setup: a loaded game rings every open slot");
            foreach (Call call in calls)
            {
                // Fix 3 (lead): waiting callers show no light; only hovering their card flares them.
                Assert.That(Socket(call.Request.From).Lamp, Is.EqualTo(SocketLamp.Waiting), call.Request.From.Label);
                Assert.That(Socket(call.Request.From).GetComponent<SocketGraphic>().LampGraphic.CurrentAlpha, Is.Zero,
                    $"{call.Request.From.Label} is dark");
            }
            var callers = calls.Select(c => c.Request.From.Label).ToList();
            foreach (SocketView socket in All<SocketView>().Where(s => !callers.Contains(s.Address)))
                Assert.That(socket.Lamp, Is.EqualTo(SocketLamp.Off), $"{socket.Address} has no waiting caller");

            Call taken = calls[1];
            Invoke(queue, "OnCardClicked", CardOf(taken));
            lights.Refresh();
            Assert.That(queue.CallInHand, Is.SameAs(taken));
            Assert.That(Socket(taken.Request.From).Lamp, Is.EqualTo(SocketLamp.Blinking), "the call in hand blinks, never steady");
            foreach (Call call in calls.Where(c => c != taken && c.Request.From != taken.Request.From))
                Assert.That(Socket(call.Request.From).Lamp, Is.EqualTo(SocketLamp.Waiting));

            // Taking another card: that one blinks, and the first one waits in the dark again.
            Call next = calls.First(c => c != taken && c.Request.From != taken.Request.From);
            Invoke(queue, "OnCardClicked", CardOf(next));
            lights.Refresh();
            Assert.That(Socket(next.Request.From).Lamp, Is.EqualTo(SocketLamp.Blinking));
            Assert.That(Socket(taken.Request.From).Lamp, Is.EqualTo(SocketLamp.Waiting));
        }

        [Test]
        public void TheLightGoesOutWhenAPlugGoesInAndWhenTheCallConnects()
        {
            Call call = Shown[0];
            SocketView from = Socket(call.Request.From), to = Socket(call.Request.To);
            Invoke(queue, "OnCardClicked", CardOf(call));
            Plug(from);
            lights.Refresh();
            Assert.That(from.IsOccupied, Is.True);
            Assert.That(from.Lamp, Is.EqualTo(SocketLamp.Off), "a plug in the caller's jack puts it out");
            Click(from.transform);
            bridge.Synchronize();
            lights.Refresh();
            Assert.That(from.IsOccupied, Is.False);
            Assert.That(from.Lamp, Is.EqualTo(SocketLamp.Blinking), "pulled out again: it blinks again");
            Plug(to);
            lights.Refresh();
            Assert.That(from.Lamp, Is.EqualTo(SocketLamp.Blinking), "a plug in the recipient's jack keeps the caller lit");
            Plug(from);
            lights.Refresh();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(from.Lamp, Is.EqualTo(SocketLamp.Off));
            Assert.That(to.Lamp, Is.EqualTo(SocketLamp.Off));
        }

        [Test]
        public void AnEndedCallsJackGoesDark()
        {
            Call call = Shown[0];
            Assert.That(Socket(call.Request.From).Lamp, Is.EqualTo(SocketLamp.Waiting), "setup");
            session.ClearCalls();
            lights.Refresh();
            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(All<SocketView>().All(s => s.Lamp == SocketLamp.Off), Is.True, "the cards still show their stamps");
        }

        [Test]
        public void ARowClickMarksItsJackClosesTheBookAndAnotherRowMovesTheMark()
        {
            book.SetOpen(true);
            RectTransform[] rows = RowsOnSpread().Where(r => lights.CanMark(JackOf(r))).ToArray();
            Assert.That(rows.Select(JackOf).Distinct().Count(), Is.GreaterThan(1), "setup: two markable jacks on the spread");
            RectTransform first = rows[0];
            RectTransform second = rows.First(r => JackOf(r) != JackOf(first));
            Assert.That(book.RowAt(Centre(first), null), Is.SameAs(first.GetComponent<PhoneBookContactRow>()));

            ClickRow(first);
            Assert.That(book.IsOpen, Is.False, "the row click closes the book");
            Assert.That(lights.MarkedJack, Is.EqualTo(JackOf(first)));
            Assert.That(Socket(JackOf(first)).Lamp, Is.EqualTo(SocketLamp.Steady));
            Assert.That(Socket(JackOf(first)).GetComponent<SocketGraphic>().IsLampShowing, Is.True);

            book.SetOpen(true);
            ClickRow(second);
            lights.Refresh();
            Assert.That(lights.MarkedJack, Is.EqualTo(JackOf(second)), "one mark at a time");
            SocketView old = Socket(JackOf(first));
            Assert.That(old.Lamp, Is.EqualTo(Shown.Any(c => c.Request.From.Label == old.Address) ? SocketLamp.Waiting : SocketLamp.Off));

            // Clicking the open book off the rows still closes it, and marks nothing.
            book.SetOpen(true);
            var opened = book.OpenBookRect;
            Vector2 spine = Centre(opened);
            Assert.That(book.RowAt(spine, null), Is.Null, "setup: the spine is no row");
            book.OnPointerClick(new PointerEventData(null) { position = spine, button = PointerEventData.InputButton.Left });
            Assert.That(book.IsOpen, Is.False);
            Assert.That(lights.MarkedJack, Is.EqualTo(JackOf(second)));
        }

        [Test]
        public void TheMarkClearsWhenAPlugGoesInAndWhenAnotherCallIsTaken()
        {
            Call call = Shown[0];
            Invoke(queue, "OnCardClicked", CardOf(call));
            Assert.That(lights.Mark(call.Request.To.Label), Is.True);
            SocketView to = Socket(call.Request.To);
            lights.Refresh();
            Assert.That(to.Lamp, Is.EqualTo(SocketLamp.Steady));
            Invoke(queue, "OnCardClicked", CardOf(call));
            Assert.That(lights.MarkedJack, Is.EqualTo(to.Address), "reopening the same card keeps it");

            Plug(to);
            lights.Refresh();
            Assert.That(to.IsOccupied, Is.True);
            Assert.That(lights.MarkedJack, Is.Null, "a plug in the marked jack clears it");

            // A jack with a plug already in can be marked; it waits for the next plug that goes in.
            Assert.That(lights.Mark(to.Address), Is.True);
            lights.Refresh();
            Assert.That(to.Lamp, Is.EqualTo(SocketLamp.Steady));
            Click(to.transform);
            lights.Refresh();
            Assert.That(to.IsOccupied, Is.False);
            Assert.That(lights.MarkedJack, Is.EqualTo(to.Address), "pulling the plug out keeps the mark");
            Plug(to);
            lights.Refresh();
            Assert.That(lights.MarkedJack, Is.Null, "putting it back in clears it");

            Assert.That(lights.Mark(to.Address), Is.True);
            Call other = Shown.First(c => c != call && c.State == CallState.Incoming);
            Invoke(queue, "OnCardClicked", CardOf(other));
            lights.Refresh();
            Assert.That(lights.MarkedJack, Is.Null, "taking another call clears it");
        }

        // What the cord input does each frame with the pointer over a place: the open book's art is the top hit over a row.
        private void Hover(Transform place, bool hover = true) => input.ProcessTarget(
            book.IsOpen && RectTransformUtility.RectangleContainsScreenPoint(book.OpenBookRect, Centre(place), null)
                ? book.OpenBookRect.gameObject : place.gameObject, null, Centre(place), false, hover);

        [Test]
        public void HoveringARowTintsItWhileTheBookIsOpenAndItsJackCanBeMarked()
        {
            book.SetOpen(true);
            RectTransform[] rows = RowsOnSpread().Where(r => lights.CanMark(JackOf(r))).ToArray();
            RectTransform first = rows[0], second = rows[1];
            var row = first.GetComponent<PhoneBookContactRow>();
            int targetsBefore = book.GetComponentsInChildren<Graphic>(true).Count(g => g.raycastTarget);

            Hover(first);
            Assert.That(book.HoveredRow, Is.SameAs(row));
            Assert.That(row.IsHovered, Is.True);
            Image tint = row.HoverTintGraphic;
            Assert.That(tint.raycastTarget, Is.False, "no new raycast target: rows are found by position");
            Assert.That(book.GetComponentsInChildren<Graphic>(true).Count(g => g.raycastTarget), Is.EqualTo(targetsBefore));
            Assert.That(tint.transform.GetSiblingIndex(), Is.Zero, "behind the row's text");
            Assert.That(tint.color.a, Is.LessThan(.5f), "a soft tint");

            Hover(second);
            Assert.That(book.HoveredRow, Is.SameAs(second.GetComponent<PhoneBookContactRow>()));
            Assert.That(row.IsHovered, Is.False, "one row at a time");

            Hover(book.OpenBookRect);
            Assert.That(book.RowAt(Centre(book.OpenBookRect), null), Is.Null, "setup: the spine is no row");
            Assert.That(book.HoveredRow, Is.Null, "off the rows: no tint");

            Hover(first);
            Hover(first, hover: false);
            Assert.That(row.IsHovered, Is.False, "a finger off the screen leaves no hover");

            // A row whose jack is off the open board takes no click, so it shows no tint either.
            SocketView socket = Socket(JackOf(first));
            socket.SetOpen(false);
            Hover(first);
            Assert.That(book.HoveredRow, Is.Null);
            Assert.That(row.IsHovered, Is.False);
            socket.SetOpen(true);
            Hover(first);
            Assert.That(row.IsHovered, Is.True);

            book.SetOpen(false);
            Assert.That(row.IsHovered, Is.False, "closing the book clears it");
            book.HoverAt(Centre(first), null);
            Assert.That(book.HoveredRow, Is.Null, "nothing while the book is shut");

            book.SetOpen(true);
            Hover(first);
            SocketView outside = All<SocketView>().First(j => j.gameObject.activeInHierarchy &&
                !RectTransformUtility.RectangleContainsScreenPoint(book.OpenBookRect, Centre(j.transform), null));
            Hover(outside.transform);
            Assert.That(row.IsHovered, Is.False, "leaving the book clears it");
            Hover(first);
            book.InputEnabled = false;
            Hover(first);
            Assert.That(row.IsHovered, Is.False, "paused: no tint");
            book.InputEnabled = true;
        }

        [Test]
        public void AJackOutsideTheOpenBoardCannotBeMarked()
        {
            book.SetOpen(true);
            RectTransform row = RowsOnSpread().First(r => lights.CanMark(JackOf(r)));
            SocketView socket = Socket(JackOf(row));
            socket.SetOpen(false);
            Assert.That(lights.CanMark(socket.Address), Is.False);
            ClickRow(row);
            Assert.That(book.IsOpen, Is.True, "the click is ignored: the book stays open");
            Assert.That(lights.MarkedJack, Is.Null);
            Assert.That(lights.Mark(socket.Address), Is.False);
            Assert.That(lights.CanMark("Z9"), Is.False, "no such jack on the board");
            socket.SetOpen(true);
            ClickRow(row);
            Assert.That(lights.MarkedJack, Is.EqualTo(socket.Address));
            // A marked jack that closes (dev reset) loses its mark.
            socket.SetOpen(false);
            lights.Refresh();
            Assert.That(lights.MarkedJack, Is.Null);
            Assert.That(socket.GetComponent<SocketGraphic>().IsLampShowing, Is.False, "a closed jack never shows its lamp");
        }

        [Test]
        public void TheLampLiesOnTheBoardTakesNoClicksAndBlinksInStep()
        {
            Call call = Shown[0];
            SocketView socket = Socket(call.Request.From);
            var graphic = socket.GetComponent<SocketGraphic>();
            Assert.That(graphic.IsLampShowing, Is.True);
            SocketLampGraphic lamp = graphic.LampGraphic;
            Assert.That(lamp.raycastTarget, Is.False);
            Assert.That(lamp.GetComponent<Canvas>(), Is.Null, "no own layer: under every plug and wire");
            Assert.That(lamp.canvas.rootCanvas, Is.SameAs(socket.GetComponentInParent<Canvas>().rootCanvas));
            Assert.That(lamp.transform.GetSiblingIndex(), Is.Zero, "under the hover and connected rings");
            Assert.That(lamp.Mode, Is.EqualTo(SocketLamp.Waiting));
            // The cord input's top hit over a lit jack is still the jack.
            Assert.That(lamp.GetComponentsInChildren<Graphic>().Any(g => g.raycastTarget), Is.False);
            Assert.That(lamp.CurrentAlpha, Is.Zero, "a waiting caller is dark");
            float low = SocketLampGraphic.BlinkAlpha(0), high = SocketLampGraphic.BlinkAlpha(SocketLampGraphic.BlinkSeconds / 2);
            Assert.That(low, Is.EqualTo(SocketLampGraphic.BlinkLow).Within(1e-4));
            Assert.That(high, Is.EqualTo(SocketLampGraphic.BlinkHigh).Within(1e-4));
            Invoke(queue, "OnCardClicked", CardOf(call));
            lights.Refresh();
            Assert.That(lamp.Mode, Is.EqualTo(SocketLamp.Blinking), "the call in hand blinks");
            Assert.That(SocketLampGraphic.AlphaOf(SocketLamp.Blinking, 0), Is.EqualTo(low));

            Assert.That(lights.Mark(call.Request.To.Label), Is.True);
            lights.Refresh();
            SocketLampGraphic marked = Socket(call.Request.To).GetComponent<SocketGraphic>().LampGraphic;
            Assert.That(marked.Mode, Is.EqualTo(SocketLamp.Steady), "only a marked jack glows steadily");
            Assert.That(marked.CurrentAlpha, Is.EqualTo(1f));
            foreach (float t in new[] { 0f, .2f, .5f, 1f })
                Assert.That(SocketLampGraphic.AlphaOf(SocketLamp.Steady, t), Is.EqualTo(1f));
        }

        [Test]
        public void OnlyTheTakenCallBlinksAtAboutHalfBrightness()
        {
            // Fix 3 (lead): too many lights. The call in hand peaks at 50-60%, with a low point well below it;
            // waiting callers are dark, and nothing but the book mark outshines the call in hand.
            Assert.That(SocketLampGraphic.BlinkHigh, Is.InRange(.5f, .6f), "the call in hand peaks at about half brightness");
            Assert.That(SocketLampGraphic.BlinkLow, Is.LessThan(SocketLampGraphic.BlinkHigh / 3), "a clear blink");
            foreach (float t in new[] { 0f, .1f, .25f, .7f, 1.3f })
                Assert.That(SocketLampGraphic.AlphaOf(SocketLamp.Waiting, t), Is.Zero, "a waiting caller is dark");
            Assert.That(SocketLampGraphic.FlarePeak, Is.LessThanOrEqualTo(SocketLampGraphic.BlinkHigh),
                "a hover preview never outshines the call in hand");
            Assert.That(SocketLampGraphic.SteadyAlpha, Is.EqualTo(1f), "the book mark stays steady");

            Call[] calls = Shown;
            Call taken = calls[0];
            Call waiting = calls.First(c => c.Request.From != taken.Request.From);
            Invoke(queue, "OnCardClicked", CardOf(taken));
            lights.Refresh();
            SocketLampGraphic bright = Socket(taken.Request.From).GetComponent<SocketGraphic>().LampGraphic;
            SocketLampGraphic faint = Socket(waiting.Request.From).GetComponent<SocketGraphic>().LampGraphic;
            Assert.That(bright.Mode, Is.EqualTo(SocketLamp.Blinking));
            Assert.That(faint.Mode, Is.EqualTo(SocketLamp.Waiting));
            Assert.That(bright.ShowsBigHalo && !faint.ShowsBigHalo, Is.True, "a bigger halo for the call in hand only");
            Assert.That(bright.GlowRadius, Is.GreaterThan(faint.GlowRadius));
            Assert.That(lights.Mark(waiting.Request.To.Label) || lights.Mark(taken.Request.To.Label), Is.True);
            lights.Refresh();
            SocketLampGraphic marked = Socket(lights.MarkedJack).GetComponent<SocketGraphic>().LampGraphic;
            Assert.That(marked.Mode, Is.EqualTo(SocketLamp.Steady), "the mark stays steady");
            Assert.That(marked.ShowsBigHalo, Is.False);
        }

        [Test]
        public void HoveringAWaitingCallersCardFlaresItsJackWithoutTakingIt()
        {
            Call[] calls = Shown;
            Call taken = calls[0];
            Call waiting = calls.First(c => c.Request.From != taken.Request.From);
            Invoke(queue, "OnCardClicked", CardOf(taken));
            lights.Refresh();
            SocketLampGraphic lamp = Socket(waiting.Request.From).GetComponent<SocketGraphic>().LampGraphic;
            SocketLampGraphic inHand = Socket(taken.Request.From).GetComponent<SocketGraphic>().LampGraphic;
            Assert.That(lamp.IsFlaring, Is.False, "setup");

            // The mouse points at the waiting card: no click, no new raycast target, just the cord input's top hit.
            Transform card = CardOf(waiting).transform;
            input.ProcessTarget(card.gameObject, null, Centre(card), false);
            Assert.That(input.HoverTarget, Is.SameAs(card.gameObject));
            lights.Refresh();
            Assert.That(lamp.IsFlaring, Is.True, "its caller's jack flares");
            Assert.That(lamp.CurrentAlpha, Is.EqualTo(SocketLampGraphic.FlarePeak), "as bright as the call in hand's peak");
            Assert.That(lamp.Mode, Is.EqualTo(SocketLamp.Waiting), "it is still a waiting caller's lamp");
            Assert.That(queue.CallInHand, Is.SameAs(taken), "pointing takes nothing");
            Assert.That(waiting.State, Is.Not.EqualTo(CallState.Ended));

            // The flare is brief: lit for a moment, then dark again.
            Assert.That(SocketLampGraphic.FlareAlpha(0, SocketLampGraphic.FlareHoldSeconds * .5f), Is.EqualTo(SocketLampGraphic.FlarePeak));
            Assert.That(SocketLampGraphic.FlareAlpha(0, SocketLampGraphic.FlareHoldSeconds + SocketLampGraphic.FlareFadeSeconds * .5f),
                Is.EqualTo(SocketLampGraphic.FlarePeak * .5f).Within(1e-4));
            Assert.That(SocketLampGraphic.FlareAlpha(0, SocketLampGraphic.FlareHoldSeconds + SocketLampGraphic.FlareFadeSeconds), Is.Zero);
            Assert.That(SocketLampGraphic.FlareHoldSeconds + SocketLampGraphic.FlareFadeSeconds, Is.LessThanOrEqualTo(1f));

            // The card in hand already stands out: pointing at it flares nothing.
            Transform takenCard = CardOf(taken).transform;
            input.ProcessTarget(takenCard.gameObject, null, Centre(takenCard), false);
            lights.Refresh();
            Assert.That(inHand.IsFlaring, Is.False);

            // A finger has no hover (touch): no hover target, so no flare.
            input.ProcessTarget(null, null, Vector2.zero, false);
            input.ProcessTarget(card.gameObject, null, Centre(card), false, hover: false);
            Assert.That(input.HoverTarget, Is.Null);
        }

        [Test]
        public void AMarkOnTheCallersOwnJackShowsSteady()
        {
            // The player's latest choice wins, so marking the caller's own row (a slip) shows where the mark went.
            Call call = Shown[0];
            Invoke(queue, "OnCardClicked", CardOf(call));
            Assert.That(lights.Mark(call.Request.From.Label), Is.True);
            lights.Refresh();
            Assert.That(Socket(call.Request.From).Lamp, Is.EqualTo(SocketLamp.Steady));
            lights.ClearMark();
            lights.Refresh();
            Assert.That(Socket(call.Request.From).Lamp, Is.EqualTo(SocketLamp.Blinking));
        }
    }
}
