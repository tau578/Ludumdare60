using NUnit.Framework;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    public sealed class CableTrayLayoutTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void CablesFitInsideTrayWithoutOverlapping(int count)
        {
            Vector2 size = CableInputController.TraySlotSize(count);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = CableInputController.TraySlotPosition(i, count);
                Assert.That(Mathf.Abs(position.x) + size.x / 2, Is.LessThanOrEqualTo(415));
                Assert.That(position.y - size.y / 2, Is.GreaterThanOrEqualTo(-240));
                Assert.That(position.y + size.y / 2, Is.LessThanOrEqualTo(-60));
                for (int j = 0; j < i; j++)
                {
                    Vector2 delta = position - CableInputController.TraySlotPosition(j, count);
                    Assert.That(Mathf.Abs(delta.x) >= size.x || Mathf.Abs(delta.y) >= size.y, Is.True);
                }
            }
            Assert.That(560 * CableInputController.TrayPreviewScaleFor(count), Is.EqualTo(size.x).Within(.001f));
            if (count > 3)
                Assert.That(CableInputController.TraySlotPosition(3, count).y,
                    Is.LessThan(CableInputController.TraySlotPosition(0, count).y));
        }
    }
}
