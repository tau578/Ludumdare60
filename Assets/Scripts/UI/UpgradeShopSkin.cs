using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed class UpgradeShopSkin : MonoBehaviour
    {
        [SerializeField] private Sprite store, neutral, pressed, dark;
        [SerializeField] private TMP_FontAsset font;
        private bool applied;
        private Sprite blank, cable, income, secret;

        public void Configure(Sprite artwork, Sprite normal, Sprite down, Sprite unavailable, TMP_FontAsset typeface)
        { store = artwork; neutral = normal; pressed = down; dark = unavailable; font = typeface; }

        private void Awake() => Apply();

        public void Apply()
        {
            if (applied || store == null) return;
            applied = true;
            var root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
            root.sizeDelta = new Vector2(469, 1004);
            root.localScale = Vector3.one * (390f / 469f);
            root.anchoredPosition = new Vector2(-24, -86);
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
            foreach (var layout in GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            var art = Picture("Store artwork", root, 0, 0, 469, 1004, store);
            art.transform.SetAsFirstSibling();
            var header = (RectTransform)transform.Find("HeaderSlot");
            Place(header, 110, 65, 270, 78);
            Place((RectTransform)transform.Find("UpgradeSlots"), 0, 0, 469, 1004);
            var board = transform.parent.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "SwitchboardPanel");
            if (board != null)
            {
                board.anchorMin = board.anchorMax = board.pivot = new Vector2(.5f, .5f);
                board.anchoredPosition = new Vector2(-60, 60);
                board.localScale = Vector3.one * .88f;
            }
            // UI regions from the supplied artwork; the bitmap is kept intact.
            blank = Slice(260, 388, 176, 176);
            cable = Slice(260, 208, 176, 176);
            income = Slice(260, 568, 176, 176);
            secret = Slice(260, 748, 176, 176);
            var slots = GetComponentsInChildren<UpgradeSlotView>(true).OrderBy(s => s.transform.GetSiblingIndex()).ToArray();
            for (int i = 0; i < slots.Length; i++) BuildSlot(slots[i], i);
        }

        private void BuildSlot(UpgradeSlotView view, int index)
        {
            var row = (RectTransform)view.transform;
            // Keep prefab children under their original container.
            Place(row, 0, 207 + index * 180, 469, 180);
            foreach (Transform child in row) child.gameObject.SetActive(false);
            var oldButton = view.GetComponent<Button>();
            if (oldButton != null) oldButton.enabled = false;
            foreach (var effect in view.GetComponents<BaseMeshEffect>()) effect.enabled = false;
            var emblem = Picture("Upgrade emblem", row, 260, 1, 176, 176,
                index == 0 ? cable : index == 2 ? income : blank);
            var texts = Rect("Offer", row, 36, 15, 222, 142);
            var group = texts.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var title = Label("Title", texts, 0, 10, 222, 42, 32);
            var description = Label("Description", texts, 0, 0, 1, 1, 1);
            description.gameObject.SetActive(false);
            var background = Picture("Buy button", row, 65, 86, 182, 68, neutral);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = neutral, pressedSprite = pressed, selectedSprite = neutral, disabledSprite = dark };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var price = Label("Price", background.rectTransform, 4, 4, 174, 49, 36);
            var placeholder = Rect("Unused", row, 0, 0, 1, 1).gameObject;
            placeholder.SetActive(false);
            var fillArea = Rect("Savings", background.transform, 12, 60, 158, 3);
            var fill = Picture("Fill", fillArea, 0, 0, 158, 3, null);
            fill.color = new Color(1, .83f, .35f, .75f);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var highlight = Picture("Ready", background.transform, 0, 0, 182, 3, null);
            highlight.color = new Color(1, .83f, .35f);
            highlight.gameObject.SetActive(false);
            view.Configure(button, background, placeholder, group, title, description, price, fill.rectTransform, highlight.gameObject);
            view.ConfigureArtwork(emblem, blank, cable, income, secret, neutral, dark);
            view.SetFill(0);
        }

        private Sprite Slice(int x, int y, int width, int height) => Sprite.Create(store.texture,
            new UnityEngine.Rect(store.rect.x + x, store.rect.y + store.rect.height - y - height, width, height), new Vector2(.5f, .5f), 100);

        private TMP_Text Label(string name, Transform parent, float x, float y, float width, float height, float size)
        {
            var label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = label.fontSizeMax = size; label.fontSizeMin = Mathf.Min(20, size);
            label.enableAutoSizing = true; label.color = new Color(1, .98f, .91f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static Image Picture(string name, Transform parent, float x, float y, float w, float h, Sprite sprite)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false); Place(rect, x, y, w, h);
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            foreach (var sprite in new[] { blank, cable, income, secret })
                if (sprite != null) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
        }
    }
}
