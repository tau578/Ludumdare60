using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Cables;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// The tutorial (D57) in the real Final scene: new games only, a spotlight that guides the first call and then the
    /// first cord purchase, and (fix 1) mandatory: only the current step's target takes clicks.
    /// </summary>
    public sealed class TutorialSpotlightTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
        private CordBridge bridge;
        private PhoneBookView book;
        private TutorialSpotlight spotlight;
        private FirstOrderGuide guide;
        private CableInputController input;
        private ShopView shop;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static T Field<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private void Open(SaveData restored = null)
        {
            // A preview scene keeps the user's open scene and unsaved edits intact.
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            foreach (var transform in All<Transform>().Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject))
                    PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Overlay canvases have no size in a preview scene; a 1920x1080 canvas at the screen's place stands in, so a
            // world point is also its screen point.
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
            book.ReloadContacts(); // Awake fills the pages in Play Mode.
            book.SetOpen(false);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Install", null, restored);
            session = installer.Session;
            Assert.That(session, Is.Not.Null);
            queue = All<CallQueueView>().Single();
            bridge = All<CordBridge>().Single();
            spotlight = All<TutorialSpotlight>().Single();
            guide = All<FirstOrderGuide>().SingleOrDefault();
            input = All<CableInputController>().Single();
            shop = All<ShopView>().Single();
            // A cord binds its tray slot in OnEnable, which runs only in Play Mode; the cord input needs the link.
            foreach (CableController cable in All<CableController>().Where(c => c.gameObject.activeInHierarchy && c.TrayView != null))
                cable.TrayView.Bind(cable);
        }

        [TearDown]
        public void TearDown()
        {
            if (spotlight != null && spotlight.Dim != null) spotlight.Dim.Release();
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private CallCardView FirstCard()
        {
            session.Tick(3);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1), "a new game deals only the tutorial call");
            return All<CallCardView>().Single(c => c.Call == session.Calls.Live[0]);
        }

        // A click the dark layer swallows: since 13a pass 5 (D81) it only reports the press, and no step moves on.
        private void PressDim()
        {
            input.ProcessTarget(spotlight.Dim.gameObject, null, new Vector2(5, 5), true);
            spotlight.Dim.OnPointerDown(null);
        }

        // The hint's OK button (13a pass 5, D81), clicked the way the cord input sees a button: no cord is dropped.
        private void ClickOk()
        {
            Assert.That(spotlight.IsOkShowing && spotlight.OkButton.gameObject.activeInHierarchy, Is.True,
                $"{guide.Stage}: the OK button shows");
            input.ProcessTarget(spotlight.OkButton.targetGraphic.gameObject, null, Centre(spotlight.OkButton.transform), true);
            spotlight.OkButton.onClick.Invoke();
            Invoke(guide, "LateUpdate");
        }

        // An information step read: OK after its short guard.
        private void Ok()
        {
            Sequence.Tick(FirstOrderGuideSequence.OkClickDelay);
            ClickOk();
        }

        // 13a pass 8: "Connected" is two OK steps, the tab and then the money.
        private void OkConnected()
        {
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "setup");
            Ok();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "the money step follows the tab step");
            Ok();
        }

        // 13a pass 6 (D81): the caller's side of the first call: the card, a tray cord, the caller's jack; the book's
        // step is next, with one plug in and the other in hand.
        private CableController CallerSide(CallCardView card)
        {
            Invoke(queue, "OnCardClicked", card);
            CableController cable = TrayCord();
            Click(cable.TrayView.transform);
            Click(Socket(card.Call.Request.From).transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "setup");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "setup");
            return cable;
        }

        private FirstOrderGuideSequence Sequence => Field<FirstOrderGuideSequence>(guide, "sequence");
        private CableController TrayCord() => All<CableController>().First(c => c.gameObject.activeInHierarchy && c.IsInTray);
        private SocketView Socket(JackId jack) => All<SocketView>().Single(s => s.Address == jack.Label);
        private SocketView OtherSocket(Call call) => All<SocketView>().First(s => s.IsOpen && s.gameObject.activeInHierarchy &&
            s.Address != call.Request.From.Label && s.Address != call.Request.To.Label);
        private RectTransform ShopButton(int index) => All<UpgradeSlotView>().OrderByDescending(s => s.transform.position.y)
            .Select(s => (RectTransform)s.transform).ElementAt(index);
        private static Vector2 Centre(Transform target)
        {
            var rect = (RectTransform)target;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private bool Blocked(Transform target) => spotlight.BlocksScreenPoint(Centre(target));

        // What a real click does: the dark layer is the top hit where it blocks, else the target beneath gets it.
        private void Click(Transform target)
        {
            GameObject top = Blocked(target) ? spotlight.Dim.gameObject : target.gameObject;
            input.ProcessTarget(top, null, Centre(target), true);
        }

        private void AssertStep(FirstOrderGuideStage stage, string text, params Transform[] targets)
        {
            Assert.That(guide.Stage, Is.EqualTo(stage));
            Assert.That(spotlight.gameObject.activeSelf && spotlight.IsShowing, Is.True, stage.ToString());
            Assert.That(spotlight.HintText, Is.EqualTo(text));
            Assert.That(Enumerable.Range(0, spotlight.TargetCount).Select(i => (Transform)spotlight.TargetAt(i).Rect),
                Is.EquivalentTo(targets), stage.ToString());
            spotlight.Advance(3);
            for (int i = 0; i < spotlight.TargetCount; i++)
            {
                if (!spotlight.TargetAt(i).Lit)
                {
                    // Clicks only (step 5's page corners): no hole.
                    Assert.That(spotlight.HasGoal(i), Is.False, $"{stage} target {i} is not lit");
                    continue;
                }
                Assert.That(spotlight.HasGoal(i), Is.True, $"{stage} hole {i}");
                Assert.That(spotlight.Dim.HoleStrength(i), Is.EqualTo(1f), $"{stage} hole {i}");
            }
            Assert.That(spotlight.Hint.gameObject.activeSelf && spotlight.Arrow.gameObject.activeSelf, Is.True);
            Assert.That(spotlight.Dim.Blocking, Is.True, $"{stage} is mandatory");
        }

        private void BookClick(RectTransform target) => book.OnPointerClick(new PointerEventData(null)
            { position = Centre(target), button = PointerEventData.InputButton.Left });

        private void AssertReadingStep(TutorialTexts texts, string recipient)
        {
            var targets = new List<Transform> { (Transform)book.RowOnSpread(recipient) ?? book.PageCorner(book.SpreadDirectionTo(recipient)) };
            Assert.That(targets[0], Is.Not.Null);
            foreach (int direction in new[] { -1, 1 })
            {
                RectTransform corner = book.PageCorner(direction);
                if (corner != null && !targets.Contains(corner)) targets.Add(corner);
            }
            AssertStep(FirstOrderGuideStage.ReadingBook, texts.findJacks, targets.ToArray());
            Assert.That(spotlight.TargetAt(0).Lit && spotlight.TargetAt(0).Clickable, Is.True, "the recipient is lit and clickable");
            for (int i = 1; i < spotlight.TargetCount; i++)
            {
                Assert.That(spotlight.TargetAt(i).Lit, Is.False, "the other corners only take clicks");
                Assert.That(Blocked(spotlight.TargetAt(i).Rect), Is.False, "page corners turn the pages");
            }
        }

        // Every place a player might click, except the step's own; the open book counts only while it is the step.
        private void AssertBlockedEverywhereBut(Call call, CallCardView card, params Transform[] allowed)
        {
            var places = new List<Transform> { card.transform, OtherSocket(call).transform, Socket(call.Request.From).transform,
                ShopButton(0), ShopButton(3) };
            places.AddRange(All<CableController>().Where(c => c.gameObject.activeInHierarchy && c.IsInTray && c.TrayView != null)
                .Select(c => c.TrayView.transform));
            if (!book.IsOpen) places.Add(book.ClosedBookGraphic.transform);
            foreach (Transform place in places.Except(allowed))
            {
                if (book.IsOpen && RectTransformUtility.RectangleContainsScreenPoint(book.OpenBookRect, Centre(place), null)) continue;
                Assert.That(Blocked(place), Is.True, $"{guide.Stage}: {place.name} must not take clicks");
            }
            foreach (Transform place in allowed) Assert.That(Blocked(place), Is.False, $"{guide.Stage}: {place.name} takes clicks");
        }

        [Test]
        public void ANewGameGuidesTheFirstCallStepByStepAndBlocksEverythingElse()
        {
            Open();
            Assert.That(session.IsTutorial, Is.True);
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.None), "a new game has seen no shop hint");
            Assert.That(spotlight.gameObject.activeSelf, Is.False, "nothing shows before the first call rings");
            TutorialTexts texts = spotlight.Texts;
            var card = FirstCard();
            Call call = card.Call;
            CableController cable = TrayCord();
            SocketView a = Socket(call.Request.From), b = Socket(call.Request.To), other = OtherSocket(call);

            AssertStep(FirstOrderGuideStage.Portrait, texts.clickCaller, card.transform);
            AssertBlockedEverywhereBut(call, card, card.transform);
            Click(cable.TrayView.transform);
            Click(other.transform);
            Assert.That(cable.IsInTray, Is.True, "no cord pickup on step 1");
            book.Toggle();
            Assert.That(book.IsOpen, Is.False, "Tab (and the book's own button) can't open the book on step 1");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Portrait));

            Invoke(queue, "OnCardClicked", card);
            Assert.That(call.State, Is.EqualTo(CallState.Taken), "setup");
            // 13a pass 6 (D81): the caller's side first. The card is taken, so the caller's jack blinks (D58); pass 7: only
            // the tray's cords are lit and take the click. No reading step, no OK.
            CallerJackLights lights = All<CallerJackLights>().Single();
            lights.Refresh();
            Transform[] trayCords = All<CableController>().Where(c => c.gameObject.activeInHierarchy && c.IsInTray &&
                c.TrayView != null).Select(c => c.TrayView.transform).ToArray();
            AssertStep(FirstOrderGuideStage.Cables, texts.takeCord, trayCords);
            Assert.That(texts.takeCord, Is.EqualTo("Take a cord from the tray."));
            Assert.That(a.Lamp, Is.EqualTo(SocketLamp.Blinking), "the caller's jack blinks");
            Assert.That(Enumerable.Range(0, spotlight.TargetCount).Any(i => spotlight.TargetAt(i).Rect ==
                (RectTransform)a.transform), Is.False, "pass 7: only the tray is lit, not the caller's jack");
            Assert.That(spotlight.IsOkShowing, Is.False, "an action step: no OK");
            AssertHintFits();
            AssertBlockedEverywhereBut(call, card, trayCords);
            Click(a.transform);
            Assert.That(a.IsOccupied, Is.False, "the lit jack takes no click yet");
            book.Toggle();
            Assert.That(book.IsOpen, Is.False, "the book waits for its own step");

            Click(cable.TrayView.transform);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "the tray cord takes the click");
            AssertStep(FirstOrderGuideStage.PluggingCaller, texts.plugCaller, a.transform);
            Assert.That(texts.plugCaller, Is.EqualTo("This blinking jack belongs to your caller. Plug the cord into it."));
            lights.Refresh();
            Assert.That(a.Lamp, Is.EqualTo(SocketLamp.Blinking), "the game blinks the caller's jack");
            Assert.That(spotlight.TargetAt(0).Round && spotlight.TargetAt(0).Clickable, Is.True, "jacks get round holes");
            AssertBlockedEverywhereBut(call, card, a.transform);
            Assert.That(Blocked(b.transform), Is.True, "the recipient waits for the book");

            // Only the caller's jack takes a plug; any other click keeps the cord in hand (no drop, no return).
            Click(other.transform);
            Click(b.transform);
            Click(cable.TrayView.transform);
            Click(card.transform);
            input.ProcessTarget(spotlight.Dim.gameObject, null, new Vector2(5, 5), true);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "a blocked click never drops a held cord");
            Assert.That(other.IsOccupied || b.IsOccupied, Is.False);
            Assert.That(input.ActiveCable, Is.SameAs(cable));
            book.Toggle();
            Assert.That(book.IsOpen, Is.False);

            Click(a.transform);
            bridge.Synchronize();
            Assert.That(a.OccupyingCable, Is.SameAs(cable));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "one plug in, the other in hand");
            Invoke(guide, "LateUpdate");
            // Then the recipient's side: the hint names them as the book lists them.
            string recipientName = book.EntryNameFor(call.Request.To.Label);
            Assert.That(recipientName, Is.Not.Null.And.Not.Empty, "setup: the recipient is in the book");
            Assert.That(texts.openBook, Is.EqualTo("They want <b>{name}</b>. Open the book."));
            AssertStep(FirstOrderGuideStage.Book, $"They want <b>{recipientName}</b>. Open the book.",
                book.ClosedBookGraphic.transform);
            Assert.That(spotlight.IsOkShowing, Is.False, "an action step: its target, no OK");
            AssertHintFits();
            AssertBlockedEverywhereBut(call, card, book.ClosedBookGraphic.transform);
            Click(a.transform);
            Click(cable.TrayView.transform);
            Assert.That(a.OccupyingCable, Is.SameAs(cable), "the caller's plug stays put");
            // The cover takes the click with the cord in hand, and keeps it there.
            Click(book.ClosedBookGraphic.transform);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "a click on the cover keeps the cord in hand");
            book.Toggle();
            Assert.That(book.IsOpen, Is.True, "the book step opens the book");

            // D58: only the recipient's row lights (Book.json sets how many spreads there are: a row on another spread
            // lights the corner that turns to it). The corners take clicks.
            string recipient = call.Request.To.Label;
            AssertReadingStep(texts, recipient);
            Assert.That(texts.findJacks, Is.EqualTo("Click their name. Their jack lights up."));
            AssertBlockedEverywhereBut(call, card);
            RectTransform callerRow = book.RowOnSpread(call.Request.From.Label);
            if (callerRow != null && callerRow != book.RowOnSpread(recipient))
                Assert.That(Blocked(callerRow), Is.True, "only the recipient's row takes the click");
            if (book.RowAt(Centre(book.OpenBookRect), null) == null)
                Assert.That(spotlight.BlocksScreenPoint(Centre(book.OpenBookRect)), Is.True, "the rest of the book is dark");
            while (book.RowOnSpread(recipient) == null)
            {
                RectTransform corner = book.PageCorner(book.SpreadDirectionTo(recipient));
                Assert.That(Blocked(corner), Is.False, "the lit corner turns the page");
                input.ProcessTarget(book.OpenBookRect.gameObject, null, Centre(corner), true);
                BookClick(corner);
                Invoke(guide, "LateUpdate");
                AssertReadingStep(texts, recipient);
            }
            RectTransform row = book.RowOnSpread(recipient);
            Assert.That(Blocked(row), Is.False, "the recipient's row takes the click");
            // 10b fix 1: the lit row tints under the pointer; a blocked row is under the dark layer and shows none.
            input.ProcessTarget(book.OpenBookRect.gameObject, null, Centre(row), false);
            Assert.That(book.HoveredRow, Is.SameAs(row.GetComponent<PhoneBookContactRow>()));
            if (callerRow != null && callerRow != row)
            {
                input.ProcessTarget(spotlight.Dim.gameObject, null, Centre(callerRow), false);
                Assert.That(book.HoveredRow, Is.Null, "a blocked row shows no hover");
            }
            // The row click as the cord input sees it (the open book), then as the book handles it.
            input.ProcessTarget(book.OpenBookRect.gameObject, null, Centre(row), true);
            BookClick(row);
            Assert.That(book.IsOpen, Is.False, "the row click closes the book");
            Assert.That(lights.MarkedJack, Is.EqualTo(recipient), "and marks the jack");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "the cord is still in hand after the book");
            Assert.That(input.ActiveCable, Is.SameAs(cable));
            Invoke(guide, "LateUpdate");

            // The marked jack, lit right before its plug.
            AssertStep(FirstOrderGuideStage.PluggingRecipient, texts.plugRecipient, b.transform);
            Assert.That(texts.plugRecipient, Is.EqualTo("The steady light marks who they want. Plug the other end in."));
            lights.Refresh();
            Assert.That(b.Lamp, Is.EqualTo(SocketLamp.Steady), "the marked jack glows");
            AssertBlockedEverywhereBut(call, card, b.transform);
            Assert.That(Blocked(a.transform), Is.True, "the caller's plug stays put");
            book.Toggle();
            Assert.That(book.IsOpen, Is.False, "the book stays shut once read");
            Click(a.transform);
            Assert.That(a.OccupyingCable, Is.SameAs(cable));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            Click(other.transform);
            Assert.That(other.IsOccupied, Is.False, "a wrong number is impossible in the tutorial");
            Click(b.transform);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Invoke(guide, "LateUpdate");
            Transform header = All<RectTransform>().Single(t => t.name == "HeaderSlot" && t.parent.name == "UpgradeShopPanel");
            // D83: the call shrank to its tab, lit alone first (13a pass 8: one focus per step; it takes no clicks).
            Assert.That(card.IsTab, Is.True, "the connected call is a tab now");
            AssertStep(FirstOrderGuideStage.Connected, texts.connected, card.PortraitRect);
            AssertBlockedEverywhereBut(call, card);
            AssertOkOnHint();
            AssertHintFits();
            Assert.That(call.WaitingHeld, Is.False, "connected: the tutorial's hold on the call is over");
            Sequence.Tick(60);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "13a pass 5: no timed ending, no any-click ending");
            ClickOk();
            // Then the money counter, alone.
            AssertStep(FirstOrderGuideStage.Earning, texts.earning, header);
            AssertBlockedEverywhereBut(call, card);
            Assert.That(Blocked(header), Is.True, "the money is lit but takes no click: its OK ends it");
            AssertOkOnHint();
            AssertHintFits();
            Sequence.Tick(60);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "no timed ending, no any-click ending");

            session.Tick(call.SecondsLeft + .1);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the call finished: normal play");
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            Assert.That(spotlight.Dim.HasMaterial, Is.False, "the spotlight frees its material while hidden");
            Assert.That(book.OpenLocked, Is.False);
        }

        // 13a pass 6 (D81): with one plug in the caller's jack and the other in hand, the player opens the book, clicks
        // a row and the book closes; every way of opening and closing it keeps the cord in hand, guided or not.
        [Test]
        public void AHalfPluggedCordStaysInHandThroughTheBookEveryWay()
        {
            Open();
            var card = FirstCard();
            Call call = card.Call;
            CableController cable = CallerSide(card);
            GamePauseMenu pause = All<GamePauseMenu>().Single();
            book.Toggle(); // Tab, or the cover's button
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook), "setup");
            pause.HandleEscape();
            Assert.That(book.IsOpen, Is.False, "Escape closes the book");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "guided: Escape keeps the cord in hand");
            Assert.That(input.ActiveCable, Is.SameAs(cable));
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient), "the marked jack's step follows anyway");
            Click(Socket(call.Request.To).transform);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "and the cord still connects the call");

            // Unguided (Continue): the same cord through every way in and out of the book.
            TearDown();
            Open(SaveWithHints(ShopHints.All));
            session.Tick(3);
            pause = All<GamePauseMenu>().Single();
            call = session.Calls.Live.First(c => c.State == CallState.Incoming && !c.Request.Script.IsEmergency);
            Assert.That(session.Desk.TryTake(call.Id), Is.True, "setup");
            cable = TrayCord();
            Click(cable.TrayView.transform);
            Click(Socket(call.Request.From).transform);
            bridge.Synchronize();
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "setup: one plug in, the other in hand");
            void AssertInHand(string how)
            {
                Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), how);
                Assert.That(input.ActiveCable, Is.SameAs(cable), how);
                Assert.That(Socket(call.Request.From).OccupyingCable, Is.SameAs(cable), how);
            }
            // The cover, clicked as the cord input sees it, then its button.
            input.ProcessTarget(book.ClosedBookGraphic.gameObject, null, Centre(book.ClosedBookGraphic.transform), true);
            book.Toggle();
            Assert.That(book.IsOpen, Is.True);
            AssertInHand("a click on the cover");
            // A click on the open pages away from any row and corner closes the book.
            Vector2 blank = BlankBookPoint();
            input.ProcessTarget(book.OpenBookRect.gameObject, null, blank, true);
            book.OnPointerClick(new PointerEventData(null) { position = blank, button = PointerEventData.InputButton.Left });
            Assert.That(book.IsOpen, Is.False, "a click on the pages closes the book");
            AssertInHand("a click on the pages");
            // Tab opens it, a click beside the open book (the board) does nothing to the cord, Tab closes it.
            book.Toggle();
            GameObject board = OtherSocket(call).transform.parent.gameObject;
            input.ProcessTarget(board, null, Centre(board.transform), true);
            AssertInHand("a click on the board beside the open book");
            book.Toggle();
            Assert.That(book.IsOpen, Is.False);
            AssertInHand("Tab");
            // Escape.
            book.Toggle();
            pause.HandleEscape();
            Assert.That(book.IsOpen || pause.IsPaused, Is.False, "Escape closes the book, not the game");
            AssertInHand("Escape");
            // The recipient's row: it marks the jack and closes the book.
            book.Toggle();
            while (book.RowOnSpread(call.Request.To.Label) == null)
            {
                int direction = book.SpreadDirectionTo(call.Request.To.Label);
                Assert.That(direction, Is.Not.EqualTo(0), "setup: the recipient is in the book");
                RectTransform corner = book.PageCorner(direction);
                input.ProcessTarget(book.OpenBookRect.gameObject, null, Centre(corner), true);
                BookClick(corner);
                AssertInHand("a page corner");
            }
            RectTransform row = book.RowOnSpread(call.Request.To.Label);
            input.ProcessTarget(book.OpenBookRect.gameObject, null, Centre(row), true);
            BookClick(row);
            Assert.That(book.IsOpen, Is.False, "the row click closes the book");
            Assert.That(All<CallerJackLights>().Single().MarkedJack, Is.EqualTo(call.Request.To.Label), "and marks the jack");
            AssertInHand("the row click");
            Click(Socket(call.Request.To).transform);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "the cord connects the call after all that");
        }

        // A point on the open pages that is neither a row nor a page corner.
        private Vector2 BlankBookPoint()
        {
            Rect rect = book.OpenBookRect.rect;
            for (float y = .05f; y < 1; y += .05f)
                for (float x = .05f; x < 1; x += .05f)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null,
                        book.OpenBookRect.TransformPoint(new Vector2(rect.xMin + rect.width * x, rect.yMin + rect.height * y)));
                    if (book.RowAt(point, null) == null && book.PageTurnDirectionAt(point, null) == 0) return point;
                }
            Assert.Fail("setup: no blank place on the open book");
            return default;
        }

        [Test]
        public void TheShopStepsLightTheFirstCordOnceAffordableThenTheMysteryUpgrade()
        {
            Open();
            Call call = ConnectTheFirstCall();
            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            // Economy.json sets the price: start below it, whatever the first call paid.
            session.Wallet.TrySpend(session.Wallet.Cents);
            OkConnected();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(spotlight.Dim.Blocking, Is.False, "normal play between the steps");
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);

            session.Wallet.Earn(first.CostCents);
            Invoke(guide, "LateUpdate");
            RectTransform button = shop.ButtonFor(first);
            Assert.That(button, Is.Not.Null);
            AssertStep(FirstOrderGuideStage.BuyCord, spotlight.Texts.buyCord, button);
            AssertBlockedEverywhereBut(call, All<CallCardView>().First(c => c.Call != null), button);
            foreach (int slot in Enumerable.Range(0, ShopView.SlotCount))
                if (ShopButton(slot) != button) Assert.That(Blocked(ShopButton(slot)), Is.True, $"shop button {slot}");
            Assert.That(All<SlowGuidePulse>().Any(p => p.IsShowing), Is.False, "no pulse beside the spotlight");

            // The button's own click (its listener is added in Awake, which EditMode doesn't run).
            Invoke(button.GetComponent<UpgradeSlotView>(), "OnClick");
            Assert.That(session.Upgrades.IsOwned(first.Id), Is.True);
            Invoke(guide, "LateUpdate");
            if (shop.Secret == null)
            {
                Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "no mystery upgrade in Economy.json");
                return;
            }
            RectTransform secret = shop.ButtonFor(shop.Secret);
            AssertStep(FirstOrderGuideStage.Mystery, spotlight.Texts.mystery, secret);
            Assert.That(Blocked(secret), Is.True, "step 10 lights the upgrade but takes no click");
            Sequence.Tick(60);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "13a pass 5: it never ends by itself or on a click");
            ClickOk();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "More Profit and More Clients may follow");
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery), "the save remembers steps 9-10");
            Assert.That(spotlight.Dim.Blocking, Is.False);
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MoreProfitThenMoreClientsFollowTheCordStepsOnceEachEvenWithMoneyFromTheStart()
        {
            Open();
            // The dev panel's money during step 1: every hint is affordable, but they wait for the first call and the cord.
            session.Wallet.Earn(1000000);
            Call call = ConnectTheFirstCall();
            OkConnected();
            IUpgrade cord = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            IUpgrade profit = session.Upgrades.ChainOf(UpgradeKind.ExtraIncome)[0];
            IUpgrade clients = session.Upgrades.ChainOf(UpgradeKind.ExtraClients)[0];
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "steps 9-10 first");
            Invoke(shop.ButtonFor(cord).GetComponent<UpgradeSlotView>(), "OnClick");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(shop.Secret != null ? FirstOrderGuideStage.Mystery : FirstOrderGuideStage.WaitingForShop));
            if (shop.Secret != null) Ok();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "a short gap after step 10");

            Sequence.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Invoke(guide, "LateUpdate");
            RectTransform profitButton = shop.ButtonFor(profit);
            Assert.That(profitButton, Is.Not.Null);
            AssertStep(FirstOrderGuideStage.Profit, spotlight.Texts.buyProfit, profitButton);
            Assert.That(spotlight.Texts.buyProfit, Is.EqualTo("More Profit: every call pays more. Buy it to earn faster."));
            var card = All<CallCardView>().First(c => c.Call == call);
            // 13a pass 8: a purchase step like the cord's: the lit button takes the click, there is no OK, buying ends it.
            AssertBlockedEverywhereBut(call, card, profitButton);
            Assert.That(spotlight.IsOkShowing, Is.False, "no OK on a purchase step");
            Sequence.Tick(60);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit), "no timer and no click elsewhere ends it");
            Invoke(profitButton.GetComponent<UpgradeSlotView>(), "OnClick");
            Invoke(guide, "LateUpdate");
            Assert.That(session.Upgrades.IsOwned(profit.Id), Is.True, "the click buys it");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the purchase ends it");
            Assert.That(session.ShopHintsDone & ShopHints.Profit, Is.EqualTo(ShopHints.Profit));
            Assert.That(spotlight.Dim.Blocking, Is.False);

            Sequence.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Invoke(guide, "LateUpdate");
            RectTransform clientsButton = shop.ButtonFor(clients);
            AssertStep(FirstOrderGuideStage.Clients, spotlight.Texts.buyClients, clientsButton);
            Assert.That(Blocked(clientsButton), Is.False, "a purchase step: the lit button takes the click");
            Assert.That(spotlight.IsOkShowing, Is.False, "no OK on a purchase step");
            AssertHintFits();
            Invoke(clientsButton.GetComponent<UpgradeSlotView>(), "OnClick");
            Invoke(guide, "LateUpdate");
            Assert.That(session.Upgrades.IsOwned(clients.Id), Is.True, "the click buys it");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "only the emergency step is left (13a pass 4)");
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.All & ~ShopHints.Emergency));
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            session.Wallet.Earn(1000000);
            Invoke(shop, "LateUpdate");
            Assert.That(spotlight.gameObject.activeSelf, Is.False, "never again");
        }

        private static SaveData SaveWithHints(ShopHints done, long cents = 0) =>
            new SaveData { version = SaveData.CurrentVersion, cents = cents, shopHintsDone = (int)done };

        [Test]
        public void ContinueShowsAPendingHintOnlyWithTheBookShutNoCordInHandAndNotPaused()
        {
            Open(SaveWithHints(ShopHints.Cord | ShopHints.Mystery));
            Assert.That(session.IsTutorial, Is.False);
            Assert.That(guide != null && guide.enabled, Is.True, "a pending shop hint binds the guide");
            session.Tick(3);
            Assert.That(session.Calls.Live.Count, Is.GreaterThan(1), "no one-call start");
            Assert.That(session.Calls.Live.All(c => !c.WaitingHeld), Is.True);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "no first-call guide");
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            book.Toggle();
            Assert.That(book.IsOpen, Is.True, "the book isn't locked between hints");

            session.Wallet.Earn(1000000);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "not with the book open");
            book.Toggle();
            CableController cable = TrayCord();
            Click(cable.TrayView.transform);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "setup");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "not with a cord in hand");
            cable.TryReturnToTray();
            float scale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                Invoke(guide, "LateUpdate");
                Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "not while paused");
            }
            finally { Time.timeScale = scale; }
            Invoke(guide, "LateUpdate");
            AssertStep(FirstOrderGuideStage.Profit, spotlight.Texts.buyProfit,
                shop.ButtonFor(session.Upgrades.ChainOf(UpgradeKind.ExtraIncome)[0]));
        }

        [Test]
        public void ContinueAfterAnEarlySaveSkipsTheFirstCallButShowsTheCordStep()
        {
            // Saved during step 8 (the first fee moved the money), then reloaded: Continue.
            Open(SaveWithHints(ShopHints.None));
            Assert.That(session.IsTutorial, Is.False);
            session.Tick(3);
            Invoke(guide, "LateUpdate");
            Assert.That(spotlight.gameObject.activeSelf, Is.False, "no first-call guide");
            session.Wallet.Earn(session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0].CostCents);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
        }

        [Test]
        public void ADoneOrBoughtHintNeverComesBackAfterAReload()
        {
            Open(SaveWithHints(ShopHints.Cord | ShopHints.Mystery));
            IUpgrade profit = session.Upgrades.ChainOf(UpgradeKind.ExtraIncome)[0];
            IUpgrade clients = session.Upgrades.ChainOf(UpgradeKind.ExtraClients)[0];
            // Bought while the book was open, before its hint could show.
            book.SetOpen(true);
            session.Wallet.Earn(profit.CostCents + clients.CostCents);
            Invoke(guide, "LateUpdate");
            Assert.That(session.Upgrades.TryBuy(profit.Id), Is.True, "setup");
            book.SetOpen(false);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients), "no Profit hint for a bought upgrade");
            Assert.That(session.Upgrades.TryBuy(clients.Id), Is.True, "13a pass 8: bought, as its step asks");
            Invoke(guide, "LateUpdate");
            SaveData saved = session.CaptureSave();
            Assert.That(saved.shopHintsDone, Is.EqualTo((int)(ShopHints.All & ~ShopHints.Emergency)));

            TearDown();
            Open(saved);
            Assert.That(session.ShopHintsPending, Is.EqualTo(ShopHints.Emergency), "every shop hint done: only the emergency step");
            session.Wallet.Earn(1000000);
            Invoke(shop, "LateUpdate");
            Invoke(guide, "LateUpdate");
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void AlreadyAffordableTheCordStepFollowsStepSevenAtOnce()
        {
            Open();
            ConnectTheFirstCall();
            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            session.Wallet.Earn(first.CostCents);
            OkConnected();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
        }

        [Test]
        public void TheShopStepsFollowTheShelf()
        {
            Open();
            ConnectTheFirstCall();
            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            session.Wallet.Earn(first.CostCents);
            Assert.That(session.Upgrades.TryBuy(first.Id), Is.True, "setup: bought during step 8");
            OkConnected();
            Assert.That(guide.Stage, Is.EqualTo(shop.Secret != null ? FirstOrderGuideStage.Mystery : FirstOrderGuideStage.WaitingForShop),
                "nothing left to buy: straight to the mystery upgrade");

            // No first cord for sale (none in the file, or not on the shelf): no shop steps.

            TearDown();
            Open();
            ConnectTheFirstCall();
            guide.BindShop(null, null);
            OkConnected();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "only the emergency step is left");
            Assert.That(Sequence.ShopHintsDone, Is.EqualTo(ShopHints.All & ~ShopHints.Emergency));
            Assert.That(spotlight.Dim.Blocking, Is.False);
        }

        private Call ConnectTheFirstCall()
        {
            var card = FirstCard();
            CallerSide(card);
            Call call = card.Call;
            book.Toggle();
            book.Toggle();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient), "setup");
            Click(Socket(call.Request.To).transform);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "setup");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "setup");
            return call;
        }

        // 13a pass 3 (D79, D81): once the first call is over, one step lights a plug of its cord; one click frees the
        // cord into the hand and ends the step. 13a pass 6 (D81): the second call then repeats the caller's side with
        // the freed cord in hand: the oldest waiting caller's card, then its blinking jack; the plug ends the guide's part.
        [Test]
        public void TheFinishedFirstCallTeachesFreeingItsCordThenTheNextCallersCardAndJack()
        {
            Open();
            Call call = FinishTheFirstCall();
            SocketView a = Socket(call.Request.From), b = Socket(call.Request.To);
            CableController cable = a.OccupyingCable;
            AssertStep(FirstOrderGuideStage.FreeCord, spotlight.Texts.freeCord, a.transform);
            Assert.That(spotlight.Texts.freeCord, Is.EqualTo(
                "The call is over. Click a plug to free the cord. Plug it into the next call, or drop it back in the tray."));
            Assert.That(spotlight.IsOkShowing, Is.False, "an action step: no OK");
            AssertHintFits();
            Assert.That(Blocked(a.transform), Is.False, "the lit plug takes the click");
            Assert.That(Blocked(b.transform), Is.True);
            Assert.That(Blocked(OtherSocket(call).transform), Is.True);
            foreach (int slot in Enumerable.Range(0, ShopView.SlotCount))
                Assert.That(Blocked(ShopButton(slot)), Is.True, $"shop button {slot}");

            // Two callers wait, the older first; the guide picks the oldest regular one with a free jack.
            ClearWaitingCalls();
            Call next = AddCallOnFreeJacks();
            Call later = AddCallOnFreeJacks();
            Click(a.transform);
            Assert.That(a.IsOccupied || b.IsOccupied, Is.False, "D79: both plugs come out");
            Assert.That(input.ActiveCable, Is.SameAs(cable), "the cord is in hand");
            SocketView nextJack = Socket(next.Request.From);
            CallCardView nextCard = queue.WaitingCardOf(next), laterCard = queue.WaitingCardOf(later);
            Assert.That(nextCard != null && laterCard != null, Is.True, "setup: both callers on cards");
            AssertStep(FirstOrderGuideStage.NextCard, spotlight.Texts.nextCard, nextCard.transform);
            Assert.That(spotlight.Texts.nextCard, Is.EqualTo("Take the next caller."));
            Assert.That(spotlight.IsOkShowing, Is.False, "an action step: no OK");
            AssertHintFits();
            Assert.That(guide.HoldsClock, Is.True, "the clock holds while it shows");
            Assert.That(Blocked(nextCard.transform), Is.False, "the oldest caller's card takes the click");
            foreach (Transform place in new[] { laterCard.transform, nextJack.transform, Socket(later.Request.From).transform,
                         a.transform, b.transform, ShopButton(0), book.ClosedBookGraphic.transform })
                Assert.That(Blocked(place), Is.True, $"{place.name} takes no click");
            CallerJackLights lights = All<CallerJackLights>().Single();
            lights.Refresh();
            Assert.That(nextJack.Lamp, Is.Not.EqualTo(SocketLamp.Blinking), "10b fix 3: no blink before the card is taken");
            Click(laterCard.transform);
            Click(nextJack.transform);
            input.ProcessTarget(spotlight.Dim.gameObject, null, new Vector2(5, 5), true);
            Assert.That(later.State == CallState.Incoming && cable.Phase == CablePhase.Held, Is.True,
                "a blocked click takes no card and keeps the cord in hand");
            // The card click as the cord input sees it (a card keeps a held cord), then as the queue handles it.
            Click(nextCard.transform);
            Invoke(queue, "OnCardClicked", nextCard);
            Assert.That(next.State, Is.EqualTo(CallState.Taken), "the card is taken");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "the cord is still in hand");

            lights.Refresh();
            AssertStep(FirstOrderGuideStage.NextCaller, spotlight.Texts.nextCaller, nextJack.transform);
            Assert.That(spotlight.Texts.nextCaller, Is.EqualTo("Their jack is blinking now. Plug the cord into it."));
            Assert.That(nextJack.Lamp, Is.EqualTo(SocketLamp.Blinking), "its jack blinks now");
            Assert.That(spotlight.IsOkShowing, Is.False, "an action step: no OK");
            Assert.That(spotlight.TargetAt(0).Round && spotlight.TargetAt(0).Clickable, Is.True, "a round hole that takes the plug");
            AssertHintFits();
            Assert.That(guide.HoldsClock, Is.True, "the clock holds while it shows");
            Assert.That(Blocked(nextJack.transform), Is.False, "the caller's jack takes the plug");
            foreach (Transform place in new[] { Socket(next.Request.To).transform, Socket(later.Request.From).transform,
                         a.transform, b.transform, ShopButton(0), book.ClosedBookGraphic.transform, nextCard.transform,
                         laterCard.transform })
                Assert.That(Blocked(place), Is.True, $"{place.name} takes no click");

            Click(Socket(later.Request.From).transform);
            input.ProcessTarget(spotlight.Dim.gameObject, null, new Vector2(5, 5), true);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "a blocked click keeps the cord in hand");
            Click(nextJack.transform);
            bridge.Synchronize();
            Assert.That(nextJack.OccupyingCable, Is.SameAs(cable), "the plug is in the next caller's jack");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the plug ends the step");
            Assert.That(spotlight.Dim.Blocking || guide.HoldsClock, Is.False, "normal play again, the clock runs");

            // The player carries on alone: looks up the recipient and plugs the other end.
            Click(Socket(next.Request.To).transform);
            bridge.Synchronize();
            Assert.That(next.State, Is.EqualTo(CallState.Connected), "the second connection, guided only to the jack");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.Not.EqualTo(FirstOrderGuideStage.NextCaller), "once only");
            Assert.That(guide.Stage, Is.Not.EqualTo(FirstOrderGuideStage.NextCard), "once only");
        }

        [Test]
        public void TheNextCallerStepWaitsForACallerWithTheClockRunningAndADroppedCordDropsIt()
        {
            Open();
            Call call = FinishTheFirstCall();
            SocketView a = Socket(call.Request.From);
            CableController cable = a.OccupyingCable;
            ClearWaitingCalls();
            Click(a.transform);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "setup");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "nobody waits: wait for a caller");
            Assert.That(guide.HoldsClock || Runner.IsClockHeld, Is.False, "the clock runs while no hint shows");
            AddCallOnFreeJacks(emergency: true);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage == FirstOrderGuideStage.NextCard || guide.Stage == FirstOrderGuideStage.NextCaller, Is.False,
                "an emergency is not the next caller");
            Call next = AddCallOnFreeJacks();
            Invoke(guide, "LateUpdate");
            if (guide.Stage == FirstOrderGuideStage.Emergency)
            {
                // The first emergency's own step comes first when its card shows (13a pass 4).
                session.Desk.TryTake(session.Calls.Live.First(c => c.Request.Script.IsEmergency).Id);
                Invoke(guide, "LateUpdate");
            }
            AssertStep(FirstOrderGuideStage.NextCard, spotlight.Texts.nextCard, queue.WaitingCardOf(next).transform);

            // Skip still ends it, with the cord kept in hand.
            spotlight.SkipButton.onClick.Invoke();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held));

            // A cord dropped back in the tray before the step shows drops the step.
            TearDown();
            Open();
            call = FinishTheFirstCall();
            cable = Socket(call.Request.From).OccupyingCable;
            ClearWaitingCalls();
            Click(Socket(call.Request.From).transform);
            Invoke(guide, "LateUpdate");
            Assert.That(cable.TryReturnToTray(), Is.True, "setup");
            Invoke(guide, "LateUpdate");
            AddCallOnFreeJacks();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage == FirstOrderGuideStage.NextCard || guide.Stage == FirstOrderGuideStage.NextCaller, Is.False,
                "no cord in hand: no step");
        }

        // 13a pass 5 (D81): Skip ends the tutorial during an information step, as during any other.
        [Test]
        public void SkipWorksDuringAnInformationStep()
        {
            Open();
            ConnectTheFirstCall();
            Assert.That(spotlight.IsOkShowing, Is.True, "setup: the Connected step with its OK");
            spotlight.SkipButton.onClick.Invoke();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(spotlight.Dim.Blocking || spotlight.IsOkShowing, Is.False);
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.All));
        }

        // Plays the first call to its end: the free-cord step is due on the next frame.
        private Call FinishTheFirstCall()
        {
            Call call = ConnectTheFirstCall();
            CableController cable = Socket(call.Request.From).OccupyingCable;
            // Below every price, so no shop step comes first.
            session.Wallet.TrySpend(session.Wallet.Cents);
            OkConnected();
            session.Tick(call.SecondsLeft + .1);
            Assert.That(call.State, Is.EqualTo(CallState.Ended), "setup");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Connected), "D45: the cord stays plugged");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord), "setup");
            return call;
        }

        // Every caller still waiting leaves (the dev panel's "Clear calls"), so a test picks who waits next.
        private void ClearWaitingCalls()
        {
            foreach (Call live in session.Calls.Live.ToArray())
                if (live.State == CallState.Incoming || live.State == CallState.Taken || live.State == CallState.Connecting)
                    session.Calls.End(live.Id, CallEndReason.Cleared);
            // Their cards leave as the stamp exit ends, a frame later in play: at once here, so new calls get cards.
            foreach (CallCardView card in All<CallCardView>().Where(c => c.Call != null && c.Call.State == CallState.Ended &&
                         c.Call.EndReason == CallEndReason.Cleared).ToArray())
                Invoke(card, "FinishCard");
        }

        // The OK button sits on the hint box, under its text, whole and on screen (13a pass 5).
        private void AssertOkOnHint()
        {
            Assert.That(spotlight.IsOkShowing, Is.True, $"{guide.Stage}: an information step shows OK");
            spotlight.Advance(.5f);
            Rect hint = LocalRect(spotlight.Hint), ok = LocalRect((RectTransform)spotlight.OkButton.transform),
                text = LocalRect(spotlight.Hint.GetComponentInChildren<TMP_Text>(true).rectTransform);
            Assert.That(hint.Contains(ok.min) && hint.Contains(ok.max), Is.True, $"OK {ok} inside the hint {hint}");
            Assert.That(ok.yMax, Is.LessThanOrEqualTo(text.yMin + .5f), "OK sits under the text");
            Assert.That(spotlight.OkButton.gameObject.activeInHierarchy && spotlight.OkButton.interactable, Is.True);
        }

        // The hint box sits whole on the 1920x1080 screen and holds its text without overflowing (13a pass 4).
        private void AssertHintFits()
        {
            RectTransform box = spotlight.Hint;
            // In the dim layer's own space, which spans the 1920x1080 play area (the preview scene moves the canvas).
            RectTransform screen = spotlight.Dim.rectTransform;
            Assert.That(screen.rect.size, Is.EqualTo(new Vector2(1920, 1080)), "setup");
            var corners = new Vector3[4];
            box.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 local = screen.InverseTransformPoint(corner);
                Assert.That(screen.rect.Contains(local), Is.True, $"the hint box stays on screen ({local} in {screen.rect})");
            }
            TMP_Text label = box.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, "no overflow");
            Rect area = label.rectTransform.rect;
            Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(area.height + .5f), "the box is tall enough for the text");
            for (int i = 0; i < label.textInfo.characterCount; i++)
            {
                TMP_CharacterInfo character = label.textInfo.characterInfo[i];
                if (!character.isVisible) continue;
                Assert.That(character.bottomLeft.x >= area.xMin - .5f && character.topRight.x <= area.xMax + .5f &&
                    character.descender >= area.yMin - .5f && character.ascender <= area.yMax + .5f, Is.True,
                    $"'{character.character}' sits inside the hint box");
            }
        }

        private GameLoopRunner Runner => All<GameLoopRunner>().Single();
        private int addedCalls;

        private Call AddCall(string from, string to, bool emergency = false, double? patience = 20) =>
            session.Calls.Add(new CallRequest(new CallScript("test." + ++addedCalls, "Test Caller " + addedCalls, "",
                "Put me through, please.", new JackId(from), new JackId(to), 20, 10, 5, emergency, emergency ? 30 : 0, patience)));

        // Two jacks no other live call uses, on the open board.
        private (string, string) FreeJacks()
        {
            var used = new HashSet<string>(session.Calls.Live.SelectMany(c => new[] { c.Request.From.Label, c.Request.To.Label }));
            string[] free = All<SocketView>().Where(s => s.IsOpen && s.gameObject.activeInHierarchy && !s.IsOccupied &&
                !used.Contains(s.Address)).Select(s => s.Address).Take(2).ToArray();
            Assert.That(free.Length, Is.EqualTo(2), "setup: two free jacks");
            return (free[0], free[1]);
        }

        private Call AddCallOnFreeJacks(bool emergency = false)
        {
            (string from, string to) = FreeJacks();
            return AddCall(from, to, emergency);
        }

        // D81 amended (lead, 2026-11-12): while any step shows, the whole game's clock holds, input stays live, and the
        // clock runs again when the step ends.
        [Test]
        public void EveryStepHoldsTheWholeClockWithInputLiveAndItRunsAgainAfterwards()
        {
            Open();
            GameLoopRunner runner = Runner;
            CallCardView card = FirstCard();
            Call call = card.Call;
            double now = session.Clock.Now;
            void AssertHeld(string step)
            {
                Assert.That(guide.HoldsClock && runner.IsClockHeld, Is.True, step);
                runner.Advance(5);
                Assert.That(session.Clock.Now, Is.EqualTo(now), $"{step}: the clock stands still");
                Assert.That(session.Calls.Live.Count, Is.EqualTo(1), $"{step}: nothing new rings");
            }
            AssertHeld("the caller step");
            Invoke(queue, "OnCardClicked", card);
            AssertHeld("the cord step");
            CableController cable = TrayCord();
            Click(cable.TrayView.transform);
            AssertHeld("the caller's jack");
            Click(Socket(call.Request.From).transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            AssertHeld("the book step");
            book.Toggle();
            AssertHeld("the reading step");
            book.Toggle();
            AssertHeld("the marked jack");
            Click(Socket(call.Request.To).transform);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "input works while the clock holds");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected));

            // Below every price, so no shop step follows the "Connected" one.
            session.Wallet.TrySpend(session.Wallet.Cents);
            Call waiting = AddCallOnFreeJacks();
            Assert.That(waiting.HasPatience, Is.True, "setup");
            double talk = call.SecondsLeft, patience = waiting.PatienceSecondsLeft, earned = call.CentsEarned,
                played = session.Career.SecondsPlayed;
            long cents = session.Wallet.Cents;
            int live = session.Calls.Live.Count;
            Assert.That(guide.HoldsClock, Is.True, "the connected step");
            runner.Advance(4);
            Assert.That(session.Clock.Now, Is.EqualTo(now));
            Assert.That(call.SecondsLeft, Is.EqualTo(talk), "talk time holds");
            Assert.That(call.CentsEarned, Is.EqualTo(earned), "pay holds");
            Assert.That(session.Wallet.Cents, Is.EqualTo(cents));
            Assert.That(waiting.PatienceSecondsLeft, Is.EqualTo(patience), "patience holds");
            Assert.That(session.Career.SecondsPlayed, Is.EqualTo(played));
            Assert.That(session.Calls.Live.Count, Is.EqualTo(live), "no arrivals");

            // 13a pass 5 (D81): only OK ends it, and OK works while the clock holds: no deadlock.
            runner.Advance(600);
            Sequence.Tick(600);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "no timed ending");
            Assert.That(session.Clock.Now, Is.EqualTo(now));
            ClickOk();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "13a pass 8: then the money step");
            runner.Advance(600);
            Assert.That(session.Clock.Now, Is.EqualTo(now), "the money step holds the clock too");
            Ok();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.HoldsClock || runner.IsClockHeld, Is.False, "no step on screen");
            runner.Advance(1);
            Assert.That(session.Clock.Now, Is.EqualTo(now + 1).Within(1e-9), "the clock runs again");
            Assert.That(call.SecondsLeft, Is.LessThan(talk));
            Assert.That(call.CentsEarned, Is.GreaterThan(earned));
            Assert.That(waiting.PatienceSecondsLeft, Is.LessThan(patience));
            Assert.That(session.Calls.Live.Count, Is.GreaterThan(live), "free windows fill again");
        }

        // 13a pass 4 (D81): the first emergency's card gets a step of its own; the clock holds, so it loses no time.
        [Test]
        public void TheFirstEmergencyGetsAStepOnItsCardUntilItIsTakenOncePerSaveAcrossContinue()
        {
            Open(SaveWithHints(ShopHints.All & ~ShopHints.Emergency));
            Assert.That(guide, Is.Not.Null, "Continue keeps the guide for the pending emergency step");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            // A save before the first emergency rang still owes the step.
            SaveData early = session.CaptureSave();
            Assert.That((ShopHints)early.shopHintsDone & ShopHints.Emergency, Is.EqualTo(ShopHints.None));
            TearDown();
            Open(early);

            Call other = AddCallOnFreeJacks();
            Call emergency = AddCallOnFreeJacks(emergency: true);
            CallCardView card = All<CallCardView>().Single(c => c.Call == emergency);
            Invoke(guide, "LateUpdate");
            AssertStep(FirstOrderGuideStage.Emergency, spotlight.Texts.emergency, card.transform);
            Assert.That(spotlight.Texts.emergency,
                Is.EqualTo("Emergency! Connect it before the red ring runs out, or you pay a fine."));
            AssertHintFits();
            Assert.That(Blocked(card.transform), Is.False, "the card takes the click");
            Assert.That(Blocked(All<CallCardView>().Single(c => c.Call == other).transform), Is.True);
            Assert.That(Blocked(OtherSocket(emergency).transform), Is.True);

            var audio = All<CallAudio>().Single();
            int bells = audio.BellsRung, ticks = audio.TicksPlayed;
            double response = emergency.ResponseSecondsLeft, patience = other.PatienceSecondsLeft;
            Runner.Advance(40);
            audio.Advance(40);
            Assert.That(emergency.ResponseSecondsLeft, Is.EqualTo(response), "the emergency loses no time while the step shows");
            Assert.That(other.PatienceSecondsLeft, Is.EqualTo(patience));
            Assert.That(audio.BellsRung == bells && audio.TicksPlayed == ticks, Is.True, "the bell's repeats and the ticks wait");

            Invoke(queue, "OnCardClicked", card);
            Assert.That(emergency.State, Is.EqualTo(CallState.Taken), "setup");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "taking the card ends it, the last hint");
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.All), "recorded for the save");
            Assert.That(spotlight.Dim.Blocking, Is.False);
            Runner.Advance(1);
            Assert.That(emergency.ResponseSecondsLeft, Is.LessThan(response), "the clock runs again");

            SaveData later = session.CaptureSave();
            TearDown();
            Open(later);
            Assert.That(guide == null || !guide.enabled, Is.True, "nothing left to show");
            AddCallOnFreeJacks(emergency: true);
            Assert.That(spotlight.gameObject.activeSelf, Is.False, "once per save");
        }

        [Test]
        public void AfterSkipTutorialNoEmergencyStep()
        {
            Open();
            FirstCard();
            spotlight.SkipButton.onClick.Invoke();
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.All));
            Call emergency = AddCallOnFreeJacks(emergency: true);
            Assert.That(guide.enabled, Is.False);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(guide.HoldsClock || Runner.IsClockHeld, Is.False);
            double response = emergency.ResponseSecondsLeft;
            Runner.Advance(1);
            Assert.That(emergency.ResponseSecondsLeft, Is.LessThan(response), "the clock runs");
            Assert.That((ShopHints)session.CaptureSave().shopHintsDone & ShopHints.Emergency, Is.EqualTo(ShopHints.Emergency));
        }

        [Test]
        public void SkipUnblocksAtOnceKeepsAHeldCordAndLetsTheCallerHangUpAgain()
        {
            Open();
            var card = FirstCard();
            Assert.That(card.Call.WaitingHeld, Is.True, "the guided caller waits while the guide runs");
            Invoke(queue, "OnCardClicked", card);
            CableController cable = TrayCord();
            Click(cable.TrayView.transform);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller));
            spotlight.Advance(1);
            Assert.That(Blocked(OtherSocket(card.Call).transform), Is.True, "setup");
            // The cord input keeps a held cord when the clicked target is a button (9a); Skip is one, not under the filter.
            Assert.That(spotlight.SkipButton.GetComponentInParent<Selectable>(), Is.Not.Null);
            Assert.That(spotlight.SkipButton.transform.IsChildOf(spotlight.Dim.transform), Is.False);
            spotlight.SkipButton.onClick.Invoke();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held));
            Assert.That(card.Call.WaitingHeld, Is.False, "skipped: an ordinary call again");
            Assert.That(Blocked(OtherSocket(card.Call).transform), Is.False, "unblocked at once, while it still fades");
            Assert.That(book.OpenLocked, Is.False);
            spotlight.Advance(.1f);
            Assert.That(spotlight.gameObject.activeSelf, Is.True, "it fades out");
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            book.SetOpen(true);
            Assert.That(spotlight.gameObject.activeSelf, Is.False, "skipped for good");
            book.SetOpen(false);
            // The control: with the guide gone, the same kind of empty-space click puts the cord back as always.
            GameObject board = OtherSocket(card.Call).transform.parent.gameObject;
            Assert.That(board.GetComponentInParent<Selectable>(), Is.Null, "setup");
            input.ProcessTarget(board, null, Centre(board.transform), true);
            Assert.That(cable.IsInTray, Is.True);
        }

        [Test]
        public void TheGuidedCallEndingMidGuideEndsTheGuideCleanly()
        {
            Open();
            var card = FirstCard();
            CallerSide(card);
            book.SetOpen(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook));
            // Only the dev panel's "Clear calls" can do this now.
            session.ClearCalls();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(spotlight.Dim.Blocking, Is.False);
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            book.SetOpen(false);
            book.Toggle();
            Assert.That(book.IsOpen, Is.True, "the book is free again");
            book.SetOpen(false);
            session.Tick(3);
            Assert.That(session.Calls.Live.Count, Is.GreaterThan(1), "the other slots open as before");
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void TheDarkLayerSwallowsClicksAndSitsBetweenTheBookAndThePauseMenu()
        {
            Open();
            var canvas = spotlight.GetComponent<Canvas>();
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.EqualTo(SwitchboardUISorting.Tutorial));
            Assert.That(spotlight.GetComponent<GraphicRaycaster>(), Is.Not.Null, "its own raycaster makes it the top hit");
            int gameUi = canvas.transform.parent.GetComponentInParent<Canvas>().rootCanvas.sortingOrder;
            Assert.That(canvas.sortingOrder, Is.GreaterThan(Mathf.Max(SwitchboardUISorting.Book, gameUi)));
            Assert.That(book.GetComponent<Canvas>().sortingOrder, Is.LessThan(canvas.sortingOrder));
            var pause = All<GamePauseMenu>().Single().GetComponent<Canvas>();
            Assert.That(pause.sortingOrder, Is.GreaterThan(canvas.sortingOrder), "the pause button and menu stay above it");
            Assert.That(SwitchboardUISorting.Cursor, Is.GreaterThan(canvas.sortingOrder), "the hand and held plugs stay on top");
            var clickable = spotlight.GetComponentsInChildren<Graphic>(true).Where(g => g.raycastTarget).ToArray();
            Assert.That(clickable.Select(g => g.gameObject), Is.EquivalentTo(new[] { spotlight.Dim.gameObject,
                spotlight.SkipButton.targetGraphic.gameObject, spotlight.OkButton.targetGraphic.gameObject }),
                string.Join(", ", clickable.Select(g => g.name)));
            // 13a pass 5 (D81): the OK button sits on the hint, drawn and hit above the dark layer, and shows only on
            // information steps; it looks like Skip.
            Assert.That(spotlight.OkButton.transform.IsChildOf(spotlight.Hint), Is.True);
            Assert.That(spotlight.Hint.GetSiblingIndex(), Is.GreaterThan(spotlight.Dim.transform.GetSiblingIndex()));
            Assert.That(spotlight.OkButton.gameObject.activeSelf, Is.False, "hidden until an information step");
            Assert.That(spotlight.OkButton.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("OK"));
            var okImage = (Image)spotlight.OkButton.targetGraphic;
            var skipImage = (Image)spotlight.SkipButton.targetGraphic;
            Assert.That(okImage.sprite, Is.SameAs(skipImage.sprite), "Skip's sprite");
            Assert.That(spotlight.OkButton.colors, Is.EqualTo(spotlight.SkipButton.colors), "Skip's tints");
            Assert.That(spotlight.OkButton.GetComponentInChildren<TMP_Text>(true).font,
                Is.SameAs(spotlight.SkipButton.GetComponentInChildren<TMP_Text>(true).font), "Skip's font");
            // A blocked click reaches the dark layer and stops: no card, book, shop or socket handler above it.
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(spotlight.Dim.gameObject), Is.Null);
            Assert.That(spotlight.Dim.GetComponentsInParent<IPointerDownHandler>(true), Is.EquivalentTo(new[] { spotlight.Dim }),
                "only its own, which reports the press (fix 2)");
            Assert.That(ExecuteEvents.GetEventHandler<IPointerUpHandler>(spotlight.Dim.gameObject), Is.Null);
            Assert.That(spotlight.Dim.GetComponent<IPointerBlocker>(), Is.Not.Null, "the cord input knows it");
            Assert.That(spotlight.Dim.Blocking, Is.False, "nothing is blocked before the guide shows");
            var stretch = (RectTransform)spotlight.transform;
            Assert.That(stretch.anchorMin == Vector2.zero && stretch.anchorMax == Vector2.one, Is.True, "covers every aspect ratio");
            Assert.That(spotlight.Dim.SpotlightShader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(spotlight.Dim.SpotlightShader), Is.False);
        }

        [Test]
        public void HolesFollowTheirTargetsAndSlideToTheNextStep()
        {
            Open();
            var card = FirstCard();
            spotlight.Advance(3);
            Rect before = spotlight.Goal(0);
            Assert.That(Distance(spotlight.Dim.HoleRect(0), before), Is.LessThan(1f));
            var rect = (RectTransform)card.transform;
            Vector3 shift = spotlight.Dim.rectTransform.InverseTransformVector(rect.parent.TransformVector(new Vector3(60, -30)));
            rect.anchoredPosition += new Vector2(60, -30);
            spotlight.Advance(3);
            Rect after = spotlight.Goal(0);
            Assert.That(shift.magnitude, Is.GreaterThan(10f), "setup");
            Assert.That(Vector2.Distance(after.center - before.center, shift), Is.LessThan(.5f), "the goal follows the card");
            Assert.That(Distance(spotlight.Dim.HoleRect(0), after), Is.LessThan(1f), "and the hole follows the goal");
            Assert.That(Blocked(card.transform), Is.False, "and so does its click area");

            // 13a pass 6: the next step lights a tray cord (pass 7: only the tray).
            Invoke(queue, "OnCardClicked", card);
            spotlight.Advance(.05f);
            Rect tray = spotlight.Goal(0);
            Assert.That(Vector2.Distance(tray.center, after.center), Is.GreaterThan(50f), "setup: the tray is away from the card");
            Rect sliding = spotlight.Dim.HoleRect(0);
            Assert.That(Vector2.Distance(sliding.center, after.center), Is.GreaterThan(.5f), "it moves at once");
            Assert.That(Vector2.Distance(sliding.center, tray.center), Is.GreaterThan(.5f), "but slides, not jumps");
            spotlight.Advance(3);
            Assert.That(Distance(spotlight.Dim.HoleRect(0), tray), Is.LessThan(1f));
        }

        [Test]
        public void ArrowsStayOffOtherJacksAndOffTheBookPages()
        {
            Open();
            var card = FirstCard();
            Call call = card.Call;
            Invoke(queue, "OnCardClicked", card);
            Click(TrayCord().TrayView.transform);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "setup");
            spotlight.Advance(3);
            AssertArrowOffOtherJacks(call);
            Click(Socket(call.Request.From).transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "setup");
            book.Toggle();
            spotlight.Advance(3);
            Rect page = LocalRect(book.OpenBookRect);
            Assert.That(page.Overlaps(LocalRect(spotlight.Arrow)), Is.False, "the reading step's arrow stops at the book's edge");

            book.Toggle();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient), "setup");
            spotlight.Advance(3);
            AssertArrowOffOtherJacks(call);
        }

        // 10b fix 3: right after a step change the arrow jumped between several places for a moment (steps 7a and
        // 7b). Every step's arrow must keep its side and direction from its first frame and glide straight to its place.
        [Test]
        public void EveryStepsArrowKeepsItsSideAndGlidesStraightToItsPlace()
        {
            Open();
            // The breathing moves the arrow by design; without it, any other movement is the arrow being re-placed.
            typeof(TutorialSpotlight).GetField("pulse", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spotlight, 0f);
            var card = FirstCard();
            Call call = card.Call;
            CableController cable = TrayCord();
            SocketView a = Socket(call.Request.From), b = Socket(call.Request.To);
            AssertArrowSteady(FirstOrderGuideStage.Portrait);
            Invoke(queue, "OnCardClicked", card);
            AssertArrowSteady(FirstOrderGuideStage.Cables);
            Click(cable.TrayView.transform);
            AssertArrowSteady(FirstOrderGuideStage.PluggingCaller);
            Click(a.transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            AssertArrowSteady(FirstOrderGuideStage.Book);
            book.Toggle();
            AssertArrowSteady(FirstOrderGuideStage.ReadingBook);
            string recipient = call.Request.To.Label;
            while (book.RowOnSpread(recipient) == null)
            {
                BookClick(book.PageCorner(book.SpreadDirectionTo(recipient)));
                Invoke(guide, "LateUpdate");
                AssertArrowSteady(FirstOrderGuideStage.ReadingBook);
            }
            BookClick(book.RowOnSpread(recipient));
            AssertArrowSteady(FirstOrderGuideStage.PluggingRecipient);
            Click(b.transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            AssertArrowSteady(FirstOrderGuideStage.Connected);
            session.Wallet.Earn(session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0].CostCents);
            OkConnected();
            AssertArrowSteady(FirstOrderGuideStage.BuyCord);
        }

        private void AssertArrowSteady(FirstOrderGuideStage stage)
        {
            Assert.That(guide.Stage, Is.EqualTo(stage), "setup");
            const int frames = 240;
            var positions = new Vector2[frames];
            var angles = new float[frames];
            var sides = new int[frames];
            var path = new System.Text.StringBuilder();
            for (int i = 0; i < frames; i++)
            {
                spotlight.Advance(1 / 60f);
                Assert.That(spotlight.Arrow.gameObject.activeSelf, Is.True, $"{stage} frame {i}: the arrow shows");
                // The tip, which rides on the hole; the tail follows the hint, and the length moves in whole units.
                positions[i] = Tip();
                angles[i] = spotlight.Arrow.localEulerAngles.z;
                sides[i] = System.Convert.ToInt32(Field<object>(spotlight, "side"));
                if (i < 40) path.Append($" {positions[i]:F1}@{angles[i]:F0}");
            }
            Vector2 end = positions[frames - 1];
            Assert.That(Vector2.Distance(positions[frames - 2], end), Is.LessThan(.01f), $"{stage}: the arrow settles");
            for (int i = 1; i < frames; i++)
            {
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(angles[i], angles[0])), Is.LessThan(.01f),
                    $"{stage} frame {i}: the arrow turned from {angles[0]} to {angles[i]};{path}");
                Assert.That(sides[i], Is.EqualTo(sides[0]), $"{stage} frame {i}: the hint changed sides");
                // Closer to where it lands every frame: never back and forth.
                Assert.That(Vector2.Distance(positions[i], end), Is.LessThanOrEqualTo(Vector2.Distance(positions[i - 1], end) + .01f),
                    $"{stage} frame {i}: the arrow moved away from its place;{path}");
            }
        }

        private void AssertArrowOffOtherJacks(Call call)
        {
            Rect arrow = LocalRect(spotlight.Arrow);
            foreach (SocketView socket in All<SocketView>().Where(s => s.gameObject.activeInHierarchy &&
                         s.Address != call.Request.From.Label && s.Address != call.Request.To.Label))
            {
                Rect jack = LocalRect((RectTransform)socket.transform);
                Assert.That(Vector2.Distance(Tip(), jack.center), Is.GreaterThan(jack.width * .5f),
                    $"{guide.Stage}: the arrow tip is on {socket.Address}");
            }
            Assert.That(arrow.width, Is.GreaterThan(0));
        }

        private Vector2 Tip()
        {
            RectTransform arrow = spotlight.Arrow;
            Vector3 world = arrow.TransformPoint(new Vector3(arrow.rect.xMax, arrow.rect.center.y));
            return spotlight.Dim.rectTransform.InverseTransformPoint(world);
        }

        // The rotated rect's bounds in the dark layer's units.
        private Rect LocalRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (Vector3 corner in corners)
            {
                Vector2 local = spotlight.Dim.rectTransform.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [Test]
        public void ALoadedGameHasNoTutorial()
        {
            // A save from before fix 2 has no shop-hint record: every hint counts as shown.
            Open(new SaveData { version = SaveData.CurrentVersion, cents = 0 });
            Assert.That(session.IsTutorial, Is.False);
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.All));
            Assert.That(guide == null || !guide.enabled || guide.Spotlight == null, Is.True, "no guide is bound");
            session.Tick(3);
            Assert.That(session.Calls.Live.Count, Is.GreaterThan(1), "no one-call start: the slots fill at once");
            Assert.That(session.Calls.Live.All(c => !c.WaitingHeld), Is.True, "every caller may hang up");
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            var card = All<CallCardView>().First(c => c.Call != null);
            Invoke(queue, "OnCardClicked", card);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            book.Toggle();
            Assert.That(book.IsOpen, Is.True, "nothing is locked");
            book.Toggle();

            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            session.Wallet.Earn(first.CostCents);
            Invoke(shop, "LateUpdate");
            Assert.That(spotlight.gameObject.activeSelf, Is.False, "no shop steps");
            Assert.That(All<SlowGuidePulse>().Any(p => p.IsShowing), Is.False, "and no first-cord pulse");
        }

        // Holes breathe a few units in and out around their goal, evenly on every side.
        private float Distance(Rect hole, Rect goal) => Vector2.Distance(hole.center, goal.center) +
            Mathf.Max(0, Mathf.Abs(hole.width - goal.width) - 2 * Field<float>(spotlight, "pulse"));
    }
}
