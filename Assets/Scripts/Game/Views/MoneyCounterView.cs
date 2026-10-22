using Switchboard.Domain;
using Switchboard.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// The balance on the shop header's reels. D60 (V1, V2): a changed reel rolls like an odometer, the plate glows
    /// softly when income lands, and a fine shakes the plaque red and sends coins to the missed card.
    /// </summary>
    public sealed class MoneyCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform plate;
        [SerializeField, Tooltip("\"-$2.00\" in red when a fine is taken.")] private FloatingTextView finePopup;
        [SerializeField, Tooltip("\"-$8.00\" in a neutral ink when an upgrade is bought.")] private FloatingTextView spendPopup;
        [SerializeField, Min(.01f), Tooltip("How long a reel takes to roll to its new digit.")] private float rollSeconds = .15f;
        [SerializeField, Min(0), Tooltip("How long the plate's glow lasts when income lands.")] private float glowSeconds = .6f;
        [SerializeField, Range(0, 1)] private float glowAlpha = .4f;
        [SerializeField] private Color glowColor = new Color32(255, 236, 160, 255);
        [SerializeField, Min(0)] private float fineShakeSeconds = .3f;
        [SerializeField, Min(0)] private float fineShakePixels = 6f;
        [SerializeField] private Color fineInk = new Color32(196, 40, 40, 255);
        [SerializeField, Min(1), Tooltip("The decimal point is small at the digits' size: drawn this much bigger on its own drum.")]
        private float dotScale = 1.9f;
        [SerializeField, Range(-.5f, .5f), Tooltip("Where the dot sits on its drum, as a share of the drum's height (negative: lower).")]
        private float dotLift = -.12f;

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
        // The digit leaving each reel while it rolls, and how far each roll has got (-1: still).
        private TMP_Text[] leaving;
        private float[] rollTime;
        private LampGlowGraphic glow;
        private float glowTime = -1;
        private bool heldForCoins;
        private const int ReelCount = 6;
        private const float InsufficientFundsDuration = .5f;
        private float shakeDuration;
        private float shakePixels;
        private Vector3 shakeAxis;
        private float insufficientFundsTime;
        private RectTransform shakeTarget;
        private Vector3 shakeOffset;
        private Color restingColor;
        private static readonly string[] Characters = BuildCharacters();
        private const string Units = "MBTQ";
        private readonly char[] reelCharacters = new char[ReelCount];

        /// <summary>The reels' current characters, for tests; null without the artwork's reels.</summary>
        public TMP_Text[] Reels => reels;
        /// <summary>Whether reel <paramref name="index"/> is rolling to a new character.</summary>
        public bool IsRolling(int index) => rollTime != null && rollTime[index] >= 0;
        public bool IsGlowing => glowTime >= 0;
        public bool IsShaking => shakeTarget != null;

        /// <param name="headerSlot">The shop panel's header plaque to sit in; null keeps the prefab position.</param>
        public void Bind(Wallet source, RectTransform headerSlot, PenaltyService fines, UpgradeService shop)
        {
            ResetInsufficientFunds();
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
                if (wallet != null) ShowBalance(shownCents, false);
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
                leaving = new TMP_Text[ReelCount];
                rollTime = new float[ReelCount];
                for (int i = 0; i < ReelCount; i++)
                {
                    // Each reel is a window that clips its rolling digits (V1).
                    var window = new GameObject("Balance reel " + i, typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                    window.SetParent(plate, false);
                    window.anchorMin = new Vector2(i / (float)ReelCount, 0);
                    window.anchorMax = new Vector2((i + 1) / (float)ReelCount, 1);
                    window.offsetMin = window.offsetMax = Vector2.zero;
                    reels[i] = CreateReel("Balance digit " + i, window);
                    leaving[i] = CreateReel("Leaving digit " + i, window);
                    leaving[i].text = "";
                    leaving[i].gameObject.SetActive(false);
                    rollTime[i] = -1;
                }
            }
            label.enabled = !artworkHasCurrencySymbol;
            if (reels == null) return;
            foreach (var reel in reels) reel.transform.parent.gameObject.SetActive(artworkHasCurrencySymbol);
        }

        private TMP_Text CreateReel(string name, RectTransform parent)
        {
            var text = Instantiate(label, parent);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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

        private void ShowBalance(long cents, bool roll)
        {
            label.text = FormatBalance(cents); // Keep the exact amount for the original layout and coin target.
            if (!artworkHasCurrencySymbol || reels == null) return;
            FillReels(cents, reelCharacters);
            for (int i = 0; i < ReelCount; i++) SetReel(i, CharacterString(reelCharacters[i]), roll);
        }

        /// <summary>
        /// What the six drums show, right-aligned, with the decimal point on a drum of its own (11c playtest):
        /// "  5.00" and "442.90" under $1,000; whole dollars ("  1000", "123456") up to $999,999; then four digits
        /// with a unit ("1.234M", "12.34M", "123.4M", then B, T and Q), cut off rather than rounded up.
        /// Blank drums are spaces. Allocates nothing.
        /// </summary>
        public static void FillReels(long cents, char[] into)
        {
            for (int i = 0; i < ReelCount; i++) into[i] = ' ';
            if (cents < 0) cents = 0;
            long dollars = cents / 100;
            if (dollars < 1000)
            {
                into[5] = (char)('0' + cents % 10);
                into[4] = (char)('0' + cents / 10 % 10);
                into[3] = '.';
                WriteDigits(dollars, into, 3);
                return;
            }
            if (dollars < 1000000)
            {
                WriteDigits(dollars, into, ReelCount);
                return;
            }
            int unit = 0;
            long unitSize = 1000000;
            while (unit < Units.Length - 1 && dollars / 1000 >= unitSize)
            {
                unitSize *= 1000;
                unit++;
            }
            long whole = dollars / unitSize;
            int fractionDigits = whole >= 100 ? 1 : whole >= 10 ? 2 : 3;
            long step = unitSize;
            for (int i = 0; i < fractionDigits; i++) step /= 10;
            long fraction = dollars % unitSize / step;
            into[5] = Units[unit];
            int at = 5;
            for (int i = 0; i < fractionDigits; i++, fraction /= 10) into[--at] = (char)('0' + fraction % 10);
            into[--at] = '.';
            WriteDigits(whole, into, at);
        }

        // Writes the number's digits so that its last one sits just before index end.
        private static void WriteDigits(long value, char[] into, int end)
        {
            do
            {
                into[--end] = (char)('0' + value % 10);
                value /= 10;
            } while (value > 0 && end > 0);
        }

        // Only a reel whose character changes rolls: the old one slides up and out, the new one comes in from below.
        private void SetReel(int index, string text, bool roll)
        {
            TMP_Text reel = reels[index];
            if (reel.text == text) return;
            if (roll)
            {
                leaving[index].text = reel.text;
                leaving[index].gameObject.SetActive(true);
                rollTime[index] = 0;
            }
            reel.text = text;
            FitCharacter(reel);
            FitCharacter(leaving[index]);
            if (!roll) StopRoll(index);
            PlaceRoll(index);
        }

        private void TickRolls(float dt)
        {
            if (rollTime == null) return;
            for (int i = 0; i < ReelCount; i++)
            {
                if (rollTime[i] < 0) continue;
                rollTime[i] += dt;
                if (rollTime[i] >= rollSeconds) StopRoll(i);
                PlaceRoll(i);
            }
        }

        private void StopRoll(int index)
        {
            rollTime[index] = -1;
            leaving[index].gameObject.SetActive(false);
        }

        private void PlaceRoll(int index)
        {
            float height = ((RectTransform)reels[index].transform.parent).rect.height;
            float t = rollTime[index] < 0 ? 1 : Mathf.SmoothStep(0, 1, rollTime[index] / rollSeconds);
            reels[index].rectTransform.anchoredPosition = new Vector2(0, -height * (1 - t) + Lift(reels[index], height));
            leaving[index].rectTransform.anchoredPosition = new Vector2(0, height * t + Lift(leaving[index], height));
        }

        // The decimal point has a drum to itself, so it is drawn bigger and a little low to read as a point.
        private void FitCharacter(TMP_Text reel)
        {
            float scale = IsDot(reel) ? dotScale : 1;
            reel.rectTransform.localScale = new Vector3(scale, scale, 1);
        }

        private float Lift(TMP_Text reel, float height) => IsDot(reel) ? height * dotLift : 0;

        private static bool IsDot(TMP_Text reel) => reel.text == ".";

        private static string CharacterString(char c) => c < Characters.Length && Characters[c] != null ? Characters[c] : c.ToString();

        private static string[] BuildCharacters()
        {
            var table = new string[128];
            foreach (char c in "0123456789." + Units) table[c] = c.ToString();
            table[' '] = "";
            return table;
        }

        /// <summary>A fine was taken (V2): coins leave the counter for the missed card and fade.</summary>
        public void FlyFineTo(RectTransform card, long takenCents)
        {
            if (!isActiveAndEnabled || card == null || label == null || takenCents <= 0) return;
            if (incomeCoins == null) incomeCoins = IncomeCoinsView.Create(transform);
            int count = 3 + (takenCents >= 200 ? 1 : 0) + (takenCents >= 500 ? 1 : 0);
            incomeCoins.FlyAway(plate != null ? plate : label.rectTransform, card, count);
        }

        private void StartGlow()
        {
            if (plate == null || glowSeconds <= 0) return;
            if (glow == null)
            {
                glow = new GameObject("Income glow", typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<LampGlowGraphic>();
                var rect = glow.rectTransform;
                rect.SetParent(plate, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                glow.raycastTarget = false;
                glow.color = glowColor;
            }
            glowTime = 0;
            glow.enabled = true;
            glow.gameObject.SetActive(true);
            TickGlow(0);
        }

        // A soft swell and fade; only the renderer's alpha changes, so the mesh is built once.
        private void TickGlow(float dt)
        {
            if (glowTime < 0 || glow == null) return;
            glowTime += dt;
            float t = glowTime / glowSeconds;
            if (t >= 1)
            {
                glowTime = -1;
                glow.gameObject.SetActive(false);
                return;
            }
            float swell = t < .2f ? t / .2f : 1 - (t - .2f) / .8f;
            glow.canvasRenderer.SetAlpha(glowAlpha * swell);
        }

        private void OnDisable()
        {
            ResetInsufficientFunds();
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

        public void ShowInsufficientFunds() => Shake(Vector3.up, InsufficientFundsDuration, 8f, new Color32(178, 62, 62, 255));

        /// <summary>A fine (V2): the plaque shakes sideways and flashes red.</summary>
        public void ShowFine() => Shake(Vector3.right, fineShakeSeconds, fineShakePixels, fineInk);

        private void Shake(Vector3 axis, float seconds, float pixels, Color ink)
        {
            if (!isActiveAndEnabled || label == null || seconds <= 0) return;
            ResetInsufficientFunds();
            shakeTarget = plate != null ? plate : label.rectTransform;
            restingColor = label.color;
            shakeAxis = axis;
            shakeDuration = seconds;
            shakePixels = pixels;
            insufficientFundsTime = seconds;
            SetBalanceColor(ink);
        }

        private void TickInsufficientFunds(float dt)
        {
            if (insufficientFundsTime <= 0) return;
            insufficientFundsTime = Mathf.Max(0, insufficientFundsTime - dt);
            float progress = 1 - insufficientFundsTime / shakeDuration;
            shakeOffset = shakeAxis * (Mathf.Sin(progress * Mathf.PI * 10) * (1 - progress) * shakePixels);
            if (shakeTarget != null) shakeTarget.localPosition += shakeOffset;
            if (insufficientFundsTime <= 0) ResetInsufficientFunds();
        }

        private void ResetInsufficientFunds()
        {
            if (shakeTarget == null) return;
            RemoveShakeOffset();
            SetBalanceColor(restingColor);
            shakeTarget = null;
            insufficientFundsTime = 0;
        }

        private void SetBalanceColor(Color color)
        {
            if (label != null) label.color = color;
            if (reels != null)
                foreach (var reel in reels) if (reel != null) reel.color = color;
            if (leaving != null)
                foreach (var reel in leaving) if (reel != null) reel.color = color;
        }

        private void RemoveShakeOffset()
        {
            if (shakeTarget != null) shakeTarget.localPosition -= shakeOffset;
            shakeOffset = Vector3.zero;
        }

        private void Unsubscribe()
        {
            if (penalties != null) penalties.FineApplied -= OnFineApplied;
            penalties = null;
            if (upgrades != null) upgrades.UpgradeBought -= OnUpgradeBought;
            upgrades = null;
        }

        // Nothing taken, nothing to show: the balance was already at zero.
        // The plaque shakes for any fine (a missed emergency), even when the wallet had nothing left to take.
        private void OnFineApplied(FineAppliedArgs e)
        {
            if (finePopup != null && e.TakenCents > 0) finePopup.Show(MoneyFormat.Dollars(-e.TakenCents));
            ShowFine();
        }

        private void OnUpgradeBought(UpgradeBought e)
        {
            if (spendPopup != null && e.SpentCents > 0) spendPopup.Show(MoneyFormat.Dollars(-e.SpentCents));
        }

        private void LateUpdate()
        {
            RemoveShakeOffset();
            FollowSlot();
            float dt = Time.unscaledDeltaTime;
            TickInsufficientFunds(dt);
            if (incomeCoins != null) incomeCoins.Tick(dt);
            RefreshBalance();
            TickRolls(dt);
            TickGlow(dt);
        }

        private void RefreshBalance()
        {
            // Money changes every frame while calls pay; the text changes only when the shown cents do.
            if (wallet == null || wallet.Cents == shownCents) return;
            // Hold positive changes until the last coin arrives. Spending and fines remain immediate.
            // This is presentation only: income, upgrade eligibility and rounding stay in the domain.
            bool rise = shownCents >= 0 && wallet.Cents > shownCents;
            if (incomeCoins != null && incomeCoins.IsFlying && rise)
            {
                heldForCoins = true;
                return;
            }
            // The glow marks a lump of income landing with its coins, not the steady trickle of per-second pay.
            if (rise && heldForCoins) StartGlow();
            heldForCoins = false;
            shownCents = wallet.Cents;
            ShowBalance(shownCents, true);
        }

        // The slot lives on the board's canvas, so keep following it through resolution and layout changes.
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
                foreach (var reel in leaving) reel.fontSize = size;
            }
        }
    }
}
