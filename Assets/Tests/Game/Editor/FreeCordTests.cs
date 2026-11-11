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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// D79 in the real Final scene: one click (or tap) on either plug of a cord whose call is over pulls both plugs and
    /// puts the cord in hand; a live call's plug keeps D07's hold.
    /// </summary>
    public sealed class FreeCordTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
        private CordBridge bridge;
        private CableInputController input;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        [SetUp]
        public void SetUp()
        {
            // A preview scene keeps the user's open scene and unsaved edits intact.
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            foreach (var transform in All<Transform>().Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject))
                    PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Canvas.ForceUpdateCanvases();
            foreach (var canvas in All<Canvas>().Where(c => c.isRootCanvas))
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            All<PhoneBookView>().Single().SetOpen(false);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Start");
            session = installer.Session;
            Assert.That(session, Is.Not.Null);
            queue = All<CallQueueView>().Single();
            bridge = All<CordBridge>().Single();
            input = All<CableInputController>().Single();
            // A cord binds its tray slot in OnEnable, which runs only in Play Mode.
            foreach (CableController cable in Cords().Where(c => c.TrayView != null)) cable.TrayView.Bind(cable);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private CableController[] Cords() => All<CableController>().Where(c => c.gameObject.activeInHierarchy).ToArray();
        private SocketView Socket(JackId jack) => All<SocketView>().Single(s => s.Address == jack.Label);

        // The start pool's first call's jacks are open on the starting board; Economy.json sets which they are.
        private Call Accept(string id, double seconds = 10)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
            CallScript first = config.LoadEconomy().StartPool[0];
            var script = new CallScript(id, first.Name, first.Icon, first.Message, first.From, first.To, seconds, 100, 10);
            Call call = session.Calls.Add(new CallRequest(script));
            Invoke(queue, "OnCardClicked", All<CallCardView>().Single(c => c.Call == call));
            return call;
        }

        // A click or a tap: both reach the cord input's one path (D28); a tap has no hover.
        private void Tap(SocketView socket) => input.ProcessTarget(socket.gameObject, null, Vector2.zero, true, hover: false);

        private CableController Patch(Call call)
        {
            CableController cable = Cords().First(c => c.IsInTray);
            Assert.That(cable.TryPickUp() && cable.TryInsert(Socket(call.Request.From)), Is.True, "setup");
            bridge.Synchronize();
            Assert.That(cable.TryInsert(Socket(call.Request.To)), Is.True, "setup");
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "setup");
            return cable;
        }

        [Test]
        public void OneClickOnAFinishedCordsPlugFreesBothPlugsAndPutsTheCordInHand()
        {
            Call call = Accept("free_first");
            CableController cable = Patch(call);
            SocketView a = Socket(call.Request.From), b = Socket(call.Request.To);
            session.Tick(call.SecondsLeft + .1);
            Assert.That(call.State, Is.EqualTo(CallState.Ended));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Connected), "D45: a finished call's cord stays plugged");
            int pickups = 0;
            cable.PickedUp += _ => pickups++;

            Tap(b);
            Assert.That(a.IsOccupied || b.IsOccupied, Is.False, "both plugs come out");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held));
            Assert.That(cable.IsInTray, Is.False);
            Assert.That(input.ActiveCable, Is.SameAs(cable), "the cord is in hand");
            Assert.That(pickups, Is.EqualTo(1), "picked up as from the tray");
        }

        [Test]
        public void TheLiftedCordReportsAsReturnedAndPlugsStraightIntoTheNextCall()
        {
            Call call = Accept("free_first");
            CableController cable = Patch(call);
            SocketView a = Socket(call.Request.From), b = Socket(call.Request.To);
            session.Tick(call.SecondsLeft + .1);
            Tap(a);
            bridge.Synchronize();
            Assert.That(session.Desk.HalfPatched, Is.Empty, "no half-patched cord left behind");

            // The desk saw the circuit go: a new call between the same jacks doesn't connect on taking its card.
            Call next = Accept("free_next");
            Assert.That(next.State, Is.EqualTo(CallState.Taken), "the old circuit is gone");
            long before = session.Wallet.Cents;
            Tap(a);
            bridge.Synchronize();
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            Tap(b);
            bridge.Synchronize();
            Assert.That(next.State, Is.EqualTo(CallState.Connected));
            Assert.That(session.Wallet.Cents, Is.GreaterThanOrEqualTo(before + 100), "the next call's fee is paid");
            Assert.That(input.ActiveCable, Is.Null);
        }

        [Test]
        public void ALiveCallsPlugStillPutsTheCallOnHold()
        {
            Call call = Accept("free_live");
            CableController cable = Patch(call);
            SocketView a = Socket(call.Request.From), b = Socket(call.Request.To);

            Tap(b);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.OnHold), "D07");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected), "only the clicked plug comes out");
            Assert.That(a.OccupyingCable, Is.SameAs(cable));
            Assert.That(b.IsOccupied, Is.False);
            Assert.That(input.ActiveCable, Is.SameAs(cable));
            Tap(b);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected), "plugged back: resumed");
        }

        [Test]
        public void ACircuitWithNoCallAlsoComesOffWhole()
        {
            JackId from = session.Calls.Live.Count > 0 ? session.Calls.Live[0].Request.From : default;
            SocketView[] open = All<SocketView>().Where(s => s.IsOpen && s.gameObject.activeInHierarchy && s.Address != from.Label)
                .Take(2).ToArray();
            CableController cable = Cords().First(c => c.IsInTray);
            Assert.That(cable.TryPickUp() && cable.TryInsert(open[0]) && cable.TryInsert(open[1]), Is.True, "setup");
            bridge.Synchronize();
            Assert.That(session.Desk.CallOn(new CordId(0)), Is.Null, "setup: no call rides on it");

            Tap(open[0]);
            Assert.That(open[0].IsOccupied || open[1].IsOccupied, Is.False);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Held));
        }

        [Test]
        public void WithoutTheGamesAnswerTheCordCodePullsOnePlugAsBefore()
        {
            Call call = Accept("free_alone");
            CableController cable = Patch(call);
            session.Tick(call.SecondsLeft + .1);
            input.CarriesLiveCall = null;

            Tap(Socket(call.Request.To));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            Assert.That(Socket(call.Request.From).OccupyingCable, Is.SameAs(cable));
        }
    }
}
