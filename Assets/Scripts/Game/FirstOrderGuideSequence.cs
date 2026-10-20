using Switchboard.Domain;

namespace Switchboard.Game
{
    public enum FirstOrderGuideStage
    {
        WaitingForOrder = 0,
        Portrait = 1,
        TaskText = 2,
        // 10b fix 1 put it after the message; the stages are never saved, so the values after it moved up by one.
        CallerJack = 3,
        Book = 4,
        ReadingBook = 5,
        Cables = 6,
        // 10b fix 2 split plugging into one jack at a time (7a, 7b); the values after it moved up by one again.
        PluggingCaller = 7,
        PluggingRecipient = 8,
        Connected = 9,
        WaitingForShop = 10,
        BuyCord = 11,
        Mystery = 12,
        Complete = 13,
        Cancelled = 14,
        Profit = 15,
        Clients = 16
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
    /// The tutorial (D57), one step at a time. A new game guides the first call (steps 1-8; step 3, the caller's
    /// blinking jack, since 10b fix 1, D58; plugging one jack at a time, 7a the caller's and 7b the found one, since
    /// 10b fix 2). Then come the shop hints, each once, while normal play goes on: buying the first extra cord once
    /// affordable (9), a look at the mystery upgrade (10), and More Profit and More Clients once each is affordable
    /// (fix 2). Continue skips the first call and shows only the shop hints the save hasn't seen. Every step is mandatory while it shows (fix 1): the
    /// spotlight lets only its own target take clicks. The cord step must be bought; the others end on any click.
    /// </summary>
    public sealed class FirstOrderGuideSequence
    {
        /// <summary>How long the "Connected" step stays before normal play resumes by itself.</summary>
        public const float ConnectedSeconds = 5f;
        /// <summary>A click this soon after a step starts is the click that started it, not "got it".</summary>
        public const float ConnectedClickDelay = .6f;
        /// <summary>How long the mystery upgrade stays lit before the tutorial ends by itself.</summary>
        public const float MysterySeconds = 5f;
        /// <summary>After the message has typed out, a click this soon is a stray double-click, not "read it".</summary>
        public const float ReadClickDelay = .5f;
        /// <summary>The pause between one shop hint and the next.</summary>
        public const float ShopHintGapSeconds = 1.5f;

        public FirstOrderGuideStage Stage { get; private set; } = FirstOrderGuideStage.WaitingForOrder;
        private FirstOrderGuideStage afterBook = FirstOrderGuideStage.Cables;
        private float stepTime;
        private float gapLeft;
        private bool textRead;

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

        /// <summary>A step is on screen (and blocks every click but its own).</summary>
        public bool IsGuiding => (Stage >= FirstOrderGuideStage.Portrait && Stage <= FirstOrderGuideStage.Connected) ||
            Stage == FirstOrderGuideStage.BuyCord || Stage == FirstOrderGuideStage.Mystery ||
            Stage == FirstOrderGuideStage.Profit || Stage == FirstOrderGuideStage.Clients;
        public bool IsOver => Stage == FirstOrderGuideStage.Complete || Stage == FirstOrderGuideStage.Cancelled;
        /// <summary>The first call's steps are behind: connected, skipped or ended.</summary>
        public bool IsFirstCallOver => Stage >= FirstOrderGuideStage.Connected;
        /// <summary>Step 2: the message has typed out, and a click moves on.</summary>
        public bool IsTaskTextRead => Stage == FirstOrderGuideStage.TaskText && textRead;
        /// <summary>Steps 7a and 7b: a cord in hand, one jack at a time.</summary>
        public bool IsPlugging => IsPluggingStage(Stage);
        private static bool IsPluggingStage(FirstOrderGuideStage stage) =>
            stage == FirstOrderGuideStage.PluggingCaller || stage == FirstOrderGuideStage.PluggingRecipient;
        private bool InShop => Stage == FirstOrderGuideStage.WaitingForShop || Stage == FirstOrderGuideStage.BuyCord ||
            Stage == FirstOrderGuideStage.Mystery || Stage == FirstOrderGuideStage.Profit || Stage == FirstOrderGuideStage.Clients;

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

        public void FirstPortraitClicked()
        {
            if (Stage == FirstOrderGuideStage.Portrait) Stage = FirstOrderGuideStage.TaskText;
        }

        /// <summary>The message has typed out: step 2 now waits for a click (fix 2), so there is time to read it.</summary>
        public void TaskTextFinished()
        {
            if (Stage != FirstOrderGuideStage.TaskText || textRead) return;
            textRead = true;
            stepTime = 0;
        }

        public void BookChanged(bool open)
        {
            if (open)
            {
                if (Stage == FirstOrderGuideStage.Book) { afterBook = FirstOrderGuideStage.Cables; Stage = FirstOrderGuideStage.ReadingBook; }
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
        }

        /// <summary>Polled: whether any cord is out of the tray. A cord back in the tray asks for it again.</summary>
        public void CordsOut(bool anyOut)
        {
            if (anyOut) return;
            if (IsPlugging) Stage = FirstOrderGuideStage.Cables;
            else if (Stage == FirstOrderGuideStage.ReadingBook && IsPluggingStage(afterBook)) afterBook = FirstOrderGuideStage.Cables;
        }

        /// <summary>
        /// Polled: whether a plug sits in the guided caller's jack. Step 7a moves on to 7b once it does; should it come
        /// out again, 7a comes back.
        /// </summary>
        public void CallerJackPlugged(bool plugged)
        {
            if (Stage == FirstOrderGuideStage.PluggingCaller && plugged) Stage = FirstOrderGuideStage.PluggingRecipient;
            else if (Stage == FirstOrderGuideStage.PluggingRecipient && !plugged) Stage = FirstOrderGuideStage.PluggingCaller;
        }

        public void FirstOrderConnected()
        {
            if (Stage < FirstOrderGuideStage.Portrait || Stage > FirstOrderGuideStage.PluggingRecipient) return;
            Stage = FirstOrderGuideStage.Connected;
            stepTime = 0;
        }

        /// <summary>Game time: the "Connected" and mystery steps end by themselves; it also runs the click guards and the gap.</summary>
        public void Tick(float seconds)
        {
            if (!(seconds > 0)) return;
            stepTime += seconds;
            if (Stage == FirstOrderGuideStage.WaitingForShop) gapLeft = System.Math.Max(0, gapLeft - seconds);
            if (Stage == FirstOrderGuideStage.Connected && stepTime >= ConnectedSeconds) Stage = FirstOrderGuideStage.WaitingForShop;
            else if (Stage == FirstOrderGuideStage.Mystery && stepTime >= MysterySeconds) EndShopHint(ShopHints.Mystery);
        }

        /// <summary>
        /// A click the dark layer swallowed: it moves on from a read message (step 2) and from the caller's blinking
        /// jack (step 3), and dismisses the "Connected", mystery, More Profit and More Clients steps. The cord step
        /// ignores it: only buying ends that one.
        /// </summary>
        public void AnyClick()
        {
            if (Stage == FirstOrderGuideStage.TaskText)
            {
                if (textRead && stepTime >= ReadClickDelay) { Stage = FirstOrderGuideStage.CallerJack; stepTime = 0; }
                return;
            }
            if (Stage == FirstOrderGuideStage.CallerJack)
            {
                // The same short guard as step 2: the click that read the message isn't also "seen the light".
                if (stepTime >= ReadClickDelay) Stage = FirstOrderGuideStage.Book;
                return;
            }
            if (stepTime < ConnectedClickDelay) return;
            if (Stage == FirstOrderGuideStage.Connected) Stage = FirstOrderGuideStage.WaitingForShop;
            else if (Stage == FirstOrderGuideStage.Mystery) EndShopHint(ShopHints.Mystery);
            else if (Stage == FirstOrderGuideStage.Profit) EndShopHint(ShopHints.Profit);
            else if (Stage == FirstOrderGuideStage.Clients) EndShopHint(ShopHints.Clients);
        }

        /// <summary>The guided call ended: before connecting the guide ends; after it, the shop hints still follow.</summary>
        public void FirstOrderEnded()
        {
            if (Stage == FirstOrderGuideStage.Connected) Stage = FirstOrderGuideStage.WaitingForShop;
            else if (!IsFirstCallOver) Cancel();
        }

        /// <summary>
        /// Polled after the first call, once per hint, cord first, then the mystery upgrade, More Profit and More
        /// Clients, so that is the order when several come due together. One hint shows at a time.
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
            if (Stage != FirstOrderGuideStage.WaitingForShop || offer != ShopOffer.Affordable || !canStart) return;
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
            if (Stage == FirstOrderGuideStage.WaitingForShop && (ShopHintsDone & ShopHints.All) == ShopHints.All)
                Stage = FirstOrderGuideStage.Complete;
        }

        private static FirstOrderGuideStage StepOf(ShopHints hint) =>
            hint == ShopHints.Cord ? FirstOrderGuideStage.BuyCord
            : hint == ShopHints.Mystery ? FirstOrderGuideStage.Mystery
            : hint == ShopHints.Profit ? FirstOrderGuideStage.Profit
            : FirstOrderGuideStage.Clients;
    }
}
