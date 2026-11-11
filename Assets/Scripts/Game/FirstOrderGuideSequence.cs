using Switchboard.Domain;

namespace Switchboard.Game
{
    public enum FirstOrderGuideStage
    {
        WaitingForOrder = 0,
        Portrait = 1,
        // 13a pass 6 (D81): the caller's side first, then the recipient's; the message and blinking-jack steps went.
        // The stages are never saved, so the first call's values follow the play order again.
        Cables = 2,
        PluggingCaller = 3,
        Book = 4,
        ReadingBook = 5,
        PluggingRecipient = 6,
        Connected = 7,
        WaitingForShop = 10,
        BuyCord = 11,
        Mystery = 12,
        Complete = 13,
        Cancelled = 14,
        Profit = 15,
        Clients = 16,
        // 13a pass 3 (D81): the first call is over and its cord still plugged; one click on a plug frees it (D79).
        FreeCord = 17,
        // 13a pass 4 (D81): the first emergency's card, until it is taken.
        Emergency = 18,
        // 13a pass 5 (D81): the freed cord, still in hand, into the next caller's blinking jack (since pass 6 after NextCard).
        NextCaller = 19,
        // 13a pass 6 (D81): the next waiting caller's card, taken with the freed cord in hand.
        NextCard = 20,
        // 13a pass 8: "Connected" split in two, one focus each: the talking tab (Connected), then the money (Earning).
        Earning = 21
    }

    /// <summary>What the shop does with the upgrade a shop hint is about, polled every frame.</summary>
    public enum ShopOffer
    {
        /// <summary>Not in the file, or not on a shop button: its hint never shows.</summary>
        NotForSale = 0,
        TooExpensive = 1,
        /// <summary>On its button and buyable now; for the mystery upgrade: on its button, to look at.</summary>
        Affordable = 2,
        Owned = 3
    }

    /// <summary>
    /// The tutorial (D57), one step at a time. A new game guides the first call, the caller's side first and then the
    /// recipient's, each light right before its action (13a pass 6, D81): the caller's card; a cord from the tray (the
    /// caller's jack now blinks, D58); the plug into that jack; the book; the recipient's row (its click marks the jack);
    /// the other plug into the marked jack; then "Connected". Then come the shop hints, each once, while normal play
    /// goes on: buying the first extra cord once affordable, a look at the mystery upgrade, and More Profit and More
    /// Clients once each is affordable (fix 2). Since 13a pass 3 (D81), once the first call is over and its cord is
    /// still plugged, one step asks for a click on a plug to free the cord (D79) before any shop hint; it ends on the
    /// pickup. Since 13a pass 4 (D81), the first emergency that rings gets its own step, ahead of any other that is due,
    /// ending when its card is taken; it is saved with the shop hints. While any step shows, the game's clock holds
    /// (<see cref="IsGuiding"/>); the OK guard and the gap between shop hints count Unity's frame time, which the hold
    /// doesn't stop. Continue skips the first call and shows only the shop hints the save hasn't seen. Every step is
    /// mandatory while it shows (fix 1). Since 13a pass 5 (D81) a step either asks for an action, lights its target and
    /// ends on that action, or only informs (<see cref="IsInformation"/>: "Connected", the mystery upgrade, More Profit
    /// and More Clients) and ends only on its OK button: nothing ends by itself or on any click. Right after the
    /// free-cord step, the second call repeats the caller's side with the freed cord in hand: the next caller's card,
    /// then its blinking jack (pass 6); the player finishes it alone. Since pass 8 "Connected" is two OK steps (the tab,
    /// then the money), and More Profit and More Clients are action steps like the cord: they show only while their
    /// upgrade is affordable and end on the purchase.
    /// </summary>
    public sealed class FirstOrderGuideSequence
    {
        /// <summary>An OK this soon after its step starts is a stray double-click from the last step, not "read it".</summary>
        public const float OkClickDelay = .5f;
        /// <summary>The pause between one shop hint and the next.</summary>
        public const float ShopHintGapSeconds = 1.5f;

        public FirstOrderGuideStage Stage { get; private set; } = FirstOrderGuideStage.WaitingForOrder;
        private FirstOrderGuideStage afterBook = FirstOrderGuideStage.Cables;
        private float stepTime;
        private float gapLeft;
        // The first call ended with its cord still plugged: the free-cord step is due once play is calm.
        private bool cordToFree;
        // The cord was freed: while it stays in hand, the next-caller step is due once a caller waits (D81).
        private bool nextCallerDue;

        /// <param name="shopHintsDone">The shop hints the save has already shown.</param>
        /// <param name="firstCall">False on Continue: straight to the shop hints.</param>
        public FirstOrderGuideSequence(ShopHints shopHintsDone = ShopHints.None, bool firstCall = true)
        {
            ShopHintsDone = shopHintsDone & ShopHints.All;
            if (firstCall) return;
            Stage = FirstOrderGuideStage.WaitingForShop;
            CompleteWhenNothingLeft();
        }

        /// <summary>The shop hints shown (or skipped for good); a skipped or ended tutorial counts as all shown.</summary>
        public ShopHints ShopHintsDone { get; private set; }

        /// <summary>A step is on screen (and blocks every click but its own); the game's clock holds meanwhile (D81).</summary>
        public bool IsGuiding => (Stage >= FirstOrderGuideStage.Portrait && Stage <= FirstOrderGuideStage.Connected) ||
            Stage == FirstOrderGuideStage.Earning || Stage == FirstOrderGuideStage.BuyCord || Stage == FirstOrderGuideStage.Mystery ||
            Stage == FirstOrderGuideStage.Profit || Stage == FirstOrderGuideStage.Clients ||
            Stage == FirstOrderGuideStage.FreeCord || Stage == FirstOrderGuideStage.Emergency ||
            Stage == FirstOrderGuideStage.NextCard || Stage == FirstOrderGuideStage.NextCaller;
        /// <summary>The step on screen only informs and ends only on its OK button (D81).</summary>
        public bool IsInformation => Stage == FirstOrderGuideStage.Connected || Stage == FirstOrderGuideStage.Earning ||
            Stage == FirstOrderGuideStage.Mystery;
        public bool IsOver => Stage == FirstOrderGuideStage.Complete || Stage == FirstOrderGuideStage.Cancelled;
        /// <summary>The first call's steps are behind: connected, skipped or ended.</summary>
        public bool IsFirstCallOver => Stage >= FirstOrderGuideStage.Connected;
        /// <summary>The caller's jack, then (after the book) the marked jack: a cord in hand, one jack at a time.</summary>
        public bool IsPlugging => IsPluggingStage(Stage);
        private static bool IsPluggingStage(FirstOrderGuideStage stage) =>
            stage == FirstOrderGuideStage.PluggingCaller || stage == FirstOrderGuideStage.PluggingRecipient;
        private bool InShop => Stage == FirstOrderGuideStage.WaitingForShop || Stage == FirstOrderGuideStage.BuyCord ||
            Stage == FirstOrderGuideStage.Mystery || Stage == FirstOrderGuideStage.Profit || Stage == FirstOrderGuideStage.Clients ||
            Stage == FirstOrderGuideStage.Emergency;

        /// <summary>Hints the save has already shown: they never come.</summary>
        public void MarkShopHintsDone(ShopHints done)
        {
            ShopHintsDone |= done & ShopHints.All;
            CompleteWhenNothingLeft();
        }

        public void FirstOrderShown()
        {
            if (Stage == FirstOrderGuideStage.WaitingForOrder) Stage = FirstOrderGuideStage.Portrait;
        }

        /// <summary>The card is taken: its caller's jack blinks now (D58), so the cord comes next (13a pass 6).</summary>
        public void FirstPortraitClicked()
        {
            if (Stage == FirstOrderGuideStage.Portrait) Stage = FirstOrderGuideStage.Cables;
        }

        public void BookChanged(bool open)
        {
            if (open)
            {
                if (Stage == FirstOrderGuideStage.Book)
                {
                    afterBook = FirstOrderGuideStage.PluggingRecipient;
                    Stage = FirstOrderGuideStage.ReadingBook;
                }
                else if (Stage == FirstOrderGuideStage.Cables || IsPlugging)
                {
                    // The guide keeps the book shut here; should it open anyway, the book step comes back and returns.
                    afterBook = Stage;
                    Stage = FirstOrderGuideStage.ReadingBook;
                }
            }
            else if (Stage == FirstOrderGuideStage.ReadingBook) Stage = afterBook;
        }

        public void CablePickedUp()
        {
            if (Stage == FirstOrderGuideStage.Cables) Stage = FirstOrderGuideStage.PluggingCaller;
            else if (Stage == FirstOrderGuideStage.FreeCord) EndFreeCord();
        }

        /// <summary>
        /// Polled after the free-cord step: the second call's two steps (D81) ask the oldest waiting caller's card to be
        /// taken (<see cref="FirstOrderGuideStage.NextCard"/>), then the freed cord, still in hand, in that caller's
        /// blinking jack (<see cref="FirstOrderGuideStage.NextCaller"/>); a card already taken skips the first. They come
        /// up as soon as a caller waits and no other step shows (the game isn't paused and the book is shut), with no
        /// gap; until then the clock runs. A cord that leaves the hand (into any jack, or back to the tray) ends them, or
        /// drops them if they haven't shown yet. Should their caller stop waiting meanwhile, they wait for the next one.
        /// </summary>
        /// <param name="cordInHand">A cord is held with no plug in.</param>
        /// <param name="callerWaiting">A regular caller waits on a card, with a free jack: the steps' target.</param>
        /// <param name="cardTaken">That caller's card is taken, so its jack blinks.</param>
        /// <param name="canStart">Not paused, and the book shut.</param>
        public void NextCallerPolled(bool cordInHand, bool callerWaiting, bool cardTaken, bool canStart)
        {
            if (Stage == FirstOrderGuideStage.NextCard || Stage == FirstOrderGuideStage.NextCaller)
            {
                if (!cordInHand) EndNextCaller();
                else if (!callerWaiting) { Stage = FirstOrderGuideStage.WaitingForShop; stepTime = 0; }
                else Stage = cardTaken ? FirstOrderGuideStage.NextCaller : FirstOrderGuideStage.NextCard;
                return;
            }
            if (!nextCallerDue) return;
            if (!cordInHand)
            {
                nextCallerDue = false;
                CompleteWhenNothingLeft();
                return;
            }
            if (Stage != FirstOrderGuideStage.WaitingForShop || !callerWaiting || !canStart) return;
            Stage = cardTaken ? FirstOrderGuideStage.NextCaller : FirstOrderGuideStage.NextCard;
            stepTime = 0;
        }

        private void EndNextCaller()
        {
            nextCallerDue = false;
            Stage = FirstOrderGuideStage.WaitingForShop;
            stepTime = 0;
            gapLeft = ShopHintGapSeconds;
            CompleteWhenNothingLeft();
        }

        /// <summary>
        /// Polled after the first call: whether its finished cord is still plugged. The free-cord step (D81) comes up
        /// only in calm play, like a shop hint; should the cord leave the board some other way, the step is dropped.
        /// </summary>
        /// <param name="canStart">The step may come up now: not paused, the book shut and no cord in hand.</param>
        public void FinishedCordPlugged(bool plugged, bool canStart)
        {
            if (!plugged)
            {
                cordToFree = false;
                if (Stage == FirstOrderGuideStage.FreeCord) EndFreeCord();
                else CompleteWhenNothingLeft();
                return;
            }
            if (!cordToFree || Stage != FirstOrderGuideStage.WaitingForShop || !canStart) return;
            cordToFree = false;
            Stage = FirstOrderGuideStage.FreeCord;
            stepTime = 0;
        }

        /// <summary>
        /// Polled after the first call: whether an emergency's card is ringing, not yet taken. The first one brings up
        /// the emergency step (D81) as soon as no other step shows, ahead of the free-cord step and the shop hints,
        /// because its time runs until then; taking the card ends it. Once per save, never after Skip.
        /// </summary>
        /// <param name="canStart">The step may come up now: not paused.</param>
        public void EmergencyRinging(bool ringing, bool canStart)
        {
            if ((ShopHintsDone & ShopHints.Emergency) != 0) return;
            if (Stage == FirstOrderGuideStage.Emergency)
            {
                if (!ringing) EndShopHint(ShopHints.Emergency);
                return;
            }
            if (!ringing || Stage != FirstOrderGuideStage.WaitingForShop || !canStart) return;
            Stage = FirstOrderGuideStage.Emergency;
            stepTime = 0;
        }

        private void EndFreeCord()
        {
            Stage = FirstOrderGuideStage.WaitingForShop;
            // Next, the freed cord into the next caller; dropped at the next poll if no cord is in hand.
            nextCallerDue = true;
            stepTime = 0;
            gapLeft = ShopHintGapSeconds;
            CompleteWhenNothingLeft();
        }

        /// <summary>Polled: whether any cord is out of the tray. A cord back in the tray asks for it again.</summary>
        public void CordsOut(bool anyOut)
        {
            if (anyOut) return;
            if (IsPlugging || Stage == FirstOrderGuideStage.Book) Stage = FirstOrderGuideStage.Cables;
            else if (Stage == FirstOrderGuideStage.ReadingBook && IsPluggingStage(afterBook)) afterBook = FirstOrderGuideStage.Cables;
        }

        /// <summary>
        /// Polled: whether a plug sits in the guided caller's jack. The caller's side is done once it does, and the
        /// book comes next (13a pass 6); should it come out again before the connection, the caller's jack comes back.
        /// </summary>
        public void CallerJackPlugged(bool plugged)
        {
            if (Stage == FirstOrderGuideStage.PluggingCaller && plugged) Stage = FirstOrderGuideStage.Book;
            else if (plugged) return;
            else if (Stage == FirstOrderGuideStage.Book || Stage == FirstOrderGuideStage.PluggingRecipient)
                Stage = FirstOrderGuideStage.PluggingCaller;
            else if (Stage == FirstOrderGuideStage.ReadingBook && afterBook == FirstOrderGuideStage.PluggingRecipient)
                afterBook = FirstOrderGuideStage.PluggingCaller;
        }

        public void FirstOrderConnected()
        {
            if (Stage < FirstOrderGuideStage.Portrait || Stage > FirstOrderGuideStage.PluggingRecipient) return;
            Stage = FirstOrderGuideStage.Connected;
            stepTime = 0;
        }

        /// <summary>
        /// Unity's frame time (0 while paused, D56; it keeps running while a step holds the game's clock, D81): the OK
        /// button's guard and the gap between shop hints. No step ends on time (13a pass 5).
        /// </summary>
        public void Tick(float seconds)
        {
            if (!(seconds > 0)) return;
            stepTime += seconds;
            if (Stage == FirstOrderGuideStage.WaitingForShop) gapLeft = System.Math.Max(0, gapLeft - seconds);
        }

        /// <summary>
        /// The hint's OK button (D81): the one way to end an information step (<see cref="IsInformation"/>); every
        /// other step ignores it. A press within <see cref="OkClickDelay"/> of the step's start is ignored.
        /// </summary>
        public void OkClicked()
        {
            if (!IsInformation || stepTime < OkClickDelay) return;
            if (Stage == FirstOrderGuideStage.Connected) { Stage = FirstOrderGuideStage.Earning; stepTime = 0; }
            else if (Stage == FirstOrderGuideStage.Earning) { Stage = FirstOrderGuideStage.WaitingForShop; stepTime = 0; }
            else if (Stage == FirstOrderGuideStage.Mystery) EndShopHint(ShopHints.Mystery);
        }

        /// <summary>
        /// The guided call ended: before connecting the guide ends; after it, the shop hints still follow, and if its
        /// cord is still plugged the free-cord step (D81) comes first.
        /// </summary>
        public void FirstOrderEnded(bool cordStillPlugged = false)
        {
            if (Stage == FirstOrderGuideStage.Connected || Stage == FirstOrderGuideStage.Earning)
                Stage = FirstOrderGuideStage.WaitingForShop;
            else if (!IsFirstCallOver) { Cancel(); return; }
            if (cordStillPlugged && !IsOver) cordToFree = true;
        }

        /// <summary>
        /// Polled after the first call, once per hint, cord first, then the mystery upgrade, More Profit and More
        /// Clients, so that is the order when several come due together. One hint shows at a time. A purchase step
        /// (cord, More Profit, More Clients) starts only while its upgrade is affordable, steps back while it isn't (the
        /// clock holds during a hint, so it must stay completable) and ends on the purchase (Owned).
        /// </summary>
        /// <param name="canStart">A hint may come up now: not paused, the book shut and no cord in hand.</param>
        public void Offer(ShopHints hint, ShopOffer offer, bool canStart)
        {
            if (!InShop || (ShopHintsDone & hint) != 0 || hint == ShopHints.None) return;
            FirstOrderGuideStage step = StepOf(hint);
            bool showing = Stage == step;
            if (offer == ShopOffer.NotForSale || offer == ShopOffer.Owned)
            {
                // A cord that can never be bought has no mystery step after it either; a bought one leads to it at once.
                if (hint == ShopHints.Cord && offer == ShopOffer.NotForSale) ShopHintsDone |= ShopHints.Mystery;
                if (showing) EndShopHint(hint);
                else { ShopHintsDone |= hint; CompleteWhenNothingLeft(); }
                return;
            }
            if (showing)
            {
                // A fine took the money while it showed: back to normal play until it is affordable again.
                if (offer != ShopOffer.Affordable && hint != ShopHints.Mystery) Stage = FirstOrderGuideStage.WaitingForShop;
                return;
            }
            // The free-cord and second-call steps, when due, come before any shop hint.
            if (Stage != FirstOrderGuideStage.WaitingForShop || offer != ShopOffer.Affordable || !canStart || cordToFree ||
                nextCallerDue) return;
            if (hint == ShopHints.Mystery)
            {
                // The mystery step belongs to the cord step: it follows the cord at once, and never comes before it.
                if ((ShopHintsDone & ShopHints.Cord) == 0) return;
            }
            else if (gapLeft > 0) return;
            Stage = step;
            stepTime = 0;
        }

        public void Skip() => Cancel();

        /// <summary>Ends the tutorial for good, shop hints included.</summary>
        public void Cancel()
        {
            if (Stage == FirstOrderGuideStage.Complete) return;
            Stage = FirstOrderGuideStage.Cancelled;
            ShopHintsDone = ShopHints.All;
        }

        private void EndShopHint(ShopHints hint)
        {
            ShopHintsDone |= hint;
            Stage = FirstOrderGuideStage.WaitingForShop;
            stepTime = 0;
            gapLeft = ShopHintGapSeconds;
            CompleteWhenNothingLeft();
        }

        private void CompleteWhenNothingLeft()
        {
            if (Stage == FirstOrderGuideStage.WaitingForShop && !cordToFree && !nextCallerDue &&
                (ShopHintsDone & ShopHints.All) == ShopHints.All)
                Stage = FirstOrderGuideStage.Complete;
        }

        private static FirstOrderGuideStage StepOf(ShopHints hint) =>
            hint == ShopHints.Cord ? FirstOrderGuideStage.BuyCord
            : hint == ShopHints.Mystery ? FirstOrderGuideStage.Mystery
            : hint == ShopHints.Profit ? FirstOrderGuideStage.Profit
            : hint == ShopHints.Emergency ? FirstOrderGuideStage.Emergency
            : FirstOrderGuideStage.Clients;
    }
}
