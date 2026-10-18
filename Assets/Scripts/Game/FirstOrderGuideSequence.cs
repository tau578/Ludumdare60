namespace Switchboard.Game
{
    public enum FirstOrderGuideStage { WaitingForOrder, Portrait, TaskText, Book, ReadingBook, Cables, Complete, Cancelled }

    /// <summary>One optional hint sequence per game session. An early action never unlocks a later hint.</summary>
    public sealed class FirstOrderGuideSequence
    {
        public FirstOrderGuideStage Stage { get; private set; } = FirstOrderGuideStage.WaitingForOrder;
        private bool outOfOrder;

        public void FirstOrderShown()
        {
            if (Stage == FirstOrderGuideStage.WaitingForOrder) Stage = FirstOrderGuideStage.Portrait;
        }

        public void FirstPortraitClicked()
        {
            if (Stage == FirstOrderGuideStage.Portrait)
                Stage = outOfOrder ? FirstOrderGuideStage.Cancelled : FirstOrderGuideStage.TaskText;
        }

        public void TaskTextFinished()
        {
            if (Stage == FirstOrderGuideStage.TaskText) Stage = FirstOrderGuideStage.Book;
        }

        public void BookChanged(bool open)
        {
            if (open)
            {
                if (Stage == FirstOrderGuideStage.Book) Stage = FirstOrderGuideStage.ReadingBook;
                else if (Stage != FirstOrderGuideStage.ReadingBook) BreakOrder();
            }
            else if (Stage == FirstOrderGuideStage.ReadingBook) Stage = FirstOrderGuideStage.Cables;
        }

        public void CablePickedUp()
        {
            if (Stage == FirstOrderGuideStage.Cables) Stage = FirstOrderGuideStage.Complete;
            else BreakOrder();
        }

        public void BreakOrder()
        {
            if (Stage == FirstOrderGuideStage.Complete || Stage == FirstOrderGuideStage.Cancelled) return;
            outOfOrder = true;
            // Still invite the player to read their first order, even if they already explored the desk.
            if (Stage != FirstOrderGuideStage.WaitingForOrder && Stage != FirstOrderGuideStage.Portrait)
                Stage = FirstOrderGuideStage.Cancelled;
        }

        public void Cancel() => Stage = FirstOrderGuideStage.Cancelled;
    }
}
