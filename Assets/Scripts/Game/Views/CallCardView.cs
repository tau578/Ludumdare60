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
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TMP_Text speechLabel;
        [SerializeField] private FloatingTextView feePopup;
        [SerializeField, Tooltip("A ring around the portrait in the colour of the call's cord.")]
        private Graphic cordRing;
        [SerializeField] private GameObject callbackBadge;
        [SerializeField] private GameObject earningsBadge;
        [SerializeField] private TMP_Text earningsLabel;
        [SerializeField, Tooltip("Dev only: the hidden duration, shown when the dev panel asks.")]
        private GameObject durationBadge;
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField, Min(0)] private float stampSeconds = 1.5f;
        [SerializeField, Min(0)] private float pulsesPerSecond = 1.5f;

        private Sprite defaultPortrait;
        private Color pulseColor;
        private bool initialized;
        private float stampTimeLeft;
        private Func<CordId, Color> cordColors;
        private long shownCents = -1;
        private int shownSeconds = -1;

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
            if (onCord && Call.Cord is CordId cord && cordColors != null) cordRing.color = cordColors(cord);

            speechBubble.SetActive(inHand || onCord);
            if (inHand || onCord)
                speechLabel.text = onHold ? "On hold"
                    : onCord ? "On the line"
                    : $"{Call.Request.CallerPerson.FullName()}\nwants {Call.Request.Recipient.FullName()}";

            stamp.SetActive(state == CallState.Ended);
            if (state != CallState.Ended) return;
            durationBadge.SetActive(false);
            feePopup.Hide();
            // Cleared calls leave at once; the others show what happened for a moment.
            stampTimeLeft = Call.EndReason == CallEndReason.Cleared ? 0 : stampSeconds;
            stamp.SetActive(stampTimeLeft > 0);
            stampLabel.text = StampText(Call);
        }

        public void ShowConnectionFee(double cents) => feePopup.Show("+" + MoneyFormat.Cents(cents));

        public void Hide()
        {
            Call = null;
            cordColors = null;
            speechBubble.SetActive(false);
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
        }

        private void Update()
        {
            if (Call == null) return;
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
