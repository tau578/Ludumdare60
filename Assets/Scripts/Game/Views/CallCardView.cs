using System;
using Switchboard.Content;
using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// A view of one call (Design 5.4): ringing while it waits, a speech bubble while it is in hand or connected,
    /// the cord's colour and the money earned while connected or on hold, then a stamp. Types with a response time
    /// (emergencies) also get their badge, their accent and a countdown ring around the portrait.
    /// </summary>
    public sealed class CallCardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text rateLabel;
        [SerializeField] private Graphic ringingPulse;
        [SerializeField] private GameObject stamp;
        [SerializeField] private TMP_Text stampLabel;
        [SerializeField] private Outline stampOutline;
        [SerializeField] private Color paidInk = new Color32(38, 104, 58, 255);
        [SerializeField] private Color wrongInk = new Color32(164, 38, 38, 255);
        [SerializeField] private Color cutOffInk = new Color32(92, 80, 68, 255);
        [SerializeField] private Color missedInk = new Color32(116, 18, 30, 255);
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TMP_Text speechLabel;
        [SerializeField, Min(1), Tooltip("How fast the bubble's text types out.")]
        private float charactersPerSecond = 40f;
        [SerializeField, Range(50, 100), Tooltip("Size of a jack the Notepad noted, in percent of the name's size.")]
        private int jackTextSize = 85;
        [SerializeField] private FloatingTextView feePopup;
        [SerializeField, Tooltip("A ring around the portrait in the colour of the call's cord.")]
        private Graphic cordRing;
        [SerializeField] private GameObject callbackBadge;
        [SerializeField, Tooltip("How far the callback badge moves down when the type badge takes the top.")]
        private float stackedBadgeOffset = -34f;
        [SerializeField, Tooltip("The caller type's badge, e.g. EMERGENCY.")] private GameObject typeBadge;
        [SerializeField] private TMP_Text typeBadgeLabel;
        [SerializeField] private Graphic typeBadgeBackground;
        [SerializeField, Tooltip("Takes the type's accent on cards of a type that has one.")] private Outline rateOutline;
        [SerializeField, Tooltip("The countdown ring's dark track; shown while a timed call waits.")]
        private GameObject responseRingRoot;
        [SerializeField, Tooltip("A radial-filled ring that empties as the response time runs out.")]
        private Image responseRing;
        [SerializeField, Min(0), Tooltip("Seconds left when the ring turns urgent.")] private float urgentSeconds = 10f;
        [SerializeField] private Color urgentRingColor = new Color32(255, 92, 48, 255);
        [SerializeField, Min(0)] private float urgentPulsesPerSecond = 3f;
        [SerializeField] private GameObject earningsBadge;
        [SerializeField] private TMP_Text earningsLabel;
        [SerializeField, Range(0, 1), Tooltip("How strongly the cord ring and earnings show while the call is on hold.")]
        private float onHoldAlpha = 0.45f;
        [SerializeField, Tooltip("Dev only: the hidden duration, shown when the dev panel asks.")]
        private GameObject durationBadge;
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField, Min(0)] private float stampSeconds = 1.5f;
        [SerializeField, Min(0)] private float pulsesPerSecond = 1.5f;
        [SerializeField, Tooltip("Squashes and bounces when a click takes the call.")]
        private RectTransform pressTarget;
        [SerializeField, Tooltip("A white copy of the portrait that flashes on the press.")]
        private Image pressFlash;
        [SerializeField, Min(0.01f)] private float pressSeconds = 0.2f;
        [SerializeField, Range(0, 1)] private float pressFlashAlpha = 0.45f;

        private Sprite defaultPortrait;
        private Color pulseColor;
        private Color earningsColor;
        private Color rateOutlineColor;
        private Color typeBadgeColor;
        private Color ringColor;
        private Color haloColor;
        private Vector2 callbackBadgePosition;
        private float shownRingFill = -1;
        private bool ringWasUrgent;
        private bool initialized;
        private float stampTimeLeft;
        private Func<CordId, Color> cordColors;
        private long shownCents = -1;
        private int shownSeconds = -1;
        private readonly TypingReveal typing = new TypingReveal();
        private string typedText;
        private float pressTime = -1;

        public event Action<CallCardView> Clicked;

        /// <summary>Raised when the stamp has shown long enough and the card can leave its slot.</summary>
        public event Action<CallCardView> Finished;

        public Call Call { get; private set; }
        public bool ShowDuration { get; set; }

        public void Show(Call call, Sprite callerPortrait, Func<CordId, Color> cordColorOf, CallCardStyle style)
        {
            gameObject.SetActive(true);
            Initialize();
            Call = call;
            cordColors = cordColorOf;
            portrait.sprite = callerPortrait != null ? callerPortrait : defaultPortrait;
            rateLabel.text = MoneyFormat.CardRate(call.Request.ConnectionFeeCents, call.Request.CentsPerSecond);
            ApplyStyle(style);
            shownRingFill = -1;
            ringWasUrgent = false;
            feePopup.Hide();
            shownCents = -1;
            shownSeconds = -1;
            typedText = null;
            StopPress();
            Refresh();
        }

        public void Refresh()
        {
            if (Call == null) return;
            CallState state = Call.State;
            bool inHand = state == CallState.Taken || state == CallState.Connecting;
            bool onHold = state == CallState.OnHold;
            // An on-hold call keeps its cord ring and its earnings, frozen until it resumes (D07).
            bool onCord = state == CallState.Connected || onHold;

            bool waiting = state == CallState.Incoming || inHand;
            ringingPulse.gameObject.SetActive(state == CallState.Incoming);
            // The type badge sits on top while the call waits; once connected the earnings take that place.
            bool showTypeBadge = waiting && typeBadge != null && !string.IsNullOrEmpty(typeBadgeLabel.text);
            if (typeBadge != null) typeBadge.SetActive(showTypeBadge);
            callbackBadge.SetActive(Call.Request.Attempt > 1 && waiting);
            ((RectTransform)callbackBadge.transform).anchoredPosition =
                callbackBadgePosition + (showTypeBadge ? new Vector2(0, stackedBadgeOffset) : Vector2.zero);
            // The response timer stops for good on a correct connection (D09), so the ring goes with it.
            if (responseRingRoot != null) responseRingRoot.SetActive(Call.HasResponseTime && waiting);
            earningsBadge.SetActive(onCord);
            cordRing.gameObject.SetActive(onCord);
            if (onCord && Call.Cord is CordId cord && cordColors != null) cordRing.color = Faded(cordColors(cord), onHold);
            earningsLabel.color = Faded(earningsColor, onHold);

            speechBubble.SetActive(inHand || onCord);
            if (inHand || onCord)
                Say(onHold ? "On hold"
                    : onCord ? "On the line"
                    : InHandText());
            else typedText = null;

            stamp.SetActive(state == CallState.Ended);
            if (state != CallState.Ended) return;
            durationBadge.SetActive(false);
            feePopup.Hide();
            // Cleared calls leave at once; the others show what happened for a moment.
            stampTimeLeft = Call.EndReason == CallEndReason.Cleared ? 0 : stampSeconds;
            stamp.SetActive(stampTimeLeft > 0);
            RefreshStamp();
        }

        /// <summary>The fine is known only after the call has ended, so the stamp is written again when it lands.</summary>
        public void RefreshStamp()
        {
            if (Call == null || Call.State != CallState.Ended) return;
            stampLabel.text = StampText(Call);
            stampLabel.color = stampOutline.effectColor = Ink(Call.EndReason);
        }

        /// <summary>A new Notepad note: the jack joins the bubble at once, without typing the bubble again.</summary>
        public void ShowNotes()
        {
            if (Call == null || !speechBubble.activeSelf) return;
            if (Call.State != CallState.Taken && Call.State != CallState.Connecting) return;
            string text = InHandText();
            if (text == typedText) return;
            typedText = text;
            speechLabel.text = text;
            speechLabel.ForceMeshUpdate();
            typing.Resize(speechLabel.textInfo.characterCount);
            speechLabel.maxVisibleCharacters = typing.Visible;
        }

        public void ShowConnectionFee(double cents) => feePopup.Show("+" + MoneyFormat.Cents(cents));

        /// <summary>A quick squash, bounce and brighten for a click that took the call.</summary>
        public void PlayPress()
        {
            if (pressTarget == null) return;
            pressTime = 0;
            if (pressFlash == null) return;
            pressFlash.sprite = portrait.sprite;
            pressFlash.gameObject.SetActive(true);
        }

        public void Hide()
        {
            Call = null;
            cordColors = null;
            speechBubble.SetActive(false);
            typedText = null;
            StopPress();
            feePopup.Hide();
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Call != null && Call.State == CallState.Incoming)
                Clicked?.Invoke(this);
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            defaultPortrait = portrait.sprite;
            pulseColor = ringingPulse.color;
            earningsColor = earningsLabel.color;
            if (rateOutline != null) rateOutlineColor = rateOutline.effectColor;
            if (typeBadgeBackground != null) typeBadgeColor = typeBadgeBackground.color;
            if (responseRing != null) ringColor = responseRing.color;
            callbackBadgePosition = ((RectTransform)callbackBadge.transform).anchoredPosition;
        }

        // Not colour alone (Design 5.4): the badge text and the ring carry the meaning, the accent only adds to it.
        // The ring keeps its own light colour, so it reads against an accent-tinted halo.
        private void ApplyStyle(CallCardStyle style)
        {
            if (typeBadgeLabel != null) typeBadgeLabel.text = style.HasBadge ? style.badgeText : string.Empty;
            if (typeBadgeBackground != null) typeBadgeBackground.color = style.HasAccent ? Opaque(style.accent, typeBadgeColor.a) : typeBadgeColor;
            if (rateOutline != null) rateOutline.effectColor = style.HasAccent ? Opaque(style.accent, rateOutlineColor.a) : rateOutlineColor;
            haloColor = style.HasAccent ? Opaque(style.accent, pulseColor.a) : pulseColor;
            if (responseRing == null) return;
            responseRing.color = ringColor;
            responseRing.rectTransform.localScale = Vector3.one;
        }

        private static Color Opaque(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private void Update()
        {
            if (Call == null) return;
            if (pressTime >= 0) AnimatePress();
            if (typing.IsTyping) TypeOn();
            if (Call.State == CallState.Incoming) Pulse();
            else if (Call.State == CallState.Ended && (stampTimeLeft -= Time.deltaTime) <= 0) Finished?.Invoke(this);
        }

        // Earnings and the dev duration change every frame; the text changes only when the shown number does.
        private void LateUpdate()
        {
            if (Call == null || Call.State == CallState.Ended) return;
            if (responseRing != null && responseRingRoot != null && responseRingRoot.activeSelf) UpdateResponseRing();
            if (Call.State == CallState.Connected || Call.State == CallState.OnHold)
            {
                long cents = (long)Math.Floor(Call.CentsEarned + 1e-9);
                if (cents != shownCents)
                {
                    shownCents = cents;
                    earningsLabel.text = MoneyFormat.Dollars(cents);
                }
            }

            durationBadge.SetActive(ShowDuration);
            if (!ShowDuration) return;
            // While an emergency waits, the dev badge also shows the seconds left to connect it: "25s / 14s".
            bool responding = Call.HasResponseTime && Call.State != CallState.Connected && Call.State != CallState.OnHold;
            int response = responding ? (int)Math.Ceiling(Call.ResponseSecondsLeft) : -1;
            int seconds = (int)Math.Ceiling(Call.SecondsLeft);
            int key = seconds * 1000 + response + 1;
            if (key == shownSeconds) return;
            shownSeconds = key;
            durationLabel.text = responding ? response + "s / " + seconds + "s" : seconds + "s";
        }

        // The fill changes every frame, so it is only written when it moves by a visible step.
        private void UpdateResponseRing()
        {
            double total = Call.Request.Type.ResponseTimeSeconds;
            float fill = total > 0 ? Mathf.Clamp01((float)(Call.ResponseSecondsLeft / total)) : 0f;
            fill = Mathf.Ceil(fill * 360f) / 360f;
            bool urgent = Call.ResponseSecondsLeft <= urgentSeconds;
            if (fill != shownRingFill)
            {
                shownRingFill = fill;
                responseRing.fillAmount = fill;
            }
            if (urgent)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * urgentPulsesPerSecond * Mathf.PI * 2f);
                Color hot = urgentRingColor;
                responseRing.color = new Color(hot.r, hot.g, hot.b, 0.55f + 0.45f * wave);
                responseRing.rectTransform.localScale = Vector3.one * (1f + 0.04f * wave);
                ringWasUrgent = true;
            }
            else if (ringWasUrgent)
            {
                ringWasUrgent = false;
                responseRing.color = ringColor;
                responseRing.rectTransform.localScale = Vector3.one;
            }
        }

        private void Pulse()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulsesPerSecond * Mathf.PI * 2f);
            ringingPulse.color = new Color(haloColor.r, haloColor.g, haloColor.b, haloColor.a * (0.35f + 0.65f * wave));
            ringingPulse.rectTransform.localScale = Vector3.one * (1f + 0.1f * wave);
        }

        // Restarts only when the words change, so Taken to Connecting keeps typing where it was.
        private void Say(string text)
        {
            if (text == typedText) return;
            typedText = text;
            speechLabel.text = text;
            speechLabel.maxVisibleCharacters = 0;
            speechLabel.ForceMeshUpdate();
            typing.Restart(speechLabel.textInfo.characterCount);
            speechLabel.maxVisibleCharacters = typing.Visible;
            if (typing.Visible > 0) OnCharacterShown(0);
        }

        private void TypeOn()
        {
            int before = typing.Visible;
            int now = typing.Advance(Time.unscaledDeltaTime, charactersPerSecond);
            for (int i = before; i < now; i++) OnCharacterShown(i);
            speechLabel.maxVisibleCharacters = now;
        }

        // The caller's talking sound will play from here (Step 8).
        private void OnCharacterShown(int index)
        {
        }

        private void AnimatePress()
        {
            pressTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(pressTime / pressSeconds);
            float y = PressScale(t);
            pressTarget.localScale = new Vector3(1f + (1f - y) * 0.5f, y, 1f);
            if (pressFlash != null)
                pressFlash.color = new Color(1f, 1f, 1f, pressFlashAlpha * (1f - t) * (1f - t));
            if (t >= 1f) StopPress();
        }

        // Down to 86%, up past full size, then settle.
        private static float PressScale(float t)
        {
            if (t < 0.3f) return Mathf.Lerp(1f, 0.86f, Mathf.SmoothStep(0, 1, t / 0.3f));
            if (t < 0.65f) return Mathf.Lerp(0.86f, 1.06f, Mathf.SmoothStep(0, 1, (t - 0.3f) / 0.35f));
            return Mathf.Lerp(1.06f, 1f, Mathf.SmoothStep(0, 1, (t - 0.65f) / 0.35f));
        }

        private void StopPress()
        {
            pressTime = -1;
            if (pressTarget != null) pressTarget.localScale = Vector3.one;
            if (pressFlash != null) pressFlash.gameObject.SetActive(false);
        }

        private Color Faded(Color color, bool onHold) => onHold ? new Color(color.r, color.g, color.b, color.a * onHoldAlpha) : color;

        private string InHandText() =>
            $"{Listed(Call.Request.CallerPerson)}{Noted(Call.CallerNote)}\n<size=80%>wants</size>\n" +
            $"{Listed(Call.Request.Recipient)}{Noted(Call.RecipientNote)}";

        private string Noted(JackId? jack) => jack.HasValue ? $" <size={jackTextSize}%>({jack.Value.Label})</size>" : string.Empty;

        // The book is sorted by surname, so the surname stands out.
        private static string Listed(IPerson person) =>
            string.IsNullOrEmpty(person.FirstName) ? $"<b>{person.LastName}</b>" : $"{person.FirstName} <b>{person.LastName}</b>";

        private Color Ink(CallEndReason reason)
        {
            switch (reason)
            {
                case CallEndReason.Finished: return paidInk;
                case CallEndReason.DisconnectedEarly: return cutOffInk;
                case CallEndReason.Missed: return missedInk;
                default: return wrongInk;
            }
        }

        private static string StampText(Call call)
        {
            switch (call.EndReason)
            {
                case CallEndReason.Finished: return "Paid " + MoneyFormat.Dollars((long)Math.Round(call.CentsEarned));
                case CallEndReason.WrongConnection: return "Wrong number!";
                case CallEndReason.DisconnectedEarly: return "Cut off";
                // The amount actually taken; nothing could be taken from an empty wallet.
                case CallEndReason.Missed: return call.FineCents > 0 ? "Missed " + MoneyFormat.Dollars(-call.FineCents) : "Missed";
                default: return string.Empty;
            }
        }
    }
}
