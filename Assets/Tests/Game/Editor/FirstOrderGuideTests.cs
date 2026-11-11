using NUnit.Framework;
using Switchboard.Domain;

namespace Switchboard.Game.Tests
{
    public sealed class FirstOrderGuideTests
    {
        // 13a pass 6 (D81): the caller's side first (card, cord, the caller's blinking jack), then the recipient's (book,
        // row, the marked jack), each step ending on its action; only "Connected" and then "Earning" (13a pass 8) wait for OK.
        [Test]
        public void TheFirstCallGoesCallerSideThenRecipientSideThenMoneyAndTheShop()
        {
            var guide = new FirstOrderGuideSequence();
            guide.FirstOrderShown();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Portrait));
            guide.FirstPortraitClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables), "the card taken: a cord, no reading step");
            guide.FirstPortraitClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            Assert.That(guide.IsGuiding && !guide.IsFirstCallOver && !guide.IsInformation, Is.True);
            guide.CablePickedUp();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "the caller's blinking jack");
            guide.CablePickedUp();
            guide.CordsOut(true);
            guide.CallerJackPlugged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "moving a cord keeps the step");
            guide.CallerJackPlugged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "then the recipient's side: the book");
            guide.CordsOut(true);
            guide.CallerJackPlugged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "the plug stays in: the step stays");
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook));
            guide.CallerJackPlugged(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient), "the row clicked: the marked jack");
            guide.CordsOut(true);
            guide.CallerJackPlugged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient));
            Assert.That(guide.IsGuiding && guide.IsPlugging && !guide.IsFirstCallOver, Is.True);
            guide.FirstOrderConnected();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected));
            Assert.That(guide.IsGuiding && guide.IsFirstCallOver, Is.True);
            Ok(guide);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "13a pass 8: the tab, then the money");
            Assert.That(guide.IsGuiding && guide.IsInformation, Is.True);
            Ok(guide);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.IsGuiding || guide.IsOver, Is.False, "normal play until the first cord is affordable");
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));
            Assert.That(guide.IsGuiding, Is.True);
            Poll(guide, ShopOffer.Owned);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "the mystery step follows the purchase at once");
            Ok(guide);
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
            guide.Tick(FirstOrderGuideSequence.OkClickDelay);
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients), "13a pass 8: a purchase step, OK does nothing");
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned, clients: ShopOffer.Owned);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "bought: only the emergency step is left");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.All & ~ShopHints.Emergency));
            guide.EmergencyRinging(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Emergency), "13a pass 4: the first emergency");
            guide.EmergencyRinging(false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete), "taken: every hint is done");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.All));
            Assert.That(guide.IsGuiding, Is.False);
            guide.FirstOrderShown(); guide.BookChanged(true); guide.CablePickedUp(); Poll(guide, ShopOffer.Affordable); guide.Skip();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
        }

        [Test]
        public void ConnectedStepEndsOnOkButNotOnThePluggingClickNorByItself()
        {
            var guide = At(FirstOrderGuideStage.PluggingRecipient);
            guide.FirstOrderConnected();
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "the click that plugged the jack");
            guide.Tick(600);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "13a pass 5: no timed ending");
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "13a pass 8: then the money, its own OK step");
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "the guard: a double-click skips nothing");
            guide.Tick(600);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Earning), "no timed ending");
            guide.OkClicked();
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
            guide.OkClicked(); guide.Tick(60);
            Poll(guide, ShopOffer.Affordable, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "only the purchase ends it");
            Poll(guide, ShopOffer.TooExpensive);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "a fine took the money: wait again");
            Poll(guide, ShopOffer.Affordable);
            Poll(guide, ShopOffer.Owned);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery));
        }

        [Test]
        public void TheMysteryStepEndsOnOkButNotOnThePurchaseClickNorByItself()
        {
            var guide = At(FirstOrderGuideStage.Mystery);
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "the click that bought the cord");
            guide.Tick(600);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "13a pass 5: no timed ending");
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "More Profit and More Clients may follow");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery));
        }

        [Test]
        public void NoCordOrNoMysteryToShowSkipsTheShopSteps()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            Poll(guide, ShopOffer.NotForSale, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "More Profit and More Clients may follow");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery), "no cord to sell: no step 10 either");
            Poll(guide, ShopOffer.NotForSale, ShopOffer.Affordable, ShopOffer.NotForSale, ShopOffer.NotForSale);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "nothing left to show in the shop");
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.All & ~ShopHints.Emergency), "only the emergency step is left");
            guide = At(FirstOrderGuideStage.BuyCord);
            Poll(guide, ShopOffer.Owned, ShopOffer.NotForSale);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Cord | ShopHints.Mystery), "no mystery upgrade in the file");
        }

        [Test]
        public void CheckingTheBookAgainReturnsToTheStepItLeft()
        {
            // The guide keeps the book shut outside its step; if it opens anyway, the sequence still recovers.
            var guide = At(FirstOrderGuideStage.Cables);
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook), "from Take a cord");
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            guide.CablePickedUp();
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook), "from the caller's jack");
            guide.CordsOut(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller));
            guide.CallerJackPlugged(true);
            guide.BookChanged(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient), "the book step");
            guide.BookChanged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.ReadingBook), "from the marked jack");
            guide.CallerJackPlugged(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient));
            guide.BookChanged(true);
            guide.CallerJackPlugged(false);
            guide.CordsOut(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "the caller's plug came out while it was open");
            guide.CallerJackPlugged(true);
            guide.BookChanged(true);
            guide.CordsOut(false);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables), "the cord went back while the book was open");
        }

        [Test]
        public void PuttingTheCordBackAsksForItAgain()
        {
            var guide = At(FirstOrderGuideStage.PluggingCaller);
            guide.CordsOut(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables));
            guide.CablePickedUp();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller));
            guide.CallerJackPlugged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "setup");
            guide.CordsOut(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables), "from the book step");
            guide = At(FirstOrderGuideStage.PluggingRecipient);
            guide.CordsOut(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cables), "from the marked jack too");
        }

        [Test]
        public void PluggingGoesOneJackAtATimeCallerFirstWithTheBookBetween()
        {
            var guide = At(FirstOrderGuideStage.PluggingCaller);
            guide.OkClicked(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "only the plug moves it on");
            guide.CallerJackPlugged(true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book));
            guide.OkClicked(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Book), "only opening the book moves it on");
            guide.CallerJackPlugged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "the caller's plug came out: back to it");
            guide.CallerJackPlugged(true);
            guide.BookChanged(true);
            guide.BookChanged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient));
            guide.OkClicked(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingRecipient), "only the connection ends it");
            guide.CallerJackPlugged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.PluggingCaller), "the caller's plug came out: back to it");
            guide.CallerJackPlugged(true);
            guide.BookChanged(true);
            guide.BookChanged(false);
            guide.FirstOrderConnected();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected));
            guide.CallerJackPlugged(false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Connected), "the plug only counts while plugging");

            foreach (FirstOrderGuideStage stage in new[] { FirstOrderGuideStage.Portrait, FirstOrderGuideStage.Cables })
            {
                var early = At(stage);
                early.CallerJackPlugged(true);
                Assert.That(early.Stage, Is.EqualTo(stage), "a plug in the caller's jack skips nothing");
            }
        }

        [TestCase(FirstOrderGuideStage.Portrait)]
        [TestCase(FirstOrderGuideStage.Cables)]
        [TestCase(FirstOrderGuideStage.Book)]
        [TestCase(FirstOrderGuideStage.ReadingBook)]
        [TestCase(FirstOrderGuideStage.PluggingCaller)]
        [TestCase(FirstOrderGuideStage.PluggingRecipient)]
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
            guide.BookChanged(false); guide.CablePickedUp(); guide.CallerJackPlugged(true); guide.FirstOrderConnected();
            guide.CordsOut(false);
            Poll(guide, ShopOffer.Affordable); Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.All), "skipping skips the shop hints too");
        }

        [TestCase(FirstOrderGuideStage.Portrait, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Book, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.ReadingBook, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.Cables, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.PluggingCaller, FirstOrderGuideStage.Cancelled)]
        [TestCase(FirstOrderGuideStage.PluggingRecipient, FirstOrderGuideStage.Cancelled)]
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
        [TestCase(FirstOrderGuideStage.Cables)]
        [TestCase(FirstOrderGuideStage.PluggingCaller)]
        [TestCase(FirstOrderGuideStage.Book)]
        public void OutOfStepEventsNeitherSkipNorEndTheGuide(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            if (stage != FirstOrderGuideStage.Cables) guide.CablePickedUp();
            if (stage != FirstOrderGuideStage.Book) { guide.BookChanged(true); guide.BookChanged(false); }
            if (stage != FirstOrderGuideStage.Portrait) guide.FirstPortraitClicked();
            Poll(guide, ShopOffer.Affordable); Poll(guide, ShopOffer.Owned, profit: ShopOffer.Affordable);
            guide.NextCallerPolled(true, true, true, true);
            guide.OkClicked(); guide.Tick(60);
            Assert.That(guide.Stage, Is.EqualTo(stage));
        }

        // 13a pass 6 (D81): the first call has no reading steps any more; every step before "Connected" asks for an
        // action, so none shows OK, and nothing but its action moves it on.
        [Test]
        public void TheFirstCallHasNoReadingStepsAndNoOkBeforeConnected()
        {
            var seen = new System.Collections.Generic.List<FirstOrderGuideStage>();
            var guide = new FirstOrderGuideSequence();
            void Note()
            {
                if (seen.Count == 0 || seen[seen.Count - 1] != guide.Stage) seen.Add(guide.Stage);
                if (guide.Stage != FirstOrderGuideStage.Connected)
                    Assert.That(guide.IsInformation, Is.False, $"{guide.Stage}: an action step, no OK");
                guide.Tick(600); guide.OkClicked();
                if (seen[seen.Count - 1] != FirstOrderGuideStage.Connected)
                    Assert.That(guide.Stage, Is.EqualTo(seen[seen.Count - 1]), "neither time nor OK moves it on");
            }
            guide.FirstOrderShown(); Note();
            guide.FirstPortraitClicked(); Note();
            guide.CablePickedUp(); Note();
            guide.CallerJackPlugged(true); Note();
            guide.BookChanged(true); Note();
            guide.BookChanged(false); Note();
            guide.FirstOrderConnected();
            seen.Add(guide.Stage);
            Assert.That(seen, Is.EqualTo(new[] { FirstOrderGuideStage.Portrait, FirstOrderGuideStage.Cables,
                FirstOrderGuideStage.PluggingCaller, FirstOrderGuideStage.Book, FirstOrderGuideStage.ReadingBook,
                FirstOrderGuideStage.PluggingRecipient, FirstOrderGuideStage.Connected }));
            Assert.That(guide.IsInformation, Is.True, "Connected keeps its OK");
        }

        [Test]
        public void ProfitAndClientsWaitForTheFirstCallAndComeOneAtATimeWithAGap()
        {
            foreach (FirstOrderGuideStage stage in new[] { FirstOrderGuideStage.Portrait, FirstOrderGuideStage.PluggingCaller,
                         FirstOrderGuideStage.PluggingRecipient, FirstOrderGuideStage.Connected })
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
            guide.Tick(60);
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Clients), "13a pass 8: a purchase step, OK doesn't end it");
            Poll(guide, ShopOffer.TooExpensive, profit: ShopOffer.Affordable, clients: ShopOffer.Owned);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.ShopHintsDone, Is.EqualTo(ShopHints.Clients), "bought is done");
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
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Mystery), "a reload during step 10 shows it again");
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
        // 13a pass 3 (D81): the first call is over and its cord still plugged: one step asks for a click on a plug.
        [TestCase(FirstOrderGuideStage.Connected)]
        [TestCase(FirstOrderGuideStage.WaitingForShop)]
        public void AFinishedFirstCallsPluggedCordGetsTheFreeCordStepWhichEndsOnThePickup(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            guide.FirstOrderEnded(cordStillPlugged: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "set up on the next poll");
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord));
            Assert.That(guide.IsGuiding && guide.IsFirstCallOver && !guide.IsOver, Is.True, "a mandatory step");
            guide.OkClicked(); guide.Tick(60); Poll(guide, ShopOffer.Affordable);
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord), "only the pickup ends it");
            guide.CablePickedUp();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "13a pass 5: the next caller comes first");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCard));
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCaller));
            guide.NextCallerPolled(cordInHand: false, callerWaiting: true, cardTaken: true, canStart: true);
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "once only: the cord plugged into a new call");
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "the shop hints follow");
        }

        [Test]
        public void TheFreeCordStepWaitsForCalmPlayAndComesBeforeAnyShopHint()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.FirstOrderEnded(cordStillPlugged: true);
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "no shop hint jumps the queue");
            guide.FinishedCordPlugged(true, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the book open, a cord held or paused");
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord));
        }

        [Test]
        public void NoPluggedCordNoFreeCordStepAndACordLeavingOtherwiseEndsIt()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.FirstOrderEnded(cordStillPlugged: false);
            guide.FinishedCordPlugged(false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord));

            guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.FirstOrderEnded(cordStillPlugged: true);
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord), "setup");
            guide.FinishedCordPlugged(false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "never stuck on a cord that is gone");

            guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.FirstOrderEnded(cordStillPlugged: true);
            guide.FinishedCordPlugged(true, canStart: true);
            guide.Skip();
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled), "Skip still ends everything");
        }

        // 13a pass 4 (D81): the first emergency gets its own step; it ends when the card is taken, and only once.
        [Test]
        public void TheFirstEmergencyGetsItsStepWhichEndsWhenTheCardIsTakenAndNeverComesAgain()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.EmergencyRinging(true, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "not while paused");
            guide.EmergencyRinging(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Emergency));
            Assert.That(guide.IsGuiding, Is.True, "a step on screen: the clock holds");
            guide.Tick(60);
            guide.OkClicked();
            guide.CablePickedUp();
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Emergency), "only taking the card ends it");
            guide.EmergencyRinging(false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            Assert.That(guide.ShopHintsDone & ShopHints.Emergency, Is.EqualTo(ShopHints.Emergency), "saved with the shop hints");
            guide.EmergencyRinging(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "once per save");
        }

        [Test]
        public void TheEmergencyStepWaitsForAShowingHintThenComesBeforeTheOthersWithNoGap()
        {
            var guide = At(FirstOrderGuideStage.Profit);
            guide.EmergencyRinging(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Profit), "it waits for the showing hint");
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop));
            guide.FirstOrderEnded(cordStillPlugged: true);
            guide.EmergencyRinging(true, canStart: true);
            guide.FinishedCordPlugged(true, canStart: true);
            Poll(guide, ShopOffer.Owned, profit: ShopOffer.Owned, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Emergency),
                "no gap, and ahead of the free-cord step and the shop hints, because its time runs until then");
            guide.EmergencyRinging(false, canStart: true);
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord), "then the others");
        }

        [Test]
        public void SkipOrADoneEmergencyHintMeansNoEmergencyStep()
        {
            var skipped = At(FirstOrderGuideStage.Portrait);
            skipped.Skip();
            skipped.EmergencyRinging(true, canStart: true);
            Assert.That(skipped.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled));
            Assert.That(skipped.ShopHintsDone, Is.EqualTo(ShopHints.All));

            var shown = new FirstOrderGuideSequence(ShopHints.Emergency, firstCall: false);
            shown.EmergencyRinging(true, canStart: true);
            Assert.That(shown.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the save has shown it");

            var pending = new FirstOrderGuideSequence(ShopHints.All & ~ShopHints.Emergency, firstCall: false);
            Assert.That(pending.IsOver, Is.False, "Continue keeps the guide for the emergency step alone");
            pending.EmergencyRinging(true, canStart: true);
            Assert.That(pending.Stage, Is.EqualTo(FirstOrderGuideStage.Emergency));
            pending.EmergencyRinging(false, canStart: true);
            Assert.That(pending.IsOver, Is.True, "every hint shown: the guide is done");
        }

        // 13a pass 5 (D81): a step that only informs ends on its OK button and nothing else: no timer, no other event.
        [TestCase(FirstOrderGuideStage.Connected)]
        [TestCase(FirstOrderGuideStage.Earning)]
        [TestCase(FirstOrderGuideStage.Mystery)]
        public void InformationStepsEndOnlyOnOk(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            Assert.That(guide.IsInformation && guide.IsGuiding, Is.True, "setup: an information step, the clock held");
            guide.Tick(600);
            guide.CablePickedUp();
            guide.CallerJackPlugged(true);
            guide.CordsOut(true);
            guide.EmergencyRinging(true, canStart: true);
            guide.NextCallerPolled(true, true, true, true);
            Poll(guide, ShopOffer.Affordable, profit: ShopOffer.Affordable, clients: ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(stage), "never on time or on another event");
            guide.OkClicked();
            Assert.That(guide.Stage, Is.Not.EqualTo(stage), "OK ends it");
        }

        [TestCase(FirstOrderGuideStage.Portrait)]
        [TestCase(FirstOrderGuideStage.Book)]
        [TestCase(FirstOrderGuideStage.ReadingBook)]
        [TestCase(FirstOrderGuideStage.Cables)]
        [TestCase(FirstOrderGuideStage.PluggingCaller)]
        [TestCase(FirstOrderGuideStage.PluggingRecipient)]
        [TestCase(FirstOrderGuideStage.BuyCord)]
        [TestCase(FirstOrderGuideStage.Profit)]
        [TestCase(FirstOrderGuideStage.Clients)]
        public void ActionStepsShowNoOkAndIgnoreIt(FirstOrderGuideStage stage)
        {
            var guide = At(stage);
            Assert.That(guide.IsInformation, Is.False, "an action step lights its target instead");
            guide.Tick(600);
            guide.OkClicked();
            Assert.That(guide.Stage, Is.EqualTo(stage));
        }

        [Test]
        public void EveryStepOnScreenHoldsTheClockAndNoneBetweenThem()
        {
            foreach (FirstOrderGuideStage stage in new[] { FirstOrderGuideStage.Portrait,
                FirstOrderGuideStage.Book, FirstOrderGuideStage.ReadingBook,
                FirstOrderGuideStage.Cables, FirstOrderGuideStage.PluggingCaller, FirstOrderGuideStage.PluggingRecipient,
                FirstOrderGuideStage.Connected, FirstOrderGuideStage.Earning, FirstOrderGuideStage.BuyCord,
                FirstOrderGuideStage.Mystery, FirstOrderGuideStage.Profit, FirstOrderGuideStage.Clients })
                Assert.That(At(stage).IsGuiding, Is.True, stage.ToString());
            var free = At(FirstOrderGuideStage.WaitingForShop);
            Assert.That(free.IsGuiding, Is.False, "between steps");
            free.FirstOrderEnded(cordStillPlugged: true);
            free.FinishedCordPlugged(true, canStart: true);
            Assert.That(free.Stage == FirstOrderGuideStage.FreeCord && free.IsGuiding, Is.True, "the free-cord step");
            free.CablePickedUp();
            free.NextCallerPolled(true, true, false, true);
            Assert.That(free.Stage == FirstOrderGuideStage.NextCard && free.IsGuiding, Is.True, "the next card");
            free.NextCallerPolled(true, true, true, true);
            Assert.That(free.Stage == FirstOrderGuideStage.NextCaller && free.IsGuiding, Is.True, "its jack");
            Assert.That(new FirstOrderGuideSequence().IsGuiding, Is.False, "before the first card");
        }

        // 13a pass 6 (D81): the second call repeats the caller's side with the freed cord in hand: the next caller's
        // card, then its blinking jack; the plug ends it.
        [Test]
        public void TheSecondCallTakesTheNextCardThenPlugsItsJackAndEndsOnThePlug()
        {
            var guide = FreedCord();
            guide.NextCallerPolled(cordInHand: true, callerWaiting: false, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "no caller waits: wait for one");
            Assert.That(guide.IsGuiding, Is.False, "the clock runs while no hint shows");
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Affordable, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "no shop hint jumps the queue");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: false);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "not while paused or the book is open");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCard), "first the card");
            Assert.That(guide.IsGuiding && !guide.IsInformation, Is.True, "an action step: the clock holds, no OK");
            guide.Tick(600); guide.OkClicked(); guide.CablePickedUp(); Poll(guide, ShopOffer.Affordable);
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCard), "only taking the card moves it on");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCaller), "then its blinking jack");
            Assert.That(guide.IsGuiding && !guide.IsInformation, Is.True, "an action step: the clock holds, no OK");
            guide.Tick(600); guide.OkClicked(); guide.CablePickedUp(); Poll(guide, ShopOffer.Affordable);
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCaller), "only the plug ends it");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: false, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "its caller stopped waiting: wait for another");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCard), "the next caller, from the card again");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: true, canStart: true);
            guide.NextCallerPolled(cordInHand: false, callerWaiting: true, cardTaken: true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "the plug is in the jack");
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "once only");

            guide = FreedCord();
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCaller), "a card already taken goes straight to its jack");
        }

        [Test]
        public void ACordDroppedBeforeTheSecondCallStepsShowDropsThemAndSkipStillWorks()
        {
            var guide = FreedCord();
            guide.NextCallerPolled(cordInHand: false, callerWaiting: true, cardTaken: false, canStart: true);
            guide.NextCallerPolled(cordInHand: true, callerWaiting: true, cardTaken: false, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.WaitingForShop), "back in the tray: no step");
            guide.Tick(FirstOrderGuideSequence.ShopHintGapSeconds);
            Poll(guide, ShopOffer.Affordable);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.BuyCord), "the shop hints follow");

            foreach (bool taken in new[] { false, true })
            {
                guide = FreedCord();
                guide.NextCallerPolled(true, true, taken, true);
                guide.Skip();
                Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Cancelled), "Skip ends the second call's steps too");
            }
        }

        [Test]
        public void TheFirstEmergencyComesBeforeTheNextCallerStep()
        {
            var guide = FreedCord();
            guide.EmergencyRinging(true, canStart: true);
            guide.NextCallerPolled(true, true, false, true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Emergency), "its time runs until it shows");
            guide.EmergencyRinging(false, canStart: true);
            guide.NextCallerPolled(true, true, false, true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCard), "then the freed cord's caller");
        }

        [Test]
        public void TheGuideCompletesOnlyAfterTheNextCallerStep()
        {
            var guide = At(FirstOrderGuideStage.Connected);
            guide.MarkShopHintsDone(ShopHints.All);
            guide.FirstOrderEnded(cordStillPlugged: true);
            guide.FinishedCordPlugged(true, canStart: true);
            guide.CablePickedUp();
            Assert.That(guide.IsOver, Is.False, "the next caller is still due");
            guide.NextCallerPolled(true, true, false, true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCard));
            Assert.That(guide.IsOver, Is.False);
            guide.NextCallerPolled(true, true, true, true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.NextCaller));
            guide.NextCallerPolled(false, true, true, true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.Complete));
        }

        // The first call is over, its cord freed and in hand.
        private static FirstOrderGuideSequence FreedCord()
        {
            var guide = At(FirstOrderGuideStage.WaitingForShop);
            guide.FirstOrderEnded(cordStillPlugged: true);
            guide.FinishedCordPlugged(true, canStart: true);
            Assert.That(guide.Stage, Is.EqualTo(FirstOrderGuideStage.FreeCord), "setup");
            guide.CablePickedUp();
            return guide;
        }

        private static void Ok(FirstOrderGuideSequence guide)
        {
            guide.Tick(FirstOrderGuideSequence.OkClickDelay);
            guide.OkClicked();
        }

        private static void Poll(FirstOrderGuideSequence guide, ShopOffer cord, ShopOffer mystery = ShopOffer.Affordable,
            ShopOffer profit = ShopOffer.TooExpensive, ShopOffer clients = ShopOffer.TooExpensive, bool canStart = true)
        {
            guide.Offer(ShopHints.Cord, cord, canStart);
            guide.Offer(ShopHints.Mystery, mystery, canStart);
            guide.Offer(ShopHints.Profit, profit, canStart);
            guide.Offer(ShopHints.Clients, clients, canStart);
        }

        private static FirstOrderGuideSequence At(FirstOrderGuideStage stage)
        {
            var guide = new FirstOrderGuideSequence();
            guide.FirstOrderShown();
            if (stage == FirstOrderGuideStage.Portrait) return guide;
            guide.FirstPortraitClicked();
            if (stage == FirstOrderGuideStage.Cables) return guide;
            guide.CablePickedUp(); guide.CordsOut(true);
            if (stage == FirstOrderGuideStage.PluggingCaller) return guide;
            guide.CallerJackPlugged(true);
            if (stage == FirstOrderGuideStage.Book) return guide;
            guide.BookChanged(true);
            if (stage == FirstOrderGuideStage.ReadingBook) return guide;
            guide.BookChanged(false);
            if (stage == FirstOrderGuideStage.PluggingRecipient) return guide;
            guide.FirstOrderConnected();
            if (stage == FirstOrderGuideStage.Connected) return guide;
            Ok(guide);
            if (stage == FirstOrderGuideStage.Earning) return guide;
            Ok(guide);
            if (stage == FirstOrderGuideStage.WaitingForShop) return guide;
            Poll(guide, ShopOffer.Affordable);
            if (stage == FirstOrderGuideStage.BuyCord) return guide;
            Poll(guide, ShopOffer.Owned);
            if (stage == FirstOrderGuideStage.Mystery) return guide;
            Ok(guide);
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
