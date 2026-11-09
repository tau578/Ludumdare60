using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Cables;
using Switchboard.Domain;
using Switchboard.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = NUnit.Framework.Is;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// Step 11c (D60): the main visual feedback in the real Final scene. A loaded game, so no tutorial; the tests add
    /// their own calls and never tick the queue, so only those calls are on cards.
    /// </summary>
    public sealed class FeedbackEffectsTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
        private CordBridge bridge;
        private CableInputController input;
        private MoneyCounterView counter;
        private int callNumber;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static object Invoke(object target, string method, params object[] args) => target.GetType()
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
            var book = All<PhoneBookView>().Single();
            book.ReloadContacts();
            book.SetOpen(false);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Install", null, new SaveData { version = SaveData.CurrentVersion, cents = 1000 });
            session = installer.Session;
            queue = All<CallQueueView>().Single();
            bridge = All<CordBridge>().Single();
            input = All<CableInputController>().Single();
            counter = All<MoneyCounterView>().Single();
            foreach (CableController cable in All<CableController>().Where(c => c.gameObject.activeInHierarchy && c.TrayView != null))
                cable.TrayView.Bind(cable);
            Assert.That(session.IsTutorial, Is.False, "setup: no tutorial in the way");
            Assert.That(session.Calls.Live.Count, Is.Zero, "setup: the queue hasn't ticked");
            callNumber = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private Call AddCall(string from, string to, bool emergency = false, double response = 30, long penalty = 0)
        {
            var script = new CallScript("test." + ++callNumber, "Test Caller " + callNumber, "", "Put me through, please.",
                new JackId(from), new JackId(to), 20, 10, 5, emergency, response, penalty);
            return session.Calls.Add(new CallRequest(script));
        }

        private CallCardView CardOf(Call call) => All<CallCardView>().SingleOrDefault(c => c.Call == call);
        private SocketView Socket(string jack) => All<SocketView>().Single(s => s.Address == jack);

        // Lets a new card's slide and ring shake finish, on real time only.
        private static void Settle(CallCardView card)
        {
            for (int i = 0; i < 4; i++) card.TickMotion(0, 1);
        }

        private static Vector2 Centre(Transform target)
        {
            var rect = (RectTransform)target;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private void Click(Transform target) => input.ProcessTarget(target.gameObject, null, Centre(target), true);

        private void Plug(SocketView socket)
        {
            if (input.ActiveCable == null)
                Click(All<CableController>().First(c => c.gameObject.activeInHierarchy && c.IsInTray).TrayView.transform);
            Assert.That(input.ActiveCable, Is.Not.Null, "setup: a cord in hand");
            Click(socket.transform);
            bridge.Synchronize();
        }

        private string Reels => string.Concat(counter.Reels.Select(r => r.text));
        private string[] ReelTexts => counter.Reels.Select(r => r.text).ToArray();

        // Brings the wallet to an exact amount and lets the reels settle there.
        private void ShowAmount(long cents)
        {
            if (cents > session.Wallet.Cents) session.Wallet.Earn(cents - session.Wallet.Cents);
            else session.Wallet.TrySpend(session.Wallet.Cents - cents);
            Assert.That(session.Wallet.Cents, Is.EqualTo(cents), "setup: the wallet holds the amount");
            Invoke(counter, "RefreshBalance");
            Invoke(counter, "TickRolls", 1f);
        }

        // Moves the wallet to a new amount and returns which reels roll for it.
        private bool[] RollTo(long cents)
        {
            session.Wallet.Earn(cents - session.Wallet.Cents);
            Invoke(counter, "RefreshBalance");
            return Enumerable.Range(0, 6).Select(counter.IsRolling).ToArray();
        }

        [Test]
        public void ReelsRollOnlyWhereTheCharacterChangesAndEndOnTheExactAmount()
        {
            Assert.That(counter.Reels, Is.Not.Null, "setup: the shop header's reels");
            Assert.That(counter.Reels.Length, Is.EqualTo(6), "six digit drums beside the painted $");
            Assert.That(ReelTexts, Is.EqualTo(new[] { "", "1", "0", ".", "0", "0" }), "$10.00, the dot on its own drum");
            Assert.That(Enumerable.Range(0, 6).Any(counter.IsRolling), Is.False, "the first balance shows without rolling");
            Assert.That(All<TMP_Text>().Any(t => t.name == "Balance decimal point"), Is.False, "no separate dot over a drum");

            ShowAmount(44290);
            Assert.That(Reels, Is.EqualTo("442.90"));
            Assert.That(RollTo(44295), Is.EqualTo(new[] { false, false, false, false, false, true }), "only the cents drum rolls");
            Assert.That(Reels, Is.EqualTo("442.95"), "the new character is already the drum's text");
            Invoke(counter, "TickRolls", 0.5f);
            Assert.That(Enumerable.Range(0, 6).Any(counter.IsRolling), Is.False);
            Assert.That(counter.Reels.Where(r => r.text != ".").All(r => r.rectTransform.anchoredPosition == Vector2.zero), Is.True,
                "every digit drum at rest");
            Assert.That(All<TMP_Text>().Where(t => t.name.StartsWith("Leaving digit")).All(t => !t.gameObject.activeSelf), Is.True);

            // $5.00 from $442.95: the dot stays on its drum, so that drum stays still.
            session.Wallet.TrySpend(44295 - 500);
            Invoke(counter, "RefreshBalance");
            Assert.That(Enumerable.Range(0, 6).Select(counter.IsRolling), Is.EqualTo(new[] { true, true, true, false, true, true }));
            Invoke(counter, "TickRolls", 0.5f);
            Assert.That(ReelTexts, Is.EqualTo(new[] { "", "", "5", ".", "0", "0" }), "$5.00");
            TMP_Text dot = counter.Reels[3];
            Assert.That(dot.rectTransform.localScale.x, Is.GreaterThan(1.2f), "the dot is drawn bigger on its drum");

            // $999.99 to $1,000: whole dollars from here, so every drum rolls, the dot's drum too.
            ShowAmount(99999);
            Assert.That(Reels, Is.EqualTo("999.99"));
            Assert.That(RollTo(100000), Is.All.True, "all six drums roll together");
            Assert.That(ReelTexts, Is.EqualTo(new[] { "", "", "1", "0", "0", "0" }));
            Invoke(counter, "TickRolls", 0.5f);
            Assert.That(dot.rectTransform.localScale, Is.EqualTo(Vector3.one), "a digit on the former dot drum is normal size");
            Assert.That(dot.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));

            // A million and more: four digits, the dot on its own drum, and a unit.
            ShowAmount(123456789);
            Assert.That(ReelTexts, Is.EqualTo(new[] { "1", ".", "2", "3", "4", "M" }), "$1,234,567.89");
            Assert.That(RollTo(123500000), Is.EqualTo(new[] { false, false, false, false, true, false }), "$1,235,000.00");
            Invoke(counter, "TickRolls", 0.5f);
            Assert.That(Reels, Is.EqualTo("1.235M"));
            Assert.That(All<TMP_Text>().Single(t => t.name == "Balance digit 5").text, Is.EqualTo("M"));
        }

        [TestCase(0, "  0.00")]
        [TestCase(5, "  0.05")]
        [TestCase(500, "  5.00")]
        [TestCase(4290, " 42.90")]
        [TestCase(44290, "442.90")]
        [TestCase(99999, "999.99")]
        [TestCase(100000, "  1000")]
        [TestCase(100099, "  1000")]
        [TestCase(12345678, "123456")]
        [TestCase(99999999, "999999")]
        [TestCase(100000000, "1.000M")]
        [TestCase(123456789, "1.234M")]
        [TestCase(1234567800, "12.34M")]
        [TestCase(12345678900, "123.4M")]
        [TestCase(99999999999, "999.9M")]
        [TestCase(100000000000, "1.000B")]
        [TestCase(123456789000000, "1.234T")]
        [TestCase(99999999999999999, "999.9T")]
        [TestCase(100000000000000000, "1.000Q")]
        [TestCase(long.MaxValue, "92.23Q")]
        public void TheDrumsShowAtMostSixCharactersAndCutRatherThanRoundUp(long cents, string expected)
        {
            var drums = new char[6];
            MoneyCounterView.FillReels(cents, drums);
            Assert.That(new string(drums), Is.EqualTo(expected));
        }

        [Test]
        public void AnIdleCounterAllocatesNothingPerFrame()
        {
            var lateUpdate = (Action)Delegate.CreateDelegate(typeof(Action), counter,
                typeof(MoneyCounterView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic));
            for (int i = 0; i < 20; i++) lateUpdate();
            Assert.That(() => GC.KeepAlive(new object[16]), UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory(),
                "setup: the allocation check works");
            Assert.That(() => { for (int i = 0; i < 50; i++) lateUpdate(); },
                UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void AFineShakesThePlaqueRedAndSendsCoinsToTheMissedCard()
        {
            Call call = AddCall("A1", "B2", emergency: true, response: 5, penalty: 300);
            CallCardView card = CardOf(call);
            Assert.That(card, Is.Not.Null, "setup: the emergency has a card");
            Assert.That(counter.IsShaking, Is.False);
            session.Calls.Tick(6);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(call.FineCents, Is.EqualTo(300));
            Assert.That(counter.IsShaking, Is.True, "the plaque shakes");
            Color error = All<EmergencyLampView>().Single().Style.Error;
            Assert.That(counter.Reels.All(r => r.color == error), Is.True, "and flashes Error crimson");
            var coins = All<IncomeCoinsView>().Single();
            Assert.That(coins.AwayCount, Is.InRange(3, 5), "a few coins fly to the missed card");
            Assert.That(coins.IsFlying, Is.False, "fine coins never hold the counter back");
            Assert.That(coins.GetComponentsInChildren<IncomeCoinGraphic>().All(g => !g.raycastTarget), Is.True);
            coins.Tick(2);
            Assert.That(coins.HasCoins, Is.False, "they fade out and go");
            Invoke(counter, "TickInsufficientFunds", 1f);
            Assert.That(counter.IsShaking, Is.False);
        }

        [Test]
        public void AFineFromAnEmptyWalletShakesButSendsNoCoins()
        {
            session.Wallet.TrySpend(session.Wallet.Cents);
            Invoke(counter, "RefreshBalance");
            Call call = AddCall("A1", "B2", emergency: true, response: 5, penalty: 300);
            session.Calls.Tick(6);
            Assert.That(call.FineCents, Is.Zero);
            Assert.That(counter.IsShaking, Is.True);
            Assert.That(All<IncomeCoinsView>().All(c => !c.HasCoins), Is.True, "no money moved, so no coins");
        }

        [Test]
        public void AnEmergencyWobblesOnlyWhileItWaitsAndFreezesOnPause()
        {
            Call normal = AddCall("A1", "B2");
            Call emergency = AddCall("C1", "D2", emergency: true, response: 30, penalty: 100);
            CallCardView normalCard = CardOf(normal), card = CardOf(emergency);
            Settle(normalCard);
            Settle(card);
            normalCard.TickMotion(.2f, 0);
            Assert.That(normalCard.MotionAngle, Is.Zero, "a normal call doesn't wobble");
            card.TickMotion(.2f, 0);
            Assert.That(Mathf.Abs(card.MotionAngle), Is.GreaterThan(.5f).And.LessThanOrEqualTo(2.01f), "a gentle wobble");

            float paused = card.MotionAngle;
            card.TickMotion(0, .3f);
            card.TickMotion(0, .3f);
            Assert.That(card.MotionAngle, Is.EqualTo(paused), "no game time, no wobble");
            var lamp = All<EmergencyLampView>().Single();
            lamp.Tick(.1f);
            Assert.That(lamp.IsWarning, Is.True, "the warning lamp flashes while it waits");
            float brightness = lamp.Brightness;
            lamp.Tick(0);
            Assert.That(lamp.Brightness, Is.EqualTo(brightness), "and freezes on pause");
            lamp.Tick(.13f);
            Assert.That(lamp.Brightness, Is.Not.EqualTo(brightness));

            // The last seconds: harder, in step with the ring's pulse.
            session.Calls.Tick(25);
            Assert.That(card.IsUrgent, Is.True);
            float strongest = 0;
            for (int i = 0; i < 20; i++)
            {
                card.TickMotion(.02f, 0);
                strongest = Mathf.Max(strongest, Mathf.Abs(card.MotionAngle));
            }
            Assert.That(strongest, Is.GreaterThan(2.5f));

            Assert.That(session.Calls.Take(emergency.Id), Is.True);
            card.TickMotion(.1f, 0);
            Assert.That(card.MotionAngle, Is.Not.Zero, "taken but not connected: still waiting");
            Assert.That(session.Calls.Connect(emergency.Id), Is.True);
            card.TickMotion(.1f, 0);
            Assert.That(card.MotionAngle, Is.Zero, "connected: still");
            Assert.That(card.transform.localRotation, Is.EqualTo(Quaternion.identity));
            lamp.Tick(.1f);
            Assert.That(lamp.IsWarning, Is.False);
            Assert.That(lamp.Brightness, Is.Zero);
        }

        [Test]
        public void ANewCardSlidesInAndArrivalKeepsTheStampHoldOrder()
        {
            var calls = new List<Call>();
            for (int i = 0; i < session.Stats.ClientSlots; i++) calls.Add(AddCall("A" + (i % 4 + 1), "B" + (i % 4 + 1)));
            Assert.That(calls.All(c => CardOf(c) != null), Is.True, "setup: every open slot has a card");
            CallCardView first = CardOf(calls[0]);
            Assert.That(first.IsArriving, Is.True, "a new card slides in");
            Assert.That(first.MotionOffset.x, Is.LessThan(0), "from the left");
            Assert.That(first.GetComponent<CanvasGroup>().blocksRaycasts, Is.True, "clickable at once");
            first.TickMotion(0, 1);
            Assert.That(first.IsArriving, Is.False);
            Assert.That(first.IsRingShaking, Is.True, "then it shakes as its bell rings");
            first.TickMotion(0, 1);
            Assert.That(first.MotionOffset, Is.EqualTo(Vector2.zero));
            Assert.That(first.MotionAngle, Is.Zero);

            // A wrong number ends the first call: its stamp holds the slot, so a new call waits without a card.
            session.Calls.Take(calls[0].Id);
            session.Calls.End(calls[0].Id, CallEndReason.WrongConnection);
            Assert.That(first.IsStampShowing, Is.True);
            Assert.That(first.IsShakingNo, Is.True, "the card shakes \"no\"");
            first.TickMotion(0, .1f);
            Assert.That(first.MotionOffset.x, Is.Not.Zero);
            Call waiting = AddCall("C1", "D1");
            Assert.That(CardOf(waiting), Is.Null, "the stamp still holds the slot (D52)");
            Invoke(first, "FinishCard");
            CallCardView arrived = CardOf(waiting);
            Assert.That(arrived, Is.Not.Null, "the waiting call shows once the stamp leaves");
            Assert.That(arrived.IsArriving, Is.True, "and slides in then");
        }

        [Test]
        public void EmergenciesShowNoNewOrderStampOverTheirBadge()
        {
            Call normal = AddCall("A1", "B2");
            Call emergency = AddCall("C1", "D2", emergency: true, response: 30, penalty: 100);
            Assert.That(CardOf(normal).IsStampShowing, Is.True, "\"New order!\" on a normal call");
            Assert.That(CardOf(emergency).IsStampShowing, Is.False, "the badge and ring say it already (V13)");
            var shown = new List<CallCardView>();
            CardOf(normal).StampShown += shown.Add;
            Invoke(CardOf(normal), "Update");
            Invoke(CardOf(normal), "Update");
            Assert.That(shown, Is.EqualTo(new[] { CardOf(normal) }), "StampShown fires once, on the pop's first frame");
        }

        [Test]
        public void AWrongNumberFlashesBothJacksAndAConnectionRingsThem()
        {
            Call call = AddCall("A1", "B2");
            Invoke(queue, "OnCardClicked", CardOf(call));
            SocketView from = Socket("A1"), to = Socket("B2"), wrong = Socket("C3");
            Plug(from);
            Plug(wrong);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            var fromFlash = from.GetComponent<SocketGraphic>().WrongFlash;
            var wrongFlash = wrong.GetComponent<SocketGraphic>().WrongFlash;
            Assert.That(fromFlash != null && fromFlash.IsPlaying, Is.True, "the caller's jack flashes red");
            Assert.That(wrongFlash != null && wrongFlash.IsPlaying, Is.True, "and so does the wrong one");
            Assert.That(wrong.GetComponent<SocketGraphic>().IsShorting, Is.True, "the line shorts where the plug went in");
            Assert.That(from.GetComponent<SocketGraphic>().IsShorting, Is.False);
            Assert.That(fromFlash.raycastTarget || wrongFlash.raycastTarget, Is.False);

            Call next = AddCall("A2", "B3");
            Invoke(queue, "OnCardClicked", CardOf(next));
            // The game may start with one cord, still in the wrong circuit: send it back first.
            session.Desk.ReturnAllCords();
            bridge.Synchronize();
            Plug(Socket("A2"));
            Plug(Socket("B3"));
            Assert.That(next.State, Is.EqualTo(CallState.Connected));
            foreach (string jack in new[] { "A2", "B3" })
            {
                var ring = Socket(jack).GetComponent<SocketGraphic>().RingFlash;
                Assert.That(ring != null && ring.IsPlaying && ring.Kind == SocketFlashKind.ConnectRing, Is.True, jack);
                Assert.That(ring.raycastTarget, Is.False);
            }
        }

        [Test]
        public void AConnectionWigglesTheCordAndItHangsExactlyAsBeforeAfterwards()
        {
            Call call = AddCall("A2", "B3");
            Invoke(queue, "OnCardClicked", CardOf(call));
            SocketView from = Socket("A2"), to = Socket("B3");
            Plug(from);
            CableController cable = from.OccupyingCable;
            Assert.That(cable, Is.Not.Null, "setup: the cord in A2");
            Assert.That(cable.IsWiggling, Is.False, "one plug in: no wiggle yet");
            Plug(to);
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(cable.IsWiggling, Is.True, "the connection starts the wiggle");
            Assert.That(cable.IsJolting, Is.False, "a connection never jolts");
            cable.WireMotion.Finish();
            Assert.That(cable.WireMotion.enabled, Is.False, "a still cord has no per-frame motion");
            Assert.That(cable.Wire.WaveAmplitude, Is.Zero);
            var still = new Vector2[CableGraphic.ShapePoints];
            cable.Wire.CopyShape(still);

            // A resume closes the same circuit again: it wiggles too, with the same plugs as the still shape.
            Assert.That(cable.TryRemove(to), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            Assert.That(cable.TryInsert(to), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(cable.IsWiggling, Is.True, "a resume wiggles too");
            Assert.That(cable.WireMotion.enabled, Is.True);

            var drawn = new Vector2[CableGraphic.ShapePoints];
            float widest = 0;
            for (int i = 0; i < 8; i++)
            {
                cable.WireMotion.Advance(.03f);
                cable.Wire.CopyShape(drawn);
                for (int p = 0; p < drawn.Length; p++) widest = Mathf.Max(widest, Vector2.Distance(drawn[p], still[p]));
                Assert.That(drawn[0], Is.EqualTo(still[0]), "the plugs stay put");
                Assert.That(drawn[drawn.Length - 1], Is.EqualTo(still[still.Length - 1]));
            }
            Assert.That(widest, Is.GreaterThan(2f), "the wire's drawn shape moves while it runs");

            cable.WireMotion.Advance(1f);
            Assert.That(cable.IsWiggling, Is.False, "it dies away within the second");
            Assert.That(cable.WireMotion.enabled, Is.False);
            Assert.That(cable.Wire.WaveAmplitude, Is.Zero);
            cable.Wire.CopyShape(drawn);
            Assert.That(drawn, Is.EqualTo(still), "the wire hangs exactly as before");
        }

        [Test]
        public void AWrongNumberJoltsTheWireAndItHangsExactlyAsBeforeAfterwards()
        {
            Call call = AddCall("A1", "B2");
            Invoke(queue, "OnCardClicked", CardOf(call));
            SocketView from = Socket("A1");
            Plug(from);
            CableController cable = from.OccupyingCable;
            Assert.That(cable.IsJolting || cable.IsWiggling, Is.False, "one plug in: still");
            Plug(Socket("C3"));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Connected), "a wrong circuit stays plugged in");
            Assert.That(cable.IsJolting, Is.True, "a wrong number jolts the wire");
            Assert.That(cable.IsWiggling, Is.False, "not the connection wiggle");
            Assert.That(cable.Wire.WaveLengths, Is.GreaterThan(CableGraphic.SmoothWaveLengths), "tighter kinks than the wiggle");
            cable.WireMotion.Finish();
            Assert.That(cable.IsJolting, Is.False);
            Assert.That(cable.WireMotion.enabled, Is.False, "a still cord has no per-frame motion");
            var still = new Vector2[CableGraphic.ShapePoints];
            cable.Wire.CopyShape(still);

            float jolt = (float)typeof(CordBridge).GetField("joltAmplitude", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bridge);
            float wiggle = (float)typeof(CordBridge).GetField("wiggleAmplitude", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bridge);
            float seconds = (float)typeof(CordBridge).GetField("joltSeconds", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bridge);
            Assert.That(jolt, Is.GreaterThan(0).And.LessThan(wiggle), "smaller than the connection wave");
            Assert.That(seconds, Is.InRange(.3f, .4f));
            cable.PlayJolt(jolt, seconds);
            var drawn = new Vector2[CableGraphic.ShapePoints];
            float widest = 0, previous = cable.Wire.WaveAmplitude;
            int flips = 0;
            for (int i = 0; i < 10; i++)
            {
                cable.WireMotion.Advance(.03f);
                Assert.That(Mathf.Abs(cable.Wire.WaveAmplitude), Is.LessThanOrEqualTo(jolt));
                if (Mathf.Sign(cable.Wire.WaveAmplitude) != Mathf.Sign(previous)) flips++;
                previous = cable.Wire.WaveAmplitude;
                cable.Wire.CopyShape(drawn);
                for (int p = 0; p < drawn.Length; p++) widest = Mathf.Max(widest, Vector2.Distance(drawn[p], still[p]));
                Assert.That(drawn[0], Is.EqualTo(still[0]), "the plugs stay put");
                Assert.That(drawn[drawn.Length - 1], Is.EqualTo(still[still.Length - 1]));
            }
            Assert.That(widest, Is.GreaterThan(1.5f), "the wire's drawn shape moves while it runs");
            Assert.That(flips, Is.GreaterThanOrEqualTo(4), "it snaps from side to side, unlike the smooth wave");

            cable.WireMotion.Advance(.2f);
            Assert.That(cable.IsJolting, Is.False, "it dies out within about 0.4 s");
            Assert.That(cable.Wire.WaveAmplitude, Is.Zero);
            cable.Wire.CopyShape(drawn);
            Assert.That(drawn, Is.EqualTo(still), "the wire hangs exactly as before");
        }

        [Test]
        public void AWrongNumberShortsTheLineWithStreaksArcsAndSmokeNotStars()
        {
            Call call = AddCall("A1", "B2");
            Invoke(queue, "OnCardClicked", CardOf(call));
            Plug(Socket("A1"));
            Plug(Socket("C3"));
            SocketGraphic socket = Socket("C3").GetComponent<SocketGraphic>();
            Assert.That(socket.IsShorting, Is.True);
            Assert.That(socket.WrongFlash.IsPlaying, Is.True, "the red jack flash stays");
            SocketFlashGraphic spark = socket.ShortCircuit;
            Assert.That(spark.Kind, Is.EqualTo(SocketFlashKind.ShortCircuit));
            Assert.That(spark.raycastTarget, Is.False, "takes no clicks");
            var layer = spark.GetComponent<Canvas>();
            Assert.That(layer != null && layer.overrideSorting, Is.True, "its own layer");
            Assert.That(layer.sortingOrder, Is.EqualTo(SwitchboardUISorting.Spark).And.LessThan(SwitchboardUISorting.Book), "under the book");
            Assert.That(All<CableController>().All(c => c.WireSortingOrder < layer.sortingOrder), Is.True, "over every wire and plug");
            Assert.That(spark.StreakCount, Is.InRange(SocketFlashGraphic.MinStreaks, SocketFlashGraphic.MaxStreaks));
            Assert.That(spark.ArcCount, Is.InRange(1, 2));

            var populate = (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), spark,
                typeof(SocketFlashGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(VertexHelper) }, null));
            var mesh = new VertexHelper();
            var vertex = new UIVertex();
            Vector2 centre = spark.rectTransform.rect.center;
            float reach = SocketFlashGraphic.MaxReach * spark.JackSize;
            var heads = new List<Vector2>[spark.StreakCount];
            for (int i = 0; i < heads.Length; i++) heads[i] = new List<Vector2>();
            var firstDirections = new float[spark.StreakCount];
            bool sawSmoke = false;
            int frames = 0;
            while (spark.IsPlaying)
            {
                populate(mesh);
                Assert.That(mesh.currentVertCount, Is.GreaterThan(0), "something shows every frame");
                for (int v = 0; v < mesh.currentVertCount; v++)
                {
                    mesh.PopulateUIVertex(ref vertex, v);
                    Assert.That(Vector2.Distance(vertex.position, centre), Is.LessThanOrEqualTo(reach), "local to the jack");
                    Color32 c = vertex.color;
                    if (spark.Age > .45f && c.a > 20 && c.r < 100 && c.g < 100 && c.b < 100 && vertex.position.y > centre.y) sawSmoke = true;
                }
                for (int i = 0; i < spark.StreakCount; i++)
                {
                    if (!spark.TryGetStreak(i, out Vector2 tail, out Vector2 head, out float width)) continue;
                    Assert.That(Vector2.Distance(tail, head), Is.GreaterThanOrEqualTo(3 * width), "a thin streak, not a star");
                    if (heads[i].Count == 0) firstDirections[i] = Mathf.Atan2(head.y, head.x) * Mathf.Rad2Deg;
                    heads[i].Add(head);
                }
                // Each streak burns 0.28-0.4 s after a start delay of up to 0.08 s, so the last is out by 0.48 s.
                if (spark.Age > .5f)
                    for (int i = 0; i < spark.StreakCount; i++)
                        Assert.That(spark.TryGetStreak(i, out _, out _, out _), Is.False, "the streaks burn out in about 0.3-0.4 s");
                if (spark.Age > SocketFlashGraphic.ArcSeconds) Assert.That(spark.ArcCount, Is.InRange(1, 2));
                spark.Advance(.02f);
                frames++;
            }
            Assert.That(frames, Is.InRange(28, 32), "about 0.6 s with the smoke");
            Assert.That(sawSmoke, Is.True, "dark smoke rises late in the effect");
            Assert.That(spark.gameObject.activeSelf, Is.False);

            // They fly out and fall: every streak's path bends downwards (one shot straight down can't bend further).
            foreach (List<Vector2> path in heads.Where(h => h.Count >= 6))
            {
                Vector2 early = (path[1] - path[0]).normalized, late = (path[path.Count - 1] - path[path.Count - 2]).normalized;
                Assert.That(late.y, Is.LessThan(early.y - (early.y > -.9f ? .05f : -.001f)));
            }
            // Uneven: the gaps between the directions differ, so no symmetric burst.
            float[] sorted = firstDirections.OrderBy(a => a).ToArray();
            var gaps = new List<float>();
            for (int i = 0; i < sorted.Length; i++) gaps.Add(i + 1 < sorted.Length ? sorted[i + 1] - sorted[i] : sorted[0] + 360 - sorted[i]);
            Assert.That(gaps.Max() - gaps.Min(), Is.GreaterThan(5f), "uneven directions");

            // The mesh is built into reused buffers: a frame allocates nothing. The helper's own lists are grown first,
            // since a new burst can draw more than the last.
            for (int i = 0; i < 3000; i++) mesh.AddVert(Vector3.zero, Color.clear, Vector4.zero);
            for (int i = 0; i < 3000; i++) mesh.AddTriangle(0, 0, 0);
            socket.FlashWrongNumber(true);
            spark.Advance(.1f);
            populate(mesh);
            Assert.That(() => { for (int i = 0; i < 5; i++) { spark.Advance(.01f); populate(mesh); } },
                UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TheLampIsTheBoardsOwnBulbTintedRedAndItsGlowFollowsTheFlash()
        {
            var lamp = All<EmergencyLampView>().Single();
            Canvas board = Socket("A1").GetComponentInParent<Canvas>().rootCanvas;
            SwitchboardLampView[] numbers = All<SwitchboardLampView>().Where(l => l.GetComponentInParent<Canvas>().rootCanvas == board).ToArray();
            Assert.That(numbers.Select(n => n.BulbOff).Distinct().Count(), Is.EqualTo(1), "setup: the number lamps share one bulb");
            Assert.That(lamp.Bulb.sprite, Is.SameAs(numbers[0].BulbOff), "the board's unlit bulb art");
            Assert.That(lamp.LitBulb.sprite, Is.SameAs(numbers[0].BulbOn), "the board's lit bulb art");
            StringAssert.EndsWith("bulb-02.png", AssetDatabase.GetAssetPath(lamp.Bulb.sprite));
            StringAssert.EndsWith("bulb-01.png", AssetDatabase.GetAssetPath(lamp.LitBulb.sprite));
            Assert.That(lamp.GetComponentsInChildren<Image>(true).Select(i => i.sprite).Distinct(),
                Is.EquivalentTo(new[] { lamp.Bulb.sprite, lamp.LitBulb.sprite }), "no other art: no bezel");

            // Tinted red: muted, not pure red, darker unlit than lit.
            Color dark = lamp.Bulb.color, lit = lamp.LitBulb.color;
            foreach (Color tint in new[] { dark, lit })
            {
                Assert.That(tint.r, Is.GreaterThan(tint.g * 2).And.GreaterThan(tint.b * 2), "red");
                Assert.That(tint.g + tint.b, Is.GreaterThan(.2f), "a warm, muted red, not #FF0000");
            }
            Assert.That(Luma(dark), Is.LessThan(Luma(lit)), "dull while unlit, and the unlit art is darker still");
            Assert.That(lamp.InnerGlow.rectTransform.rect.width, Is.LessThan(lamp.LitBulb.rectTransform.rect.width * .75f),
                "lit from inside: the inner light is brightest in the middle");
            Assert.That(lamp.Glow.rectTransform.rect.width, Is.GreaterThan(lamp.Bulb.rectTransform.rect.width * 1.2f)
                .And.LessThan(lamp.Bulb.rectTransform.rect.width * 1.8f), "a soft glow on the frame, not a big bloom");
            int Order(Component part) => part.transform.GetSiblingIndex();
            Assert.That(Order(lamp.Glow), Is.LessThan(Order(lamp.Bulb)));
            Assert.That(Order(lamp.Bulb), Is.LessThan(Order(lamp.LitBulb)), "the lit bulb fades in over the dark one");
            Assert.That(Order(lamp.LitBulb), Is.LessThan(Order(lamp.InnerGlow)));
            Assert.That(Order(lamp.InnerGlow), Is.LessThan(Order(lamp.Sign)), "the plate is drawn last: the lamp's light never tints it");

            Graphic[] lights = { lamp.LitBulb, lamp.InnerGlow, lamp.Glow, lamp.SignGlow, lamp.SignWash };
            lamp.Tick(.1f);
            Assert.That(lamp.IsWarning, Is.False);
            Assert.That(lamp.Bulb.canvasRenderer.GetAlpha(), Is.EqualTo(1), "the dark bulb always shows");
            foreach (Graphic light in lights) Assert.That(light.canvasRenderer.GetAlpha(), Is.Zero, light.name + " is off while unlit");

            Call emergency = AddCall("C1", "D2", emergency: true, response: 30, penalty: 100);
            float lowest = 1, highest = 0;
            for (int i = 0; i < 12; i++)
            {
                lamp.Tick(.05f);
                foreach (Graphic light in lights) Assert.That(light.canvasRenderer.GetAlpha(), Is.EqualTo(lamp.Brightness), light.name);
                Assert.That(lamp.Bulb.canvasRenderer.GetAlpha(), Is.EqualTo(1));
                lowest = Mathf.Min(lowest, lamp.Brightness);
                highest = Mathf.Max(highest, lamp.Brightness);
            }
            // 12 steps of 0.05 s cover more than one 0.5 s flash, and step 5 lands on its dark point exactly.
            Assert.That(highest, Is.GreaterThan(.95f));
            Assert.That(lowest, Is.LessThan(.05f));
            float frozen = lamp.Glow.canvasRenderer.GetAlpha();
            lamp.Tick(0);
            Assert.That(lamp.Glow.canvasRenderer.GetAlpha(), Is.EqualTo(frozen), "frozen on pause");
            Assert.That(() => lamp.Tick(.01f), UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory(), "a flash frame allocates nothing");

            Assert.That(session.Calls.Take(emergency.Id), Is.True);
            Assert.That(session.Calls.Connect(emergency.Id), Is.True);
            lamp.Tick(.1f);
            foreach (Graphic light in lights) Assert.That(light.canvasRenderer.GetAlpha(), Is.Zero, light.name + " is dark once it is connected");
        }

        private static float Luma(Color c) => .2126f * c.r + .7152f * c.g + .0722f * c.b;

        // WCAG 2 relative luminance and contrast ratio (StyleGuide §5).
        private static float Luminance(Color c)
        {
            Func<float, float> linear = v => v <= .04045f ? v / 12.92f : Mathf.Pow((v + .055f) / 1.055f, 2.4f);
            return .2126f * linear(c.r) + .7152f * linear(c.g) + .0722f * linear(c.b);
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + .05f) / (Mathf.Min(la, lb) + .05f);
        }

        // A layer of colour `over` with alpha `alpha` on `under`, blended as in gamma or in linear space.
        private static Color Over(Color under, Color over, float alpha, bool linear) => linear
            ? Color.Lerp(under.linear, over.linear, alpha).gamma
            : Color.Lerp(under, over, alpha);

        private static List<UIVertex> MeshOf(Graphic graphic)
        {
            var helper = new VertexHelper();
            graphic.GetType().GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(VertexHelper) }, null).Invoke(graphic, new object[] { helper });
            var vertices = new List<UIVertex>();
            helper.GetUIVertexStream(vertices);
            helper.Dispose();
            return vertices;
        }

        private static Color Hsv(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return new Color(h, s, v);
        }

        private static float HueDegreesApart(float a, float b)
        {
            float apart = Mathf.Abs(a - b) * 360;
            return Mathf.Min(apart, 360 - apart);
        }

        private const string SignFontPath = "Assets/Fonts/DellaRespira/Della Respira SDF Static.asset";

        [Test]
        public void TheSignIsABrassPlateSetIntoTheFrameWithEngravedLettersAndRedLightWhileAnEmergencyWaits()
        {
            var lamp = All<EmergencyLampView>().Single();
            Assert.That(lamp.Sign, Is.Not.Null, "a plate under the lamp");
            Assert.That(lamp.Sign.GetComponentsInChildren<Image>(true), Is.Empty, "drawn, no sprite");
            LabelStripGraphic plate = lamp.SignPlate;
            Assert.That(plate.Part, Is.EqualTo(LabelStripGraphic.Layer.Plate));
            Assert.That(lamp.SignWash.Part, Is.EqualTo(LabelStripGraphic.Layer.Wash));
            Assert.That(lamp.SignGlow.Part, Is.EqualTo(LabelStripGraphic.Layer.Halo));
            foreach (LabelStripGraphic layer in new[] { plate, lamp.SignWash, lamp.SignGlow })
                Assert.That(layer.rectTransform.rect, Is.EqualTo(lamp.Sign.rect), layer.name + " lines up with the plate's box");
            Assert.That(plate.Mounting, Is.EqualTo(LabelStripGraphic.Mount.Recessed), "set into the frame, not stuck on it");

            // Matte brass, lit from above, in a thin darker brass rim.
            Color top = plate.FaceTop, bottom = plate.FaceBottom, rim = plate.Rim;
            Assert.That(top, Is.EqualTo((Color)LabelStripGraphic.BrassTop));
            Assert.That(bottom, Is.EqualTo((Color)LabelStripGraphic.BrassBottom));
            Assert.That(rim, Is.EqualTo((Color)LabelStripGraphic.BrassRim));
            foreach (Color brass in new[] { top, bottom, rim })
                Assert.That(brass.r > brass.g && brass.g > brass.b * 1.5f && brass.r > brass.b * 2, Is.True, "brass: " + brass);
            Assert.That(Luma(top), Is.GreaterThan(Luma(bottom)), "lighter on top");
            Assert.That(Luminance(top), Is.LessThan(.45f), "a mid brass, far from the old light ivory");
            Assert.That(Luma(rim), Is.LessThan(Luma(bottom) * .75f), "the rim is a darker brass");
            float height = lamp.Sign.rect.height;
            Assert.That(plate.RimWidth, Is.GreaterThan(0).And.LessThan(height * .08f), "thin");

            List<UIVertex> mesh = MeshOf(plate);
            Rect box = lamp.Sign.rect, face = plate.Face;
            Func<UIVertex, Color32, bool> coloured = (v, c) => v.color.r == c.r && v.color.g == c.g && v.color.b == c.b;
            // Recessed: everything lies on the plate, nothing falls on the wood.
            Assert.That(plate.DrawnRect, Is.EqualTo(box), "no drop shadow");
            Assert.That(mesh.All(v => v.position.x >= box.xMin - 1e-3f && v.position.x <= box.xMax + 1e-3f &&
                v.position.y >= box.yMin - 1e-3f && v.position.y <= box.yMax + 1e-3f), Is.True, "nothing drawn outside the plate");
            Assert.That(mesh.Any(v => coloured(v, LabelStripGraphic.BrassRim) && v.color.a == 255), Is.True, "the rim is drawn");
            // The face is shaded from bottom to top of the box: a vertex's colour is its gradient's colour at its height.
            Assert.That(mesh.Any(v =>
            {
                Color want = Color.Lerp(bottom, top, Mathf.InverseLerp(box.yMin, box.yMax, v.position.y));
                Color got = v.color;
                return v.position.y > box.center.y && Mathf.Abs(got.r - want.r) + Mathf.Abs(got.g - want.g) + Mathf.Abs(got.b - want.b) < .01f && got.a > .99f;
            }), Is.True, "the face is drawn");
            // A soft inner shadow under the top edge: dark and low alpha at the face's top, fading out downwards.
            Color32 shade = LabelStripGraphic.InnerShadow;
            Assert.That(shade.a, Is.LessThan(160));
            Assert.That(Luma(shade), Is.LessThan(.15f));
            List<UIVertex> shadow = mesh.Where(v => coloured(v, shade)).ToList();
            Assert.That(shadow.Where(v => v.color.a == shade.a).All(v => Mathf.Approximately(v.position.y, face.yMax)) &&
                shadow.Any(v => v.color.a == shade.a), Is.True, "the inner shadow is darkest at the face's top edge");
            Assert.That(shadow.Where(v => v.color.a == 0).All(v => Mathf.Approximately(v.position.y, face.yMax - plate.InnerShadowDepth)) &&
                shadow.Any(v => v.color.a == 0), Is.True, "and fades out downwards");
            Assert.That(plate.InnerShadowDepth, Is.LessThan(height * .2f), "a soft edge, not a dark band");
            // A faint light line along the bottom edge.
            Color32 light = LabelStripGraphic.InnerLight;
            List<UIVertex> lit = mesh.Where(v => coloured(v, light)).ToList();
            Assert.That(lit, Is.Not.Empty, "the bottom edge catches a little light");
            Assert.That(lit.All(v => v.position.y <= box.yMin + height * .1f), Is.True, "only along the bottom");
            Assert.That(light.a, Is.LessThanOrEqualTo(51), "faint");
            // Matte: no light vertex but that faint line, and nothing lighter than the face's top.
            Assert.That(mesh.Where(v => v.color.a > 0 && v.color.a < 255 && !coloured(v, light)).All(v => Luma(v.color) < .2f), Is.True,
                "no gloss, no light glints");
            Assert.That(mesh.Where(v => v.color.a == 255).All(v => Luma(v.color) <= Luma(top) + .001f), Is.True, "no hard highlight");

            // The lettering: Della Respira capitals, engraved: dark letters with a faint light edge just below them.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SignFontPath);
            TMP_Text[] labels = lamp.Sign.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text edge = lamp.SignEdgeLabel, quiet = lamp.SignLabel, bright = lamp.SignLitLabel;
            Assert.That(labels, Is.EquivalentTo(new[] { edge, quiet, bright }), "the light edge, the quiet and the lit letters");
            foreach (TMP_Text label in labels)
            {
                Assert.That(label.text, Is.EqualTo("EMERGENCY"));
                Assert.That(label.font, Is.SameAs(font), "Della Respira, its static atlas");
                Assert.That(label.font.HasCharacters(label.text), Is.True);
                Assert.That(label.fontSize, Is.EqualTo(quiet.fontSize), "one size");
                Assert.That(label.fontSharedMaterial, Is.SameAs(lamp.SignMaterial), "one thickened copy of the font's material");
                Assert.That(label.raycastTarget, Is.False);
            }
            // Thin strokes at this size never reach their colour, so the lettering's own material copy thickens them a
            // little; the font's shared material stays as it is.
            Material letterMaterial = lamp.SignMaterial;
            Assert.That(letterMaterial, Is.Not.Null.And.Not.SameAs(font.material));
            Assert.That(AssetDatabase.Contains(letterMaterial), Is.False, "made at run time, not an asset");
            Assert.That(letterMaterial.shader, Is.SameAs(font.material.shader));
            Assert.That(letterMaterial.mainTexture, Is.SameAs(font.material.mainTexture), "the font's own atlas");
            Assert.That(letterMaterial.GetFloat(ShaderUtilities.ID_FaceDilate), Is.EqualTo(EmergencyLampView.SignLetterWeight));
            Assert.That(EmergencyLampView.SignLetterWeight, Is.GreaterThan(0).And.LessThanOrEqualTo(.4f), "a little heavier, not bold");
            Assert.That(font.material.GetFloat(ShaderUtilities.ID_FaceDilate), Is.Zero, "the shared font material is untouched");
            Color edgeColour = edge.color;
            Assert.That(edgeColour, Is.EqualTo((Color)EmergencyLampView.SignEngraveEdge));
            Assert.That(Luma(edgeColour), Is.GreaterThan(.85f), "a light edge");
            Assert.That(edgeColour.r >= edgeColour.g && edgeColour.g > edgeColour.b, Is.True, "warm");
            Assert.That(edgeColour.a, Is.InRange(.2f, .5f), "at low alpha");
            Vector2 down = quiet.rectTransform.anchoredPosition - edge.rectTransform.anchoredPosition;
            Assert.That(down.x, Is.Zero);
            Assert.That(down.y, Is.EqualTo(.6f * lamp.BulbSize / 44).Within(1e-4f), "just below the letters: 0.6 units at 44");
            Assert.That(bright.rectTransform.anchoredPosition, Is.EqualTo(quiet.rectTransform.anchoredPosition));

            // One dark brown-red family, quiet and lit: no switch to ivory or white.
            Assert.That(quiet.color, Is.EqualTo(lamp.Style.SignQuiet));
            Assert.That(bright.color, Is.EqualTo(lamp.Style.SignLit));
            Color quietHsv = Hsv(quiet.color), brightHsv = Hsv(bright.color);
            Assert.That(HueDegreesApart(quietHsv.r, brightHsv.r), Is.LessThan(15), "one hue family in both states");
            foreach (Color hsv in new[] { quietHsv, brightHsv })
            {
                Assert.That(HueDegreesApart(hsv.r, 0), Is.LessThan(15), "a red");
                Assert.That(hsv.g, Is.GreaterThan(.6f), "not a grey or a white");
                Assert.That(hsv.b, Is.LessThan(.35f), "dark, cut into the metal");
            }
            int Order(Component part) => part.transform.GetSiblingIndex();
            Assert.That(Order(lamp.SignGlow), Is.LessThan(Order(plate)), "the glow is round the plate, under it");
            Assert.That(Order(plate), Is.LessThan(Order(lamp.SignWash)), "the red wash lies on the face");
            Assert.That(Order(lamp.SignWash), Is.LessThan(Order(edge)), "and under the lettering");
            Assert.That(Order(edge), Is.LessThan(Order(quiet)), "the light edge peeks out under the letters");
            Assert.That(Order(quiet), Is.LessThan(Order(bright)), "the lit letters fade in over the quiet ones");

            lamp.Tick(.1f);
            Assert.That(lamp.IsWarning, Is.False);
            Assert.That(lamp.SignBrightness, Is.Zero, "quiet while nothing waits");
            Assert.That(quiet.canvasRenderer.GetAlpha(), Is.EqualTo(1), "the engraved letters always show");
            Assert.That(edge.canvasRenderer.GetAlpha(), Is.EqualTo(1), "and their light edge");
            Assert.That(plate.canvasRenderer.GetAlpha(), Is.EqualTo(1), "and so does the plate");
            Assert.That(bright.canvasRenderer.GetAlpha(), Is.Zero);
            Assert.That(lamp.SignWash.canvasRenderer.GetAlpha(), Is.Zero, "no red light while quiet");

            AddCall("A1", "B2");
            lamp.Tick(.1f);
            Assert.That(lamp.SignBrightness, Is.Zero, "a normal call doesn't light it");

            Call emergency = AddCall("C1", "D2", emergency: true, response: 30, penalty: 100);
            float lowest = 1, highest = 0;
            for (int i = 0; i < 12; i++)
            {
                lamp.Tick(.05f);
                Assert.That(lamp.SignBrightness, Is.EqualTo(EmergencyLampView.SignLitFloor + (1 - EmergencyLampView.SignLitFloor) * lamp.Brightness).Within(1e-5f), "in step with the lamp");
                Assert.That(bright.canvasRenderer.GetAlpha(), Is.EqualTo(lamp.SignBrightness));
                Assert.That(lamp.SignWash.canvasRenderer.GetAlpha(), Is.EqualTo(lamp.Brightness), "the red light on the plate flashes with the bulb");
                Assert.That(lamp.SignGlow.canvasRenderer.GetAlpha(), Is.EqualTo(lamp.Brightness), "and so does the light round it");
                Assert.That(edge.canvasRenderer.GetAlpha(), Is.EqualTo(1));
                lowest = Mathf.Min(lowest, lamp.SignBrightness);
                highest = Mathf.Max(highest, lamp.SignBrightness);
            }
            Assert.That(highest, Is.GreaterThan(.9f), "lit");
            Assert.That(highest - lowest, Is.GreaterThan(.4f), "and flashing");
            Assert.That(lowest, Is.EqualTo(EmergencyLampView.SignLitFloor).Within(1e-4f), "step 5 lands on the flash's dim end");
            Assert.That(EmergencyLampView.SignWashAlpha, Is.EqualTo(.14f).Within(.01f), "a red wash of about 14% at full flash");
            Assert.That(() => lamp.Tick(.01f), UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory(), "a flash frame allocates nothing");

            float paused = lamp.SignBrightness;
            lamp.Tick(0);
            lamp.Tick(0);
            Assert.That(lamp.SignBrightness, Is.EqualTo(paused), "frozen on pause");

            Assert.That(session.Calls.Take(emergency.Id), Is.True);
            Assert.That(session.Calls.Connect(emergency.Id), Is.True);
            lamp.Tick(.1f);
            Assert.That(lamp.SignBrightness, Is.Zero, "quiet again once it is connected");
            Assert.That(bright.canvasRenderer.GetAlpha(), Is.Zero);
            Assert.That(lamp.SignWash.canvasRenderer.GetAlpha(), Is.Zero);
        }

        // The frame art behind the board's top frame, read from its file (the import isn't readable).
        private Texture2D FrameArt(out RectTransform panel)
        {
            Image frame = All<Image>().Single(i => i.name == "SwitchboardPanel" && i.sprite != null && i.sprite.name == "board_matte");
            panel = frame.rectTransform;
            var art = new Texture2D(2, 2);
            art.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(frame.sprite)));
            return art;
        }

        /// <summary>The frame's colour round the lamp, measured from the frame art (board_matte.png).</summary>
        private static readonly Color32 FrameColour = new Color32(133, 92, 66, 255);

        private static Color32 ArtAt(Texture2D art, RectTransform panel, Vector2 world)
        {
            Rect area = WorldRect(panel);
            int x = Mathf.Clamp(Mathf.FloorToInt((world.x - area.xMin) / area.width * art.width), 0, art.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((world.y - area.yMin) / area.height * art.height), 0, art.height - 1);
            return art.GetPixel(x, y);
        }

        private static bool IsPlainFrame(Color32 c, Color32 frame) =>
            Mathf.Abs(c.r - frame.r) < 24 && Mathf.Abs(c.g - frame.g) < 24 && Mathf.Abs(c.b - frame.b) < 24 && c.a > 200;

        // The sign's letters as drawn: the glyphs themselves, not the font's line box.
        private static Rect LetteringRect(EmergencyLampView lamp)
        {
            Rect all = Rect.zero;
            bool first = true;
            foreach (TMP_Text label in lamp.Sign.GetComponentsInChildren<TMP_Text>(true))
            {
                Rect glyphs = GlyphRect(label);
                all = first ? glyphs : Rect.MinMaxRect(Mathf.Min(all.xMin, glyphs.xMin), Mathf.Min(all.yMin, glyphs.yMin),
                    Mathf.Max(all.xMax, glyphs.xMax), Mathf.Max(all.yMax, glyphs.yMax));
                first = false;
            }
            return all;
        }

        // TMP grows each glyph's box by its material's padding (the room a dilated face may need); take it off, so this
        // is the glyph's own outline.
        private static Rect GlyphRect(TMP_Text label)
        {
            label.ForceMeshUpdate();
            float padding = ShaderUtilities.GetPadding(label.fontSharedMaterial, label.extraPadding, false) * label.fontSize /
                label.font.faceInfo.pointSize;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < label.textInfo.characterCount; i++)
            {
                TMP_CharacterInfo character = label.textInfo.characterInfo[i];
                if (!character.isVisible) continue;
                xMin = Mathf.Min(xMin, character.bottomLeft.x);
                yMin = Mathf.Min(yMin, character.bottomLeft.y);
                xMax = Mathf.Max(xMax, character.topRight.x);
                yMax = Mathf.Max(yMax, character.topRight.y);
            }
            Vector3 min = label.transform.TransformPoint(new Vector3(xMin + padding, yMin + padding)),
                max = label.transform.TransformPoint(new Vector3(xMax - padding, yMax - padding));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect WorldRectOf(Transform space, Rect local)
        {
            Vector3 min = space.TransformPoint(local.min), max = space.TransformPoint(local.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [Test]
        public void TheBulbIsHalfItsOldSizeFromOneSerializedValueAndItsLightsScaleWithIt()
        {
            var installer = All<GameInstaller>().Single();
            float serialized = new SerializedObject(installer).FindProperty("emergencyLampSize").floatValue;
            Assert.That(serialized, Is.EqualTo(44).And.EqualTo(88f / 2), "half of fix pass 4's 88 units");
            Assert.That(EmergencyLampView.DefaultBulbSize, Is.EqualTo(serialized));
            var lamp = All<EmergencyLampView>().Single();
            Assert.That(lamp.BulbSize, Is.EqualTo(serialized), "the game passes its serialized value");

            // Measured on the board: the number lamps' bulbs are 96 board units.
            Canvas board = Socket("A1").GetComponentInParent<Canvas>().rootCanvas;
            SwitchboardLampView number = All<SwitchboardLampView>().First(l => l.GetComponentInParent<Canvas>().rootCanvas == board);
            RectTransform numberBulb = number.GetComponentsInChildren<Image>(true).First(i => i.sprite == number.BulbOff).rectTransform;
            float unit = WorldRect(numberBulb).width / 96f;
            Assert.That(WorldRect(lamp.Bulb.rectTransform).width / unit, Is.EqualTo(44).Within(.01f));
            Assert.That(WorldRect(lamp.LitBulb.rectTransform).width / unit, Is.EqualTo(44).Within(.01f));
            Assert.That(WorldRect(lamp.Glow.rectTransform).width / unit, Is.EqualTo(44 * EmergencyLampView.GlowScale).Within(.01f), "the glow scales with it");
            Assert.That(WorldRect(lamp.InnerGlow.rectTransform).width / unit, Is.LessThan(44 * .75f).And.GreaterThan(44 * .4f), "and so does the inner light");
            Assert.That(WorldRect((RectTransform)lamp.transform).width / unit, Is.EqualTo(44).Within(.01f), "the lamp's own box is the bulb");
        }

        [Test]
        public void ThePlateAndItsLetteringAreMeasuredInBulbsSoTheyScaleWithTheBulb()
        {
            var installer = All<GameInstaller>().Single();
            SerializedProperty serialized = new SerializedObject(installer).FindProperty("emergencySign");
            var lamp = All<EmergencyLampView>().Single();
            EmergencyLampView.SignProportions proportions = lamp.Proportions;
            Assert.That(proportions.Width, Is.EqualTo(serialized.FindPropertyRelative("width").floatValue), "the game passes its serialized ratios");
            Assert.That(proportions.Height, Is.EqualTo(serialized.FindPropertyRelative("height").floatValue));
            Assert.That(proportions.CapHeight, Is.EqualTo(serialized.FindPropertyRelative("capHeight").floatValue));
            Assert.That(proportions.Gap, Is.EqualTo(serialized.FindPropertyRelative("gap").floatValue));
            Assert.That(proportions.Width, Is.InRange(2f, 2.6f), "a plate 2 to 2.6 bulbs wide");
            Assert.That(proportions.CapHeight, Is.InRange(.22f, .36f), "capitals about a third of the bulb");
            Assert.That(proportions.Height, Is.GreaterThan(proportions.CapHeight * 1.6f), "room round the letters");

            Canvas board = Socket("A1").GetComponentInParent<Canvas>().rootCanvas;
            SwitchboardLampView[] boardLamps = All<SwitchboardLampView>().Where(l => l.GetComponentInParent<Canvas>().rootCanvas == board).ToArray();
            RectTransform numberBulb = boardLamps[0].GetComponentsInChildren<Image>(true).First(i => i.sprite == boardLamps[0].BulbOff).rectTransform;
            float unit = WorldRect(numberBulb).width / 96f;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SignFontPath);
            EmergencyLampView other = EmergencyLampView.Create(boardLamps, session.Calls, lamp.Style, 30, proportions);
            try
            {
                foreach (EmergencyLampView each in new[] { lamp, other })
                {
                    float size = each.BulbSize;
                    Invoke(each, "Follow");
                    Canvas.ForceUpdateCanvases();
                    Rect sign = WorldRect(each.Sign), bulb = WorldRect(each.Bulb.rectTransform);
                    Assert.That(sign.width / unit, Is.EqualTo(proportions.Width * size).Within(.01f), size + ": the plate's width");
                    Assert.That(sign.height / unit, Is.EqualTo(proportions.Height * size).Within(.01f), size + ": its height");
                    Assert.That(sign.center.x, Is.EqualTo(bulb.center.x).Within(.01f * unit), size + ": centred under the bulb");
                    Assert.That((bulb.yMin - sign.yMax) / unit, Is.EqualTo(proportions.Gap * size).Within(.01f), size + ": a gap under it");
                    float engraved = (GlyphRect(each.SignLabel).yMin - GlyphRect(each.SignEdgeLabel).yMin) / unit;
                    Assert.That(engraved, Is.EqualTo(EmergencyLampView.SignEngraveDepth * size).Within(.01f), size + ": the light edge's offset follows the bulb");
                    foreach (TMP_Text label in new[] { each.SignEdgeLabel, each.SignLabel, each.SignLitLabel })
                    {
                        label.ForceMeshUpdate();
                        float wanted = EmergencyLampView.FontSizeFor(font, proportions.CapHeight * size);
                        Assert.That(label.fontSize, Is.EqualTo(wanted).Within(1e-3f), size + ": the font size for the capitals' height, not shrunk");
                        // "EMERGENCY" has no descender; only the round C and G overshoot a little.
                        float drawn = GlyphRect(label).height / unit;
                        Assert.That(drawn, Is.EqualTo(proportions.CapHeight * size).Within(proportions.CapHeight * size * .1f), size + ": cap height");
                    }
                    Rect face = WorldRectOf(each.SignPlate.transform, each.SignPlate.Face);
                    Rect letters = LetteringRect(each);
                    Assert.That(face.Contains(letters.min) && face.Contains(letters.max), Is.True, size + ": the letters sit on the face");
                    Assert.That(letters.center.y, Is.EqualTo(face.center.y).Within(.08f * face.height), size + ": in its middle");
                    Assert.That(letters.width, Is.GreaterThan(face.width * .75f), size + ": and fill most of it");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(other.gameObject); }
        }

        [Test]
        public void TheSignsLettersReadAtFourAndAHalfToOneOnTheDrawnPlateQuietAndAtEveryPointOfTheFlash()
        {
            var lamp = All<EmergencyLampView>().Single();
            LabelStripGraphic plate = lamp.SignPlate;
            Color quiet = lamp.SignLabel.color, bright = lamp.SignLitLabel.color, wash = lamp.SignWash.color;
            Color washColour = new Color(wash.r, wash.g, wash.b, 1);
            Assert.That(plate.color, Is.EqualTo(Color.white), "setup: the plate draws its own colours");
            // The face runs from the darker bottom to the lighter top; a letter's contrast lies between the two, and the
            // darkest brass is the plate's own bottom colour (the face starts a rim's width above it).
            Color[] faces = { plate.FaceBottom, plate.FaceTop };
            // The inner shadow darkens only the band under the top edge: it must end above the letters.
            Rect letters = LetteringRect(lamp);
            Rect shadowed = Rect.MinMaxRect(plate.Face.xMin, plate.Face.yMax - plate.InnerShadowDepth, plate.Face.xMax, plate.Face.yMax);
            Assert.That(letters.yMax, Is.LessThan(WorldRectOf(plate.transform, shadowed).yMin), "the inner shadow lies above the letters");
            Action<float, float, string> readable = (washAlpha, brightAlpha, when) =>
            {
                foreach (bool linear in new[] { false, true })
                foreach (Color face in faces)
                {
                    Color under = Over(face, washColour, washAlpha, linear);
                    Color ink = Over(quiet, bright, brightAlpha, linear);
                    Assert.That(Contrast(ink, under), Is.GreaterThanOrEqualTo(4.5f),
                        when + (linear ? " (linear)" : " (gamma)") + ": " + ink + " on " + under);
                }
            };

            lamp.Tick(.1f);
            Assert.That(lamp.SignWash.canvasRenderer.GetAlpha(), Is.Zero, "setup: no wash while quiet");
            readable(0, lamp.SignLitLabel.canvasRenderer.GetAlpha(), "quiet");

            // The real flash, as drawn: the wash and the lit letters at each step, the dim end included (step 5 lands
            // on it exactly, so nothing depends on chance).
            Call emergency = AddCall("C1", "D2", emergency: true, response: 30, penalty: 100);
            float dimmest = 1, fullest = 0;
            for (int i = 0; i < 12; i++)
            {
                lamp.Tick(.05f);
                float washAlpha = wash.a * lamp.SignWash.canvasRenderer.GetAlpha();
                float brightAlpha = lamp.SignLitLabel.canvasRenderer.GetAlpha();
                readable(washAlpha, brightAlpha, "flash step " + i);
                dimmest = Mathf.Min(dimmest, lamp.SignBrightness);
                fullest = Mathf.Max(fullest, lamp.Brightness);
            }
            Assert.That(dimmest, Is.EqualTo(EmergencyLampView.SignLitFloor).Within(1e-4f), "the dim end was checked");
            Assert.That(fullest, Is.GreaterThan(.99f), "and the full flash, with the full red wash");
            // And every point between, by the flash's own rule.
            for (int step = 0; step <= 20; step++)
            {
                float brightness = step / 20f;
                readable(wash.a * brightness, EmergencyLampView.SignLitFloor + (1 - EmergencyLampView.SignLitFloor) * brightness,
                    "brightness " + brightness);
            }
            Assert.That(wash.a, Is.GreaterThan(.05f), "the red light shows on the plate");
            Assert.That(session.Calls.Take(emergency.Id), Is.True);
        }

        [Test]
        public void TheLampItsGlowAndTheSignCoverNothingAtAnyBoardSizeAndTakeNoClicks()
        {
            var lamp = All<EmergencyLampView>().Single();
            Assert.That(lamp.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True, "takes no clicks");
            Canvas board = Socket("A1").GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(lamp.Sign.GetComponentInParent<Canvas>().rootCanvas, Is.SameAs(board), "on the board, under the book");
            Rect play = WorldRect((RectTransform)board.transform);
            SocketView[] sockets = All<SocketView>();
            SwitchboardLampView[] labels = All<SwitchboardLampView>().Where(l => l.GetComponentInParent<Canvas>().rootCanvas == board).ToArray();
            Assert.That(labels.Length, Is.GreaterThanOrEqualTo(12), "setup: the row and column lamps");
            SpeechBubbleGraphic[] bubbles = All<SpeechBubbleGraphic>();
            Assert.That(bubbles.Length, Is.GreaterThan(0), "setup: the speech bubble");
            RectTransform numberBulb = labels[0].GetComponentsInChildren<Image>(true).First(i => i.sprite == labels[0].BulbOff).rectTransform;
            float unit = WorldRect(numberBulb).width / 96f;
            // The bulb art's visible disc is 105 of its 121 pixels.
            float numberRadius = WorldRect(numberBulb).width * 105f / 121f * .5f;
            Texture2D art = FrameArt(out RectTransform panel);
            try
            {
                for (int side = 3; side <= BoardSize.MaxSide; side++)
                {
                    var size = new BoardSize(side, side);
                    foreach (SocketView socket in sockets) socket.SetOpen(size.Contains(socket.Address));
                    Canvas.ForceUpdateCanvases();
                    Invoke(lamp, "Follow");
                    Rect lampArea = WorldRect((RectTransform)lamp.transform);
                    Rect sign = WorldRect(lamp.Sign);
                    Rect plate = WorldRectOf(lamp.SignPlate.transform, lamp.SignPlate.DrawnRect);
                    Rect signGlow = WorldRectOf(lamp.SignGlow.transform, lamp.SignGlow.DrawnRect);
                    Rect glowSquare = WorldRect(lamp.Glow.rectTransform);
                    Assert.That(signGlow.Contains(plate.min) && signGlow.Contains(plate.max), Is.True, side + ": the glow reaches round the plate");
                    Assert.That(signGlow.width, Is.LessThan(sign.width * 1.3f), side + ": a little round it, not a bloom");
                    foreach (Rect part in new[] { lampArea, sign, signGlow, glowSquare })
                        Assert.That(play.Contains(part.min) && part.max.x <= play.xMax && part.max.y <= play.yMax, Is.True, side + ": inside the play area");
                    Assert.That(sign.yMax, Is.LessThan(WorldRect(lamp.Bulb.rectTransform).yMin), side + ": the sign is under the bulb");

                    // The frame round the lamp is the colour the plain-frame check compares with.
                    float r = 0, g = 0, b = 0;
                    int samples = 0;
                    for (int i = 0; i <= 20; i++)
                    for (int j = 0; j <= 6; j++)
                    {
                        Color32 c = ArtAt(art, panel, new Vector2(sign.xMin + sign.width * i / 20, sign.yMin + sign.height * j / 6));
                        r += c.r; g += c.g; b += c.b;
                        samples++;
                    }
                    Assert.That(new[] { r / samples, g / samples, b / samples },
                        Is.EqualTo(new float[] { FrameColour.r, FrameColour.g, FrameColour.b }).Within(8), "the frame's colour under the plate");

                    // On the plain frame: the plate and its glow, plus 2 units, keep clear of the gold
                    // curl, the frame's lines and the jacks' panel.
                    Rect around = new Rect(signGlow.xMin - 2 * unit, signGlow.yMin - 2 * unit, signGlow.width + 4 * unit, signGlow.height + 4 * unit);
                    for (int i = 0; i <= 60; i++)
                    for (int j = 0; j <= 16; j++)
                    {
                        var at = new Vector2(around.xMin + around.width * i / 60, around.yMin + around.height * j / 16);
                        Assert.That(IsPlainFrame(ArtAt(art, panel, at), FrameColour), Is.True,
                            side + ": the sign touches the frame's art at " + at);
                    }
                    Vector2 bulbCentre = WorldRect(lamp.Bulb.rectTransform).center;
                    float bulbRadius = WorldRect(lamp.Bulb.rectTransform).width * 105f / 121f * .5f;
                    for (int i = 0; i < 24; i++)
                    for (int ring = 0; ring <= 2; ring++)
                    {
                        float angle = i * Mathf.PI / 12, reach = bulbRadius * ring / 2 + (ring == 2 ? 2 * unit : 0);
                        var at = bulbCentre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
                        Assert.That(IsPlainFrame(ArtAt(art, panel, at), FrameColour), Is.True,
                            side + ": the bulb touches the frame's art at " + at);
                    }

                    // The glow is a disc of light that fades to nothing at its edge, so the disc is what must stay clear.
                    Vector2 centre = glowSquare.center;
                    float radius = glowSquare.width * .5f;
                    Action<Rect, string> clear = (other, what) =>
                    {
                        Assert.That(lampArea.Overlaps(other), Is.False, side + ": the lamp over " + what);
                        Assert.That(signGlow.Overlaps(other), Is.False, side + ": the sign or its glow over " + what);
                        var nearest = new Vector2(Mathf.Clamp(centre.x, other.xMin, other.xMax), Mathf.Clamp(centre.y, other.yMin, other.yMax));
                        Assert.That(Vector2.Distance(nearest, centre), Is.GreaterThan(radius), side + ": the glow over " + what);
                    };
                    foreach (SocketView socket in sockets) clear(WorldRect((RectTransform)socket.transform, .8f), socket.Address);
                    foreach (CallCardView card in All<CallCardView>()) clear(WorldRect((RectTransform)card.transform), card.name);
                    foreach (SpeechBubbleGraphic bubble in bubbles) clear(WorldRect(bubble.rectTransform), "the bubble");
                    // The number lamps are round: nothing of the sign may come within 3 units of their visible discs.
                    foreach (SwitchboardLampView label in labels)
                    {
                        Vector2 lampCentre = WorldRect((RectTransform)label.transform).center;
                        var nearest = new Vector2(Mathf.Clamp(lampCentre.x, signGlow.xMin, signGlow.xMax), Mathf.Clamp(lampCentre.y, signGlow.yMin, signGlow.yMax));
                        Assert.That(Vector2.Distance(nearest, lampCentre), Is.GreaterThan(numberRadius + 3 * unit), side + ": the sign by " + label.name);
                        Assert.That(Vector2.Distance(bulbCentre, lampCentre), Is.GreaterThan(numberRadius + bulbRadius + 3 * unit), side + ": the bulb by " + label.name);
                        Rect labelRect = WorldRect((RectTransform)label.transform, .8f);
                        Assert.That(lampArea.Overlaps(labelRect), Is.False, side + ": the lamp over " + label.name);
                        nearest = new Vector2(Mathf.Clamp(centre.x, labelRect.xMin, labelRect.xMax), Mathf.Clamp(centre.y, labelRect.yMin, labelRect.yMax));
                        Assert.That(Vector2.Distance(nearest, centre), Is.GreaterThan(radius), side + ": the glow over " + label.name);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(art); }
        }

        private static string Look(SocketGraphic socket)
        {
            Transform halo = socket.transform.Find("Socket halo");
            string haloLook = halo == null ? "no halo" : halo.gameObject.activeSelf + " " + halo.GetComponent<Graphic>().color;
            return $"{socket.IsHighlighted}/{socket.IsDangerShowing}/{socket.IsCallConnected}/{socket.IsDimmed}/" +
                $"{socket.Lamp}/{socket.IsLampShowing}/{socket.color}/{haloLook}";
        }

        [Test]
        public void TheWrongNumberFlashHandsEachJackBackAsItWas()
        {
            SocketGraphic blinking = Socket("A1").GetComponent<SocketGraphic>();
            SocketGraphic marked = Socket("A2").GetComponent<SocketGraphic>();
            SocketGraphic connected = Socket("B1").GetComponent<SocketGraphic>();
            SocketGraphic hovered = Socket("B2").GetComponent<SocketGraphic>();
            SocketGraphic plain = Socket("C1").GetComponent<SocketGraphic>();
            SocketView closed = All<SocketView>().First(s => !s.IsOpen);
            blinking.SetLamp(SocketLamp.Blinking);
            marked.SetLamp(SocketLamp.Steady);
            connected.SetCallConnected(true);
            hovered.SetCallConnected(true);
            hovered.SetHighlighted(true, blocked: true);
            var all = new[] { blinking, marked, connected, hovered, plain };
            string[] before = all.Select(Look).ToArray();
            string closedBefore = Look(closed.GetComponent<SocketGraphic>());

            foreach (SocketGraphic socket in all) socket.FlashWrongNumber(true);
            closed.FlashWrongNumber(true);
            Assert.That(all.All(s => s.WrongFlash.IsPlaying), Is.True);
            Assert.That(closed.GetComponent<SocketGraphic>().WrongFlash, Is.Null, "a closed jack never flashes");
            Assert.That(all.Select(Look).ToArray(), Is.EqualTo(before), "the flash is its own layer");
            foreach (SocketGraphic socket in all) socket.WrongFlash.Advance(.3f);
            Assert.That(all.All(s => s.WrongFlash.IsPlaying), Is.True, "about half a second");
            foreach (SocketGraphic socket in all) socket.WrongFlash.Advance(.3f);
            Assert.That(all.All(s => !s.WrongFlash.IsPlaying && !s.WrongFlash.gameObject.activeSelf), Is.True);
            foreach (SocketGraphic socket in all) socket.ShortCircuit.Advance(.7f);
            Assert.That(all.All(s => !s.IsShorting && !s.ShortCircuit.gameObject.activeSelf), Is.True, "the smoke is gone too");
            Assert.That(all.Select(Look).ToArray(), Is.EqualTo(before), "every jack looks as it did before");
            Assert.That(Look(closed.GetComponent<SocketGraphic>()), Is.EqualTo(closedBefore));
        }

        [Test]
        public void NewGraphicsTakeNoClicksAndStayUnderTheBook()
        {
            var lamp = All<EmergencyLampView>().Single();
            Assert.That(lamp.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
            Canvas board = Socket("A1").GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(lamp.GetComponentInParent<Canvas>().rootCanvas, Is.SameAs(board), "on the board's canvas");
            Assert.That(board.sortingOrder, Is.LessThan(SwitchboardUISorting.Book), "under the open book");

            // It covers no jack and no row or column label.
            Invoke(lamp, "Follow");
            Rect lampRect = WorldRect((RectTransform)lamp.transform);
            foreach (SocketView socket in All<SocketView>())
                Assert.That(lampRect.Overlaps(WorldRect((RectTransform)socket.transform, .8f)), Is.False, socket.Address);
            foreach (SwitchboardLampView label in All<SwitchboardLampView>().Where(l => l.GetComponentInParent<Canvas>().rootCanvas == board))
                Assert.That(lampRect.Overlaps(WorldRect((RectTransform)label.transform, .8f)), Is.False, label.name);

            session.Wallet.Earn(500);
            counter.FlyIncomeFrom((RectTransform)Socket("A1").transform);
            Invoke(counter, "StartGlow");
            Graphic[] counterGraphics = counter.GetComponentsInChildren<Graphic>(true);
            Assert.That(counterGraphics.Any(g => g is LampGlowGraphic), Is.True, "setup: the glow exists");
            Assert.That(counterGraphics.All(g => !g.raycastTarget), Is.True, "reels, rolling digits and the glow take no clicks");
        }

        private static Rect WorldRect(RectTransform rect, float scale = 1)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 size = (corners[2] - corners[0]) * scale;
            Vector2 centre = (corners[0] + corners[2]) * .5f;
            return new Rect(centre - size * .5f, size);
        }
    }
}
