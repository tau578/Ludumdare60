using NUnit.Framework;

namespace Switchboard.Game.Tests
{
    public sealed class FirstOrderGuideTests
    {
        [Test]
        public void OrderedStepsWaitForTextAndBookCloseThenStopOnAnyCable()
        {
            var guide = new FirstOrderGuideSequence();
            guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Portrait));
            guide.FirstPortraitClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText));
            guide.FirstPortraitClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText));
            guide.TaskTextFinished();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book));
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook));
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            guide.CablePickedUp();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
            guide.FirstOrderShown(); guide.BookChanged(true); guide.CablePickedUp();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void EarlyExplorationKeepsOnlyPortraitHint(bool beforeArrival, bool cable)
        {
            var guide = new FirstOrderGuideSequence();
            if (!beforeArrival) guide.FirstOrderShown();
            if (cable) guide.CablePickedUp();
            else { guide.BookChanged(true); guide.BookChanged(false); }
            if (beforeArrival) guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Portrait));
            guide.FirstPortraitClicked();
            guide.TaskTextFinished(); guide.BookChanged(true); guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
        }

        [TestCase(FirstOrderGuideStage.TaskText)]
        [TestCase(FirstOrderGuideStage.Book)]
        [TestCase(FirstOrderGuideStage.ReadingBook)]
        public void TakingWireBeforeBookWasClosedCancelsRemainder(FirstOrderGuideStage stage)
        {
            var guide = Accepted();
            if (stage != FirstOrderGuideStage.TaskText) guide.TaskTextFinished();
            if (stage == FirstOrderGuideStage.ReadingBook) guide.BookChanged(true);
            guide.CablePickedUp();
            guide.BookChanged(false); guide.TaskTextFinished();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
        }

        [Test]
        public void BookOpenedBeforeTypingFinishedDoesNotSkipTypingStep()
        {
            var guide = Accepted();
            guide.BookChanged(true); guide.TaskTextFinished(); guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
        }

        [Test]
        public void EndedFirstOrderNeverTransfersHintToNextAndNewSessionStartsFresh()
        {
            var guide = Accepted();
            guide.Cancel(); guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            guide = new FirstOrderGuideSequence(); guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Portrait));
        }

        private static FirstOrderGuideSequence Accepted()
        {
            var guide = new FirstOrderGuideSequence();
            guide.FirstOrderShown(); guide.FirstPortraitClicked();
            return guide;
        }
    }
}
