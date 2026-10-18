using NUnit.Framework;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    // A closed jack (D42) is dimmed, takes no plug and never lights its row or column lamp.
    public sealed class ClosedSocketTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        private SocketView Socket(int row, int column)
        {
            if (root == null) root = new GameObject("Board", typeof(RectTransform));
            var go = new GameObject("Socket", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var socket = go.AddComponent<SocketView>();
            var anchor = new GameObject("Anchor", typeof(RectTransform)).GetComponent<RectTransform>();
            anchor.SetParent(go.transform, false);
            socket.Configure(row, column, anchor);
            return socket;
        }

        private CableController Cable()
        {
            var go = new GameObject("Cable", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            return go.AddComponent<CableController>();
        }

        [Test]
        public void AClosedSocketRefusesACordAndAnOpenOneTakesIt()
        {
            SocketView closed = Socket(3, 3);
            SocketView open = Socket(0, 0);
            CableController cable = Cable();
            closed.SetOpen(false);

            Assert.That(closed.IsOpen, Is.False);
            Assert.That(closed.GetComponent<SocketGraphic>().IsDimmed, Is.True);
            Assert.That(cable.TryInsert(closed), Is.False);
            Assert.That(closed.IsOccupied, Is.False);
            Assert.That(cable.FirstSocket, Is.Null);

            Assert.That(cable.TryInsert(open), Is.True);
            Assert.That(open.IsOccupied, Is.True);

            closed.SetOpen(true);
            Assert.That(closed.GetComponent<SocketGraphic>().IsDimmed, Is.False);
            Assert.That(cable.TryInsert(closed), Is.True, "an opened socket takes the second plug");
        }

        [Test]
        public void AClosedSocketIgnoresHoverAndItsLampStaysDarkAndDimmed()
        {
            SocketView socket = Socket(4, 0);
            CableController cable = Cable();
            var lampObject = new GameObject("Lamp_E", typeof(RectTransform));
            lampObject.transform.SetParent(root.transform, false);
            var bulb = lampObject.AddComponent<Image>();
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(lampObject.transform, false);
            var text = labelObject.AddComponent<Text>();
            var lamp = lampObject.AddComponent<SwitchboardLampView>();
            lamp.Configure(bulb, null, null, null, null, null, new[] { socket });
            Color bulbColor = bulb.color, textColor = text.color;

            socket.SetPointerState(true, cable);
            lamp.Refresh();
            Assert.That(socket.IsNavigationHighlighted, Is.EqualTo(socket.isActiveAndEnabled), "an open socket lights its lamp");
            Assert.That(lamp.IsDimmed, Is.False);

            socket.SetOpen(false);
            socket.SetPointerState(true, cable);
            lamp.Refresh();
            Assert.That(socket.IsNavigationHighlighted, Is.False);
            Assert.That(socket.GetComponent<SocketGraphic>().IsHighlighted, Is.False);
            Assert.That(lamp.IsLit, Is.False);
            Assert.That(lamp.IsDimmed, Is.True);
            Assert.That(bulb.color, Is.Not.EqualTo(bulbColor));
            Assert.That(text.color, Is.Not.EqualTo(textColor), "a plain-text label dims too");

            socket.SetOpen(true);
            lamp.Refresh();
            Assert.That(lamp.IsDimmed, Is.False);
            Assert.That(bulb.color, Is.EqualTo(bulbColor));
            Assert.That(text.color, Is.EqualTo(textColor));
        }
    }
}
