using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>Step 1 queue: one card, laid over the first slot of the team's caller panel (Design 4.3).</summary>
    public sealed class CallQueueView : MonoBehaviour
    {
        [SerializeField] private CallCardView card;
        [SerializeField] private RectTransform cardArea;

        private readonly Vector3[] slotCorners = new Vector3[4];
        private CallService calls;
        private OperatorDesk desk;
        private RectTransform[] slots = new RectTransform[0];
        private Vector3 alignedMin;
        private Vector3 alignedMax;

        public void Bind(CallService callService, OperatorDesk operatorDesk, RectTransform[] callerSlots)
        {
            Unsubscribe();
            calls = callService;
            desk = operatorDesk;
            slots = callerSlots ?? new RectTransform[0];
            calls.CallAdded += OnCallAdded;
            calls.CallStateChanged += OnCallStateChanged;
            card.Clicked += OnCardClicked;
            card.Hide();
        }

        private void OnDisable() => Unsubscribe();

        private void Unsubscribe()
        {
            if (calls != null)
            {
                calls.CallAdded -= OnCallAdded;
                calls.CallStateChanged -= OnCallStateChanged;
            }
            if (card != null) card.Clicked -= OnCardClicked;
            calls = null;
        }

        private void OnCallAdded(CallAddedArgs e) => card.Show(e.Call);

        private void OnCallStateChanged(CallStateChangedArgs e)
        {
            if (e.Call == card.Call) card.Refresh();
        }

        private void OnCardClicked(CallCardView clicked) => desk.TryTake(clicked.Call.Id);

        // The slots are laid out by the team's layout group, possibly a frame late, so keep following them.
        private void LateUpdate()
        {
            if (slots.Length == 0 || slots[0] == null) return;
            slots[0].GetWorldCorners(slotCorners);
            if (slotCorners[0] == alignedMin && slotCorners[2] == alignedMax) return;
            alignedMin = slotCorners[0];
            alignedMax = slotCorners[2];

            // Both canvases are Screen Space Overlay, so world corners are screen pixels.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cardArea, alignedMin, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cardArea, alignedMax, null, out Vector2 max);
            var cardRect = (RectTransform)card.transform;
            cardRect.localPosition = (min + max) * 0.5f;
            cardRect.sizeDelta = max - min;
        }
    }
}
