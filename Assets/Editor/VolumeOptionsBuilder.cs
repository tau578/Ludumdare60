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
    public static class VolumeOptionsBuilder
    {
        const string PanelPath = "Assets/Prefabs/UI/VolumeCablePanel.prefab";
        [MenuItem("Tools/Switchboard/Create Three Volume Sliders")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Open MainMenu first.");
            var menu = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Menu>(true)).First();
            var data = new SerializedObject(menu);
            var options = ((GameObject)data.FindProperty("optionsPanel").objectReferenceValue).transform;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (prefab == null) prefab = BuildPanel();
            if (options.Find("VolumeCablePanel") == null)
            {
                var old = options.Find("MasterVolumeCableSlider");
                if (old != null)
                {
                    old.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
                }
                var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, options);
                ((RectTransform)panel.transform).anchoredPosition = new Vector2(0, -49);
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel.transform);
                Undo.RegisterCreatedObjectUndo(panel, "Add three volume rows");
                var toggle = (Toggle)data.FindProperty("fullscreenToggle").objectReferenceValue;
                if (toggle != null) Place((RectTransform)toggle.transform, new Vector2(-99, 340), new Vector3(2.5f, 2.5f, 1));
                var title = options.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.text.Trim().Equals("Options", StringComparison.OrdinalIgnoreCase));
                if (title != null)
                {
                    Place(title.rectTransform, new Vector2(0, 440), Vector3.one);
                    title.rectTransform.sizeDelta = new Vector2(800, 100);
                    title.fontSize = 76;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(title);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(title.rectTransform);
                }
                Selection.activeGameObject = panel;
            }
            var back = options.Find("Back_Button") as RectTransform;
            if (back != null) Place(back, new Vector2(-640, -465), new Vector3(2.4f, 2.4f, 1));
            // Music belongs to the imported menu; configure its source without changing that package's scripts.
            foreach (var music in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<RandomMusicPlayer>(true)))
                Bind(music.audioSource != null ? music.audioSource : music.GetComponent<AudioSource>(), AudioVolumeChannel.Music);
            var cablePrefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/CableInteraction.prefab");
            try
            {
                foreach (var source in cablePrefab.GetComponentsInChildren<AudioSource>(true)) Bind(source, AudioVolumeChannel.Sfx, 1f);
                PrefabUtility.SaveAsPrefabAsset(cablePrefab, "Assets/Prefabs/CableInteraction.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(cablePrefab); }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void Bind(AudioSource source, AudioVolumeChannel channel, float baseVolume = .6f)
        {
            if (source == null) return;
            var binding = source.GetComponent<AudioChannelSource>();
            if (binding == null) binding = source.gameObject.AddComponent<AudioChannelSource>();
            source.volume = baseVolume;
            binding.Configure(channel, baseVolume);
            PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
        }

        static GameObject BuildPanel()
        {
            var root = Rect("VolumeCablePanel", null, new Vector2(1320, 756));
            try
            {
                var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MasterVolumeCableSlider.prefab");
                var rows = new VolumeCableSlider[3];
                string[] names = { "MASTER", "MUSIC", "SFX" };
                Color[] colors = { new Color32(230, 188, 72, 255), new Color32(78, 166, 220, 255), new Color32(218, 96, 77, 255) };
                for (int i = 0; i < rows.Length; i++)
                {
                    var row = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, root);
                    PrefabUtility.UnpackPrefabInstance(row, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    row.name = names[i];
                    var rect = (RectTransform)row.transform;
                    rect.sizeDelta = new Vector2(1320, 240);
                    rect.anchoredPosition = new Vector2(0, 234 - i * 234);
                    rect.localScale = new Vector3(.9f, .9f, 1);
                    foreach (string box in new[] { "Frame", "Shadow", "Faceplate" })
                        ((RectTransform)row.transform.Find(box)).sizeDelta = box == "Faceplate" ? new Vector2(1300, 220) : new Vector2(1320, 240);
                    var title = row.transform.Find("Title").GetComponent<Text>();
                    title.text = names[i]; title.rectTransform.anchoredPosition = new Vector2(0, 94); title.fontSize = 28;
                    rows[i] = row.GetComponent<VolumeCableSlider>();
                    rows[i].SetChannel((AudioVolumeChannel)i, colors[i]);
                    rows[i].FixedSocket.rectTransform.anchoredPosition = new Vector2(-580, 0);
                    rows[i].FixedSocket.rectTransform.sizeDelta = new Vector2(84, 84);
                    foreach (var socket in rows[i].Sockets)
                    {
                        socket.rectTransform.anchoredPosition = new Vector2(socket.rectTransform.anchoredPosition.x, 0);
                        socket.rectTransform.sizeDelta = new Vector2(84, 84);
                        socket.HitRadiusScale = 1.15f;
                    }
                    foreach (var label in row.GetComponentsInChildren<Text>())
                        if (label.name.StartsWith("Label_")) { label.rectTransform.anchoredPosition = new Vector2(label.rectTransform.anchoredPosition.x, 52); label.fontSize = 28; }
                    var mute = (RectTransform)row.transform.Find("MuteIcon");
                    mute.anchoredPosition = new Vector2(mute.anchoredPosition.x, 52); mute.sizeDelta = new Vector2(38, 38);
                    Bind(row.GetComponent<AudioSource>(), AudioVolumeChannel.Sfx);
                }
                var overlay = Rect("CableOverlay", root, root.sizeDelta);
                foreach (var row in rows)
                {
                    row.Wire.transform.SetParent(overlay, false);
                    row.Wire.name = row.Channel + "Wire";
                    row.Wire.rectTransform.anchoredPosition = Vector2.zero;
                    row.Wire.rectTransform.sizeDelta = root.sizeDelta;
                    row.Wire.transform.localScale = Vector3.one;
                    row.Wire.MaximumSag = 60;
                }
                foreach (var row in rows)
                {
                    row.FixedPlug.transform.SetParent(overlay, true);
                    row.MovingPlug.transform.SetParent(overlay, true);
                    row.FixedPlug.name = row.Channel + "FixedPlug";
                    row.MovingPlug.name = row.Channel + "MovingPlug";
                    row.Hand.gameObject.SetActive(false);
                }
                var hand = rows[0].Hand;
                hand.transform.SetParent(overlay, false);
                hand.transform.localScale = Vector3.one;
                root.gameObject.AddComponent<VolumeCablePanel>().Configure(rows, hand, overlay);
                ConvertWallOutputs(root.gameObject);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PanelPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [MenuItem("Tools/Switchboard/Use Wall Outputs For Volume Cables")]
        public static void UpgradeWallOutputs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                ConvertWallOutputs(root);
                PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ConvertWallOutputs(GameObject root)
        {
            foreach (var row in root.GetComponentsInChildren<VolumeCableSlider>(true))
            {
                var socket = row.FixedSocket;
                var plug = row.FixedPlug;
                var anchor = row.WallAnchor != null ? row.WallAnchor : Rect("WallAnchor", row.transform, Vector2.zero);
                anchor.anchoredPosition = new Vector2(-650, 0);
                row.UseWallAnchor(anchor);
                if (socket != null) Object.DestroyImmediate(socket.gameObject);
                if (plug != null) Object.DestroyImmediate(plug.gameObject);
                row.Render(row.MovingPlug.transform.position);
            }
        }

        static void Place(RectTransform target, Vector2 position, Vector3 scale)
        {
            target.anchoredPosition = position; target.localScale = scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        static RectTransform Rect(string name, Transform parent, Vector2 size)
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
