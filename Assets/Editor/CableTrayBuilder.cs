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
                EnsureStoredCables((RectTransform)existing);
                BindCableViews(area);
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
                    image.raycastTarget = true;
                    EnsureStoredCables(rect);
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
            EnsureStoredCables(trayRect);
            BindCableViews(area);
            Undo.RegisterCreatedObjectUndo(tray, "Add cable tray");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = tray;
        }

        public static StoredCableView[] EnsureStoredCables(RectTransform tray)
        {
            tray.GetComponent<Image>().raycastTarget = true;
            Transform legacy = tray.Find("StoredCable");
            if (legacy != null) legacy.name = "StoredCable_01";
            var views = new StoredCableView[CableBuilder.CableCount];
            for (int i = 0; i < views.Length; i++)
            {
                string name = $"StoredCable_{i + 1:00}";
                Transform existing = tray.Find(name);
                var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
                go.layer = LayerMask.NameToLayer("UI");
                RectTransform rect = (RectTransform)go.transform;
                if (existing == null) rect.SetParent(tray, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(252, 94.5f);
                rect.anchoredPosition = new Vector2((i - (views.Length - 1) * 0.5f) * 270, -140);
                Image image = go.GetComponent<Image>();
                image.sprite = null;
                image.color = Color.clear;
                image.raycastTarget = true;
                image.enabled = true;
                RectTransform preview = Child("Preview", rect);
                preview.sizeDelta = new Vector2(560, 210);
                preview.localScale = Vector3.one * .45f;
                var wireRect = Child("Wire", preview);
                var wire = wireRect.GetComponent<CableGraphic>() ?? wireRect.gameObject.AddComponent<CableGraphic>();
                wire.raycastTarget = false;
                var first = PreviewPlug("FirstPlug", preview, new Vector2(-200, 92));
                var second = PreviewPlug("SecondPlug", preview, new Vector2(200, 92));
                var view = go.GetComponent<StoredCableView>() ?? go.AddComponent<StoredCableView>();
                view.Configure(preview, wire, first, second);
                view.SetCableColor(CableBuilder.CableColors[i]);
                view.SetAvailable(true);
                views[i] = view;
            }
            return views;
        }

        private static RectTransform Child(string name, RectTransform parent)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
                rect.SetParent(parent, false);
            }
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(560, 210);
            return rect;
        }

        private static PlugGraphic PreviewPlug(string name, RectTransform parent, Vector2 position)
        {
            var rect = Child(name, parent);
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(36, 68);
            rect.anchoredPosition = position;
            var plug = rect.GetComponent<PlugGraphic>() ?? rect.gameObject.AddComponent<PlugGraphic>();
            plug.raycastTarget = false;
            return plug;
        }

        public static void BindCableViews(RectTransform area)
        {
            Transform root = area.Find("CableInteraction");
            RectTransform tray = area.Find("CableTray") as RectTransform;
            if (root == null || tray == null) return;
            StoredCableView[] views = EnsureStoredCables(tray);
            CableController[] cables = root.GetComponentsInChildren<CableController>(true).OrderBy(c => c.name).ToArray();
            for (int i = 0; i < Mathf.Min(cables.Length, views.Length); i++)
            {
                Undo.RecordObject(cables[i], "Assign tray cable image");
                cables[i].SetTrayCableView(views[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cables[i]);
            }
        }
    }
}
