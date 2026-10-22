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
            Assert.That(counter.Reels.All(r => r.color == new Color32(196, 40, 40, 255)), Is.True, "and flashes red");
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
        public void TheEmergencySignIsDarkUntilAnEmergencyWaitsThenFlashesWithTheLampAndFreezesOnPause()
        {
            var lamp = All<EmergencyLampView>().Single();
            Assert.That(lamp.Sign, Is.Not.Null, "a sign beside the lamp");
            TMP_Text[] labels = lamp.Sign.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(labels.Length, Is.EqualTo(2), "a dark and a lit lettering");
            foreach (TMP_Text label in labels)
            {
                Assert.That(label.text, Is.EqualTo("EMERGENCY"));
                Assert.That(label.font, Is.Not.Null);
                Assert.That(label.font.atlasPopulationMode, Is.EqualTo(TMPro.AtlasPopulationMode.Static), "a static font");
                Assert.That(label.font.HasCharacters(label.text), Is.True);
            }
            TMP_Text dark = labels.Single(l => l.name == "Sign text"), lit = labels.Single(l => l.name == "Sign lit text");

            lamp.Tick(.1f);
            Assert.That(lamp.IsWarning, Is.False);
            Assert.That(lamp.SignBrightness, Is.Zero, "unlit while nothing waits");
            Assert.That(dark.canvasRenderer.GetAlpha(), Is.EqualTo(1), "the dark lettering stays readable");
            Assert.That(lit.canvasRenderer.GetAlpha(), Is.Zero);

            Call normal = AddCall("A1", "B2");
            lamp.Tick(.1f);
            Assert.That(lamp.SignBrightness, Is.Zero, "a normal call doesn't light it");

            Call emergency = AddCall("C1", "D2", emergency: true, response: 30, penalty: 100);
            float lowest = 1, highest = 0;
            for (int i = 0; i < 12; i++)
            {
                lamp.Tick(.05f);
                Assert.That(lamp.SignBrightness, Is.EqualTo(.35f + .65f * lamp.Brightness).Within(1e-5f), "in step with the lamp");
                Assert.That(lit.canvasRenderer.GetAlpha(), Is.EqualTo(lamp.SignBrightness));
                lowest = Mathf.Min(lowest, lamp.SignBrightness);
                highest = Mathf.Max(highest, lamp.SignBrightness);
            }
            Assert.That(highest, Is.GreaterThan(.9f), "lit");
            Assert.That(highest - lowest, Is.GreaterThan(.4f), "and flashing");
            Assert.That(lowest, Is.GreaterThanOrEqualTo(.35f), "never dark while it waits");

            float paused = lamp.SignBrightness;
            lamp.Tick(0);
            lamp.Tick(0);
            Assert.That(lamp.SignBrightness, Is.EqualTo(paused), "frozen on pause");

            Assert.That(session.Calls.Take(emergency.Id), Is.True);
            Assert.That(session.Calls.Connect(emergency.Id), Is.True);
            lamp.Tick(.1f);
            Assert.That(lamp.SignBrightness, Is.Zero, "dark again once it is connected");
            Assert.That(lit.canvasRenderer.GetAlpha(), Is.Zero);
        }

        [Test]
        public void TheEmergencySignCoversNothingAtAnyBoardSizeAndTakesNoClicks()
        {
            var lamp = All<EmergencyLampView>().Single();
            Assert.That(lamp.Sign.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True, "takes no clicks");
            Canvas board = Socket("A1").GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(lamp.Sign.GetComponentInParent<Canvas>().rootCanvas, Is.SameAs(board), "on the board, under the book");
            Rect play = WorldRect((RectTransform)board.transform);
            SocketView[] sockets = All<SocketView>();
            SwitchboardLampView[] labels = All<SwitchboardLampView>().Where(l => l.GetComponentInParent<Canvas>().rootCanvas == board).ToArray();
            Assert.That(labels.Length, Is.GreaterThanOrEqualTo(12), "setup: the row and column lamps");
            for (int side = 3; side <= BoardSize.MaxSide; side++)
            {
                var size = new BoardSize(side, side);
                foreach (SocketView socket in sockets) socket.SetOpen(size.Contains(socket.Address));
                Canvas.ForceUpdateCanvases();
                Invoke(lamp, "Follow");
                Rect sign = WorldRect(lamp.Sign);
                Assert.That(play.Contains(sign.min) && play.Contains(sign.max), Is.True, side + ": inside the play area");
                Assert.That(sign.Overlaps(WorldRect((RectTransform)lamp.transform)), Is.False, side + ": beside the lamp, not over it");
                foreach (SocketView socket in sockets)
                    Assert.That(sign.Overlaps(WorldRect((RectTransform)socket.transform, .8f)), Is.False, side + ": " + socket.Address);
                foreach (SwitchboardLampView label in labels)
                    Assert.That(sign.Overlaps(WorldRect((RectTransform)label.transform, .8f)), Is.False, side + ": " + label.name);
                foreach (CallCardView card in All<CallCardView>())
                    Assert.That(sign.Overlaps(WorldRect((RectTransform)card.transform)), Is.False, side + ": " + card.name);
            }
        }

        private static string Look(SocketGraphic socket)
        {
            Transform halo = socket.transform.Find("Socket halo");
            string haloLook = halo == null ? "no halo" : halo.gameObject.activeSelf + " " + halo.GetComponent<Graphic>().color;
            return $"{socket.IsHighlighted}/{socket.IsBlockedHighlight}/{socket.IsCallConnected}/{socket.IsDimmed}/" +
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
