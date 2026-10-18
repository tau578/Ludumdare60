using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    public sealed class FirstOrderGuide : MonoBehaviour
    {
        private FirstOrderGuideSequence sequence = new FirstOrderGuideSequence();
        private CallQueueView queue;
        private CallService calls;
        private PhoneBookView book;
        private readonly List<CableController> cables = new List<CableController>();
        private CordBridge cordBridge;
        private Call firstCall;
        private CallCardView firstCard;
        private FirstOrderGuideStage? renderedStage;
        private readonly Dictionary<Graphic, SlowGuidePulse> pulses = new Dictionary<Graphic, SlowGuidePulse>();
        public FirstOrderGuideStage Stage => sequence.Stage;

        public void Bind(CallQueueView callQueue, CallService service, PhoneBookView phoneBook, CableController[] wires,
            CordBridge bridge = null)
        {
            Unbind();
            sequence = new FirstOrderGuideSequence();
            renderedStage = null;
            firstCall = null;
            firstCard = null;
            queue = callQueue; calls = service; book = phoneBook; cordBridge = bridge;
            if (cordBridge != null) cordBridge.CableAdded += RegisterCable;
            queue.OrderShown += OnOrderShown;
            queue.CardOpened += OnCardOpened;
            calls.CallEnded += OnCallEnded;
            if (book != null)
            {
                book.OpenChanged += OnBookChanged;
                if (book.IsOpen) sequence.BreakOrder();
            }
            foreach (var cable in wires)
            {
                RegisterCable(cable);
                if (cable != null && !cable.IsInTray) sequence.BreakOrder();
            }
        }

        private void RegisterCable(CableController cable)
        {
            if (cable == null || !cable.gameObject.activeInHierarchy || cables.Contains(cable)) return;
            cables.Add(cable);
            cable.PickedUp += OnCablePickedUp;
            // A bought cord joins the current hint without restarting the tutorial.
            renderedStage = null;
            RefreshHints();
        }

        private void OnOrderShown(CallCardView card)
        {
            if (sequence.Stage != FirstOrderGuideStage.WaitingForOrder) return;
            firstCard = card; firstCall = card.Call;
            sequence.FirstOrderShown();
            RefreshHints();
        }

        private void OnCardOpened(CallCardView card)
        {
            if (card.Call == firstCall) sequence.FirstPortraitClicked();
            else sequence.BreakOrder();
            RefreshHints();
        }

        private void OnBookChanged(bool open)
        {
            AdvanceText(); // Also handles a click in the frame the last character appeared.
            sequence.BookChanged(open);
            RefreshHints();
        }

        private void OnCablePickedUp(CableController cable)
        {
            sequence.CablePickedUp();
            RefreshHints();
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            if (e.Call != firstCall) return;
            sequence.Cancel();
            RefreshHints();
        }

        private void LateUpdate()
        {
            if (queue == null) return;
            if (firstCard != null && firstCard.Call != firstCall) sequence.Cancel();
            AdvanceText();
            RefreshHints();
        }

        private void AdvanceText()
        {
            if (firstCard != null && firstCard.Call == firstCall && firstCard.IsTaskTextComplete)
                sequence.TaskTextFinished();
        }

        private void RefreshHints()
        {
            if (renderedStage == Stage) return;
            renderedStage = Stage;
            foreach (var pair in pulses)
                if (pair.Value != null) pair.Value.SetShowing(false);
            if (Stage == FirstOrderGuideStage.Portrait && firstCard != null)
                Show(firstCard.PortraitRect.GetComponent<Graphic>());
            else if (Stage == FirstOrderGuideStage.Book && book != null)
                Show(book.ClosedBookGraphic);
            else if (Stage == FirstOrderGuideStage.Cables)
                foreach (var cable in cables)
                    if (cable != null && cable.IsInTray && cable.TrayView != null)
                        foreach (var graphic in cable.TrayView.GetComponentsInChildren<Graphic>())
                            if (graphic is CableGraphic || graphic is PlugGraphic || graphic == cable.TrayView.Artwork) Show(graphic);
        }

        private void Show(Graphic graphic)
        {
            if (graphic == null) return;
            if (!pulses.TryGetValue(graphic, out var pulse) || pulse == null)
            {
                pulse = graphic.GetComponent<SlowGuidePulse>();
                if (pulse == null) pulse = graphic.gameObject.AddComponent<SlowGuidePulse>();
                pulses[graphic] = pulse;
            }
            pulse.SetShowing(true);
        }

        private void OnDisable() => Unbind();
        private void Unbind()
        {
            if (queue != null) { queue.OrderShown -= OnOrderShown; queue.CardOpened -= OnCardOpened; }
            if (calls != null) calls.CallEnded -= OnCallEnded;
            if (book != null) book.OpenChanged -= OnBookChanged;
            if (cordBridge != null) cordBridge.CableAdded -= RegisterCable;
            foreach (var cable in cables) if (cable != null) cable.PickedUp -= OnCablePickedUp;
            cables.Clear();
            cordBridge = null;
            foreach (var pulse in pulses.Values) if (pulse != null) pulse.SetShowing(false);
            pulses.Clear();
            queue = null; calls = null; book = null;
        }
    }
}
