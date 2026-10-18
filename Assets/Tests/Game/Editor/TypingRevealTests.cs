using NUnit.Framework;

namespace Switchboard.Game.Tests
{
    public sealed class TypingRevealTests
    {
        [Test]
        public void ShowsTheFirstCharacterAtOnce()
        {
            var reveal = new TypingReveal();
            reveal.Restart(10);
            Assert.AreEqual(1, reveal.Visible);
            Assert.IsTrue(reveal.IsTyping);
        }

        [Test]
        public void RevealsCharactersAtTheGivenRate()
        {
            var reveal = new TypingReveal();
            reveal.Restart(30);
            Assert.AreEqual(5, reveal.Advance(0.1f, 40));
            Assert.AreEqual(9, reveal.Advance(0.1f, 40));
        }

        [Test]
        public void StopsAtTheEndOfTheLine()
        {
            var reveal = new TypingReveal();
            reveal.Restart(28);
            Assert.AreEqual(28, reveal.Advance(5f, 40));
            Assert.IsFalse(reveal.IsTyping);
            Assert.AreEqual(28, reveal.Advance(1f, 40));
        }

        [Test]
        public void ALongLineFinishesInUnderASecondAtTheDefaultRate()
        {
            var reveal = new TypingReveal();
            reveal.Restart(36);
            reveal.Advance(0.9f, 40);
            Assert.IsFalse(reveal.IsTyping);
        }

        [Test]
        public void RestartTypesTheNewLineFromTheStart()
        {
            var reveal = new TypingReveal();
            reveal.Restart(20);
            reveal.Advance(1f, 40);
            reveal.Restart(7);
            Assert.AreEqual(1, reveal.Visible);
            Assert.AreEqual(7, reveal.Total);
        }

        [Test]
        public void AnEmptyLineIsNeverTyping()
        {
            var reveal = new TypingReveal();
            reveal.Restart(0);
            Assert.AreEqual(0, reveal.Visible);
            Assert.IsFalse(reveal.IsTyping);
            Assert.AreEqual(0, reveal.Advance(1f, 40));
        }

        [Test]
        public void AFinishedLineStaysFinishedWhenItGrows()
        {
            var reveal = new TypingReveal();
            reveal.Restart(20);
            reveal.Advance(5f, 40);
            reveal.Resize(25);
            Assert.AreEqual(25, reveal.Visible);
            Assert.IsFalse(reveal.IsTyping);
        }

        [Test]
        public void ALineStillTypingGoesOnFromWhereItWas()
        {
            var reveal = new TypingReveal();
            reveal.Restart(30);
            reveal.Advance(0.1f, 40);
            reveal.Resize(36);
            Assert.AreEqual(5, reveal.Visible);
            Assert.AreEqual(36, reveal.Total);
            Assert.AreEqual(9, reveal.Advance(0.1f, 40));
        }
    }
}
