using NUnit.Framework;
using Switchboard.Domain;

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
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText), "fix 2: it waits for a click to move on");
            Assert.That(guide.IsTaskTextRead, Is.True);
            guide.Tick(FirstOrderGuideSequence.ReadClickDelay);
            guide.AnyClick();
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
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
            Assert.That(guide.IsGuiding, Is.True);
            Poll(guide, ShopOffer.Owned);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "step 9 follows the purchase at once");
            guide.Tick(FirstOrderGuideSequence.MysterySeconds);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "a short gap between hints");
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit), "Profit before Clients");
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "bought: done");
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients));
            guide.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "dismissed: every hint is done");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.All));
            Assert.That(guide.IsGuiding, Is.False);
            guide.FirstOrderShown(); guide.BookChanged(true); guide.CablePickedUp(); Poll(guide, ShopOffer.Affordable); guide.Skip();
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
            Poll(guide, ShopOffer.TooExpensive);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Poll(guide, ShopOffer.Affordable, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the book is open, a cord is held or paused");
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
            guide.AnyClick(); guide.Tick(60);
            Poll(guide, ShopOffer.Affordable, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "only the purchase ends it");
            Poll(guide, ShopOffer.TooExpensive);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "a fine took the money: wait again");
            Poll(guide, ShopOffer.Affordable);
            Poll(guide, ShopOffer.Owned);
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
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "More Profit and More Clients may follow");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery));
        }

        [Test]
        public void NoCordOrNoMysteryToShowSkipsTheShopSteps()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            Poll(guide, ShopOffer.NotForSale, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "More Profit and More Clients may follow");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery), "no cord to sell: no step 9 either");
            Poll(guide, ShopOffer.NotForSale, ShopOffer.Affordable, ShopOffer.NotForSale, ShopOffer.NotForSale);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "nothing left to show");
            guide = At(FirstOrderGuideStage.BuyCord);
            Poll(guide, ShopOffer.Owned, ShopOffer.NotForSale);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery), "no mystery upgrade in the file");
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
        [TestCase(FirstOrderGuideStage.Profit)]
        [TestCase(FirstOrderGuideStage.Clients)]
        public void SkipEndsEveryStepForGood(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            Assert.That(guide.Stage, Is.EqualTo(stage), "setup");
            guide.Skip();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(guide.IsOver, Is.True);
            guide.BookChanged(false); guide.CablePickedUp(); guide.FirstOrderConnected(); guide.CordsOut(false);
            Poll(guide, ShopOffer.Affordable); Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.All), "skipping skips the shop hints too");
        }

        [TestCase(FirstOrderGuideStage.Portrait, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Book, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.ReadingBook, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Cables, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Plugging, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Connected, FirstOrderGuideStage.WaitingForShop)]
        [TestCase(FirstOrderGuideStage.BuyCord, FirstOrderGuideStage.BuyCord)]
        [TestCase(FirstOrderGuideStage.Mystery, FirstOrderGuideStage.Mystery)]
        [TestCase(FirstOrderGuideStage.Profit, FirstOrderGuideStage.Profit)]
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
            Poll(guide, ShopOffer.Affordable); Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable);
            guide.AnyClick(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(stage));
        }

        [Test]
        public void TheMessageWaitsForAClickOnceTypedAndAnEarlyClickIsSwallowed()
        {
            var guide = Accepted();
            guide.Tick(30); guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText), "still typing: a click does nothing");
            Assert.That(guide.IsTaskTextRead, Is.False);
            guide.TaskTextFinished();
            guide.AnyClick();
            guide.Tick(FirstOrderGuideSequence.ReadClickDelay * .5f);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.TaskText), "the guard swallows a double-click");
            guide.TaskTextFinished();
            guide.Tick(FirstOrderGuideSequence.ReadClickDelay * .5f);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "the guard runs from the end of the text, once");
        }

        [Test]
        public void ProfitAndClientsWaitForTheFirstCallAndComeOneAtATimeWithAGap()
        {
            foreach (FirstOrderGuideStage stage in new[] { FirstOrderGuideStage.Portrait, FirstOrderGuideStage.Plugging,
                         FirstOrderGuideStage.Connected })
            {
                var during = At(stage);
                Poll(during, ShopOffer.Affordable, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
                Assert.That(during.Stage, Is.EqualTo(stage), "never during the first-call guide");
            }
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            Poll(guide, ShopOffer.Affordable, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "the cord step comes first");
            Poll(guide, ShopOffer.Affordable, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "one hint at a time");

            // Cord too dear, Profit affordable: Profit needn't wait for the cord.
            guide = At(FirstOrderGuideStage.WaitingForShop);
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "not with the book open, a cord held or paused");
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.TooExpensive, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients));
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients), "a hint that shows stays");
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients), "a click as it appears doesn't dismiss it");
            guide.Tick(FirstOrderGuideSequence.ConnectedClickDelay);
            guide.AnyClick();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Clients), "dismissed is done");
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the gap");
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit));
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.TooExpensive, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the money went: wait again");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Clients), "not done yet");
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit));
        }

        [Test]
        public void AnOwnedOrUnsoldUpgradeNeverGetsItsHint()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Owned, clients: ShopOffer.NotForSale);
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Profit | ShopHints.Clients));
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "done for good");
        }

        [Test]
        public void ContinueSkipsTheFirstCallAndShowsOnlyTheHintsNotDone()
        {
            var guide = new FirstOrderGuideSequence(ShopHints.Cord | ShopHints.Mystery, firstCall: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.IsGuiding, Is.False);
            guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "no first-call guide");
            Poll(guide, ShopOffer.Affordable, profit: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit), "the cord step was done before the reload");

            guide = new FirstOrderGuideSequence(ShopHints.None, firstCall: false);
            guide.MarkShopHintsDone(ShopHints.All);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "an old save: nothing left");
            Assert.That(new FirstOrderGuideSequence(ShopHints.All, firstCall: false).IsOver, Is.True);

            guide = new FirstOrderGuideSequence(ShopHints.Cord, firstCall: false);
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "a reload during step 9 shows it again");
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

        // What FirstOrderGuide.PollShop does each frame: cord, mystery, More Profit, More Clients, in that order.
        private static void Poll(FirstOrderGuideSequence guide, ShopOffer cord, ShopOffer mystery = ShopOffer.Affordable,
            ShopOffer profit = ShopOffer.TooExpensive, ShopOffer clients = ShopOffer.TooExpensive, bool canStart = true)
        {
            guide.Offer(ShopHints.Cord, cord, canStart);
            guide.Offer(ShopHints.Mystery, mystery, canStart);
            guide.Offer(ShopHints.Profit, profit, canStart);
            guide.Offer(ShopHints.Clients, clients, canStart);
        }

        private static void ReadMessage(FirstOrderGuideSequence guide)
        {
            guide.TaskTextFinished();
            guide.Tick(FirstOrderGuideSequence.ReadClickDelay);
            guide.AnyClick();
        }

        private static FirstOrderGuideSequence BookRead()
        {
            var guide = Accepted();
            ReadMessage(guide); guide.BookChanged(true); guide.BookChanged(false);
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
            ReadMessage(guide);
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
            Poll(guide, ShopOffer.Affordable);
            if (stage == FirstOrderGuideStage.BuyCord) return guide;
            Poll(guide, ShopOffer.Owned);
            if (stage == FirstOrderGuideStage.Mystery) return guide;
            guide.Tick(FirstOrderGuideSequence.MysterySeconds);
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable);
            if (stage == FirstOrderGuideStage.Profit) return guide;
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned);
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned, clients: ShopOffer.Affordable);
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
