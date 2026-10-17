using System;
using System.Linq;
using Switchboard.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Editor
{
    public static class GamePresentationBuilder
    {
        [MenuItem("Tools/Switchboard/Apply English UI And Menu Cursor")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var main = SceneManager.GetActiveScene();
            if (main.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Open MainMenu first.");
            var heading = AssetDatabase.LoadAssetAtPath<Font>(MenuTypographyBuilder.HeadingFontPath);
            var body = AssetDatabase.LoadAssetAtPath<Font>(MenuTypographyBuilder.BodyFontPath);
            foreach (var path in new[] { "Assets/Prefabs/UI/SwitchboardPanel.prefab", "Assets/Prefabs/UI/PhoneBook.prefab", "Assets/Prefabs/UI/MasterVolumeCableSlider.prefab" })
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try { ApplyTexts(prefab, heading, body); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            var menu = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Menu>(true)).Single();
            var volumePanel = menu.GetComponentInChildren<VolumeCablePanel>(true);
            if (menu.GetComponentInChildren<MenuHandCursor>(true) == null)
            {
                var overlay = new GameObject("MenuCursor", typeof(RectTransform), typeof(Canvas));
                overlay.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)overlay.transform;
                rect.SetParent(menu.transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                SwitchboardUISorting.SetLayer(overlay, SwitchboardUISorting.Cursor);
                var hand = new GameObject("PointingHand", typeof(RectTransform)).AddComponent<HandCursorGraphic>();
                hand.gameObject.layer = overlay.layer;
                hand.transform.SetParent(rect, false);
                hand.rectTransform.sizeDelta = new Vector2(64,64);
                hand.raycastTarget = false;
                hand.gameObject.SetActive(false);
                overlay.AddComponent<MenuHandCursor>().Configure(hand, volumePanel);
                Undo.RegisterCreatedObjectUndo(overlay, "Add menu hand cursor");
            }
            foreach (var button in menu.GetComponentsInChildren<Button>(true))
            {
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
            foreach (var events in main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)))
            {
                events.firstSelectedGameObject = null;
                PrefabUtility.RecordPrefabInstancePropertyModifications(events);
            }
            // Translate the retired single-row control too, without altering current menu layout.
            var old = menu.transform.Find("OptionsMenu/MasterVolumeCableSlider/Title");
            if (old != null) { var text = old.GetComponent<Text>(); text.text = "MASTER"; PrefabUtility.RecordPrefabInstancePropertyModifications(text); }
            EditorSceneManager.MarkSceneDirty(main);
            EditorSceneManager.SaveScene(main);
            var level = SceneManager.GetSceneByPath("Assets/Scenes/Level.unity");
            bool opened = !level.IsValid() || !level.isLoaded;
            if (opened) level = EditorSceneManager.OpenScene("Assets/Scenes/Level.unity", OpenSceneMode.Additive);
            try
            {
                foreach (var root in level.GetRootGameObjects()) ApplyTexts(root, heading, body);
                EditorSceneManager.MarkSceneDirty(level);
                EditorSceneManager.SaveScene(level);
            }
            finally { if (opened) EditorSceneManager.CloseScene(level, true); }
            AssetDatabase.SaveAssets();
        }
        static void ApplyTexts(GameObject root, Font heading, Font body)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                bool title = text.name == "Heading" || text.name == "Title" || text.name == "MainTitle";
                text.font = title ? heading : body;
                text.fontStyle = FontStyle.Normal;
                if (text.GetComponentInParent<PhoneBookPageView>(true) != null)
                {
                    switch (text.name)
                    {
                        case "Heading": text.text = "CONTACTS"; break;
                        case "NameHeading": text.text = "Name"; break;
                        case "SocketHeading": text.text = "Socket"; break;
                        case "EmptyMessage": text.text = "No contacts"; break;
                    }
                }
                if (root.name == "MasterVolumeCableSlider" && text.name == "Title") text.text = "MASTER";
                EditorUtility.SetDirty(text);
                if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool title = text.name == "Heading" || text.name == "Title" || text.name == "MainTitle";
                text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(title ? "Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset" : "Assets/Fonts/DellaRespira/Della Respira SDF.asset");
                text.fontSharedMaterial = text.font.material;
                text.fontStyle = FontStyles.Normal;
                EditorUtility.SetDirty(text);
                if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
        }
    }
}
