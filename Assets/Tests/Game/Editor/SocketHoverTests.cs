using NUnit.Framework;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    public sealed class SocketHoverTests
    {
        [Test]
        public void HeldCableReplacesConnectedCableAndReturnsBothOldEndsToTray()
        {
            var root = new GameObject("Cable replacement", typeof(RectTransform));
            try
            {
                var input = root.AddComponent<CableInputController>();
                var oldObject = new GameObject("Old cable"); oldObject.transform.SetParent(root.transform);
                var newObject = new GameObject("New cable"); newObject.transform.SetParent(root.transform);
                var oldCable = oldObject.AddComponent<CableController>();
                var newCable = newObject.AddComponent<CableController>();
                input.Configure((RectTransform)root.transform, new[] { oldCable, newCable }, null);
                var sockets = new SocketView[4];
                for (int i = 0; i < sockets.Length; i++)
                {
                    var go = new GameObject("Socket", typeof(RectTransform));
                    go.transform.SetParent(root.transform);
                    sockets[i] = go.AddComponent<SocketView>();
                    sockets[i].Configure(0, i, (RectTransform)go.transform);
                }
                Assert.That(oldCable.TryPickUp() && oldCable.TryInsert(sockets[0]) && oldCable.TryInsert(sockets[1]), Is.True);
                Assert.That(newCable.TryPickUp() && newCable.TryInsert(sockets[2]) && newCable.TryInsert(sockets[3]), Is.True);
                input.HandleSocketClick(sockets[3]);
                Assert.That(input.ActiveCable, Is.SameAs(newCable));
                input.HandleSocketClick(sockets[0]);
                Assert.That(oldCable.IsInTray, Is.True);
                Assert.That(oldCable.FirstSocket, Is.Null);
                Assert.That(oldCable.SecondSocket, Is.Null);
                Assert.That(sockets[1].IsOccupied, Is.False);
                Assert.That(sockets[0].OccupyingCable, Is.SameAs(newCable));
                Assert.That(sockets[2].OccupyingCable, Is.SameAs(newCable));
                Assert.That(input.ActiveCable, Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static readonly Color Amber = new Color32(245, 210, 120, 255);
        private static readonly Color Green = new Color32(125, 255, 155, 255);
        private static readonly Color Red = new Color32(238, 68, 80, 255);

        private static SocketHaloGraphic VisibleHalo(SocketView socket) => socket.GetComponentInChildren<SocketHaloGraphic>();

        [Test]
        public void AClosedSocketNeverShowsAHalo()
        {
            var root = new GameObject("Closed socket halo", typeof(RectTransform));
            try
            {
                var socket = root.AddComponent<SocketView>();
                var heldObject = new GameObject("Held"); heldObject.transform.SetParent(root.transform);
                var held = heldObject.AddComponent<CableController>();
                var ownerObject = new GameObject("Owner"); ownerObject.transform.SetParent(root.transform);
                var owner = ownerObject.AddComponent<CableController>();
                socket.SetOpen(false);
                socket.SetPointerState(true, held);
                Assert.That(VisibleHalo(socket), Is.Null, "A held cord over a closed jack must not glow");
                socket.SetPointerState(true, null);
                Assert.That(VisibleHalo(socket), Is.Null, "An empty hand over a closed jack must not glow");
                Assert.That(socket.GetComponent<SocketGraphic>().IsHighlighted, Is.False);

                // A plug left in a jack that closes (the dev reset) keeps its call flag but shows no green.
                socket.SetOpen(true);
                socket.TryOccupy(owner);
                socket.SetCallConnected(true);
                Assert.That(VisibleHalo(socket), Is.Not.Null);
                socket.SetOpen(false);
                Assert.That(VisibleHalo(socket), Is.Null, "Dimmed wins over green");
                socket.SetPointerState(true, held);
                Assert.That(VisibleHalo(socket), Is.Null, "Dimmed wins over the red ring");
                socket.SetPointerState(true, null);
                Assert.That(VisibleHalo(socket), Is.Null, "Dimmed wins over the red ring with an empty hand too");
                socket.SetOpen(true);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo(Green), "Reopening brings the green back");
            }
            finally { Object.DestroyImmediate(root); }
        }

        // The lead's hover table (2026-11-09): one board with every kind of jack.
        private sealed class Board
        {
            public GameObject Root;
            public GameStyle Style;
            public CableInputController Input;
            public CableController Live, Half, Over, Held;
            public SocketView LiveJack, LiveOther, HalfJack, OverJack, OverOther, EmptyJack, ClosedJack, Spare;

            public Board()
            {
                Root = new GameObject("Hover table", typeof(RectTransform));
                Style = ScriptableObject.CreateInstance<GameStyle>();
                Input = Root.AddComponent<CableInputController>();
                Live = Cable("Live call"); Half = Cable("Half-patched"); Over = Cable("Call over"); Held = Cable("Held");
                Input.Configure((RectTransform)Root.transform, new[] { Live, Half, Over, Held }, null);
                var sockets = new SocketView[8];
                for (int i = 0; i < sockets.Length; i++)
                {
                    var go = new GameObject("Socket", typeof(RectTransform));
                    go.transform.SetParent(Root.transform);
                    sockets[i] = go.AddComponent<SocketView>();
                    sockets[i].Configure(0, i, (RectTransform)go.transform);
                    sockets[i].Palette = Style;
                }
                LiveJack = sockets[0]; LiveOther = sockets[1]; HalfJack = sockets[2]; OverJack = sockets[3];
                OverOther = sockets[4]; EmptyJack = sockets[5]; ClosedJack = sockets[6]; Spare = sockets[7];
                Assert.That(Live.TryPickUp() && Live.TryInsert(LiveJack) && Live.TryInsert(LiveOther), Is.True);
                Assert.That(Half.TryPickUp() && Half.TryInsert(HalfJack), Is.True);
                Assert.That(Over.TryPickUp() && Over.TryInsert(OverJack) && Over.TryInsert(OverOther), Is.True);
                LiveJack.SetCallConnected(true);
                LiveOther.SetCallConnected(true);
                ClosedJack.SetOpen(false);
            }

            private CableController Cable(string name)
            {
                var go = new GameObject(name); go.transform.SetParent(Root.transform);
                return go.AddComponent<CableController>();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(Style);
            }
        }

        private static void AssertHalo(SocketView socket, Color? expected, string what)
        {
            var halo = VisibleHalo(socket);
            if (expected == null) { Assert.That(halo, Is.Null, what); return; }
            Assert.That(halo, Is.Not.Null, what);
            Assert.That(halo.color, Is.EqualTo(expected.Value), what);
            Assert.That(halo.raycastTarget, Is.False);
        }

        [Test]
        public void WithAnEmptyHandOnlyAHalfPatchedJackHoversAmber()
        {
            var board = new Board();
            try
            {
                // Through the cord input's own hover, which reports an empty hand every frame.
                var cases = new[]
                {
                    new { Jack = board.EmptyJack, Expected = (Color?)null, What = "an empty jack shows nothing" },
                    new { Jack = board.LiveJack, Expected = (Color?)Green, What = "a live call's jack stays green" },
                    new { Jack = board.HalfJack, Expected = (Color?)Amber, What = "a half-patched jack hovers amber" },
                    new { Jack = board.OverJack, Expected = (Color?)null, What = "a finished call's jack shows nothing" },
                    new { Jack = board.ClosedJack, Expected = (Color?)null, What = "a closed jack shows nothing" },
                };
                foreach (var c in cases)
                {
                    board.Input.ProcessTarget(c.Jack.gameObject, null, Vector2.zero, false);
                    Assert.That(board.Input.ActiveCable, Is.Null);
                    AssertHalo(c.Jack, c.Expected, c.What);
                    Assert.That(c.Jack.GetComponent<SocketGraphic>().IsDangerShowing, Is.False, c.What + ": never red with an empty hand");
                }
                board.Input.ProcessTarget(board.HalfJack.gameObject, null, Vector2.zero, false);
                board.Input.ProcessTarget(null, null, Vector2.zero, false);
                AssertHalo(board.HalfJack, null, "the amber clears when the pointer leaves");
                AssertHalo(board.LiveJack, Green, "the live call stays green");
            }
            finally { board.Dispose(); }
        }

        [Test]
        public void WithACordInHandOnlyALiveCallsJackIsRedAndEveryOtherOpenJackIsAmber()
        {
            var board = new Board();
            try
            {
                var held = board.Held;
                AssertHaloWhileHovered(board.EmptyJack, held, Amber, "an empty jack hovers amber");
                AssertHaloWhileHovered(board.HalfJack, held, Amber, "an unfinished cord's jack hovers amber, not red");
                AssertHaloWhileHovered(board.OverJack, held, Amber, "a finished call's jack hovers amber, not red");
                AssertHaloWhileHovered(board.ClosedJack, held, null, "a closed jack shows nothing");
                AssertHaloWhileHovered(board.HalfJack, board.Half, Amber, "a cord over its own plug hovers amber");

                board.LiveJack.SetPointerState(true, held);
                AssertHalo(board.LiveJack, board.Style.ErrorGlow, "a live call's jack is red");
                Assert.That(VisibleHalo(board.LiveJack).Intensity, Is.EqualTo(board.Style.BlockedGlowIntensity));
                Assert.That(board.LiveJack.GetComponent<SocketGraphic>().IsDangerShowing, Is.True);
                board.LiveJack.SetPointerState(false, held);
                AssertHalo(board.LiveJack, Green, "the green comes back when the pointer leaves");
                Assert.That(VisibleHalo(board.LiveJack).Intensity, Is.GreaterThan(1));
            }
            finally { board.Dispose(); }
        }

        private static void AssertHaloWhileHovered(SocketView jack, CableController held, Color? expected, string what)
        {
            jack.SetPointerState(true, held);
            AssertHalo(jack, expected, what);
            if (expected != null) Assert.That(VisibleHalo(jack).Intensity, Is.EqualTo(1f), what);
            jack.SetPointerState(false, held);
            AssertHalo(jack, null, what + ", and clears when the pointer leaves");
        }

        [Test]
        public void TheRingUpdatesWithoutThePointerMovingAndAClickStillPicksThePlugUp()
        {
            var board = new Board();
            try
            {
                // A held cord over a live call: red, amber when the call ends, red again when a call connects.
                board.LiveJack.SetPointerState(true, board.Held);
                board.LiveJack.SetCallConnected(false);
                AssertHalo(board.LiveJack, Amber, "the call ends under a held cord");
                board.LiveJack.SetCallConnected(true);
                AssertHalo(board.LiveJack, board.Style.ErrorGlow, "a call connects under a held cord");
                board.LiveJack.SetOpen(false);
                AssertHalo(board.LiveJack, null, "the jack closes while hovered");
                board.LiveJack.SetOpen(true);
                AssertHalo(board.LiveJack, Green, "reopened, no longer hovered");

                // An empty hand over a half-patched jack: amber, then the second plug goes in elsewhere (the cord input
                // refreshes the hovered jack every frame), then the call connects, then it ends.
                board.Input.ProcessTarget(board.HalfJack.gameObject, null, Vector2.zero, false);
                AssertHalo(board.HalfJack, Amber, "half-patched");
                Assert.That(board.Half.TryInsert(board.Spare), Is.True);
                board.Input.ProcessTarget(board.HalfJack.gameObject, null, Vector2.zero, false);
                AssertHalo(board.HalfJack, null, "both plugs in, no call yet: nothing with an empty hand");
                board.HalfJack.SetCallConnected(true);
                AssertHalo(board.HalfJack, Green, "the call connects: green, not red, with an empty hand");
                board.HalfJack.SetCallConnected(false);
                AssertHalo(board.HalfJack, null, "the call ends: nothing with an empty hand");
                board.Input.ProcessTarget(null, null, Vector2.zero, false);

                // Only a look: an empty-handed click on a live call's jack still picks its plug up.
                board.Input.ProcessTarget(board.LiveJack.gameObject, null, Vector2.zero, true);
                Assert.That(board.Input.ActiveCable, Is.SameAs(board.Live), "the ring blocks nothing");
                Assert.That(board.LiveJack.IsOccupied, Is.False);
                AssertHalo(board.LiveJack, Amber, "now a held cord over an empty jack");
                board.Input.ProcessTarget(null, null, Vector2.zero, false);
                AssertHalo(board.LiveJack, null, "and it clears when the pointer leaves");
            }
            finally { board.Dispose(); }
        }

        [Test]
        public void ASocketWithoutAPaletteGlowsAndFlashesLikeTheDefaultPaintBox()
        {
            var root = new GameObject("Palette defaults", typeof(RectTransform));
            var style = ScriptableObject.CreateInstance<GameStyle>();
            try
            {
                var plainObject = new GameObject("Unstyled", typeof(RectTransform)); plainObject.transform.SetParent(root.transform);
                var styledObject = new GameObject("Styled", typeof(RectTransform)); styledObject.transform.SetParent(root.transform);
                var plain = plainObject.AddComponent<SocketView>().GetComponent<SocketGraphic>();
                var styled = styledObject.AddComponent<SocketView>().GetComponent<SocketGraphic>();
                styled.Palette = style;
                foreach (SocketGraphic socket in new[] { plain, styled })
                {
                    string which = socket == plain ? "unstyled" : "styled";
                    Assert.That(socket.ErrorGlow, Is.EqualTo(style.ErrorGlow), which);
                    Assert.That(socket.WrongNumberRingOuter, Is.EqualTo(style.WrongNumberRingOuter), which);
                    Assert.That(socket.WrongNumberStrength, Is.EqualTo(style.WrongNumberStrength), which);
                    Assert.That(socket.BlockedGlowIntensity, Is.EqualTo(style.BlockedGlowIntensity), which);
                    socket.FlashWrongNumber(false);
                    Assert.That(socket.WrongFlash.WrongNumberColor, Is.EqualTo(style.ErrorGlow), which);
                    Assert.That(socket.WrongFlash.WrongNumberRingOuter, Is.EqualTo(style.WrongNumberRingOuter), which);
                    Assert.That(socket.WrongFlash.WrongNumberStrength, Is.EqualTo(style.WrongNumberStrength), which);
                    socket.SetCallConnected(true);
                    socket.SetHighlighted(true, blocked: true);
                    var halo = socket.GetComponentInChildren<SocketHaloGraphic>();
                    Assert.That(halo.color, Is.EqualTo(style.ErrorGlow), which);
                    Assert.That(halo.Intensity, Is.EqualTo(style.BlockedGlowIntensity), which);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(style);
            }
        }
    }
}
