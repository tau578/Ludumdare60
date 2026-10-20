using NUnit.Framework;

namespace Switchboard.Game.Tests
{
    public sealed class FirstOrderGuideTests
    {
        [Test]
        public void OrderedStepsWaitForTextAndBookCloseThenCordJacksMoneyAndTheShop()
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
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Plugging));
            guide.CablePickedUp();
            guide.CordsOut(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Plugging), "moving a plug keeps the step");
            Assert.That(guide.IsFirstCallOver, Is.False);
            guide.FirstOrderConnected();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected));
            Assert.That(guide.IsGuiding && guide.IsFirstCallOver, Is.True);
            guide.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.IsGuiding || guide.IsOver, Is.False, "normal play until the first cord is affordable");
            guide.CordAffordable(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
            Assert.That(guide.IsGuiding, Is.True);
            guide.CordBought(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery));
            guide.Tick(FirstOrderGuideSequence.MysterySeconds);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
            Assert.That(guide.IsGuiding, Is.False);
            guide.FirstOrderShown(); guide.BookChanged(true); guide.CablePickedUp(); guide.CordAffordable(true); guide.Skip();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
        }

        [Test]
        public void ConnectedStepEndsOnAClickButNotOnThePluggingClick()
        {
            var guide = Plugging();
            guide.FirstOrderConnected();
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "the click that plugged the jack");
            guide.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
        }

        [Test]
        public void TheCordStepAppearsOnlyWhileAffordableAndEndsOnPurchase()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.CordAffordable(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            guide.CordAffordable(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
            guide.AnyClick(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "only the purchase ends it");
            guide.CordAffordable(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "a fine took the money: wait again");
            guide.CordAffordable(true);
            guide.CordBought(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery));
        }

        [Test]
        public void TheMysteryStepEndsOnAClickButNotOnThePurchaseClick()
        {
            var guide = At(FirstOrderGuideStage.Mystery);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "the click that bought the cord");
            guide.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
        }

        [Test]
        public void NoCordOrNoMysteryToShowSkipsTheShopSteps()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.ShopUnavailable();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
            guide = At(FirstOrderGuideStage.BuyCord);
            guide.CordBought(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "no mystery upgrade in the file");
        }

        [Test]
        public void CheckingTheBookAgainReturnsToTheStepItLeft()
        {
            // The guide keeps the book shut after the book step; if it opens anyway, the sequence still recovers.
            var guide = BookRead();
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook), "from Take a cord");
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            guide.CablePickedUp();
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook), "from Plug the jacks");
            guide.CordsOut(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Plugging));
            guide.BookChanged(true);
            guide.CordsOut(false);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables), "the cord went back while the book was open");
        }

        [Test]
        public void PuttingTheCordBackAsksForItAgain()
        {
            var guide = Plugging();
            guide.CordsOut(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            guide.CablePickedUp();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Plugging));
        }

        [TestCase(FirstOrderGuideStage.Portrait)]
        [TestCase(FirstOrderGuideStage.TaskText)]
        [TestCase(FirstOrderGuideStage.ReadingBook)]
        [TestCase(FirstOrderGuideStage.Plugging)]
        [TestCase(FirstOrderGuideStage.Connected)]
        [TestCase(FirstOrderGuideStage.WaitingForShop)]
        [TestCase(FirstOrderGuideStage.BuyCord)]
        [TestCase(FirstOrderGuideStage.Mystery)]
        public void SkipEndsEveryStepForGood(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            Assert.That(guide.Stage, Is.EqualTo(stage), "setup");
            guide.Skip();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(guide.IsOver, Is.True);
            guide.BookChanged(false); guide.CablePickedUp(); guide.FirstOrderConnected(); guide.CordsOut(false);
            guide.CordAffordable(true); guide.CordBought(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
        }

        [TestCase(FirstOrderGuideStage.Portrait, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Book, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.ReadingBook, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Cables, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Plugging, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Connected, FirstOrderGuideStage.WaitingForShop)]
        [TestCase(FirstOrderGuideStage.BuyCord, FirstOrderGuideStage.BuyCord)]
        [TestCase(FirstOrderGuideStage.Mystery, FirstOrderGuideStage.Mystery)]
        public void TheGuidedCallEndingEndsOnlyItsOwnSteps(FirstOrderGuideStage stage, FirstOrderGuideStage after)
        {
            // Before connecting only a dev "Clear calls" can end it now; after it, the call simply finishes.
            var guide = At(stage);
            guide.FirstOrderEnded();
            Assert.That(guide.Stage, Is.EqualTo(after));
            guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(after), "the next call gets no guide");
        }

        // Fix 1: the tutorial is mandatory, so acting ahead can't happen; an out-of-step event changes nothing.
        [TestCase(FirstOrderGuideStage.Portrait)]
        [TestCase(FirstOrderGuideStage.TaskText)]
        [TestCase(FirstOrderGuideStage.Book)]
        [TestCase(FirstOrderGuideStage.Cables)]
        public void OutOfStepEventsNeitherSkipNorEndTheGuide(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            if (stage != FirstOrderGuideStage.Cables) guide.CablePickedUp();
            if (stage != FirstOrderGuideStage.TaskText) guide.TaskTextFinished();
            if (stage != FirstOrderGuideStage.Book) { guide.BookChanged(true); guide.BookChanged(false); }
            if (stage != FirstOrderGuideStage.Portrait) guide.FirstPortraitClicked();
            guide.CordAffordable(true); guide.CordBought(true); guide.AnyClick(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(stage));
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

        private static FirstOrderGuideSequence BookRead()
        {
            var guide = Accepted();
            guide.TaskTextFinished(); guide.BookChanged(true); guide.BookChanged(false);
            return guide;
        }

        private static FirstOrderGuideSequence Plugging()
        {
            var guide = BookRead();
            guide.CablePickedUp(); guide.CordsOut(true);
            return guide;
        }

        private static FirstOrderGuideSequence At(FirstOrderGuideStage stage)
        {
            var guide = new FirstOrderGuideSequence();
            guide.FirstOrderShown();
            if (stage == FirstOrderGuideStage.Portrait) return guide;
            guide.FirstPortraitClicked();
            if (stage == FirstOrderGuideStage.TaskText) return guide;
            guide.TaskTextFinished();
            if (stage == FirstOrderGuideStage.Book) return guide;
            guide.BookChanged(true);
            if (stage == FirstOrderGuideStage.ReadingBook) return guide;
            guide.BookChanged(false);
            if (stage == FirstOrderGuideStage.Cables) return guide;
            guide.CablePickedUp();
            if (stage == FirstOrderGuideStage.Plugging) return guide;
            guide.FirstOrderConnected();
            if (stage == FirstOrderGuideStage.Connected) return guide;
            guide.Tick(FirstOrderGuideSequence.ConnectedSeconds);
            if (stage == FirstOrderGuideStage.WaitingForShop) return guide;
            guide.CordAffordable(true);
            if (stage == FirstOrderGuideStage.BuyCord) return guide;
            guide.CordBought(true);
            return guide;
        }

        private static FirstOrderGuideSequence Accepted()
        {
            var guide = new FirstOrderGuideSequence();
            guide.FirstOrderShown(); guide.FirstPortraitClicked();
            return guide;
        }
    }
}
