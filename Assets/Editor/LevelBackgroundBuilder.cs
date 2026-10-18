using System;
using System.Linq;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Editor
{
    public static class LevelBackgroundBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/UI/BackgroundPanel.prefab";

        [MenuItem("Tools/Switchboard/Create Level Background And Lamps")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Level.unity" ||
                PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open Level in Edit Mode first.");

            var canvas = scene.GetRootGameObjects().Single(go => go.name == "SwitchboardCanvas");
            var workArea = (RectTransform)canvas.transform.Find("WorkArea");
            if (workArea.Find("BackgroundPanel") != null)
            {
                Selection.activeGameObject = workArea.Find("BackgroundPanel").gameObject;
                return;
            }

            // Resolve the art before modifying the scene.
            Sprite background = SpriteAt("backgrounds/main_background.png");
            Sprite off = SpriteAt("bulbs/bulb-02.png"), on = SpriteAt("bulbs/bulb-01.png");
            var board = (RectTransform)workArea.Find("SwitchboardPanel");
            var callerPanel = workArea.Find("CallerQueuePanel");
            var sockets = board.GetComponentsInChildren<SocketView>(true);
            if (sockets.Length != 36) throw new InvalidOperationException("Expected the existing 6 by 6 board.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add level background and lamps");
            RectTransform root = Rect("BackgroundPanel", workArea, Vector2.zero, workArea.sizeDelta);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            root.SetAsFirstSibling();
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create background container");

            RectTransform art = Rect("main_background_0", root, Vector2.zero, new Vector2(1920, 1080));
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.sizeDelta = Vector2.zero;
            Image image = art.gameObject.AddComponent<Image>();
            image.sprite = background;
            image.raycastTarget = false;
            // Cover the work area without distorting the circular windows in the painting.
            var aspect = art.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = background.rect.width / background.rect.height;

            // Keep sockets and their lamps together so the saved prefab has no scene references.
            Undo.SetTransformParent(board, root, "Group board with background");
            PrefabUtility.RecordPrefabInstancePropertyModifications(board);
            foreach (string strip in new[] { "HeaderStrip", "RowStrip" })
                DisableGraphics(board.Find(strip));

            foreach (Transform labels in new[] { board.Find("ColumnLabels"), board.Find("RowLabels") })
            {
                bool row = labels.name == "RowLabels";
                for (int i = 0; i < labels.childCount; i++)
                {
                    var badge = (RectTransform)labels.GetChild(i);
                    DisableGraphics(badge);
                    RectTransform lamp = Rect("Lamp_" + (row ? ((char)('A' + i)).ToString() : (i + 1).ToString()),
                        badge, Vector2.zero, new Vector2(96, 96));
                    Image glass = lamp.gameObject.AddComponent<Image>();
                    glass.sprite = off;
                    glass.raycastTarget = false;
                    glass.preserveAspect = true;
                    string prefix = row ? ((char)('A' + i)).ToString() : (i + 1).ToString("00");
                    Sprite textOff = TrySpriteAt("bulbs/" + prefix + "02.png");
                    Sprite textOn = TrySpriteAt("bulbs/" + prefix + "01.png");
                    Image glyph = null;
                    if (textOff != null && textOn != null)
                    {
                        glyph = Rect("Label", lamp, Vector2.zero, new Vector2(57.6f, 57.6f)).gameObject.AddComponent<Image>();
                        glyph.raycastTarget = false;
                        glyph.preserveAspect = true;
                    }
                    else
                    {
                        var original = badge.GetComponentInChildren<TMPro.TMP_Text>(true);
                        if (original != null)
                        {
                            var fallback = UnityEngine.Object.Instantiate(original, lamp, false);
                            fallback.name = "Label";
                            fallback.enabled = true;
                            fallback.color = Color.black;
                            fallback.rectTransform.anchoredPosition = Vector2.zero;
                            fallback.raycastTarget = false;
                        }
                        else
                        {
                            var text = Rect("Label", lamp, Vector2.zero, new Vector2(64, 64)).gameObject.AddComponent<Text>();
                            text.font = AssetDatabase.LoadAssetAtPath<Font>(MenuTypographyBuilder.BodyFontPath);
                            text.text = prefix;
                            text.fontSize = 38;
                            text.alignment = TextAnchor.MiddleCenter;
                            text.color = Color.black;
                            text.raycastTarget = false;
                        }
                    }
                    int index = i;
                    lamp.gameObject.AddComponent<SwitchboardLampView>().Configure(glass, glyph, off, on, textOff, textOn,
                        sockets.Where(socket => row ? socket.Row == index : socket.Column == index).ToArray());
                }
            }

            // The painting replaces only the caller panel's decoration. Retain all six slot anchors.
            DisableGraphics(callerPanel);
            Canvas.ForceUpdateCanvases();
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new InvalidOperationException("Could not save BackgroundPanel prefab.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Level.");
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Level background saved: 12 lamps, 36 sockets, 6 caller anchors; " + PrefabPath, root);
        }

        private static Sprite SpriteAt(string path) => TrySpriteAt(path)
            ?? throw new InvalidOperationException("Missing sprite: " + path);

        private static Sprite TrySpriteAt(string path) =>
            AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/" + path).OfType<Sprite>().FirstOrDefault();

        private static void DisableGraphics(Transform root)
        {
            if (root == null) return;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Undo.RecordObject(graphic, "Hide replaced placeholder");
                graphic.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);
            }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
