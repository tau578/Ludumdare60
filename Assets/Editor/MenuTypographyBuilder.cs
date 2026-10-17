using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Switchboard.Editor
{
    public static class MenuTypographyBuilder
    {
        public const string HeadingFontPath = "Assets/Fonts/BoecklinsUniverse/Boecklins Universe.ttf";
        public const string BodyFontPath = "Assets/Fonts/DellaRespira/DellaRespira-Regular.ttf";
        [MenuItem("Tools/Switchboard/Apply Main Menu Fonts")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Open MainMenu first.");
            var heading = AssetDatabase.LoadAssetAtPath<Font>(HeadingFontPath);
            var body = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            var headingSdf = FontAsset(heading, "Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset");
            var bodySdf = FontAsset(body, "Assets/Fonts/DellaRespira/Della Respira SDF.asset");
            foreach (var path in new[] { "Assets/Prefabs/UI/VolumeCablePanel.prefab", "Assets/Prefabs/UI/MasterVolumeCableSlider.prefab" })
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try { ApplyLegacyTexts(prefab, heading, body); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    bool button = text.GetComponentInParent<Button>(true) != null;
                    bool title = !button && (text.name == "MainTitle" || text.name == "Title" || text.text.Trim() == "Options" || text.text.Trim() == "Credits");
                    Undo.RecordObject(text, "Apply menu typography");
                    text.font = title ? headingSdf : bodySdf;
                    text.fontSharedMaterial = text.font.material;
                    text.fontStyle = FontStyles.Normal;
                    if (button)
                    {
                        text.enableAutoSizing = true;
                        text.fontSizeMax = 24;
                        text.fontSizeMin = 20;
                    }
                    EditorUtility.SetDirty(text);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                }
                ApplyLegacyTexts(root, heading, body);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static void ApplyLegacyTexts(GameObject root, Font heading, Font body)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = text.name == "Title" ? heading : body;
                text.fontStyle = FontStyle.Normal;
                EditorUtility.SetDirty(text);
                if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
        }
        static TMP_FontAsset FontAsset(Font source, string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
            if (source == null) throw new InvalidOperationException("Menu font is not imported.");
            font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(font, path);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()));
            EditorUtility.SetDirty(font);
            return font;
        }
    }
}
