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
        private Image emblem;
        private Sprite blankEmblem, cableEmblem, incomeEmblem, secretEmblem, neutralButton, darkButton;

        public void ConfigureArtwork(Image image, Sprite blank, Sprite cable, Sprite income, Sprite secret, Sprite neutral, Sprite dark)
        {
            emblem = image; blankEmblem = blank; cableEmblem = cable; incomeEmblem = income; secretEmblem = secret;
            neutralButton = neutral; darkButton = dark;
        }

        public void SetArtwork(bool cable, bool income, bool secret = false)
        {
            if (emblem != null) emblem.sprite = cable ? cableEmblem : income ? incomeEmblem : secret ? secretEmblem : blankEmblem;
        }

        public void Configure(Button slotButton, Image slotBackground, GameObject icon, CanvasGroup texts, TMP_Text titleLabel,
            TMP_Text descriptionLabel, TMP_Text priceLabel, RectTransform savingsFill, GameObject frame)
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            button = slotButton;
            background = slotBackground;
            placeholderIcon = icon;
            details = texts;
            title = titleLabel;
            description = descriptionLabel;
            price = priceLabel;
            fill = savingsFill;
            highlight = frame;
            button.onClick.AddListener(OnClick);
        }

        private void Awake() { button.onClick.RemoveListener(OnClick); button.onClick.AddListener(OnClick); }

        private void OnDestroy() => button.onClick.RemoveListener(OnClick);

        public void Show(string titleText, string descriptionText, string priceText, bool affordable)
        {
            placeholderIcon.SetActive(false);
            details.gameObject.SetActive(true);
            title.text = titleText;
            description.text = descriptionText;
            price.text = priceText;
            if (emblem != null)
            {
                details.alpha = 1;
                background.color = Color.white;
                background.sprite = affordable ? neutralButton : darkButton;
                button.interactable = !string.IsNullOrEmpty(priceText);
                if (string.IsNullOrEmpty(priceText)) price.text = "MAX";
                return;
            }
            details.alpha = affordable ? 1 : dimmedTextAlpha;
            background.color = affordable ? Color.white : dimmedTint;
        }

        public void ShowEmpty()
        {
            if (emblem != null)
            {
                SetArtwork(false, false);
                Show("MAX", "", "", false);
                SetFill(0); SetHighlight(false);
                return;
            }
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
