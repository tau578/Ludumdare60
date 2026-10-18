using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    public sealed class MergedSystemsTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
        private FirstOrderGuide guide;
        private CordBridge bridge;
        private PhoneBookView book;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static T Field<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [SetUp]
        public void SetUp()
        {
            // A preview scene keeps the user's open scene and unsaved edits intact.
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            Canvas.ForceUpdateCanvases();
            foreach (var canvas in All<Canvas>().Where(c => c.isRootCanvas))
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            book = All<PhoneBookView>().Single();
            book.SetOpen(false);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Start");
            session = installer.Session;
            Assert.That(session, Is.Not.Null);
            queue = All<CallQueueView>().Single();
            guide = All<FirstOrderGuide>().Single();
            bridge = All<CordBridge>().Single();
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private CallCardView AcceptOrder()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
            var people = config.LoadPeople(config.LoadEconomy(), null);
            var caller = people.Callers.First(c => !c.Type.EveryoneCalls);
            var recipient = people.People.First(p => p != caller.Person);
            var call = session.Calls.Add(new CallRequest(caller, recipient, 10, 30, 100));
            var card = All<CallCardView>().Single(c => c.Call == call);
            Invoke(queue, "OnCardClicked", card);
            return card;
        }

        [Test]
        public void PurchasedCordJoinsCurrentGuideAndPickupClearsEveryPulse()
        {
            var card = AcceptOrder();
            Field<TypingReveal>(card, "typing").Advance(60, 40);
            Invoke(card, "TypeOn");
            Invoke(guide, "LateUpdate");
            book.SetOpen(true);
            book.SetOpen(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            session.Wallet.Earn(10000);
            Assert.That(session.Upgrades.TryBuy("wires_1"), Is.True);
            Assert.That(bridge.CordCount, Is.EqualTo(4));
            var bought = All<CableController>().Single(c => c.name == "Cable_04");
            var hints = bought.TrayView.GetComponentsInChildren<SlowGuidePulse>();
            Assert.That(hints.Length, Is.EqualTo(3));
            Assert.That(hints.All(p => p.IsShowing), Is.True, "Cloned and newly registered hints must work together");
            foreach (var hint in hints) Invoke(hint, "Update");
            Assert.That(bought.TryPickUp(), Is.True);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
            Assert.That(All<SlowGuidePulse>().Any(p => p.IsShowing), Is.False);
            Assert.That(session.Upgrades.TryBuy("wires_2"), Is.True);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "Purchase must not restart hints");
            Assert.That(All<SlowGuidePulse>().Any(p => p.IsShowing), Is.False);
        }

        [Test]
        public void NotepadAndNewCordKeepConnectionHighlightsCoinsAndAudio()
        {
            var card = AcceptOrder();
            session.Wallet.Earn(10000);
            Assert.That(session.Upgrades.TryBuy("notepad"), Is.True);
            Assert.That(book.ContactTapsEnabled, Is.True);
            var call = card.Call;
            var first = session.Directory.JackOf(call.Request.CallerPerson);
            var second = session.Directory.JackOf(call.Request.Recipient);
            Assert.That(session.Notepad.Note(first), Is.True);
            Assert.That(call.CallerNote, Is.EqualTo(first));
            Assert.That(Field<TMPro.TMP_Text>(card, "speechLabel").text, Does.Contain(first.Label));
            Assert.That(session.Upgrades.TryBuy("wires_1"), Is.True);
            var cable = All<CableController>().Single(c => c.name == "Cable_04");
            var a = All<SocketView>().Single(s => new JackId(s.Address) == first);
            var b = All<SocketView>().Single(s => new JackId(s.Address) == second);
            int audioCues = 0;
            All<CallAudio>().Single().ConnectionSucceeded += () => audioCues++;
            long before = session.Wallet.Cents;
            Assert.That(cable.TryPickUp() && cable.TryInsert(a), Is.True);
            bridge.Synchronize();
            Assert.That(cable.TryInsert(b), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(a.GetComponent<SocketGraphic>().IsCallConnected && b.GetComponent<SocketGraphic>().IsCallConnected, Is.True);
            var coins = All<IncomeCoinsView>().Single();
            Assert.That(coins.IsFlying, Is.True);
            Assert.That(session.Wallet.Cents, Is.EqualTo(before + 100));
            Assert.That(audioCues, Is.EqualTo(1));
            coins.Tick(2);
            Assert.That(cable.TryRemove(b), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            Assert.That(a.GetComponent<SocketGraphic>().IsCallConnected || b.GetComponent<SocketGraphic>().IsCallConnected, Is.False);
            Assert.That(cable.TryInsert(b), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(audioCues, Is.EqualTo(2));
            Assert.That(coins.IsFlying, Is.False, "Resume must not pay or replay reward coins");
            Assert.That(session.Wallet.Cents, Is.EqualTo(before + 100));
        }
    }
}
