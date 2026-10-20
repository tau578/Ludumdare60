using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>The dev panel's IMGUI "Dev" button follows the pause button, which sits in the play area's corner.</summary>
    public sealed class DevPanelLayoutTests
    {
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void At16By9TheDevButtonKeepsItsPlace(int width, int height)
        {
            Vector2 dev = DevPanel.FirstButtonPosition(width, height);
            Assert.That(dev.x, Is.EqualTo(1920f - 84f - 74f).Within(.01f));
            Assert.That(dev.y, Is.EqualTo(6f).Within(.01f));
        }

        [TestCase(1920, 1080)]
        [TestCase(2160, 1080)] // 18:9 phone
        [TestCase(2340, 1080)] // 19.5:9 phone
        [TestCase(3440, 1440)] // ultrawide
        [TestCase(1024, 768)]  // 4:3, taller than 16:9
        public void TheDevButtonSitsJustLeftOfThePauseButton(int width, int height)
        {
            GameObject ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/GameUI.prefab");
            CanvasScaler scaler = ui.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            RectTransform pause = ui.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "PauseButton");
            Assert.That(pause.anchorMin, Is.EqualTo(Vector2.one * .5f));
            Assert.That(pause.pivot, Is.EqualTo(Vector2.one));

            // The Expand canvas and the IMGUI matrix share one scale, so both measure in the same units; IMGUI's y
            // runs down from the screen's top-left corner, the canvas's up from its centre.
            float scale = Mathf.Min(width / 1920f, height / 1080f);
            float pauseLeft = width / scale / 2f + pause.anchoredPosition.x - pause.sizeDelta.x;
            float pauseTop = height / scale / 2f - pause.anchoredPosition.y;
            Vector2 dev = DevPanel.FirstButtonPosition(width, height);
            Assert.That(dev.x + DevPanel.ButtonWidth + DevPanel.Gap, Is.EqualTo(pauseLeft).Within(.01f));
            Assert.That(dev.y, Is.EqualTo(pauseTop).Within(.01f));
        }
    }
}
