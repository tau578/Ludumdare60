using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Switchboard.UI
{
    public sealed class UpgradeSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        private ColorBlock defaultColors;
        private bool pointerInside;
        private bool canAfford;
        private Material hoverMaterial;
        private Color baseMaterialColor;
        private Sprite blankEmblem, cableEmblem, incomeEmblem, secretEmblem, neutralButton, darkButton;

        public void ConfigureArtwork(Image image, Sprite blank, Sprite cable, Sprite income, Sprite secret, Sprite neutral, Sprite dark)
        {
            emblem = image; blankEmblem = blank; cableEmblem = cable; incomeEmblem = income; secretEmblem = secret;
            neutralButton = neutral; darkButton = dark;
            if (hoverMaterial == null)
            {
                hoverMaterial = new Material(background.material);
                baseMaterialColor = hoverMaterial.GetColor("_Color");
                background.material = hoverMaterial;
            }
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
            defaultColors = button.colors;
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

        private void Awake() { defaultColors = button.colors; button.onClick.RemoveListener(OnClick); button.onClick.AddListener(OnClick); }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClick);
            if (hoverMaterial != null) Destroy(hoverMaterial);
        }

        public void Show(string titleText, string descriptionText, string priceText, bool affordable)
        {
            canAfford = affordable && !string.IsNullOrEmpty(priceText);
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
                var sprites = button.spriteState;
                sprites.highlightedSprite = affordable ? neutralButton : darkButton;
                sprites.selectedSprite = affordable ? neutralButton : darkButton;
                button.spriteState = sprites;
                button.interactable = !string.IsNullOrEmpty(priceText);
                RefreshHover();
                if (string.IsNullOrEmpty(priceText)) price.text = "MAX";
                return;
            }
            details.alpha = affordable ? 1 : dimmedTextAlpha;
            var colors = defaultColors;
            if (!affordable) colors.highlightedColor = colors.selectedColor = colors.normalColor;
            button.colors = colors;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            RefreshHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            RefreshHover();
        }

        private void OnDisable()
        {
            pointerInside = false;
            RefreshHover();
        }

        private void RefreshHover()
        {
            if (hoverMaterial == null) return;
            float brightness = pointerInside && canAfford ? 1.12f : 1f;
            hoverMaterial.SetColor("_Color", new Color(baseMaterialColor.r * brightness,
                baseMaterialColor.g * brightness, baseMaterialColor.b * brightness, baseMaterialColor.a));
        }

        private void OnClick() => Clicked?.Invoke();
    }
}
