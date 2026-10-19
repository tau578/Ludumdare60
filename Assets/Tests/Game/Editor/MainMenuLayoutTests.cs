using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Switchboard.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>The main menu after the lead's 9a playtest (D55): save-aware buttons, centred labels, mute first.</summary>
    public sealed class MainMenuLayoutTests
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string StartMenuPath = "Assets/Tools/rodrigo_menu_tools/Runtime/Prefabs/StartMenu.prefab";

        [Test]
        public void SlotsAreEvenlySpacedAroundTheCentre()
        {
            float[] five = Enumerable.Range(0, 5).Select(i => NewGameMenu.SlotY(i, 5, -50f, 122f)).ToArray();
            float[] four = Enumerable.Range(0, 4).Select(i => NewGameMenu.SlotY(i, 4, -50f, 122f)).ToArray();
            Assert.That(five, Is.EqualTo(new[] { 194f, 72f, -50f, -172f, -294f }));
            Assert.That(four, Is.EqualTo(new[] { 133f, 11f, -111f, -233f }));
        }

        [Test]
        public void WithoutASaveNewGameComesFirstAndContinueIsHidden()
        {
            WithMenu(menu =>
            {
                menu.Apply(false);
                Assert.That(VisibleButtons(menu), Is.EqualTo(new[] { "New_Game_Button", "Options_Button", "Credits_Button", "Quit_Game_Button" }));
                AssertEvenlySpaced(menu);
                Assert.That(Panel(menu).Find("Continue_Button").gameObject.activeSelf, Is.False);
            });
        }

        [Test]
        public void WithASaveContinueComesFirstAndNewGameAsks()
        {
            WithMenu(menu =>
            {
                menu.Apply(true);
                Assert.That(VisibleButtons(menu), Is.EqualTo(new[] { "Continue_Button", "New_Game_Button", "Options_Button", "Credits_Button", "Quit_Game_Button" }));
                AssertEvenlySpaced(menu);
                GameObject confirm = menu.transform.Find("ConfirmPanel").gameObject;
                Assert.That(confirm.activeSelf, Is.False);
                menu.OnNewGame();
                Assert.That(confirm.activeSelf, Is.True);
                menu.Cancel();
                Assert.That(confirm.activeSelf, Is.False);
            });
        }

        // 9a pass 3: Application.Quit does nothing on Web, so the Web build hides Quit and the rest close up.
        [TestCase(true, new[] { "Continue_Button", "New_Game_Button", "Options_Button", "Credits_Button" })]
        [TestCase(false, new[] { "New_Game_Button", "Options_Button", "Credits_Button" })]
        public void OnWebQuitIsHiddenAndTheRestStayEvenlySpaced(bool saveExists, string[] expected)
        {
            WithMenu(menu =>
            {
                menu.Apply(saveExists, false);
                Assert.That(VisibleButtons(menu), Is.EqualTo(expected));
                AssertEvenlySpaced(menu);
                Assert.That(Panel(menu).Find("Quit_Game_Button").gameObject.activeSelf, Is.False);
                menu.Apply(saveExists, true);
                Assert.That(VisibleButtons(menu).Last(), Is.EqualTo("Quit_Game_Button"));
                AssertEvenlySpaced(menu);
            });
        }

        [Test]
        public void QuitShowsInTheEditorAndDesktopBuilds()
        {
            Assert.That(NewGameMenu.QuitAvailable, Is.True, "the editor counts as a desktop build");
            WithMenu(menu =>
            {
                menu.Apply(true);
                Assert.That(Panel(menu).Find("Quit_Game_Button").gameObject.activeSelf, Is.True);
            });
        }

        [Test]
        public void PlayGameIsGoneAndNothingPointsAtAMissingObject()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StartMenuPath);
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Play_Game")), Is.False);
            AssertNoMissingReferences(prefab);
            WithMenu(menu =>
            {
                AssertNoMissingReferences(menu.transform.root.gameObject);
                Assert.That(menu.transform.root.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Play_Game")), Is.False);
            });
        }

        [Test]
        public void EveryMenuLabelIsCentredInItsButtonAndInTheFont()
        {
            WithMenu(menu =>
            {
                var buttons = Panel(menu).GetComponentsInChildren<Button>(true).ToList();
                buttons.AddRange(menu.transform.Find("ConfirmPanel").GetComponentsInChildren<Button>(true));
                Assert.That(buttons.Count, Is.EqualTo(7));
                foreach (Button button in buttons)
                {
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                    RectTransform rect = label.rectTransform;
                    Assert.That(label.alignment, Is.EqualTo(TextAlignmentOptions.Center), button.name);
                    Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero), button.name);
                    Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one), button.name);
                    Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero), button.name);
                    Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero), button.name);
                    Assert.That(label.fontSizeMax, Is.EqualTo(18f), button.name);
                    Assert.That(label.margin, Is.EqualTo(new Vector4(0, 0, 0, 3)), button.name);
                    Assert.That(label.text, Is.EqualTo(label.text.Trim()), button.name);
                    // The menu font is dynamic, so its glyphs come from the source file, not the saved atlas.
                    Assert.That(FontEngine.LoadFontFace(label.font.sourceFontFile), Is.EqualTo(FontEngineError.Success), button.name);
                    foreach (char c in label.text)
                        Assert.That(FontEngine.TryGetGlyphIndex(c, out uint glyph) && glyph != 0, Is.True, button.name + " '" + c + "'");
                }
                Assert.That(Panel(menu).Find("New_Game_Button").GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("New Game"));
                Assert.That(Panel(menu).Find("Quit_Game_Button").GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("Quit"));
            });
        }

        [TestCase("Assets/Prefabs/UI/VolumeCablePanel.prefab", 3)]
        [TestCase("Assets/Prefabs/UI/MasterVolumeCableSlider.prefab", 1)]
        public void TheMuteSocketIsDrawnFirst(string path, int rowCount)
        {
            VolumeCableSlider[] rows = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<VolumeCableSlider>(true);
            Assert.That(rows.Length, Is.EqualTo(rowCount));
            string[] labels = { "Label_25", "Label_50", "Label_75", "Label_100", "MuteIcon" };
            foreach (VolumeCableSlider row in rows)
            {
                float[] x = row.Sockets.Select(s => s.rectTransform.anchoredPosition.x).ToArray();
                Assert.That(new[] { x[4], x[0], x[1], x[2], x[3] }, Is.Ordered, row.name);
                for (int i = 0; i < 5; i++)
                    Assert.That(((RectTransform)row.transform.Find(labels[i])).anchoredPosition.x, Is.EqualTo(x[i]), row.name + " " + labels[i]);
            }
        }

        // 9a pass 2: the five sockets sit centred on the panel, mute first, and the cable's left end keeps its distance.
        [TestCase("Assets/Prefabs/UI/VolumeCablePanel.prefab")]
        [TestCase("Assets/Prefabs/UI/MasterVolumeCableSlider.prefab")]
        public void TheSocketRowIsCentredOnThePanel(string path)
        {
            foreach (VolumeCableSlider row in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<VolumeCableSlider>(true))
            {
                float[] x = row.Sockets.Select(s => s.rectTransform.anchoredPosition.x).ToArray();
                Assert.That(new[] { x[4], x[0], x[1], x[2], x[3] }, Is.EqualTo(new[] { -410f, -205f, 0f, 205f, 410f }), row.name);
                Assert.That(((RectTransform)row.transform).rect.center.x, Is.EqualTo(0f), row.name);
                float anchor = row.WallAnchor != null ? row.WallAnchor.anchoredPosition.x : row.FixedSocket.rectTransform.anchoredPosition.x;
                float muteLeft = x[4] - row.Sockets[4].rectTransform.sizeDelta.x / 2;
                float anchorRight = anchor + (row.FixedSocket != null ? row.FixedSocket.rectTransform.sizeDelta.x / 2 : 0);
                Assert.That(muteLeft - anchorRight, Is.GreaterThanOrEqualTo(60f), row.name + ": the cable's left end crowds the mute socket");
            }
        }

        [Test]
        public void SavedSocketIndicesKeepTheirVolumes()
        {
            VolumePatchState patch = AudioVolumeSettings.Patch;
            int[] rows = Enumerable.Range(0, 3).Select(patch.RowOf).ToArray();
            int[] sockets = Enumerable.Range(0, 3).Select(patch.SocketOf).ToArray();
            try
            {
                // A v1 patch saved before mute moved: Master 25%, Music mute, SFX 100%.
                Assert.That(patch.Restore(new[] { 0, 1, 2 }, new[] { 0, 4, 3 }), Is.True);
                Assert.That(AudioVolumeSettings.GetVolume(AudioVolumeChannel.Master), Is.EqualTo(.25f));
                Assert.That(AudioVolumeSettings.GetVolume(AudioVolumeChannel.Music), Is.EqualTo(0f));
                Assert.That(AudioVolumeSettings.GetVolume(AudioVolumeChannel.Sfx), Is.EqualTo(1f));
            }
            finally { patch.Restore(rows, sockets); }
            var fresh = new VolumePatchState();
            for (int i = 0; i < 3; i++) Assert.That(fresh.SocketOf(i), Is.EqualTo(1), "the default patch stays at 50%");
        }

        private static void WithMenu(System.Action<NewGameMenu> check)
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                NewGameMenu menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NewGameMenu>(true)).Single();
                check(menu);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static Transform Panel(NewGameMenu menu) => menu.transform.parent.Find("MainMenu");

        private static string[] VisibleButtons(NewGameMenu menu) =>
            Panel(menu).GetComponentsInChildren<Button>(true)
                .Where(b => b.gameObject.activeSelf)
                .OrderByDescending(b => ((RectTransform)b.transform).anchoredPosition.y)
                .Select(b => b.name).ToArray();

        private static void AssertEvenlySpaced(NewGameMenu menu)
        {
            float[] y = Panel(menu).GetComponentsInChildren<Button>(true)
                .Where(b => b.gameObject.activeSelf)
                .Select(b => ((RectTransform)b.transform).anchoredPosition.y)
                .OrderByDescending(v => v).ToArray();
            for (int i = 1; i < y.Length; i++) Assert.That(y[i - 1] - y[i], Is.EqualTo(122f).Within(0.01f));
            Assert.That((y.First() + y.Last()) / 2f, Is.EqualTo(-50f).Within(0.01f));
            foreach (Button b in Panel(menu).GetComponentsInChildren<Button>(true))
                Assert.That(((RectTransform)b.transform).anchoredPosition.x, Is.EqualTo(0f), b.name);
        }

        private static void AssertNoMissingReferences(GameObject root)
        {
            var missing = new List<string>();
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) { missing.Add("a missing script"); continue; }
                if (!(component is global::Menu || component is Selectable || component is NewGameMenu || component is UnityEngine.EventSystems.EventSystem)) continue;
                SerializedProperty it = new SerializedObject(component).GetIterator();
                while (it.Next(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
                        missing.Add(component.name + "/" + component.GetType().Name + "." + it.propertyPath);
            }
            Assert.That(missing, Is.Empty);
        }
    }
}
