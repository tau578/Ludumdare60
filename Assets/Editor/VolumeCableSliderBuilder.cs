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
    public static class VolumeCableSliderBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MasterVolumeCableSlider.prefab";

        [MenuItem("Tools/Switchboard/Create Master Volume Cable Slider")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Open MainMenu first.");
            var menu = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Menu>(true)).First();
            var menuData = new SerializedObject(menu);
            var options = ((GameObject)menuData.FindProperty("optionsPanel").objectReferenceValue).transform;
            if (options.Find("MasterVolumeCableSlider") != null) return;
            var oldSlider = menuData.FindProperty("volumeSlider").objectReferenceValue as Slider;
            if (oldSlider != null)
            {
                Undo.RecordObject(oldSlider.gameObject, "Replace volume slider");
                oldSlider.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldSlider.gameObject);
            }
            menuData.FindProperty("volumeSlider").objectReferenceValue = null;
            menuData.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, options);
            ((RectTransform)instance.transform).anchoredPosition = new Vector2(0, -145);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Add volume cable slider");
            // The old slider must not remain the keyboard navigation destination.
            foreach (var selectable in options.GetComponentsInChildren<Selectable>(true))
            {
                var navigation = selectable.navigation;
                if (oldSlider != null && (navigation.selectOnDown == oldSlider || navigation.selectOnUp == oldSlider))
                {
                    navigation.mode = Navigation.Mode.Automatic;
                    selectable.navigation = navigation;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(selectable);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
        }

        private static GameObject BuildPrefab()
        {
            var root = Rect("MasterVolumeCableSlider", null, new Vector2(1320, 330), Vector2.zero);
            try
            {
                Box("Shadow", root, new Vector2(1320, 330), new Vector2(7, -9), new Color32(22, 26, 22, 220));
                Box("Frame", root, new Vector2(1320, 330), Vector2.zero, new Color32(46, 50, 46, 255));
                var face = Box("Faceplate", root, new Vector2(1300, 310), Vector2.zero, new Color32(70, 76, 68, 255));
                face.GetComponent<Image>().raycastTarget = true;
                Label("Title", root, "ОБЩАЯ ГРОМКОСТЬ", new Vector2(0, 120), 30, new Vector2(650, 45));
                var fixedSocket = Socket("FixedSocket", root, new Vector2(-530, 8));
                var choices = new SocketGraphic[5];
                string[] labels = { "25", "50", "75", "100", "Mute" };
                for (int i = 0; i < 5; i++)
                {
                    float x = -300 + i * 205;
                    choices[i] = Socket("Socket_" + labels[i], root, new Vector2(x, 8));
                    if (i < 4) Label("Label_" + labels[i], root, labels[i], new Vector2(x, 70), 32, new Vector2(110, 45));
                    else
                    {
                        var icon = Rect("MuteIcon", root, new Vector2(46, 46), new Vector2(x, 70)).gameObject.AddComponent<MuteIconGraphic>();
                        icon.color = new Color32(218, 221, 204, 255);
                        icon.raycastTarget = false;
                    }
                }
                var wire = Rect("Wire", root, root.sizeDelta, Vector2.zero).gameObject.AddComponent<CableGraphic>();
                wire.raycastTarget = false;
                wire.InsulationColor = new Color32(230, 188, 72, 255);
                var first = Plug("FixedPlug", root);
                var second = Plug("MovingPlug", root);
                var hand = Rect("HandCursor", root, new Vector2(60, 72), Vector2.zero).gameObject.AddComponent<HandCursorGraphic>();
                hand.rectTransform.pivot = new Vector2(.5f, 1);
                hand.raycastTarget = false;
                hand.gameObject.SetActive(false);
                var audio = root.gameObject.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0;
                audio.volume = .6f;
                root.gameObject.AddComponent<VolumeCableSlider>().Configure(fixedSocket, choices, wire, first, second, hand, audio,
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugInsert.wav"),
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cable/PlugRemove.wav"));
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        private static PlugGraphic Plug(string name, Transform parent)
        {
            var rect = Rect(name, parent, new Vector2(36, 70), Vector2.zero);
            rect.pivot = new Vector2(.5f, 1);
            var plug = rect.gameObject.AddComponent<PlugGraphic>();
            plug.SleeveColor = new Color32(230, 188, 72, 255);
            plug.raycastTarget = true;
            return plug;
        }

        private static SocketGraphic Socket(string name, Transform parent, Vector2 position)
        {
            var socket = Rect(name, parent, new Vector2(100, 100), position).gameObject.AddComponent<SocketGraphic>();
            socket.raycastTarget = true;
            return socket;
        }

        private static void Label(string name, Transform parent, string text, Vector2 position, int size, Vector2 dimensions)
        {
            var label = Rect(name, parent, dimensions, position).gameObject.AddComponent<Text>();
            label.font = AssetDatabase.LoadAssetAtPath<Font>(name == "Title" ? MenuTypographyBuilder.HeadingFontPath : MenuTypographyBuilder.BodyFontPath);
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color32(218, 221, 204, 255);
            label.raycastTarget = false;
        }

        private static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var rect = Rect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
