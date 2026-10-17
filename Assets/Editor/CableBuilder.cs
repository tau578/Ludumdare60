using System;
using System.Linq;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Editor
{
    public static class CableBuilder
    {
        public const int CableCount = 3;
        public static readonly Color[] CableColors = {
            new Color32(218, 96, 77, 255),
            new Color32(78, 166, 220, 255),
            new Color32(230, 188, 72, 255)
        };
        [MenuItem("Tools/Switchboard/Add Cable Interaction")]
        public static void AddCableInteraction()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open Level in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            GameObject canvas = scene.GetRootGameObjects().Single(go => go.name == "SwitchboardCanvas");
            RectTransform area = (RectTransform)canvas.transform.Find("WorkArea");
            RectTransform root = area.Find("CableInteraction") as RectTransform;
            CableController legacy = root != null ? root.GetComponent<CableController>() : null;
            if (root == null || legacy != null)
            {
                root = Rect("CableInteractionSystem", area);
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.sizeDelta = Vector2.zero;
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Create cable input coordinator");
                if (legacy != null)
                {
                    Undo.RecordObject(legacy.gameObject, "Name first cable");
                    legacy.name = "Cable_01";
                    Undo.SetTransformParent(legacy.transform, root, "Group first cable");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(legacy.gameObject);
                }
                root.name = "CableInteraction";
            }
            CableInputController input = root.GetComponent<CableInputController>();
            if (input == null) input = Undo.AddComponent<CableInputController>(root.gameObject);
            var cables = new CableController[CableCount];
            for (int i = 0; i < cables.Length; i++)
            {
                string name = $"Cable_{i + 1:00}";
                Transform existing = root.Find(name);
                CableController controller = existing != null ? existing.GetComponent<CableController>() : CreateCable(name, root);
                RectTransform rect = (RectTransform)controller.transform;
                Undo.RecordObject(rect, "Size cable overlay");
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.localScale = Vector3.one;
                var audio = controller.GetComponent<AudioSource>();
                Undo.RecordObject(controller, "Configure cable");
                Undo.RecordObject(audio, "Set cable volume");
                audio.volume = 0.6f;
                controller.Configure(rect, rect.Find("Wire").GetComponent<CableGraphic>(),
                    rect.Find("FirstPlug").GetComponent<PlugGraphic>(), rect.Find("SecondPlug").GetComponent<PlugGraphic>(), audio,
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugInsert.wav"),
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugRemove.wav"),
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugBlocked.wav"));
                controller.SetCableColor(CableColors[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
                PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
                cables[i] = controller;
            }
            Undo.RecordObject(input, "Assign cable inventory");
            RectTransform cursorRect = root.Find("HandCursor") as RectTransform;
            if (cursorRect == null)
            {
                cursorRect = Rect("HandCursor", root);
                cursorRect.sizeDelta = new Vector2(64, 64);
                cursorRect.gameObject.AddComponent<HandCursorGraphic>();
                Undo.RegisterCreatedObjectUndo(cursorRect.gameObject, "Add hand cursor");
            }
            var cursor = cursorRect.GetComponent<HandCursorGraphic>();
            cursor.raycastTarget = false;
            cursor.gameObject.SetActive(false);
            input.Configure(area.Find("CableTray") as RectTransform, cables, cursor);
            CableTrayBuilder.BindCableViews(area);
            Selection.activeGameObject = root.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static CableController CreateCable(string name, RectTransform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CableInteraction.prefab");
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                Undo.RegisterCreatedObjectUndo(instance, "Add cable");
                return instance.GetComponent<CableController>();
            }
            RectTransform root = Rect(name, parent);
            RectTransform lineRect = Rect("Wire", root);
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            CableGraphic line = lineRect.gameObject.AddComponent<CableGraphic>();
            line.raycastTarget = false;
            line.enabled = false;

            PlugGraphic first = CreatePlug("FirstPlug", root, new Color32(178, 99, 51, 255));
            PlugGraphic second = CreatePlug("SecondPlug", root, new Color32(178, 99, 51, 255));

            AudioSource audio = root.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0;
            audio.volume = 0.6f;
            audio.priority = 100;
            CableController controller = root.gameObject.AddComponent<CableController>();
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Add cable");
            return controller;
        }

        private static PlugGraphic CreatePlug(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(36, 68);
            PlugGraphic plug = rect.gameObject.AddComponent<PlugGraphic>();
            plug.SleeveColor = color;
            plug.raycastTarget = false;
            rect.gameObject.SetActive(false);
            return plug;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }
    }
}
