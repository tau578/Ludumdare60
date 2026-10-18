using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public sealed class PhoneBookContactRow : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Text contactName;
        [SerializeField] private Text socketAddress;
        [SerializeField, Tooltip("A transparent full-row image, left over from the Notepad (D54). It never takes raycasts, " +
            "so clicks fall through to page turning.")]
        private Image tapArea;

        public void Configure(Text nameLabel, Text addressLabel, Image hitArea = null)
        {
            contactName = nameLabel;
            socketAddress = addressLabel;
            tapArea = hitArea;
            if (tapArea != null) tapArea.raycastTarget = false;
        }

        public void Show(PhoneBookContact contact, int fontSize)
        {
            contactName.text = contact.name;
            socketAddress.text = contact.socket;
            contactName.fontSize = socketAddress.fontSize = fontSize;
        }

        // Taking the press keeps the open book from flashing its pressed tint under the row.
        public void OnPointerDown(PointerEventData eventData) { }

        private void Awake()
        {
            if (tapArea != null) tapArea.raycastTarget = false;
        }
    }
}
