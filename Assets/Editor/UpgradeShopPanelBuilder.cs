using System;
using System.Linq;
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
            var root = Rect("UpgradePlaceholderButton", null, new Vector2(144, 144));
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
                layout.minWidth = layout.preferredWidth = 144;
                layout.minHeight = layout.preferredHeight = 144;
                layout.flexibleWidth = layout.flexibleHeight = 0;

                var icon = Box("PlaceholderIcon", root, new Vector2(100, 100), Vector2.zero, new Color32(65, 73, 62, 255));
                Box("PlusHorizontal", icon, new Vector2(40, 8), Vector2.zero, new Color32(186, 192, 172, 255));
                Box("PlusVertical", icon, new Vector2(8, 40), Vector2.zero, new Color32(186, 192, 172, 255));
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, ButtonPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
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
                var slots = Rect("UpgradeSlots", root, PanelSize);
                var layout = slots.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 48;
                layout.childAlignment = TextAnchor.MiddleCenter;
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
