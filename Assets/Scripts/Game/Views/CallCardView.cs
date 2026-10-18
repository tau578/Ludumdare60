using System;
using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// A view of one call (Design 5.4): ringing while it waits, a speech bubble while it is in hand or connected,
    /// the cord's colour and the money earned while connected or on hold, then a stamp.
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
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TMP_Text speechLabel;
        [SerializeField, Min(1), Tooltip("How fast the bubble's text types out.")]
        private float charactersPerSecond = 40f;
        [SerializeField] private FloatingTextView feePopup;
        [SerializeField, Tooltip("A ring around the portrait in the colour of the call's cord.")]
        private Graphic cordRing;
        [SerializeField] private GameObject callbackBadge;
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

        public void Show(Call call, Sprite callerPortrait, Func<CordId, Color> cordColorOf)
        {
            gameObject.SetActive(true);
            Initialize();
            Call = call;
            cordColors = cordColorOf;
            portrait.sprite = callerPortrait != null ? callerPortrait : defaultPortrait;
            rateLabel.text = MoneyFormat.CardRate(call.Request.ConnectionFeeCents, call.Request.CentsPerSecond);
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

            ringingPulse.gameObject.SetActive(state == CallState.Incoming);
            callbackBadge.SetActive(Call.Request.Attempt > 1 && (state == CallState.Incoming || inHand));
            earningsBadge.SetActive(onCord);
            cordRing.gameObject.SetActive(onCord);
            if (onCord && Call.Cord is CordId cord && cordColors != null) cordRing.color = Faded(cordColors(cord), onHold);
            earningsLabel.color = Faded(earningsColor, onHold);

            speechBubble.SetActive(inHand || onCord);
            if (inHand || onCord)
                Say(onHold ? "On hold"
                    : onCord ? "On the line"
                    : $"{Listed(Call.Request.CallerPerson)}\n<size=80%>wants</size>\n{Listed(Call.Request.Recipient)}");
            else typedText = null;

            stamp.SetActive(state == CallState.Ended);
            if (state != CallState.Ended) return;
            durationBadge.SetActive(false);
            feePopup.Hide();
            // Cleared calls leave at once; the others show what happened for a moment.
            stampTimeLeft = Call.EndReason == CallEndReason.Cleared ? 0 : stampSeconds;
            stamp.SetActive(stampTimeLeft > 0);
            stampLabel.text = StampText(Call);
            stampLabel.color = stampOutline.effectColor = Ink(Call.EndReason);
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
        }

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
            int seconds = (int)Math.Ceiling(Call.SecondsLeft);
            if (seconds == shownSeconds) return;
            shownSeconds = seconds;
            durationLabel.text = seconds + "s";
        }

        private void Pulse()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulsesPerSecond * Mathf.PI * 2f);
            ringingPulse.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, pulseColor.a * (0.35f + 0.65f * wave));
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

        // The book is sorted by surname, so the surname stands out.
        private static string Listed(IPerson person) =>
            string.IsNullOrEmpty(person.FirstName) ? $"<b>{person.LastName}</b>" : $"{person.FirstName} <b>{person.LastName}</b>";

        private Color Ink(CallEndReason reason)
        {
            switch (reason)
            {
                case CallEndReason.Finished: return paidInk;
                case CallEndReason.DisconnectedEarly: return cutOffInk;
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
                case CallEndReason.Missed: return "Missed";
                default: return string.Empty;
            }
        }
    }
}
