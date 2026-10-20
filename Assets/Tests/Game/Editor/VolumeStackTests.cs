using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>The volume patch's wires (lead, 2026-10-20): the wire plugged last lies on top, and the order is saved.</summary>
    public sealed class VolumeStackTests
    {
        private const string PanelPath = "Assets/Prefabs/UI/VolumeCablePanel.prefab";
        private const int Quarter = 0, Half = 1, ThreeQuarters = 2, Full = 3, Mute = 4;
        private const int Master = 0, Music = 1, Sfx = 2;

        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public string Read(string key) => Values.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string value) => Values[key] = value;
            public void Delete(string key) => Values.Remove(key);
        }

        private static int[] Stack(VolumePatchState patch) => Enumerable.Range(0, 3).Select(patch.CableInLayer).ToArray();

        [Test]
        public void TheDefaultStackIsMasterMusicSfxFromTheBottom()
        {
            Assert.That(Stack(new VolumePatchState()), Is.EqualTo(new[] { Master, Music, Sfx }));
        }

        [Test]
        public void EachConnectLiftsThatWireToTheTop()
        {
            var patch = new VolumePatchState();
            Assert.That(patch.PickUp(Master), Is.True);
            Assert.That(patch.Connect(1, ThreeQuarters), Is.True);
            Assert.That(Stack(patch), Is.EqualTo(new[] { Music, Sfx, Master }));
            Assert.That(patch.PickUp(Music), Is.True);
            Assert.That(patch.Connect(0, Quarter), Is.True);
            Assert.That(Stack(patch), Is.EqualTo(new[] { Sfx, Master, Music }));
            Assert.That(patch.LayerOf(Music), Is.EqualTo(2));
            Assert.That(patch.LayerOf(Sfx), Is.EqualTo(0));
        }

        [Test]
        public void AHeldWireIsOnTopAndStaysThereWhenPutBack()
        {
            var patch = new VolumePatchState();
            Assert.That(patch.PickUp(Music), Is.True);
            Assert.That(Stack(patch), Is.EqualTo(new[] { Master, Sfx, Music }), "held");
            patch.Cancel();
            Assert.That(Stack(patch), Is.EqualTo(new[] { Master, Sfx, Music }), "put back");
            Assert.That(patch.PickUp(Master), Is.True);
            Assert.That(patch.Connect(2, Half), Is.False, "SFX holds that socket");
            patch.Cancel();
            Assert.That(Stack(patch), Is.EqualTo(new[] { Sfx, Music, Master }), "a refused drop also counts");
        }

        [Test]
        public void TheOrderIsSavedAndLoaded()
        {
            var patch = new VolumePatchState();
            Assert.That(patch.PickUp(Master), Is.True);
            Assert.That(patch.Connect(1, ThreeQuarters), Is.True);
            Assert.That(patch.PickUp(Sfx), Is.True);
            Assert.That(patch.Connect(0, Mute), Is.True);
            var storage = new MemoryStorage();
            AudioVolumeSettings.Save(patch, storage);
            StringAssert.Contains("\"order\":[1,0,2]", storage.Values[AudioVolumeSettings.SaveKey]);

            VolumePatchState loaded = AudioVolumeSettings.Load(storage);
            Assert.That(Stack(loaded), Is.EqualTo(new[] { Music, Master, Sfx }));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(loaded.RowOf(i), Is.EqualTo(patch.RowOf(i)));
                Assert.That(loaded.SocketOf(i), Is.EqualTo(patch.SocketOf(i)));
            }
        }

        [Test]
        public void ASaveFromBeforeTheOrderGetsTheDefaultStack()
        {
            var storage = new MemoryStorage();
            storage.Values[AudioVolumeSettings.SaveKey] = "{\"version\":1,\"rows\":[1,1,2],\"sockets\":[1,0,1]}";
            VolumePatchState loaded = AudioVolumeSettings.Load(storage);
            Assert.That(Stack(loaded), Is.EqualTo(new[] { Master, Music, Sfx }));
            Assert.That(loaded.RowOf(Master), Is.EqualTo(1));
            Assert.That(loaded.SocketOf(Music), Is.EqualTo(0));
        }

        [TestCase("[0,0,1]")]
        [TestCase("[0,1]")]
        [TestCase("[0,1,2,1]")]
        [TestCase("[0,1,3]")]
        [TestCase("[-1,0,1]")]
        [TestCase("[]")]
        public void ABadOrderFallsBackToTheDefaultButKeepsTheWires(string order)
        {
            var storage = new MemoryStorage();
            storage.Values[AudioVolumeSettings.SaveKey] = "{\"version\":1,\"rows\":[1,1,2],\"sockets\":[1,0,1],\"order\":" + order + "}";
            VolumePatchState loaded = AudioVolumeSettings.Load(storage);
            Assert.That(Stack(loaded), Is.EqualTo(new[] { Master, Music, Sfx }));
            Assert.That(loaded.RowOf(Master), Is.EqualTo(1));
            Assert.That(loaded.SocketOf(Master), Is.EqualTo(1));
            Assert.That(loaded.RowOf(Music), Is.EqualTo(1));
            Assert.That(loaded.SocketOf(Music), Is.EqualTo(0));
            Assert.That(loaded.RowOf(Sfx), Is.EqualTo(2));
        }

        // The lead's playtest (2026-10-20): Master at Music/100, SFX at Master/100, Music at Music/50 plugged last.
        private static void WithLeadCase(System.Action<GameObject, VolumeCablePanel, RectTransform, HandCursorGraphic> check)
        {
            VolumePatchState patch = AudioVolumeSettings.Patch;
            int[] rows = Enumerable.Range(0, 3).Select(patch.RowOf).ToArray();
            int[] sockets = Enumerable.Range(0, 3).Select(patch.SocketOf).ToArray();
            int[] stack = Stack(patch);
            GameObject root = PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                var panel = root.GetComponent<VolumeCablePanel>();
                var overlay = (RectTransform)root.transform.Find("CableOverlay");
                var hand = overlay.GetComponentInChildren<HandCursorGraphic>(true);
                Assert.That(patch.Restore(new[] { 0, 1, 2 }, new[] { Half, Half, Half }), Is.True);
                Assert.That(patch.PickUp(Master), Is.True);
                Assert.That(patch.Connect(1, Full), Is.True);
                Assert.That(patch.PickUp(Sfx), Is.True);
                Assert.That(patch.Connect(0, Full), Is.True);
                Assert.That(patch.PickUp(Music), Is.True);
                Assert.That(patch.Connect(1, Half), Is.True);
                panel.Configure(panel.Rows, hand, overlay);
                check(root, panel, overlay, hand);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                patch.Cancel();
                patch.Restore(rows, sockets, stack);
            }
        }

        private static int Index(VolumeCableSlider row, bool wire) =>
            (wire ? row.Wire.transform.parent : row.MovingPlug.transform).GetSiblingIndex();

        [Test]
        public void EveryWireLiesOverTheRestingJacksAndWiresStackByRecency()
        {
            WithLeadCase((root, panel, overlay, hand) =>
            {
                VolumeCableSlider[] rows = panel.Rows;
                foreach (VolumeCableSlider row in rows)
                {
                    Assert.That(row.MovingPlug.transform.parent, Is.EqualTo(overlay), row.name);
                    Assert.That(row.Wire.transform.parent.parent, Is.EqualTo(overlay), row.name);
                }
                int topJack = rows.Max(r => Index(r, false));
                Assert.That(rows.Min(r => Index(r, true)), Is.GreaterThan(topJack), "every wire lies over every resting jack, its own included");
                Assert.That(Index(rows[Master], true), Is.LessThan(Index(rows[Sfx], true)));
                Assert.That(Index(rows[Sfx], true), Is.LessThan(Index(rows[Music], true)), "Music was plugged last");
                Assert.That(hand.transform.GetSiblingIndex(), Is.GreaterThan(rows.Max(r => Index(r, true))), "the hand stays above");

                Assert.That(AudioVolumeSettings.Patch.PickUp(Sfx), Is.True);
                panel.Configure(rows, hand, overlay);
                Assert.That(Index(rows[Sfx], false), Is.GreaterThan(rows.Max(r => Index(r, true))), "the held jack is above every wire");
                Assert.That(Index(rows[Sfx], true), Is.GreaterThan(Index(rows[Music], true)), "and so is its wire");
                Assert.That(Index(rows[Master], false), Is.LessThan(rows.Min(r => Index(r, true))));
                Assert.That(Index(rows[Music], false), Is.LessThan(rows.Min(r => Index(r, true))));
                Assert.That(hand.transform.GetSiblingIndex(), Is.GreaterThan(Index(rows[Sfx], false)), "the hand stays above");
            });
        }

        [Test]
        public void AWireOverAJackStillLetsThatJackBePickedUp()
        {
            WithLeadCase((root, panel, overlay, hand) =>
            {
                // Hierarchy order is the raycaster's depth order inside one canvas; the last hit target is the top hit.
                Graphic[] targets = root.GetComponentsInChildren<Graphic>(true).Where(g => g.raycastTarget && g.gameObject.activeInHierarchy).ToArray();
                foreach (VolumeCableSlider row in panel.Rows)
                {
                    Assert.That(row.Wire.raycastTarget, Is.False, row.name);
                    Assert.That(row.Wire.transform.parent.GetComponent<Graphic>(), Is.Null, row.name + ": the clip draws nothing");
                    Vector2 point = row.MovingPlug.transform.position;
                    Graphic top = targets.LastOrDefault(g => RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform, point, null));
                    Assert.That(top, Is.EqualTo(row.MovingPlug), row.name + ": the top hit over its jack is the jack");
                }
            });
        }

        [Test]
        public void TheWiresStartHiddenUnderTheFrameAndTheJacksAreNeverClipped()
        {
            WithLeadCase((root, panel, overlay, hand) =>
            {
                var cover = (RectTransform)root.transform.Find("LeftFrameCover");
                float frameEdge = cover.localPosition.x + cover.rect.xMax;
                foreach (VolumeCableSlider row in panel.Rows)
                {
                    var clip = row.Wire.transform.parent.GetComponent<RectMask2D>();
                    Assert.That(clip, Is.Not.Null, row.name + ": the wire is clipped");
                    Assert.That(row.MovingPlug.GetComponentInParent<RectMask2D>(), Is.Null, row.name + ": the jack is never clipped");
                    var corners = new Vector3[4];
                    ((RectTransform)clip.transform).GetWorldCorners(corners);
                    Assert.That(root.transform.InverseTransformPoint(corners[0]).x, Is.EqualTo(frameEdge).Within(.01f), row.name + ": the wire starts under the frame");
                    float anchor = root.transform.InverseTransformPoint(row.WallAnchor.position).x;
                    Assert.That(anchor, Is.LessThan(frameEdge), row.name + ": the wall end stays hidden");
                }
            });
        }
    }
}
