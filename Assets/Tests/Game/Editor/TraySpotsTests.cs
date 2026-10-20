using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Cables;
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
    /// The cord tray has fixed spots (D58 fix 2, lead 2026-10-20): a cord coming back, or a bought one, takes the first
    /// free spot from the left. No cord owns a spot, and no spot ever holds two. The real Final scene, a loaded game.
    /// </summary>
    public sealed class TraySpotsTests
    {
        private Scene scene;
        private CordBridge bridge;
        private CableInputController input;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

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
            installer.GetType().GetMethod("Install", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(installer, new object[] { null, new SaveData { version = SaveData.CurrentVersion, cents = 0 } });
            Assert.That(installer.Session.IsTutorial, Is.False, "setup: no tutorial in the way");
            bridge = All<CordBridge>().Single();
            input = All<CableInputController>().Single();
            if (bridge.CordCount < 3) bridge.EnsureCords(3);
            foreach (CableController cable in Cords()) cable.TrayView.Bind(cable);
            input.RefreshTraySpots();
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private CableController[] Cords() => All<CableController>()
            .Where(c => c.gameObject.activeInHierarchy && c.TrayView != null).ToArray();

        private CableController InSpot(int spot) => Cords().Single(c => input.TraySpotOf(c) == spot);

        private static Vector2 Centre(Transform target)
        {
            var rect = (RectTransform)target;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private void Click(Transform target) => input.ProcessTarget(target.gameObject, null, Centre(target), true);

        // Out of the tray and onto the board: both ends in two free jacks.
        private void PatchOut(CableController cable)
        {
            Click(cable.TrayView.transform);
            Assert.That(input.ActiveCable, Is.SameAs(cable), "setup: the tray cord takes the click");
            SocketView[] free = All<SocketView>().Where(s => s.IsOpen && s.isActiveAndEnabled && !s.IsOccupied).Take(2).ToArray();
            Assert.That(free.Length, Is.EqualTo(2), "setup: two free jacks");
            Click(free[0].transform);
            Click(free[1].transform);
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Connected), "setup");
        }

        // What CordBridge.ReturnToTray does for a refused or finished circuit: both plugs out, back to the tray.
        private static void Return(CableController cable)
        {
            SocketView first = cable.FirstSocket, second = cable.SecondSocket;
            if (first != null) cable.TryRemove(first);
            if (second != null) cable.TryRemove(second);
            Assert.That(cable.IsInTray, Is.True, "setup: back in the tray");
        }

        private void AssertTray()
        {
            CableController[] cords = Cords();
            var used = new HashSet<int>();
            foreach (CableController cable in cords)
            {
                int spot = input.TraySpotOf(cable);
                if (!cable.IsInTray)
                {
                    Assert.That(spot, Is.EqualTo(-1), $"{cable.name} is out: no spot");
                    continue;
                }
                Assert.That(spot, Is.InRange(0, cords.Length - 1), cable.name);
                Assert.That(used.Add(spot), Is.True, $"spot {spot} holds two cords");
                var rect = (RectTransform)cable.TrayView.transform;
                Assert.That(Vector2.Distance(rect.anchoredPosition, CableInputController.TraySlotPosition(spot, cords.Length)),
                    Is.LessThan(.01f), $"{cable.name} sits in spot {spot}");
                Assert.That(rect.sizeDelta, Is.EqualTo(CableInputController.TraySlotSize(cords.Length)));
            }
        }

        [Test]
        public void ReturnedCordsTakeTheFirstFreeSpotFromTheLeft()
        {
            int count = Cords().Length;
            Assert.That(count, Is.GreaterThanOrEqualTo(3), "setup");
            AssertTray();
            Assert.That(Cords().Select(c => input.TraySpotOf(c)), Is.EquivalentTo(Enumerable.Range(0, count)), "a full tray");

            CableController middle = InSpot(1), left = InSpot(0), right = InSpot(2);
            PatchOut(middle);
            PatchOut(left);
            Assert.That(input.TraySpotOf(middle), Is.EqualTo(-1));
            Assert.That(input.TraySpotOf(right), Is.EqualTo(2), "a cord at rest never moves");
            AssertTray();

            // The middle cord comes back first: the first free spot is the left one, not its old one.
            Return(middle);
            input.RefreshTraySpots();
            Assert.That(input.TraySpotOf(middle), Is.EqualTo(0));
            AssertTray();
            Return(left);
            input.RefreshTraySpots();
            Assert.That(input.TraySpotOf(left), Is.EqualTo(1), "the next free spot");
            Assert.That(input.TraySpotOf(right), Is.EqualTo(2));
            AssertTray();

            // Back through the cord input (pull one plug, click the tray): it finds the first free spot at once.
            PatchOut(InSpot(0));
            CableController back = Cords().Single(c => !c.IsInTray);
            Click(back.FirstSocket.transform);
            Assert.That(input.ActiveCable, Is.SameAs(back), "setup: one end pulled");
            Click(InSpot(1).TrayView.transform.parent);
            Assert.That(back.IsInTray, Is.True, "setup: a click on the tray puts the held cord back");
            Assert.That(input.TraySpotOf(back), Is.EqualTo(0));
            AssertTray();
        }

        [Test]
        public void ABoughtCordTakesTheFirstFreeSpotToo()
        {
            int count = Cords().Length;
            if (count >= CableInputController.MaxCables) Assert.Ignore("the tray is already full");
            CableController left = InSpot(0);
            PatchOut(left);
            bridge.EnsureCords(count + 1);
            CableController bought = Cords().Single(c => c.name == $"Cable_{count + 1:00}");
            Assert.That(input.TraySpotOf(bought), Is.EqualTo(0), "the gap the cord on the board left");
            AssertTray();
            Return(left);
            input.RefreshTraySpots();
            Assert.That(input.TraySpotOf(left), Is.EqualTo(count), "the only free spot is the new one");
            AssertTray();
        }
    }
}
