using NUnit.Framework;
using Switchboard.UI;

namespace Switchboard.Game.Tests
{
    public sealed class MusicDuckTests
    {
        [Test]
        public void CueFadesMusicDownAndRestoresItWithoutAPlayerOrCoroutine()
        {
            var duck = new MusicDuckEnvelope();
            Assert.That(duck.Evaluate(10), Is.EqualTo(1));
            duck.Trigger(10, 2.05f);
            Assert.That(duck.Evaluate(10), Is.EqualTo(1));
            Assert.That(duck.Evaluate(10.06f), Is.InRange(.3f, 1f));
            Assert.That(duck.Evaluate(10.2f), Is.EqualTo(.3f).Within(.001f));
            Assert.That(duck.Evaluate(11.8f), Is.EqualTo(.3f).Within(.001f));
            Assert.That(duck.Evaluate(12.05f), Is.InRange(.3f, 1f));
            Assert.That(duck.Evaluate(12.31f), Is.EqualTo(1));
        }

        [Test]
        public void AnotherCueStartsFromTheCurrentGainWithoutJumping()
        {
            var duck = new MusicDuckEnvelope();
            duck.Trigger(10, 2);
            float before = duck.Evaluate(12);
            duck.Trigger(12, 2);
            Assert.That(duck.Evaluate(12), Is.EqualTo(before).Within(.0001f));
            Assert.That(duck.Evaluate(12.2f), Is.EqualTo(.3f).Within(.001f));
            Assert.That(duck.Evaluate(15), Is.EqualTo(1));
        }
    }
}
