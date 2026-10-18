using Switchboard.Domain;
using System.Globalization;
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
        private bool artworkHasCurrencySymbol;
        private TMP_Text[] reels;
        private TMP_Text decimalPoint;
        private const int ReelCount = 6;

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
            if (incomeCoins != null) incomeCoins.Clear();
            slot = headerSlot;
            artworkHasCurrencySymbol = slot != null && slot.GetComponentInParent<Switchboard.UI.UpgradeShopSkin>() != null;
            if (label != null)
            {
                if (artworkHasCurrencySymbol) label.color = new Color32(35, 76, 40, 255);
                ConfigureReels();
                if (wallet != null) ShowBalance(shownCents);
            }
            if (artworkHasCurrencySymbol && plate != null)
                foreach (var graphic in plate.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    if (!(graphic is TMP_Text)) graphic.enabled = false;
            alignedMin = alignedMax = Vector3.zero;
        }

        private string FormatBalance(long cents) => artworkHasCurrencySymbol ? MoneyFormat.Dollars(cents).TrimStart('$') : MoneyFormat.Dollars(cents);

        private void ConfigureReels()
        {
            if (artworkHasCurrencySymbol && reels == null)
            {
                reels = new TMP_Text[ReelCount];
                for (int i = 0; i < ReelCount; i++)
                {
                    reels[i] = CreateReel("Balance digit " + i);
                    var rect = reels[i].rectTransform;
                    rect.anchorMin = new Vector2(i / (float)ReelCount, 0);
                    rect.anchorMax = new Vector2((i + 1) / (float)ReelCount, 1);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                decimalPoint = CreateReel("Balance decimal point");
                decimalPoint.text = ".";
            }
            label.enabled = !artworkHasCurrencySymbol;
            if (reels == null) return;
            foreach (var reel in reels) reel.gameObject.SetActive(artworkHasCurrencySymbol);
            decimalPoint.gameObject.SetActive(artworkHasCurrencySymbol);
        }

        private TMP_Text CreateReel(string name)
        {
            var text = Instantiate(label, plate);
            text.name = name;
            text.enabled = true;
            text.margin = Vector4.zero;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.MidlineGeoAligned;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void ShowBalance(long cents)
        {
            label.text = FormatBalance(cents); // Keep the exact amount for the original layout and coin target.
            if (!artworkHasCurrencySymbol || reels == null) return;
            decimal dollars = cents / 100m;
            string suffix = "";
            // Large balances still fit the six physical reels without clipping significant digits.
            if (dollars >= 10000)
            {
                const string units = "KMBTQ";
                int unit = -1;
                do { dollars /= 1000; unit++; } while (decimal.Round(dollars, 2) >= 1000 && unit < units.Length - 1);
                suffix = units[unit].ToString();
            }
            string amount = dollars.ToString("0.00", CultureInfo.InvariantCulture) + suffix;
            string characters = amount.Replace(".", "");
            int padding = ReelCount - characters.Length;
            for (int i = 0; i < ReelCount; i++) reels[i].text = i < padding ? "" : characters[i - padding].ToString();
            float separator = (padding + amount.IndexOf('.')) / (float)ReelCount;
            var point = decimalPoint.rectTransform;
            point.anchorMin = point.anchorMax = new Vector2(separator, .32f);
            point.pivot = new Vector2(.5f, .5f);
            // Keep the dot on the lit face of the dollar reel, clear of the dark divider.
            point.anchoredPosition = new Vector2(-plate.rect.width / ReelCount * .14f, 0);
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
            ShowBalance(shownCents);
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
            if (artworkHasCurrencySymbol && reels != null)
            {
                float size = plate.rect.width / ReelCount;
                foreach (var reel in reels) reel.fontSize = size;
                decimalPoint.fontSize = size;
                decimalPoint.rectTransform.sizeDelta = new Vector2(size * .3f, size);
                decimalPoint.rectTransform.anchoredPosition = new Vector2(-size * .14f, 0);
            }
        }
    }
}
