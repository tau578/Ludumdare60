using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Cables;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>The pause menu (D56), built on <c>Final</c> in a preview scene like the other game tests.</summary>
    public sealed class GamePauseMenuTests
    {
        private Scene scene;
        private GamePauseMenu pause;
        private CableInputController cords;
        private PhoneBookView book;
        private CallAudio audio;
        private readonly List<string> log = new List<string>();

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            log.Clear();
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            foreach (GameObject root in scene.GetRootGameObjects())
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(root))
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            book = All<PhoneBookView>().Single();
            book.SetOpen(false);
            GameInstaller installer = All<GameInstaller>().Single();
            typeof(GameInstaller).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(installer, null);
            pause = All<GamePauseMenu>().Single();
            cords = All<CableInputController>().Single();
            audio = All<CallAudio>().Single();
            pause.Bind(() => log.Add("save"), cords, book, audio, index => log.Add("load " + index));
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private GameObject Dialog => pause.transform.Find("Dialog").gameObject;

        [Test]
        public void PauseFreezesTheGameAndResumeRestoresThePreviousTimeScale()
        {
            Time.timeScale = 5f;
            pause.Pause();
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(Dialog.activeSelf, Is.True);
            Assert.That(cords.Suspended, Is.True, "the cord input is off");
            Assert.That(book.InputEnabled, Is.False);
            Assert.That(audio.IsPaused, Is.True, "call sounds pause");
            Assert.That(log, Is.EqualTo(new[] { "save" }), "pausing saves");
            pause.Resume();
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(5f), "the dev panel's x5 survives");
            Assert.That(Dialog.activeSelf, Is.False);
            Assert.That(cords.Suspended || !book.InputEnabled || audio.IsPaused, Is.False);
        }

        [Test]
        public void EscWithTheBookOpenClosesOnlyTheBook()
        {
            Assert.That(book.ClosesOnEscape, Is.False, "the pause menu decides what Esc does");
            book.SetOpen(true);
            pause.HandleEscape();
            Assert.That(book.IsOpen, Is.False);
            Assert.That(pause.IsPaused, Is.False);
            pause.HandleEscape();
            Assert.That(pause.IsPaused, Is.True);
            pause.HandleEscape();
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void MainMenuSavesBeforeItLoadsAndStartsTheClockAgain()
        {
            Time.timeScale = 5f;
            pause.Pause();
            log.Clear();
            pause.GoToMainMenu();
            Assert.That(log, Is.EqualTo(new[] { "save", "load 0" }));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void OptionsShowTheVolumePatchAndBackReturns()
        {
            pause.Pause();
            Transform options = Dialog.transform.Find("Options");
            Assert.That(options.GetComponentInChildren<VolumeCablePanel>(true), Is.Not.Null);
            pause.ShowOptions();
            Assert.That(pause.OptionsOpen, Is.True);
            Assert.That(Dialog.transform.Find("Main").gameObject.activeSelf, Is.False);
            pause.ShowMain();
            Assert.That(pause.OptionsOpen, Is.False);
            Assert.That(Dialog.transform.Find("Main").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void AHeldCordSurvivesPauseAndResume()
        {
            CableController cable = All<CableController>().First(c => c.gameObject.activeInHierarchy);
            SocketView[] jacks = All<SocketView>().Where(s => s.IsOpen && s.gameObject.activeInHierarchy).Take(2).ToArray();
            Assert.That(cable.TryPickUp() && cable.TryInsert(jacks[0]) && cable.TryInsert(jacks[1]), Is.True);
            // Pulling one plug of a patched cord puts that end in hand, as a click on its jack does.
            cords.HandleSocketClick(jacks[1]);
            Assert.That(cords.ActiveCable, Is.SameAs(cable));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            pause.Pause();
            // A click on empty space would put the plug back; while paused nothing happens.
            cords.ProcessPointer(new Vector2(-5000, -5000), true);
            cords.ProcessPointer(new Vector2(-5000, -5000), true, false);
            Assert.That(cords.ActiveCable, Is.SameAs(cable), "clicks while paused don't drop it");
            pause.Resume();
            Assert.That(cords.ActiveCable, Is.SameAs(cable));
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            cords.ProcessPointer(new Vector2(-5000, -5000), true);
            Assert.That(cords.ActiveCable, Is.Null, "after Resume the same click works again");
            Assert.That(cable.Phase, Is.EqualTo(CablePhase.Connected));
        }

        [Test]
        public void ThePanelIsAModalAboveTheBookAndBelowTheCursor()
        {
            Canvas canvas = pause.GetComponent<Canvas>();
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.GreaterThan(SwitchboardUISorting.Book).And.LessThan(SwitchboardUISorting.Cursor));
            Assert.That(Dialog.transform.Find("Blocker").GetComponent<Image>().raycastTarget, Is.True);
            foreach (TMPro.TMP_Text label in pause.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                Assert.That(label.raycastTarget, Is.False, label.name);
                Assert.That(label.font.HasCharacters(label.text), Is.True, label.name + " '" + label.text + "'");
                Assert.That(label.font.atlasPopulationMode, Is.EqualTo(TMPro.AtlasPopulationMode.Static), label.name);
            }
            var button = (RectTransform)pause.transform.Find("PauseButton");
            foreach (Image bar in button.GetComponentsInChildren<Image>(true).Where(i => i.transform != button))
                Assert.That(bar.raycastTarget, Is.False, bar.name);
        }

        // Anchored to the 1920x1080 play area's top-right corner, including on letterboxed screens, and short
        // enough to stay above the shop panel's art (about 85 reference px from the top at 16:9).
        [Test]
        public void ThePauseButtonSitsInThePlayAreaTopRightCorner()
        {
            var button = (RectTransform)pause.transform.Find("PauseButton");
            Assert.That(button.anchorMin, Is.EqualTo(Vector2.one * .5f));
            Assert.That(button.anchorMax, Is.EqualTo(Vector2.one * .5f));
            Assert.That(button.pivot, Is.EqualTo(Vector2.one));
            Assert.That(button.sizeDelta.x, Is.GreaterThanOrEqualTo(72f));
            Assert.That(button.sizeDelta.y, Is.GreaterThanOrEqualTo(72f));
            Assert.That(button.anchoredPosition.x, Is.InRange(944f, 960f));
            Assert.That(button.anchoredPosition.y, Is.InRange(524f, 540f));
            Assert.That(540f - button.anchoredPosition.y + button.sizeDelta.y, Is.LessThanOrEqualTo(80f), "the button reaches down into the shop panel");
            var stretch = (RectTransform)pause.transform;
            Assert.That(stretch.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(stretch.anchorMax, Is.EqualTo(Vector2.one));
        }
    }
}
