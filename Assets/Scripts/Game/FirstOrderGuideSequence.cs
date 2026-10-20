namespace Switchboard.Game
{
    public enum FirstOrderGuideStage
    {
        WaitingForOrder = 0,
        Portrait = 1,
        TaskText = 2,
        Book = 3,
        ReadingBook = 4,
        Cables = 5,
        Plugging = 6,
        Connected = 7,
        WaitingForShop = 8,
        BuyCord = 9,
        Mystery = 10,
        Complete = 11,
        Cancelled = 12
    }

    /// <summary>
    /// The tutorial of a new game (D57), one step at a time: the first call (steps 1-7), then, once the player can
    /// afford the first extra cord, buying it (8) and a look at the mystery upgrade (9). The tutorial is mandatory
    /// (fix 1): the spotlight lets only the current step's target take clicks, so the player can't act ahead.
    /// </summary>
    public sealed class FirstOrderGuideSequence
    {
        /// <summary>How long the "Connected" step stays before normal play resumes by itself.</summary>
        public const float ConnectedSeconds = 5f;
        /// <summary>A click this soon after a step starts is the click that started it, not "got it".</summary>
        public const float ConnectedClickDelay = .6f;
        /// <summary>How long the mystery upgrade stays lit before the tutorial ends by itself.</summary>
        public const float MysterySeconds = 5f;

        public FirstOrderGuideStage Stage { get; private set; } = FirstOrderGuideStage.WaitingForOrder;
        private FirstOrderGuideStage afterBook = FirstOrderGuideStage.Cables;
        private float stepTime;

        /// <summary>A step is on screen (and blocks every click but its own).</summary>
        public bool IsGuiding => (Stage >= FirstOrderGuideStage.Portrait && Stage <= FirstOrderGuideStage.Connected) ||
            Stage == FirstOrderGuideStage.BuyCord || Stage == FirstOrderGuideStage.Mystery;
        public bool IsOver => Stage == FirstOrderGuideStage.Complete || Stage == FirstOrderGuideStage.Cancelled;
        /// <summary>The first call's steps are behind: connected, skipped or ended.</summary>
        public bool IsFirstCallOver => Stage >= FirstOrderGuideStage.Connected;

        public void FirstOrderShown()
        {
            if (Stage == FirstOrderGuideStage.WaitingForOrder) Stage = FirstOrderGuideStage.Portrait;
        }

        public void FirstPortraitClicked()
        {
            if (Stage == FirstOrderGuideStage.Portrait) Stage = FirstOrderGuideStage.TaskText;
        }

        public void TaskTextFinished()
        {
            if (Stage == FirstOrderGuideStage.TaskText) Stage = FirstOrderGuideStage.Book;
        }

        public void BookChanged(bool open)
        {
            if (open)
            {
                if (Stage == FirstOrderGuideStage.Book) { afterBook = FirstOrderGuideStage.Cables; Stage = FirstOrderGuideStage.ReadingBook; }
                else if (Stage == FirstOrderGuideStage.Cables || Stage == FirstOrderGuideStage.Plugging)
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
            if (Stage == FirstOrderGuideStage.Cables) Stage = FirstOrderGuideStage.Plugging;
        }

        /// <summary>Polled: whether any cord is out of the tray. A cord back in the tray asks for it again.</summary>
        public void CordsOut(bool anyOut)
        {
            if (Stage == FirstOrderGuideStage.Plugging && !anyOut) Stage = FirstOrderGuideStage.Cables;
            else if (Stage == FirstOrderGuideStage.ReadingBook && afterBook == FirstOrderGuideStage.Plugging && !anyOut)
                afterBook = FirstOrderGuideStage.Cables;
        }

        public void FirstOrderConnected()
        {
            if (Stage < FirstOrderGuideStage.Portrait || Stage > FirstOrderGuideStage.Plugging) return;
            Stage = FirstOrderGuideStage.Connected;
            stepTime = 0;
        }

        /// <summary>Game time: the "Connected" and mystery steps end by themselves.</summary>
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || (Stage != FirstOrderGuideStage.Connected && Stage != FirstOrderGuideStage.Mystery)) return;
            stepTime += seconds;
            if (Stage == FirstOrderGuideStage.Connected && stepTime >= ConnectedSeconds) Stage = FirstOrderGuideStage.WaitingForShop;
            else if (Stage == FirstOrderGuideStage.Mystery && stepTime >= MysterySeconds) Stage = FirstOrderGuideStage.Complete;
        }

        /// <summary>A click anywhere: it dismisses the "Connected" and mystery steps.</summary>
        public void AnyClick()
        {
            if (stepTime < ConnectedClickDelay) return;
            if (Stage == FirstOrderGuideStage.Connected) Stage = FirstOrderGuideStage.WaitingForShop;
            else if (Stage == FirstOrderGuideStage.Mystery) Stage = FirstOrderGuideStage.Complete;
        }

        /// <summary>The guided call ended: before connecting the guide ends; after it, the shop steps still follow.</summary>
        public void FirstOrderEnded()
        {
            if (Stage == FirstOrderGuideStage.Connected) Stage = FirstOrderGuideStage.WaitingForShop;
            else if (!IsFirstCallOver) Cancel();
        }

        /// <summary>Polled while waiting: the first extra cord is on the shelf and the player can buy it now.</summary>
        public void CordAffordable(bool affordable)
        {
            if (Stage == FirstOrderGuideStage.WaitingForShop && affordable) Stage = FirstOrderGuideStage.BuyCord;
            else if (Stage == FirstOrderGuideStage.BuyCord && !affordable) Stage = FirstOrderGuideStage.WaitingForShop;
        }

        /// <summary>The shelf has no first extra cord to sell (none in the file, or not on the shelf): no shop steps.</summary>
        public void ShopUnavailable()
        {
            if (Stage == FirstOrderGuideStage.WaitingForShop || Stage == FirstOrderGuideStage.BuyCord)
                Stage = FirstOrderGuideStage.Complete;
        }

        /// <param name="hasMystery">Whether the shop shows a mystery upgrade to look at.</param>
        public void CordBought(bool hasMystery)
        {
            if (Stage != FirstOrderGuideStage.WaitingForShop && Stage != FirstOrderGuideStage.BuyCord) return;
            Stage = hasMystery ? FirstOrderGuideStage.Mystery : FirstOrderGuideStage.Complete;
            stepTime = 0;
        }

        public void Skip() => Cancel();

        public void Cancel()
        {
            if (Stage != FirstOrderGuideStage.Complete) Stage = FirstOrderGuideStage.Cancelled;
        }
    }
}
