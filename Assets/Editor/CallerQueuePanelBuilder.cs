using System;
using System.Linq;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Switchboard.Editor
{
    public static class CallerQueuePanelBuilder
    {
        private const string SlotPath = "Assets/Prefabs/UI/CallerSlot.prefab";
        private const string PanelPath = "Assets/Prefabs/UI/CallerQueuePanel.prefab";
        private static readonly Vector2 PanelSize = new Vector2(188, 860);

        [MenuItem("Tools/Switchboard/Create Caller Panel")]
        public static void CreateCallerPanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open the level scene in Edit Mode first.");

            Scene scene = SceneManager.GetActiveScene();
            GameObject canvas = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SwitchboardCanvas");
            if (canvas == null) throw new InvalidOperationException("The scene needs a SwitchboardCanvas.");
            RectTransform workArea = canvas.transform.Find("WorkArea") as RectTransform;
            RectTransform board = workArea != null ? workArea.Find("SwitchboardPanel") as RectTransform : null;
            if (board == null) throw new InvalidOperationException("The central switchboard panel was not found.");
            Transform existing = workArea.Find("CallerQueuePanel");
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath);
            if (slotPrefab == null) slotPrefab = CreateSlotPrefab();
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (panelPrefab == null) panelPrefab = CreatePanelPrefab(slotPrefab);

            // Place the new panel beside the actual board without changing its layout overrides.
            var corners = new Vector3[4];
            board.GetLocalCorners(corners);
            Vector3 boardLeft = board.localPosition + Vector3.Scale(corners[0], board.localScale);
            float x = boardLeft.x - 24f - PanelSize.x * 0.5f;
            float y = Mathf.Clamp(board.localPosition.y,
                workArea.rect.yMin + PanelSize.y * 0.5f + 16f,
                workArea.rect.yMax - PanelSize.y * 0.5f - 16f);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add caller portrait panel");
            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, workArea);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchoredPosition = new Vector2(x, y);
            rect.SetSiblingIndex(board.GetSiblingIndex());
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            Undo.RegisterCreatedObjectUndo(panel, "Add caller portrait panel");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = panel;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static GameObject CreateSlotPrefab()
        {
            RectTransform slot = Rect("CallerSlot", null, new Vector2(128, 128));
            LayoutElement layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 128;
            layout.minHeight = layout.preferredHeight = 128;
            layout.flexibleWidth = layout.flexibleHeight = 0;

            Circle("Shadow", slot, 128, new Color32(28, 32, 29, 255)).rectTransform.anchoredPosition = new Vector2(0, -3);
            Circle("Rim", slot, 128, new Color32(166, 170, 158, 255));
            CircleGraphic maskSurface = Circle("PortraitMask", slot, 116, new Color32(80, 86, 78, 255));
            Mask mask = maskSurface.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            RectTransform portraitRect = Rect("Portrait", maskSurface.transform, Vector2.zero);
            portraitRect.anchorMin = Vector2.zero;
            portraitRect.anchorMax = Vector2.one;
            portraitRect.offsetMin = portraitRect.offsetMax = Vector2.zero;
            Image portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.color = Color.white;
            portrait.raycastTarget = false;
            portrait.maskable = true;
            portrait.preserveAspect = true;
            // Enable this Image when a portrait sprite is assigned; empty slots stay neutral.
            portrait.enabled = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot.gameObject, SlotPath);
            Object.DestroyImmediate(slot.gameObject);
            return prefab;
        }

        private static GameObject CreatePanelPrefab(GameObject slotPrefab)
        {
            RectTransform panel = Rect("CallerQueuePanel", null, PanelSize);
            Box("Shadow", panel, PanelSize, new Vector2(8, -12), new Color32(25, 28, 26, 255));
            Box("Frame", panel, PanelSize, Vector2.zero, new Color32(46, 50, 46, 255));
            Box("Faceplate", panel, new Vector2(172, 844), Vector2.zero, new Color32(70, 76, 68, 255));
            Box("FrameHighlight", panel, new Vector2(180, 3), new Vector2(0, 424), new Color32(145, 149, 137, 255));

            RectTransform slots = Rect("Slots", panel, PanelSize);
            VerticalLayoutGroup layout = slots.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 16, 16);
            layout.spacing = 40;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            for (int i = 0; i < 6; i++)
            {
                GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, slots);
                slot.name = $"CallerSlot_{i + 1:00}";
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(panel.gameObject, PanelPath);
            Object.DestroyImmediate(panel.gameObject);
            return prefab;
        }

        private static CircleGraphic Circle(string name, Transform parent, float diameter, Color color)
        {
            CircleGraphic circle = Rect(name, parent, new Vector2(diameter, diameter)).gameObject.AddComponent<CircleGraphic>();
            circle.color = color;
            circle.raycastTarget = false;
            return circle;
        }

        private static void Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            RectTransform rect = Rect(name, parent, size);
            rect.anchoredPosition = position;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }
    }
}
