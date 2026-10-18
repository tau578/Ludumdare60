using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public sealed class PhoneBookTab : MonoBehaviour
    {
        [SerializeField] private string letter;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Color available = new Color32(196, 166, 108, 255);
        [SerializeField] private Color current = new Color32(255, 248, 222, 255);
        [SerializeField] private Color unavailable = new Color32(170, 160, 136, 255);
        [SerializeField] private Vector2 restPosition;
        [SerializeField, Tooltip("How far the tabs of the spread on show stick out from the page.")]
        private Vector2 currentOffset;
        private PhoneBookView book;

        public char Letter => string.IsNullOrEmpty(letter) ? ' ' : letter[0];

        public void Configure(char value, Button tabButton, Image tabBackground, Text tabLabel)
        {
            letter = value.ToString();
            button = tabButton;
            background = tabBackground;
            label = tabLabel;
            label.text = letter;
        }

        public void SetRest(Vector2 position, Vector2 outwards)
        {
            restPosition = position;
            currentOffset = outwards;
        }

        public void Bind(PhoneBookView view)
        {
            book = view;
            button.onClick.AddListener(Select);
        }

        public void Unbind()
        {
            if (button != null) button.onClick.RemoveListener(Select);
            book = null;
        }

        public void Show(bool hasContacts, bool onCurrentSpread)
        {
            button.interactable = hasContacts;
            background.color = !hasContacts ? unavailable : onCurrentSpread ? current : available;
            label.fontStyle = onCurrentSpread ? FontStyle.Bold : FontStyle.Normal;
            if (currentOffset != Vector2.zero)
                ((RectTransform)transform).anchoredPosition = restPosition + (onCurrentSpread ? currentOffset : Vector2.zero);
            var color = label.color;
            color.a = hasContacts ? 1 : .35f;
            label.color = color;
        }

        private void Select()
        {
            if (book != null) book.ShowLetter(Letter);
        }
    }
}
