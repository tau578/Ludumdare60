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

        [TestCase(false)]
        [TestCase(true)]
        public void InactiveSocketsRequireHoverAndAHeldCord(bool occupied)
        {
            var root = new GameObject("Socket hover test", typeof(RectTransform));
            try
            {
                var socket = root.AddComponent<SocketView>();
                var ownerObject = new GameObject("Owner"); ownerObject.transform.SetParent(root.transform);
                var heldObject = new GameObject("Held"); heldObject.transform.SetParent(root.transform);
                var owner = ownerObject.AddComponent<CableController>();
                var held = heldObject.AddComponent<CableController>();
                if (occupied) socket.TryOccupy(owner);
                Assert.That(VisibleHalo(socket), Is.Null, "An idle socket must not glow");
                socket.SetPointerState(true, null);
                Assert.That(VisibleHalo(socket), Is.Null, "Empty-hand hover must not glow");
                socket.SetPointerState(false, held);
                Assert.That(VisibleHalo(socket), Is.Null, "Holding a wire without hovering must not glow");
                socket.SetPointerState(true, held);
                var halo = VisibleHalo(socket);
                Assert.That(halo, Is.Not.Null);
                Color expected = occupied ? new Color32(241, 87, 70, 255) : new Color32(245, 210, 120, 255);
                Assert.That(halo.color, Is.EqualTo(expected));
                Assert.That(halo.raycastTarget, Is.False);
                socket.SetPointerState(false, held);
                Assert.That(VisibleHalo(socket), Is.Null, "Pointer exit must clear the hover halo");
                socket.SetPointerState(true, held);
                socket.SetPointerState(true, null);
                Assert.That(VisibleHalo(socket), Is.Null, "Putting down the wire must clear the halo");
                if (occupied)
                {
                    socket.SetPointerState(true, owner);
                    Assert.That(socket.GetComponent<SocketGraphic>().IsBlockedHighlight, Is.False,
                        "The same wire's first plug retains its no-red exception");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

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
                Assert.That(socket.GetComponent<SocketGraphic>().IsHighlighted, Is.False);

                // A plug left in a jack that closes (the dev reset) keeps its call flag but shows no green.
                socket.SetOpen(true);
                socket.TryOccupy(owner);
                socket.SetCallConnected(true);
                Assert.That(VisibleHalo(socket), Is.Not.Null);
                socket.SetOpen(false);
                Assert.That(VisibleHalo(socket), Is.Null, "Dimmed wins over green");
                socket.SetPointerState(true, held);
                Assert.That(VisibleHalo(socket), Is.Null, "Dimmed wins over the red blocked halo");
                socket.SetOpen(true);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)new Color32(125, 255, 155, 255)), "Reopening brings the green back");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ActiveCallStaysGreenWithoutHoverAndClearsWhenTheCallEnds()
        {
            var root = new GameObject("Active call halo", typeof(RectTransform));
            try
            {
                var socket = root.AddComponent<SocketView>();
                var ownerObject = new GameObject("Owner"); ownerObject.transform.SetParent(root.transform);
                var owner = ownerObject.AddComponent<CableController>();
                socket.TryOccupy(owner);
                socket.SetCallConnected(true);
                var green = new Color32(125, 255, 155, 255);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)green));
                socket.SetPointerState(true, null);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)green));
                socket.SetPointerState(false, null);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)green));
                Assert.That(VisibleHalo(socket).Intensity, Is.GreaterThan(1));
                var otherObject = new GameObject("New wire"); otherObject.transform.SetParent(root.transform);
                var other = otherObject.AddComponent<CableController>();
                socket.SetPointerState(true, other);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)new Color32(241, 87, 70, 255)));
                socket.SetPointerState(false, other);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)green));
                socket.SetPointerState(true, owner);
                Assert.That(VisibleHalo(socket).color, Is.EqualTo((Color)green), "The same wire keeps its own-socket exception");
                socket.SetPointerState(false, null);
                socket.SetCallConnected(false);
                Assert.That(VisibleHalo(socket), Is.Null);
                socket.SetCallConnected(true);
                socket.Release(owner);
                Assert.That(VisibleHalo(socket), Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
