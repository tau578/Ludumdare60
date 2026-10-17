using Switchboard.Domain;
using TMPro;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class MoneyCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform plate;

        private readonly Vector3[] slotCorners = new Vector3[4];
        private Wallet wallet;
        private long shownCents = -1;
        private RectTransform slot;
        private Vector3 alignedMin;
        private Vector3 alignedMax;

        /// <param name="headerSlot">The shop panel's header plaque to sit in; null keeps the prefab position.</param>
        public void Bind(Wallet source, RectTransform headerSlot)
        {
            wallet = source;
            shownCents = -1;
            slot = headerSlot;
            alignedMin = alignedMax = Vector3.zero;
        }

        private void LateUpdate()
        {
            FollowSlot();

            // Money changes every frame while calls pay; the text changes only when the shown cents do.
            if (wallet == null || wallet.Cents == shownCents) return;
            shownCents = wallet.Cents;
            label.text = MoneyFormat.Dollars(shownCents);
        }

        // The slot lives on the team's canvas, so keep following it through resolution and layout changes.
        private void FollowSlot()
        {
            if (slot == null || plate == null) return;
            slot.GetWorldCorners(slotCorners);
            if (slotCorners[0] == alignedMin && slotCorners[2] == alignedMax) return;
            alignedMin = slotCorners[0];
            alignedMax = slotCorners[2];

            // Both canvases are Screen Space Overlay, so world corners are screen pixels.
            var area = (RectTransform)plate.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, alignedMin, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, alignedMax, null, out Vector2 max);
            plate.localPosition = (min + max) * 0.5f;
            plate.sizeDelta = max - min;
        }
    }
}
