using NUnit.Framework;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    // The cord input lives in Assembly-CSharp, so this test sits beside the book's tests (Assembly-CSharp-Editor).
    public sealed class CableTrayLayoutTests
    {
        private const float UsableWidth = 830;

        [Test]
        public void ThreeSlotsKeepTodaysLayout()
        {
            Assert.That(CableInputController.TraySlotPosition(0, 3), Is.EqualTo(new Vector2(-270, -140)));
            Assert.That(CableInputController.TraySlotPosition(1, 3), Is.EqualTo(new Vector2(0, -140)));
            Assert.That(CableInputController.TraySlotPosition(2, 3), Is.EqualTo(new Vector2(270, -140)));
            Assert.That(CableInputController.TraySlotSize(3), Is.EqualTo(new Vector2(252, 94.5f)));
            Assert.That(CableInputController.TrayPreviewScaleFor(3), Is.EqualTo(.45f).Within(1e-6));
        }

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void MoreSlotsFitTheFeltWithNoOverlap(int count)
        {
            Vector2 size = CableInputController.TraySlotSize(count);
            float spacing = CableInputController.TraySpacing(count);
            Assert.That(size.x, Is.LessThan(spacing), "neighbouring hit areas don't overlap");
            float left = CableInputController.TraySlotPosition(0, count).x - size.x / 2;
            float right = CableInputController.TraySlotPosition(count - 1, count).x + size.x / 2;
            Assert.That(left, Is.GreaterThanOrEqualTo(-UsableWidth / 2 - 1e-3));
            Assert.That(right, Is.LessThanOrEqualTo(UsableWidth / 2 + 1e-3));
            Assert.That(CableInputController.TraySlotPosition(0, count).x + CableInputController.TraySlotPosition(count - 1, count).x,
                Is.EqualTo(0).Within(1e-3), "the row stays centred");
            // The preview shrinks with the slot, so a cord never draws wider than its slot.
            Assert.That(560 * CableInputController.TrayPreviewScaleFor(count), Is.EqualTo(size.x).Within(1e-3));
        }
    }
}
