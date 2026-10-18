using Switchboard.Domain;
using TMPro;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class MoneyCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform plate;
        [SerializeField, Tooltip("\"-$2.00\" in red when a fine is taken.")] private FloatingTextView finePopup;
        [SerializeField, Tooltip("\"-$8.00\" in a neutral ink when an upgrade is bought.")] private FloatingTextView spendPopup;

        private readonly Vector3[] slotCorners = new Vector3[4];
        private Wallet wallet;
        private PenaltyService penalties;
        private UpgradeService upgrades;
        private long shownCents = -1;
        private RectTransform slot;
        private Vector3 alignedMin;
        private Vector3 alignedMax;
        private IncomeCoinsView incomeCoins;

        /// <param name="headerSlot">The shop panel's header plaque to sit in; null keeps the prefab position.</param>
        public void Bind(Wallet source, RectTransform headerSlot, PenaltyService fines, UpgradeService shop)
        {
            Unsubscribe();
            wallet = source;
            penalties = fines;
            upgrades = shop;
            if (penalties != null) penalties.FineApplied += OnFineApplied;
            if (upgrades != null) upgrades.UpgradeBought += OnUpgradeBought;
            if (finePopup != null) finePopup.Hide();
            if (spendPopup != null) spendPopup.Hide();
            shownCents = wallet != null ? wallet.Cents : -1;
            if (label != null && wallet != null) label.text = MoneyFormat.Dollars(shownCents);
            if (incomeCoins != null) incomeCoins.Clear();
            slot = headerSlot;
            alignedMin = alignedMax = Vector3.zero;
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (incomeCoins != null) incomeCoins.Clear();
        }

        private void OnDestroy()
        {
            if (incomeCoins == null) return;
            if (Application.isPlaying) Destroy(incomeCoins.gameObject);
            else DestroyImmediate(incomeCoins.gameObject);
        }

        public void FlyIncomeFrom(RectTransform portrait)
        {
            if (!isActiveAndEnabled || portrait == null || label == null) return;
            if (incomeCoins == null) incomeCoins = IncomeCoinsView.Create(transform);
            incomeCoins.Fly(portrait, label.rectTransform);
        }

        private void Unsubscribe()
        {
            if (penalties != null) penalties.FineApplied -= OnFineApplied;
            penalties = null;
            if (upgrades != null) upgrades.UpgradeBought -= OnUpgradeBought;
            upgrades = null;
        }

        // Nothing taken, nothing to show: the balance was already at zero.
        private void OnFineApplied(FineAppliedArgs e)
        {
            if (finePopup != null && e.TakenCents > 0) finePopup.Show(MoneyFormat.Dollars(-e.TakenCents));
        }

        private void OnUpgradeBought(UpgradeBought e)
        {
            if (spendPopup != null && e.SpentCents > 0) spendPopup.Show(MoneyFormat.Dollars(-e.SpentCents));
        }

        private void LateUpdate()
        {
            FollowSlot();
            if (incomeCoins != null) incomeCoins.Tick(Time.unscaledDeltaTime);
            RefreshBalance();
        }

        private void RefreshBalance()
        {
            // Money changes every frame while calls pay; the text changes only when the shown cents do.
            if (wallet == null || wallet.Cents == shownCents) return;
            // Hold positive changes until the last coin arrives. Spending and fines remain immediate.
            // This is presentation only: income, upgrade eligibility and rounding stay in the domain.
            if (incomeCoins != null && incomeCoins.IsFlying && shownCents >= 0 && wallet.Cents > shownCents) return;
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
