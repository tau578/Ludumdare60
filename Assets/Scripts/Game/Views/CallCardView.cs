using System;
using Switchboard.Content;
using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>A view of one call: the ringing card, the ticket while it is in hand or connected, then a stamp.</summary>
    public sealed class CallCardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text rateLabel;
        [SerializeField] private Graphic ringingPulse;
        [SerializeField] private GameObject stamp;
        [SerializeField] private TMP_Text stampLabel;
        [SerializeField] private GameObject ticket;
        [SerializeField] private TMP_Text ticketTitle;
        [SerializeField] private TMP_Text ticketNames;
        [SerializeField, Min(0)] private float stampSeconds = 1.5f;
        [SerializeField, Min(0)] private float pulsesPerSecond = 1.5f;

        private Sprite defaultPortrait;
        private Color pulseColor;
        private bool initialized;
        private float stampTimeLeft;

        public event Action<CallCardView> Clicked;
        public Call Call { get; private set; }

        public void Show(Call call)
        {
            gameObject.SetActive(true);
            Initialize();
            Call = call;
            var caller = call.Request.CallerPerson as PersonDefinition;
            portrait.sprite = caller != null && caller.Portrait != null ? caller.Portrait : defaultPortrait;
            rateLabel.text = MoneyFormat.Rate(call.Request.CentsPerSecond);
            ticketNames.text = $"{call.Request.CallerPerson.FullName()}\nwants {call.Request.Recipient.FullName()}";
            Refresh();
        }

        public void Refresh()
        {
            if (Call == null) return;
            CallState state = Call.State;
            ringingPulse.gameObject.SetActive(state == CallState.Incoming);
            ticket.SetActive(state == CallState.Taken || state == CallState.Connected);
            ticketTitle.text = state == CallState.Connected ? "On the line" : "Call in hand";
            stamp.SetActive(state == CallState.Ended);
            if (state != CallState.Ended) return;
            stampLabel.text = StampText(Call);
            stampTimeLeft = stampSeconds;
        }

        public void Hide()
        {
            Call = null;
            ticket.SetActive(false);
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
            else if (Call.State == CallState.Ended && (stampTimeLeft -= Time.deltaTime) <= 0) Hide();
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
