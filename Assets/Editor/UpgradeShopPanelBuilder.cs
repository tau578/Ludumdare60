using System;
using System.Linq;
using Switchboard.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Switchboard.Editor
{
    public static class UpgradeShopPanelBuilder
    {
        private const string PanelPath = "Assets/Prefabs/UI/UpgradeShopPanel.prefab";
        private const string ButtonPath = "Assets/Prefabs/UI/UpgradePlaceholderButton.prefab";
        private static readonly Vector2 PanelSize = new Vector2(236, 860);
        // The header plaque is the money counter's slot (Step 1c); the buttons start below it.
        private static readonly Vector2 HeaderSize = new Vector2(204, 96);
        private const float HeaderY = 366;
        private const int SlotsTopPadding = 144;
        private const float SlotSpacing = 32;
        // The buttons may be as wide as the header plaque, so their texts fit (Step 5a).
        private static readonly Vector2 ButtonSize = new Vector2(204, 144);
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const float TextInset = 10;
        private const float FillInset = 6;
        private const float HighlightThickness = 4;
        private static readonly Color TitleColor = new Color32(238, 236, 222, 255);
        private static readonly Color DescriptionColor = new Color32(214, 218, 200, 255);
        // The money counter's gold.
        private static readonly Color PriceColor = new Color32(242, 210, 122, 255);
        private static readonly Color FillColor = new Color32(92, 134, 78, 255);
        private static readonly Color HighlightColor = new Color32(242, 210, 122, 255);

        [MenuItem("Tools/Switchboard/Create Upgrade Shop Panel")]
        public static void CreateUpgradeShopPanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open Level in Edit Mode first.");
            var scene = SceneManager.GetActiveScene();
            var canvas = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SwitchboardCanvas");
            var area = canvas != null ? canvas.transform.Find("WorkArea") as RectTransform : null;
            var board = area != null ? area.Find("SwitchboardPanel") as RectTransform : null;
            if (board == null) throw new InvalidOperationException("The switchboard panel was not found.");
            var existing = area.Find("UpgradeShopPanel");
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }

            var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if (buttonPrefab == null) buttonPrefab = CreateButtonPrefab();
            var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (panelPrefab == null) panelPrefab = CreatePanelPrefab(buttonPrefab);

            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            board.GetWorldCorners(corners);
            var topRight = area.InverseTransformPoint(corners[2]);
            var bottomRight = area.InverseTransformPoint(corners[3]);
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, area);
            var rect = (RectTransform)panel.transform;
            float y = Mathf.Clamp((topRight.y + bottomRight.y) * .5f,
                area.rect.yMin + PanelSize.y * .5f + 16, area.rect.yMax - PanelSize.y * .5f - 16);
            rect.anchoredPosition = new Vector2(topRight.x + 24 + PanelSize.x * .5f, y);
            var overlay = area.Find("CableInteraction");
            if (overlay != null) rect.SetSiblingIndex(overlay.GetSiblingIndex());
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            Undo.RegisterCreatedObjectUndo(panel, "Add upgrade shop placeholder");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = panel;
        }

        private static GameObject CreateButtonPrefab()
        {
            var root = Rect("UpgradePlaceholderButton", null, ButtonSize);
            try
            {
                var image = root.gameObject.AddComponent<Image>();
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                if (image.sprite == null) throw new InvalidOperationException("Unity UI button sprite was not found.");
                image.type = Image.Type.Sliced;
                image.raycastTarget = true;
                var shadow = root.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color32(20, 25, 20, 180);
                shadow.effectDistance = new Vector2(4, -5);
                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var colors = button.colors;
                colors.normalColor = new Color32(112, 119, 105, 255);
                colors.highlightedColor = new Color32(174, 180, 157, 255);
                colors.pressedColor = new Color32(75, 86, 67, 255);
                colors.selectedColor = colors.normalColor;
                colors.fadeDuration = .08f;
                button.colors = colors;
                var layout = root.gameObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = layout.flexibleHeight = 0;

                var icon = Box("PlaceholderIcon", root, new Vector2(100, 100), Vector2.zero, new Color32(65, 73, 62, 255));
                Box("PlusHorizontal", icon, new Vector2(40, 8), Vector2.zero, new Color32(186, 192, 172, 255));
                Box("PlusVertical", icon, new Vector2(8, 40), Vector2.zero, new Color32(186, 192, 172, 255));
                BuildSlotParts(root);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, ButtonPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [MenuItem("Tools/Switchboard/Update Upgrade Shop Buttons")]
        public static void UpdateShopButtons()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            var button = PrefabUtility.LoadPrefabContents(ButtonPath);
            try
            {
                BuildSlotParts((RectTransform)button.transform);
                PrefabUtility.SaveAsPrefabAsset(button, ButtonPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(button); }
            // The panel's copies keep the size its layout last gave them; the layout itself only runs in a scene.
            var panel = PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                foreach (var slot in panel.GetComponentsInChildren<UpgradeSlotView>(true))
                    ((RectTransform)slot.transform).sizeDelta = ButtonSize;
                PrefabUtility.SaveAsPrefabAsset(panel, PanelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
        }

        // The offer texts, the savings fill and the highlight frame that UpgradeSlotView drives.
        private static void BuildSlotParts(RectTransform root)
        {
            foreach (var part in new[] { "FillArea", "Details", "Highlight" })
            {
                var old = root.Find(part);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            root.sizeDelta = ButtonSize;
            var layout = root.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = ButtonSize.x;
            layout.minHeight = layout.preferredHeight = ButtonSize.y;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("LiberationSans SDF was not found at " + FontPath);

            var fillArea = Stretch("FillArea", root, FillInset);
            fillArea.SetAsFirstSibling();
            var fill = Stretch("Fill", fillArea, 0);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = FillColor;
            fillImage.raycastTarget = false;
            fill.anchorMax = new Vector2(1, 0);
            fill.gameObject.SetActive(false);

            var details = Stretch("Details", root, 0);
            var group = details.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var title = Label("Title", details, 1, -10, 36, 26, 16, FontStyles.Bold, TitleColor, font);
            var description = Label("Description", details, .5f, 2, 48, 21, 14, FontStyles.Normal, DescriptionColor, font);
            description.textWrappingMode = TextWrappingModes.Normal;
            var price = Label("Price", details, 0, 10, 34, 26, 16, FontStyles.Bold, PriceColor, font);
            details.gameObject.SetActive(false);

            var highlight = Stretch("Highlight", root, 0);
            Bar("Top", highlight, new Vector2(0, 1), new Vector2(1, 1));
            Bar("Bottom", highlight, new Vector2(0, 0), new Vector2(1, 0));
            Bar("Left", highlight, new Vector2(0, 0), new Vector2(0, 1));
            Bar("Right", highlight, new Vector2(1, 0), new Vector2(1, 1));
            highlight.gameObject.SetActive(false);

            var view = root.GetComponent<UpgradeSlotView>();
            if (view == null) view = root.gameObject.AddComponent<UpgradeSlotView>();
            view.Configure(root.GetComponent<Button>(), root.GetComponent<Image>(), root.Find("PlaceholderIcon").gameObject,
                group, title, description, price, fill, highlight.gameObject);
        }

        private static RectTransform Stretch(string name, Transform parent, float inset)
        {
            var rect = Rect(name, parent, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, float anchorY, float y, float height, float size,
            float minSize, FontStyles style, Color color, TMP_FontAsset font)
        {
            var rect = Rect(name, parent, new Vector2(-2 * TextInset, height));
            rect.anchorMin = new Vector2(0, anchorY);
            rect.anchorMax = new Vector2(1, anchorY);
            rect.pivot = new Vector2(.5f, anchorY);
            rect.anchoredPosition = new Vector2(0, y);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = text.fontSizeMax = size;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Bar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = Rect(name, parent, Vector2.zero);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : .5f, anchorMin.y == anchorMax.y ? anchorMin.y : .5f);
            rect.sizeDelta = anchorMin.x == anchorMax.x ? new Vector2(HighlightThickness, 0) : new Vector2(0, HighlightThickness);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = HighlightColor;
            image.raycastTarget = false;
        }

        private static GameObject CreatePanelPrefab(GameObject buttonPrefab)
        {
            var root = Rect("UpgradeShopPanel", null, PanelSize);
            try
            {
                Box("Shadow", root, PanelSize, new Vector2(8, -12), new Color32(25, 28, 26, 255));
                Box("Frame", root, PanelSize, Vector2.zero, new Color32(46, 50, 46, 255));
                var face = Box("Faceplate", root, PanelSize - new Vector2(16, 16), Vector2.zero, new Color32(70, 76, 68, 255));
                face.GetComponent<Image>().raycastTarget = true;
                Box("FrameHighlight", root, new Vector2(228, 3), new Vector2(0, 424), new Color32(145, 149, 137, 255));
                var header = Box("HeaderSlot", root, HeaderSize, new Vector2(0, HeaderY), new Color32(46, 50, 46, 255));
                Box("HeaderShade", header, new Vector2(HeaderSize.x, 3), new Vector2(0, HeaderSize.y * .5f - 1.5f), new Color32(25, 28, 26, 255));
                Box("HeaderHighlight", header, new Vector2(HeaderSize.x, 2), new Vector2(0, 1 - HeaderSize.y * .5f), new Color32(145, 149, 137, 255));
                var slots = Rect("UpgradeSlots", root, PanelSize);
                var layout = slots.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(0, 0, SlotsTopPadding, 0);
                layout.spacing = SlotSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                for (int i = 0; i < 4; i++)
                {
                    var button = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, slots);
                    button.name = $"UpgradeButton_{i + 1:00}";
                    PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PanelPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        private static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var rect = Rect(name, parent, size);
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            return rect;
        }
    }
}
