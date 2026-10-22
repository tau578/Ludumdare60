using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Game.Tests
{
    /// <summary>
    /// Step 10d: the game draws Sergei's fonts only from their static copies. A dynamic TMP font writes every new
    /// letter into its own file in the editor, so one reference to it brings back thousands of changed lines per Play.
    /// </summary>
    public sealed class StaticFontTests
    {
        private static readonly string[] DynamicFonts =
        {
            "Assets/Fonts/DellaRespira/Della Respira SDF.asset",
            "Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset"
        };

        private const string DellaStatic = "Assets/Fonts/DellaRespira/Della Respira SDF Static.asset";
        private const string BoecklinsStatic = "Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF Static.asset";

        private static IEnumerable<string> BuildScenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);

        private static IEnumerable<string> Prefabs =>
            AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath);

        // Resources folders go into the build whole, whether a scene uses them or not.
        private static IEnumerable<string> ResourcesAssets => AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p));

        [Test]
        public void NothingInTheGameReferencesTheDynamicFonts()
        {
            List<string> roots = BuildScenes.Concat(Prefabs).Concat(ResourcesAssets).ToList();
            Assert.That(roots, Does.Contain("Assets/Scenes/MainMenu.unity").And.Contain("Assets/Scenes/Final.unity"));
            foreach (string root in roots)
                foreach (string dependency in AssetDatabase.GetDependencies(root, true))
                    Assert.That(DynamicFonts, Does.Not.Contain(dependency), root + " uses " + dependency);
        }

        [Test]
        public void EveryLabelInOurFontsIsStaticAndHasItsCharacters()
        {
            int labels = 0;
            foreach (string path in BuildScenes)
            {
                Scene scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects()) labels += CheckLabels(root, path);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            foreach (string path in Prefabs) labels += CheckLabels(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
            Assert.That(labels, Is.GreaterThan(20), "the menu, the pause menu, the tutorial and the book's Tab label");
        }

        // The dynamic fonts kerned pairs such as "Ta" from the TTF as they drew them; the static copies carry the same
        // pairs, or the menu and credits would space out. A font regenerated without "Get Font Features" loses them.
        [TestCase(DellaStatic)]
        [TestCase(BoecklinsStatic)]
        public void TheStaticFontsKeepTheirKerning(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            AssertStatic(font, path);
            Assert.That(font.fontFeatureTable.glyphPairAdjustmentRecords.Count, Is.GreaterThan(0));
        }

        // The shop and the victory screen make their labels at runtime from a font field.
        [Test]
        public void FontsSetFromCodeAreStatic()
        {
            var shop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UpgradeShopPanel.prefab");
            AssertStatic(FontField(shop.GetComponentInChildren<UpgradeShopSkin>(true), "font"), "UpgradeShopSkin.font");
            var gameUI = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/GameUI.prefab");
            var victory = gameUI.GetComponentInChildren<VictorySequenceView>(true);
            AssertStatic(FontField(victory, "titleFont"), "VictorySequenceView.titleFont");
            AssertStatic(FontField(victory, "bodyFont"), "VictorySequenceView.bodyFont");
        }

        [Test]
        public void TheShopsTextsAreInItsStaticFont()
        {
            var shop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UpgradeShopPanel.prefab");
            TMP_FontAsset font = FontField(shop.GetComponentInChildren<UpgradeShopSkin>(true), "font");
            AssertStatic(font, "the shop's font");
            // ShopView's fixed titles and states, then every entry of the designer's Economy.json.
            var texts = new List<string> { "More Cables", "More Clients", "More Profit", "??????????", "Owned", "MAX" };
            foreach (IUpgrade upgrade in Config.LoadEconomy().Upgrades)
            {
                texts.Add(upgrade.Name);
                texts.Add(upgrade.Description);
                texts.Add(MoneyFormat.Dollars(upgrade.CostCents));
            }
            foreach (string text in texts) AssertDrawable(font, text, "shop");
        }

        [Test]
        public void TheTutorialTextsAreInTheHintsStaticFont()
        {
            var gameUI = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/GameUI.prefab");
            var spotlight = gameUI.GetComponentInChildren<TutorialSpotlight>(true);
            var hint = (TMP_Text)new SerializedObject(spotlight).FindProperty("hintLabel").objectReferenceValue;
            AssertStatic(hint.font, "the tutorial hint");
            foreach (FieldInfo field in typeof(TutorialTexts).GetFields())
                AssertDrawable(hint.font, (string)field.GetValue(spotlight.Texts), "tutorial " + field.Name);
        }

        // The book's rows draw straight from the TTF (legacy Text), but Book.json stays inside the static set too, so
        // any of our labels can show a name from it.
        [Test]
        public void TheBooksEntriesAreInTheStaticFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DellaStatic);
            AssertStatic(font, DellaStatic);
            BookCatalog book = Config.LoadBook();
            Assert.That(book.Entries.Count, Is.GreaterThan(0));
            foreach (BookEntry entry in book.Entries)
                foreach (string text in new[] { entry.Name, entry.Profession, entry.Address, entry.Jack })
                    AssertDrawable(font, text, "book");
        }

        private static GameConfig Config => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");

        private static TMP_FontAsset FontField(Object owner, string field)
        {
            Assert.That(owner, Is.Not.Null, field);
            return (TMP_FontAsset)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
        }

        private static int CheckLabels(GameObject root, string where)
        {
            int count = 0;
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.font == null || !AssetDatabase.GetAssetPath(label.font).StartsWith("Assets/Fonts/")) continue;
                string name = where + ": " + label.name;
                AssertStatic(label.font, name);
                Assert.That(label.fontSharedMaterial, Is.Not.Null, name);
                Assert.That(label.fontSharedMaterial.GetTexture(ShaderUtilities.ID_MainTex), Is.SameAs(label.font.atlasTexture),
                    name + ": its material draws from its own font's atlas");
                AssertDrawable(label.font, label.text, name);
                count++;
            }
            return count;
        }

        private static void AssertStatic(TMP_FontAsset font, string what)
        {
            Assert.That(font, Is.Not.Null, what);
            Assert.That(DynamicFonts, Does.Not.Contain(AssetDatabase.GetAssetPath(font)), what);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static), what + " uses " + font.name);
        }

        private static void AssertDrawable(TMP_FontAsset font, string text, string what)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Rich-text tags and line breaks are not drawn as letters.
            string shown = Regex.Replace(Regex.Replace(text, "<[^>]*>", string.Empty), "[\\x00-\\x1F]", string.Empty);
            Assert.That(font.HasCharacters(shown, out List<char> missing), Is.True,
                $"{what}: \"{text}\" needs {string.Join(" ", missing ?? new List<char>())}, which {font.name} lacks");
        }
    }
}
