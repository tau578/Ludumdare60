using System.Linq;
using NUnit.Framework;
using Switchboard.Content;
using Switchboard.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    public sealed class VictorySequenceTests
    {
        private GameObject root;
        private Scene scene;
        private GamePauseMenu pause;
        private GoalPanelView goalPanel;
        private VictorySequenceView view;
        private UpgradeService upgrades;
        private GoalProgress progress;
        private int saves, loadedScene;
        private bool savedSecret;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1;
            saves = 0; loadedScene = -1; savedSecret = false;
            scene = EditorSceneManager.NewPreviewScene();
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/GameUI.prefab"));
            SceneManager.MoveGameObjectToScene(root, scene);
            pause = root.GetComponentInChildren<GamePauseMenu>(true);
            goalPanel = root.GetComponentInChildren<GoalPanelView>(true);
            view = root.GetComponentInChildren<VictorySequenceView>(true);
            var wallet = new Wallet(200);
            upgrades = new UpgradeService(new IUpgrade[] {
                new UpgradeRecord("test-secret", UpgradeKind.Secret, "Secret", "", 100, 1)
            }, wallet);
            progress = new GoalProgress(upgrades, wallet);
            pause.Bind(() => { saves++; savedSecret = upgrades.IsOwned("test-secret"); }, null, null, null, index => loadedScene = index);
            goalPanel.ConfigurePresentation(pause);
            goalPanel.Bind(progress);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Time.timeScale = 1;
        }

        private Button Button(string name) => view.PresentationCanvas.GetComponentsInChildren<Button>(true).Single(b => b.name == name);

        [Test]
        public void SecretPurchaseSavesBeforePresentationAndContinueRestoresSpeed()
        {
            Time.timeScale = 5;
            Assert.That(upgrades.TryBuy("test-secret"), Is.True);
            Assert.That(view.IsOpen && pause.PresentationOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(savedSecret, Is.True, "the save already owns the upgrade");
            view.Skip();
            Button("Keep Playing").onClick.Invoke();
            Assert.That(view.IsOpen || pause.PresentationOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(5));
            Assert.That(upgrades.TryBuy("test-secret"), Is.False);
            Assert.That(view.IsOpen, Is.False, "an owned upgrade cannot replay the sequence");
        }

        [Test]
        public void RestoringAnOwnedSecretDoesNotReplayVictory()
        {
            upgrades.Restore(new[] { "test-secret" });
            goalPanel.Bind(new GoalProgress(upgrades, new Wallet()));
            Assert.That(view.IsOpen || pause.PresentationOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(saves, Is.Zero);
        }

        [Test]
        public void SkipRevealsActionsAndEscapeCannotOpenPauseBehindThePresentation()
        {
            upgrades.TryBuy("test-secret");
            Assert.That(Button("Keep Playing").IsInteractable(), Is.False);
            pause.HandleEscape();
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            view.Skip();
            Assert.That(view.IsFinished, Is.True);
            Assert.That(Button("Keep Playing").IsInteractable(), Is.True);
            Assert.That(view.PresentationCanvas.GetComponentsInChildren<Button>(true).Any(b => b.name == "Skip"), Is.False);
        }

        [Test]
        public void CityUsesEveryPortraitOnceAndConnectionsAnimateInSeparatePairs()
        {
            goalPanel.PreviewVictory();
            var images = view.PresentationCanvas.GetComponentsInChildren<Image>(true).Where(i => i.name == "Portrait").ToArray();
            var allPortraits = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites/callers" })
                .SelectMany(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<Sprite>()).ToArray();
            Assert.That(images.Select(i => i.sprite), Is.EquivalentTo(allPortraits), "every portrait, including police, appears exactly once");
            Assert.That(images.All(i => i.sprite != null), Is.True);
            Assert.That(images.Select(i => i.sprite).Distinct().Count(), Is.EqualTo(images.Length));
            var links = (int[,])typeof(VictorySequenceView).GetField("Links", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var endpoints = links.Cast<int>().ToArray();
            Assert.That(endpoints.Distinct().Count(), Is.EqualTo(endpoints.Length), "no resident may be connected twice");
            Assert.That(links.GetLength(0), Is.EqualTo(images.Length / 2));
            var plugs = view.PresentationCanvas.GetComponentsInChildren<RectTransform>(true).Where(r => r.name.StartsWith("Plug ")).ToArray();
            view.RenderAt(6f);
            var before = plugs.Select(r => r.anchoredPosition).ToArray();
            view.RenderAt(6.2f);
            int moving = plugs.Where((r, i) => Vector2.Distance(r.anchoredPosition, before[i]) > .1f).Count();
            Assert.That(moving, Is.GreaterThanOrEqualTo(2), "several wires must be travelling at once");
            view.RenderAt(14f);
            Assert.That(view.IsFinished, Is.False, "the old ending now falls inside the slower animation");
            Assert.That(Button("Keep Playing").IsInteractable(), Is.False);
            view.Skip();
            Assert.That(Button("Keep Playing").IsInteractable(), Is.True);
        }

        [Test]
        public void MainMenuSavesAndResetsClockBeforeLoading()
        {
            upgrades.TryBuy("test-secret");
            view.Skip();
            Button("Main Menu").onClick.Invoke();
            Assert.That(loadedScene, Is.EqualTo(0));
            Assert.That(saves, Is.EqualTo(2));
            Assert.That(savedSecret, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(view.IsOpen || pause.PresentationOpen, Is.False);
        }

        [Test]
        public void PreviewDoesNotPurchaseSecretAndPreservesAnExistingPause()
        {
            Time.timeScale = 5;
            pause.Pause();
            goalPanel.PreviewVictory();
            view.Skip();
            Button("Keep Playing").onClick.Invoke();
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(upgrades.IsOwned("test-secret"), Is.False);
            pause.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(5));
        }
    }
}
