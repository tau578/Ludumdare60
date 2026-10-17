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
    public static class CableTrayBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/UI/CableTray.prefab";
        private const string SpritePath = "Assets/Sprites/tray/tray.png";
        private const string StoredCableSpritePath = "Assets/Sprites/cables/cable_stored.png";

        [MenuItem("Tools/Switchboard/Create Cable Tray")]
        public static void CreateCableTray()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open the level scene in Edit Mode first.");

            Scene scene = SceneManager.GetActiveScene();
            GameObject canvas = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SwitchboardCanvas");
            RectTransform area = canvas != null ? canvas.transform.Find("WorkArea") as RectTransform : null;
            RectTransform board = area != null ? area.Find("SwitchboardPanel") as RectTransform : null;
            if (board == null) throw new InvalidOperationException("The central switchboard panel was not found.");
            Transform existing = area.Find("CableTray");
            if (existing != null)
            {
                EnsureStoredCable((RectTransform)existing);
                BindCableImage(area);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException("The tray sprite was not found.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                GameObject source = new GameObject("CableTray", typeof(RectTransform), typeof(Image));
                source.layer = LayerMask.NameToLayer("UI");
                try
                {
                    RectTransform rect = (RectTransform)source.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 1);
                    rect.sizeDelta = new Vector2(1120, 1120 * sprite.rect.height / sprite.rect.width);
                    Image image = source.GetComponent<Image>();
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    EnsureStoredCable(rect);
                    prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
                }
                finally { Object.DestroyImmediate(source); }
            }

            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            board.GetWorldCorners(corners);
            Vector3 bottomLeft = area.InverseTransformPoint(corners[0]);
            Vector3 bottomRight = area.InverseTransformPoint(corners[3]);
            float width = bottomRight.x - bottomLeft.x;

            GameObject tray = (GameObject)PrefabUtility.InstantiatePrefab(prefab, area);
            RectTransform trayRect = (RectTransform)tray.transform;
            trayRect.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width);
            // Keep the top edge clear of the board; the open foreground tray extends below the screen.
            trayRect.anchoredPosition = new Vector2((bottomLeft.x + bottomRight.x) * 0.5f,
                bottomLeft.y - 16 - area.rect.yMin);
            Transform cable = area.Find("CableInteraction");
            if (cable != null) trayRect.SetSiblingIndex(cable.GetSiblingIndex());
            PrefabUtility.RecordPrefabInstancePropertyModifications(trayRect);
            EnsureStoredCable(trayRect);
            BindCableImage(area);
            Undo.RegisterCreatedObjectUndo(tray, "Add cable tray");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = tray;
        }

        public static Image EnsureStoredCable(RectTransform tray)
        {
            Transform existing = tray.Find("StoredCable");
            if (existing != null) return existing.GetComponent<Image>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StoredCableSpritePath);
            if (sprite == null) throw new InvalidOperationException("The stored cable sprite was not found.");
            var go = new GameObject("StoredCable", typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(tray, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(336, 126);
            rect.anchoredPosition = new Vector2(0, -140);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static void BindCableImage(RectTransform area)
        {
            CableController cable = area.Find("CableInteraction")?.GetComponent<CableController>();
            Image image = area.Find("CableTray/StoredCable")?.GetComponent<Image>();
            if (cable == null || image == null) return;
            Undo.RecordObject(cable, "Assign tray cable image");
            cable.SetTrayCableImage(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cable);
        }
    }
}
