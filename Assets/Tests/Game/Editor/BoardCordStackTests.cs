using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// The board's cords stack like the volume patch (lead, 2026-10-20): resting plugs under every wire,
    /// the wire moved last on top, a held plug on the Cursor layer, and a jack under a wire still takes the click.
    /// </summary>
    public sealed class BoardCordStackTests
    {
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private GameObject board;
        private GameObject events;
        private RenderTexture texture;
        private Camera camera;
        private EventSystem previousEventSystem;
        private BaseRaycaster[] otherRaycasters;
        private CableInputController input;
        private CableController crossing, crossed;
        private SocketView left, right, under, below;

        [SetUp]
        public void SetUp()
        {
            previousEventSystem = EventSystem.current;
            events = new GameObject("Cord stack events", typeof(EventSystem));
            var eventSystem = events.GetComponent<EventSystem>();
            typeof(EventSystem).GetMethod("OnEnable", Hidden).Invoke(eventSystem, null);
            camera = events.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(960, 540, -10);
            camera.orthographic = true;
            camera.orthographicSize = 540;
            texture = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = texture;

            board = new GameObject("Board", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = board.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10;
            var raycaster = board.GetComponent<GraphicRaycaster>();
            typeof(BaseRaycaster).GetMethod("OnEnable", Hidden).Invoke(raycaster, null);
            otherRaycasters = RaycasterManager.GetRaycasters().Where(r => r != raycaster && r.isActiveAndEnabled).ToArray();
            foreach (var other in otherRaycasters) other.enabled = false;
            EventSystem.current = eventSystem;

            var sockets = new GameObject("Sockets", typeof(RectTransform));
            sockets.transform.SetParent(board.transform, false);
            left = Socket(sockets.transform, 0, 0, new Vector2(-300, 0));
            right = Socket(sockets.transform, 0, 2, new Vector2(300, 0));
            // The left-right wire sags through here: both tails sit 64 below their sockets and the sag is 155.
            under = Socket(sockets.transform, 1, 1, new Vector2(0, -180));
            below = Socket(sockets.transform, 3, 1, new Vector2(0, -420));
            crossing = Cable("Crossing");
            crossed = Cable("Crossed");
            input = board.AddComponent<CableInputController>();
            input.Configure(null, new[] { crossing, crossed }, null);
            Render();

            Plug(crossing, left, right);
            Plug(crossed, under, below);
            input.ProcessPointer(Screen(new Vector2(800, 400)), false);
        }

        [TearDown]
        public void TearDown()
        {
            if (events != null) typeof(EventSystem).GetMethod("OnDisable", Hidden).Invoke(events.GetComponent<EventSystem>(), null);
            if (board != null) typeof(BaseRaycaster).GetMethod("OnDisable", Hidden).Invoke(board.GetComponent<GraphicRaycaster>(), null);
            if (camera != null) camera.targetTexture = null;
            if (texture != null) { texture.Release(); Object.DestroyImmediate(texture); }
            if (board != null) Object.DestroyImmediate(board);
            if (events != null) Object.DestroyImmediate(events);
            if (previousEventSystem != null) EventSystem.current = previousEventSystem;
            if (otherRaycasters != null) foreach (var other in otherRaycasters) if (other != null) other.enabled = true;
        }

        private SocketView Socket(Transform parent, int row, int column, Vector2 position)
        {
            var go = new GameObject($"Socket_{row}_{column}", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(70, 70);
            var socket = go.AddComponent<SocketView>();
            socket.Configure(row, column, rect);
            return socket;
        }

        private CableController Cable(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(board.transform, false);
            var wire = Part<CableGraphic>(go, "Wire");
            var first = Part<PlugGraphic>(go, "FirstPlug");
            var second = Part<PlugGraphic>(go, "SecondPlug");
            wire.raycastTarget = first.raycastTarget = second.raycastTarget = false;
            var cable = go.AddComponent<CableController>();
            cable.Configure((RectTransform)board.transform, wire, first, second, null, null, null, null);
            return cable;
        }

        private static T Part<T>(GameObject parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<T>();
        }

        private static void Plug(CableController cable, SocketView a, SocketView b) =>
            Assert.That(cable.TryPickUp() && cable.TryInsert(a) && cable.TryInsert(b), Is.True, cable.name);

        private Vector2 Screen(Vector2 boardPosition) =>
            RectTransformUtility.WorldToScreenPoint(camera, board.transform.TransformPoint(boardPosition));

        private Vector2 ScreenOf(SocketView socket) => RectTransformUtility.WorldToScreenPoint(camera, socket.transform.position);

        // Lays the canvas out and gives each graphic the depth the raycaster sorts by.
        private void Render()
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
        }

        private void Click(SocketView socket)
        {
            Render();
            input.ProcessPointer(ScreenOf(socket), true);
        }

        // The order a graphic draws at: its nearest canvas that sorts on its own, else the board's.
        private static int Order(Component part)
        {
            for (var canvas = part.GetComponentInParent<Canvas>(); canvas != null;
                canvas = canvas.transform.parent != null ? canvas.transform.parent.GetComponentInParent<Canvas>() : null)
                if (canvas.isRootCanvas || canvas.overrideSorting) return canvas.isRootCanvas ? 0 : canvas.sortingOrder;
            return 0;
        }

        private static IEnumerable<PlugGraphic> RestingPlugs(params CableController[] cables)
        {
            foreach (var cable in cables)
            {
                var plugs = cable.GetComponentsInChildren<PlugGraphic>(true);
                if (cable.FirstSocket != null) yield return plugs[0];
                if (cable.SecondSocket != null) yield return plugs[1];
            }
        }

        private static CableGraphic Wire(CableController cable) => cable.GetComponentInChildren<CableGraphic>(true);

        private static PlugGraphic HeldPlug(CableController cable)
        {
            var plugs = cable.GetComponentsInChildren<PlugGraphic>(true);
            return cable.FirstSocket == null ? plugs[0] : plugs[1];
        }

        private void AssertWiresOverRestingPlugs(string when)
        {
            int lowestWire = Mathf.Min(Order(Wire(crossing)), Order(Wire(crossed)));
            foreach (var plug in RestingPlugs(crossing, crossed))
                Assert.That(Order(plug), Is.LessThan(lowestWire), $"{when}: {plug.transform.parent.name}/{plug.name} lies under every wire");
            Assert.That(Mathf.Max(Order(Wire(crossing)), Order(Wire(crossed))), Is.LessThan(SwitchboardUISorting.Book),
                $"{when}: the wires stay under the book");
        }

        [Test]
        public void RestingPlugsLieUnderEveryWireAndTheWireMovedLastIsOnTop()
        {
            AssertWiresOverRestingPlugs("start");
            Assert.That(Order(Wire(crossing)), Is.LessThan(Order(Wire(crossed))), "the cord plugged second starts on top");

            Click(left);
            Assert.That(input.ActiveCable, Is.SameAs(crossing));
            Click(left);
            Assert.That(crossing.Phase, Is.EqualTo(Switchboard.Cables.CablePhase.Connected));
            AssertWiresOverRestingPlugs("crossing cord replugged");
            Assert.That(Order(Wire(crossing)), Is.GreaterThan(Order(Wire(crossed))), "the replugged crossing wire is on top");

            Click(under);
            Assert.That(input.ActiveCable, Is.SameAs(crossed));
            Click(under);
            Assert.That(crossed.Phase, Is.EqualTo(Switchboard.Cables.CablePhase.Connected));
            AssertWiresOverRestingPlugs("crossed cord replugged");
            Assert.That(Order(Wire(crossed)), Is.GreaterThan(Order(Wire(crossing))), "the replugged crossed wire is on top");
        }

        [Test]
        public void AHeldPlugIsOnTheCursorLayerAndItsWireOverEveryOtherWire()
        {
            Click(left);
            Assert.That(input.ActiveCable, Is.SameAs(crossing));
            PlugGraphic held = HeldPlug(crossing);
            Assert.That(held.gameObject.activeSelf, Is.True);
            Assert.That(Order(held), Is.EqualTo(SwitchboardUISorting.Cursor));
            Assert.That(Order(Wire(crossing)), Is.GreaterThan(Order(Wire(crossed))));
            AssertWiresOverRestingPlugs("holding");

            input.ProcessPointer(Screen(new Vector2(800, 400)), true);
            Assert.That(crossing.FirstSocket, Is.SameAs(left), "an empty-spot click puts the held end back");
            Assert.That(Order(held), Is.LessThan(Order(Wire(crossed))), "back in its socket, the plug is a resting plug again");
            Assert.That(Order(Wire(crossing)), Is.GreaterThan(Order(Wire(crossed))), "the wire moved last stays on top");
        }

        [Test]
        public void AJackUnderAnotherCordsWireStillTakesTheClick()
        {
            Render();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = ScreenOf(under) }, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<SocketView>(), Is.SameAs(under),
                "the top hit over the crossed jack is its socket: " + string.Join(", ", hits.Select(h => h.gameObject.name)));
            Assert.That(hits.Any(h => h.gameObject.GetComponent<CableGraphic>() != null || h.gameObject.GetComponent<PlugGraphic>() != null),
                Is.False, "wires and plugs take no clicks");

            Click(under);
            Assert.That(input.ActiveCable, Is.SameAs(crossed), "clicking the jack under the wire picks it up");
        }

        [Test]
        public void WireLayersFitBetweenTheBoardAndTheBookAndNothingInTheGameSitsThere()
        {
            Assert.That(SwitchboardUISorting.Wire, Is.GreaterThan(SwitchboardUISorting.Cable));
            Assert.That(SwitchboardUISorting.Wire + CableInputController.MaxCables - 1, Is.LessThan(SwitchboardUISorting.Book));
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            try
            {
                var canvases = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).ToArray();
                foreach (var canvas in canvases.Where(c => c.isRootCanvas || c.overrideSorting))
                    if (canvas.GetComponent<CableGraphic>() == null && canvas.sortingOrder > SwitchboardUISorting.Cable)
                        Assert.That(canvas.sortingOrder, Is.GreaterThanOrEqualTo(SwitchboardUISorting.Book),
                            canvas.name + " would fall between the board and the wires");
                var cableArea = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CableInputController>(true)).Single().transform;
                Assert.That(cableArea.GetSiblingIndex(), Is.EqualTo(cableArea.parent.childCount - 1),
                    "nothing else in the board's canvas draws after the cords");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
