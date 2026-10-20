using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Cables;
using Switchboard.Content;
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

        private void FinishTyping(CallCardView card)
        {
            Field<TypingReveal>(card, "typing").Advance(60, 40);
            Invoke(card, "TypeOn");
            Invoke(guide, "LateUpdate");
        }

        // The advancing click: the dark layer swallows it and only reports the press (fix 2).
        private void PressDim()
        {
            input.ProcessTarget(spotlight.Dim.gameObject, null, new Vector2(5, 5), true);
            spotlight.Dim.OnPointerDown(null);
        }

        // Steps 2 and 3: the message, then the caller's blinking jack; each moves on with a click after its guard.
        private void ReadMessage(CallCardView card)
        {
            FinishTyping(card);
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.CallerJack), "setup");
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "setup");
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
            AssertStep(FirstOrderGuideStage.TaskText, texts.readMessage, card.SpeechBubbleRect);
            AssertBlockedEverywhereBut(call, card);
            Assert.That(Blocked(card.SpeechBubbleRect), Is.True, "nothing takes clicks while the message types");
            PressDim();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText), "a click while it types does nothing");

            // Fix 2: once typed, the message waits for a click, so there is time to read it.
            FinishTyping(card);
            AssertStep(FirstOrderGuideStage.TaskText, texts.readMessageDone, card.SpeechBubbleRect);
            AssertBlockedEverywhereBut(call, card);
            CallState before = call.State;
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText), "a click right after the text is a stray one");
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(call.State, Is.EqualTo(before), "the advancing click does nothing else");
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));
            Assert.That(book.IsOpen, Is.False);
            Assert.That(cable.IsInTray, Is.True);

            // 10b fix 1: step 3 lights the caller's jack, which the game makes blink (the call in hand: brighter).
            CallerJackLights lights = All<CallerJackLights>().Single();
            lights.Refresh();
            AssertStep(FirstOrderGuideStage.CallerJack, texts.callerJack, a.transform);
            Assert.That(texts.callerJack, Is.EqualTo("This blinking light is the caller's line. Now find who they want."));
            Assert.That(a.Lamp, Is.EqualTo(SocketLamp.Blinking), "the caller's jack blinks");
            Assert.That(spotlight.TargetAt(0).Round && !spotlight.TargetAt(0).Clickable, Is.True, "a round hole that only shows");
            AssertBlockedEverywhereBut(call, card);
            Assert.That(Blocked(a.transform), Is.True, "the jack takes no plug here");
            book.Toggle();
            Assert.That(book.IsOpen, Is.False, "the book waits for its own step");
            Click(cable.TrayView.transform);
            Click(a.transform);
            Assert.That(cable.IsInTray && !a.IsOccupied, Is.True, "a blocked click changes nothing");
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.CallerJack), "the click that read the message is a stray one");
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(call.State, Is.EqualTo(before), "the advancing click does nothing else");
            Assert.That(cable.IsInTray && !a.IsOccupied, Is.True);
            AssertStep(FirstOrderGuideStage.Book, texts.openBook, book.ClosedBookGraphic.transform);
            AssertBlockedEverywhereBut(call, card, book.ClosedBookGraphic.transform);
            Click(cable.TrayView.transform);
            Assert.That(cable.IsInTray, Is.True);
            book.Toggle();
            Assert.That(book.IsOpen, Is.True, "the book step opens the book");

            // D58: the board lights the caller's jack, so only the recipient's row lights (Book.json sets how many
            // spreads there are: a row on another spread lights the corner that turns to it). The corners take clicks.
            string recipient = call.Request.To.Label;
            AssertReadingStep(texts, recipient);
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
            BookClick(row);
            Assert.That(book.IsOpen, Is.False, "the row click closes the book");
            Assert.That(lights.MarkedJack, Is.EqualTo(recipient), "and marks the jack");

            AssertStep(FirstOrderGuideStage.Cables, texts.takeCord, All<CableController>()
                .Where(c => c.gameObject.activeInHierarchy && c.IsInTray).Select(c => c.TrayView.transform).ToArray());
            AssertBlockedEverywhereBut(call, card, All<CableController>()
                .Where(c => c.gameObject.activeInHierarchy && c.IsInTray).Select(c => c.TrayView.transform).ToArray());
            Click(a.transform);
            Assert.That(a.IsOccupied, Is.False);
            book.Toggle();
            Assert.That(book.IsOpen, Is.False, "the book stays shut once read");

            Click(cable.TrayView.transform);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held), "the tray cord takes the click");
            // 10b fix 2: one jack at a time. Step 7a lights only the caller's blinking jack.
            AssertStep(FirstOrderGuideStage.PluggingCaller, texts.plugCaller, a.transform);
            lights.Refresh();
            // As in real play: the caller's jack blinks, the marked recipient glows steadily.
            Assert.That(a.Lamp, Is.EqualTo(SocketLamp.Blinking), "the game blinks the caller's jack");
            Assert.That(b.Lamp, Is.EqualTo(SocketLamp.Steady), "and the marked one glows");
            Assert.That(spotlight.TargetAt(0).Round, Is.True, "jacks get round holes");
            AssertBlockedEverywhereBut(call, card, a.transform);
            Assert.That(Blocked(b.transform), Is.True, "the found jack waits for step 7b");

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
            Invoke(guide, "LateUpdate");
            // Step 7b: only the jack found in the book.
            AssertStep(FirstOrderGuideStage.PluggingRecipient, texts.plugRecipient, b.transform);
            lights.Refresh();
            Assert.That(b.Lamp, Is.EqualTo(SocketLamp.Steady), "the found jack still glows");
            AssertBlockedEverywhereBut(call, card, b.transform);
            Assert.That(Blocked(a.transform), Is.True, "the caller's plug stays put");
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
            AssertStep(FirstOrderGuideStage.Connected, texts.connected, header);
            AssertBlockedEverywhereBut(call, card);
            Assert.That(Blocked(header), Is.True, "step 8 takes any click only to dismiss itself");
            Assert.That(call.WaitingHeld, Is.False, "connected: the tutorial's hold on the call is over");

            session.Tick(call.SecondsLeft + .1);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the call finished: normal play");
            spotlight.Advance(1);
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
            Assert.That(spotlight.Dim.HasMaterial, Is.False, "the spotlight frees its material while hidden");
            Assert.That(book.OpenLocked, Is.False);
        }

        [Test]
        public void TheShopStepsLightTheFirstCordOnceAffordableThenTheMysteryUpgrade()
        {
            Open();
            Call call = ConnectTheFirstCall();
            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            // Economy.json sets the price: start below it, whatever the first call paid.
            session.Wallet.TrySpend(session.Wallet.Cents);
            Sequence.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Invoke(guide, "LateUpdate");
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
            Assert.That(Blocked(secret), Is.True, "any click only dismisses step 10");

            Sequence.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
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
            Sequence.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Invoke(guide, "LateUpdate");
            IUpgrade cord = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            IUpgrade profit = session.Upgrades.ChainOf(UpgradeKind.ExtraIncome)[0];
            IUpgrade clients = session.Upgrades.ChainOf(UpgradeKind.ExtraClients)[0];
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "steps 9-10 first");
            Invoke(shop.ButtonFor(cord).GetComponent<UpgradeSlotView>(), "OnClick");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(shop.Secret != null ? FirstOrderGuideStage.Mystery : FirstOrderGuideStage.WaitingForShop));
            Sequence.Tick(FirstOrderGuideSequence.MysterySeconds);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "a short gap after step 10");

            Sequence.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Invoke(guide, "LateUpdate");
            RectTransform profitButton = shop.ButtonFor(profit);
            Assert.That(profitButton, Is.Not.Null);
            AssertStep(FirstOrderGuideStage.Profit, spotlight.Texts.buyProfit, profitButton);
            var card = All<CallCardView>().First(c => c.Call == call);
            AssertBlockedEverywhereBut(call, card, profitButton);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit), "a click as it appears doesn't dismiss it");
            Sequence.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "any click elsewhere dismisses it");
            Assert.That(session.Upgrades.IsOwned(profit.Id), Is.False, "the dismissing click buys nothing");
            Assert.That(session.ShopHintsDone & ShopHints.Profit, Is.EqualTo(ShopHints.Profit));
            Assert.That(spotlight.Dim.Blocking, Is.False);

            Sequence.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Invoke(guide, "LateUpdate");
            RectTransform clientsButton = shop.ButtonFor(clients);
            AssertStep(FirstOrderGuideStage.Clients, spotlight.Texts.buyClients, clientsButton);
            Assert.That(Blocked(clientsButton), Is.False, "the lit button buys as usual");
            Invoke(clientsButton.GetComponent<UpgradeSlotView>(), "OnClick");
            Assert.That(session.Upgrades.IsOwned(clients.Id), Is.True);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
            Assert.That(guide.enabled, Is.False, "nothing runs once the guide is done");
            Assert.That(session.ShopHintsDone, Is.EqualTo(ShopHints.All));
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
            Sequence.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            SaveData saved = session.CaptureSave();
            Assert.That(saved.shopHintsDone, Is.EqualTo((int)ShopHints.All));

            TearDown();
            Open(saved);
            Assert.That(guide == null || !guide.enabled, Is.True, "every hint done: no guide");
            session.Wallet.Earn(1000000);
            Invoke(shop, "LateUpdate");
            Assert.That(spotlight.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void AlreadyAffordableTheCordStepFollowsStepSevenAtOnce()
        {
            Open();
            ConnectTheFirstCall();
            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            session.Wallet.Earn(first.CostCents);
            Sequence.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Invoke(guide, "LateUpdate");
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
            Sequence.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(shop.Secret != null ? FirstOrderGuideStage.Mystery : FirstOrderGuideStage.Complete),
                "nothing left to buy: straight to the mystery upgrade");

            // No first cord for sale (none in the file, or not on the shelf): no shop steps.

            TearDown();
            Open();
            ConnectTheFirstCall();
            guide.BindShop(null, null);
            Sequence.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
            Assert.That(spotlight.Dim.Blocking, Is.False);
        }

        private Call ConnectTheFirstCall()
        {
            var card = FirstCard();
            Invoke(queue, "OnCardClicked", card);
            ReadMessage(card);
            book.Toggle();
            book.Toggle();
            CableController cable = TrayCord();
            Click(cable.TrayView.transform);
            Call call = card.Call;
            Click(Socket(call.Request.From).transform);
            Invoke(guide, "LateUpdate"); // step 7b
            Click(Socket(call.Request.To).transform);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "setup");
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "setup");
            return call;
        }

        [Test]
        public void SkipUnblocksAtOnceKeepsAHeldCordAndLetsTheCallerHangUpAgain()
        {
            Open();
            var card = FirstCard();
            Assert.That(card.Call.WaitingHeld, Is.True, "the guided caller waits while the guide runs");
            Invoke(queue, "OnCardClicked", card);
            ReadMessage(card);
            book.Toggle();
            book.Toggle();
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
            Invoke(queue, "OnCardClicked", card);
            ReadMessage(card);
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
                spotlight.SkipButton.targetGraphic.gameObject }), string.Join(", ", clickable.Select(g => g.name)));
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

            Invoke(queue, "OnCardClicked", card);
            spotlight.Advance(.05f);
            Rect bubble = spotlight.Goal(0);
            Assert.That(Vector2.Distance(bubble.center, after.center), Is.GreaterThan(50f), "setup: the bubble is beside the card");
            Rect sliding = spotlight.Dim.HoleRect(0);
            Assert.That(Vector2.Distance(sliding.center, after.center), Is.GreaterThan(.5f), "it moves at once");
            Assert.That(Vector2.Distance(sliding.center, bubble.center), Is.GreaterThan(.5f), "but slides, not jumps");
            spotlight.Advance(3);
            Assert.That(Distance(spotlight.Dim.HoleRect(0), bubble), Is.LessThan(1f));
        }

        [Test]
        public void ArrowsStayOffOtherJacksAndOffTheBookPages()
        {
            Open();
            var card = FirstCard();
            Call call = card.Call;
            Invoke(queue, "OnCardClicked", card);
            FinishTyping(card);
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.CallerJack), "setup");
            spotlight.Advance(3);
            AssertArrowOffOtherJacks(call);
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "setup");
            book.Toggle();
            spotlight.Advance(3);
            Rect page = LocalRect(book.OpenBookRect);
            Assert.That(page.Overlaps(LocalRect(spotlight.Arrow)), Is.False, "step 5's arrow stops at the book's edge");

            book.Toggle();
            Click(TrayCord().TrayView.transform);
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
            AssertArrowSteady(FirstOrderGuideStage.TaskText);
            FinishTyping(card);
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
            Invoke(guide, "LateUpdate");
            AssertArrowSteady(FirstOrderGuideStage.CallerJack);
            Sequence.Tick(FirstOrderGuideSequence.ReadClickDelay);
            PressDim();
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
            AssertArrowSteady(FirstOrderGuideStage.Cables);
            Click(cable.TrayView.transform);
            AssertArrowSteady(FirstOrderGuideStage.PluggingCaller);
            Click(a.transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            AssertArrowSteady(FirstOrderGuideStage.PluggingRecipient);
            Click(b.transform);
            bridge.Synchronize();
            Invoke(guide, "LateUpdate");
            AssertArrowSteady(FirstOrderGuideStage.Connected);
            session.Wallet.Earn(session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0].CostCents);
            Sequence.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Invoke(guide, "LateUpdate");
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
