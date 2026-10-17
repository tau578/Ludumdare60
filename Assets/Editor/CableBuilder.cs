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
        [MenuItem("Tools/Switchboard/Add Cable Interaction")]
        public static void AddCableInteraction()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open Level in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            GameObject canvas = scene.GetRootGameObjects().Single(go => go.name == "SwitchboardCanvas");
            RectTransform area = (RectTransform)canvas.transform.Find("WorkArea");
            if (area.Find("CableInteraction") != null)
            {
                CableTrayBuilder.BindCableImage(area);
                return;
            }

            RectTransform root = Rect("CableInteraction", area);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
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
            controller.Configure((RectTransform)area.Find("SwitchboardPanel"), root, line, first, second, audio,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugInsert.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugRemove.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugBlocked.wav"));
            CableTrayBuilder.BindCableImage(area);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Add cable interaction");
            Selection.activeGameObject = root.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
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
