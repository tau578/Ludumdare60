using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public sealed class PhoneBookContactRow : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] private Text contactName;
        [SerializeField] private Text socketAddress;
        [SerializeField, Tooltip("A transparent full-row image; a raycast target only while taps are on.")]
        private Image tapArea;
        private PhoneBookContact shown;

        public event Action<PhoneBookContact> Tapped;

        public bool IsTappable => tapArea != null && tapArea.raycastTarget;

        public void Configure(Text nameLabel, Text addressLabel, Image hitArea = null)
        {
            contactName = nameLabel;
            socketAddress = addressLabel;
            tapArea = hitArea;
            SetTappable(false);
        }

        public void Show(PhoneBookContact contact, int fontSize)
        {
            shown = contact;
            contactName.text = contact.name;
            socketAddress.text = contact.socket;
            contactName.fontSize = socketAddress.fontSize = fontSize;
        }

        // Off, the row lets clicks fall through to the open book, which closes it.
        public void SetTappable(bool value)
        {
            if (tapArea != null) tapArea.raycastTarget = value;
        }

        // Taking the press keeps the open book from flashing its pressed tint under the row.
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsTappable && shown != null) Tapped?.Invoke(shown);
        }
    }
}
