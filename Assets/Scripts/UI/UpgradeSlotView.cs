using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public sealed class UpgradeSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private GameObject placeholderIcon;
        [SerializeField] private CanvasGroup details;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text price;
        [SerializeField] private RectTransform fill;
        [SerializeField] private GameObject highlight;
        [SerializeField] private Color dimmedTint = new Color32(150, 150, 150, 255);
        [SerializeField, Range(0, 1)] private float dimmedTextAlpha = .45f;

        public event Action Clicked;

        public void Configure(Button slotButton, Image slotBackground, GameObject icon, CanvasGroup texts, TMP_Text titleLabel,
            TMP_Text descriptionLabel, TMP_Text priceLabel, RectTransform savingsFill, GameObject frame)
        {
            button = slotButton;
            background = slotBackground;
            placeholderIcon = icon;
            details = texts;
            title = titleLabel;
            description = descriptionLabel;
            price = priceLabel;
            fill = savingsFill;
            highlight = frame;
        }

        private void Awake() => button.onClick.AddListener(OnClick);

        private void OnDestroy() => button.onClick.RemoveListener(OnClick);

        public void Show(string titleText, string descriptionText, string priceText, bool affordable)
        {
            placeholderIcon.SetActive(false);
            details.gameObject.SetActive(true);
            title.text = titleText;
            description.text = descriptionText;
            price.text = priceText;
            details.alpha = affordable ? 1 : dimmedTextAlpha;
            background.color = affordable ? Color.white : dimmedTint;
        }

        public void ShowEmpty()
        {
            placeholderIcon.SetActive(true);
            details.gameObject.SetActive(false);
            background.color = Color.white;
            SetFill(0);
            SetHighlight(false);
        }

        public void SetFill(float amount)
        {
            amount = Mathf.Clamp01(amount);
            fill.gameObject.SetActive(amount > 0);
            fill.anchorMax = new Vector2(fill.anchorMax.x, amount);
        }

        public void SetHighlight(bool on) => highlight.SetActive(on);

        private void OnClick() => Clicked?.Invoke();
    }
}
