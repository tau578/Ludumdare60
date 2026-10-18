using System;
using System.Linq;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Switchboard.Editor
{
    public static class SwitchboardPanelBuilder
    {
        private const string SocketPath = "Assets/Prefabs/UI/Socket.prefab";
        private const string PanelPath = "Assets/Prefabs/UI/SwitchboardPanel.prefab";
        private static readonly Vector2 ReferenceSize = new Vector2(1920, 1080);

        [MenuItem("Tools/Switchboard/Create Central Panel")]
        public static void CreateCentralPanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open a scene in Edit Mode before creating the panel.");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("No loaded active scene.");

            GameObject existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SwitchboardCanvas");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                return;
            }

            EnsureFolder("Assets/Prefabs", "UI");
            GameObject socketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SocketPath);
            if (socketPrefab == null) socketPrefab = CreateSocketPrefab();
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (panelPrefab == null) panelPrefab = CreatePanelPrefab(socketPrefab);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create central switchboard panel");

            RectTransform canvasRect = Rect("SwitchboardCanvas", null, Vector2.zero, ReferenceSize);
            SceneManager.MoveGameObjectToScene(canvasRect.gameObject, scene);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform workArea = Rect("WorkArea", canvasRect, Vector2.zero, ReferenceSize);
            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, workArea);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchoredPosition = new Vector2(-36, 60);
            Undo.RegisterCreatedObjectUndo(canvasRect.gameObject, "Create switchboard UI");

            bool hasEventSystem = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            if (!hasEventSystem)
            {
                GameObject input = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(input, scene);
                Undo.RegisterCreatedObjectUndo(input, "Create UI input");
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = panel;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("Created central switchboard panel with 36 sockets (A1–F6).", panel);
        }

        private static GameObject CreateSocketPrefab()
        {
            RectTransform root = Rect("Socket", null, Vector2.zero, new Vector2(112, 112));
            root.gameObject.AddComponent<SocketGraphic>();
            RectTransform anchor = Rect("PlugAnchor", root, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<SocketView>().Configure(0, 0, anchor);
            root.name = "Socket";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, SocketPath);
            Object.DestroyImmediate(root.gameObject);
            return asset;
        }

        private static GameObject CreatePanelPrefab(GameObject socketPrefab)
        {
            RectTransform panel = Rect("SwitchboardPanel", null, Vector2.zero, new Vector2(1120, 860));
            Box("Shadow", panel, 8, 12, 1120, 860, new Color32(25, 28, 26, 255));
            Box("Frame", panel, 0, 0, 1120, 860, new Color32(46, 50, 46, 255));
            Box("FrameHighlight", panel, 4, 4, 1112, 4, new Color32(145, 149, 137, 255));
            Box("Faceplate", panel, 18, 18, 1084, 824, new Color32(110, 115, 105, 255));
            Box("HeaderStrip", panel, 186, 42, 812, 80, new Color32(121, 126, 116, 255));
            Box("RowStrip", panel, 88, 140, 84, 666, new Color32(121, 126, 116, 255));
            Box("SocketField", panel, 186, 132, 812, 684, new Color32(94, 100, 92, 255));

            RectTransform columns = Rect("ColumnLabels", panel, Vector2.zero, panel.sizeDelta);
            RectTransform rows = Rect("RowLabels", panel, Vector2.zero, panel.sizeDelta);
            for (int i = 0; i < 6; i++)
            {
                Badge((i + 1).ToString(), columns, 230 + i * 132, 50);
                Badge(((char)('A' + i)).ToString(), rows, 98, 156 + i * 114);
            }

            // Larger faces, with the original row/column centres preserved.
            RectTransform gridRect = TopLeft("SocketGrid", panel, 206, 132, 772, 682);
            GridLayoutGroup grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112, 112);
            grid.spacing = new Vector2(20, 2);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            for (int row = 0; row < 6; row++)
            for (int column = 0; column < 6; column++)
            {
                GameObject socket = (GameObject)PrefabUtility.InstantiatePrefab(socketPrefab, gridRect);
                SocketView view = socket.GetComponent<SocketView>();
                view.Configure(row, column, (RectTransform)socket.transform.Find("PlugAnchor"));
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(socket);
            }

            foreach (Vector2 position in new[] { new Vector2(36, 36), new Vector2(1060, 36),
                         new Vector2(36, 800), new Vector2(1060, 800) })
            {
                Image screw = Box("MountingScrew", panel, position.x, position.y, 24, 24,
                    new Color32(72, 77, 70, 255));
                screw.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                Box("Slot", screw.transform, 5, 11, 14, 2, new Color32(39, 42, 38, 255));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(panel.gameObject, PanelPath);
            Object.DestroyImmediate(panel.gameObject);
            return asset;
        }

        private static void Badge(string label, Transform parent, float x, float y)
        {
            Image badge = Box($"Label_{label}", parent, x, y, 64, 64, new Color32(188, 190, 175, 255));
            badge.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform rect = Rect("Text", badge.transform, Vector2.zero, new Vector2(64, 64));
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>(MenuTypographyBuilder.BodyFontPath);
            text.fontSize = 38;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(54, 60, 53, 255);
            text.text = label;
            text.raycastTarget = false;
        }

        private static Image Box(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = TopLeft(name, parent, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RectTransform TopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = Rect(name, parent, new Vector2(x, -y), new Vector2(width, height));
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            return rect;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
