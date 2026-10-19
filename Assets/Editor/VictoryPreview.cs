using Switchboard.Game;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Editor
{
    public static class VictoryPreview
    {
        [MenuItem("Tools/Switchboard/Preview Victory (Play Mode)")]
        public static void Preview()
        {
            var panel = Object.FindFirstObjectByType<GoalPanelView>();
            if (panel == null) { Debug.LogWarning("Open the Final scene in Play Mode first."); return; }
            panel.PreviewVictory();
            var sound = Object.FindFirstObjectByType<ShopAudio>();
            if (sound != null) sound.PlayPurchased(true);
        }

        [MenuItem("Tools/Switchboard/Preview Victory (Play Mode)", true)]
        private static bool CanPreview() => EditorApplication.isPlaying;
    }
}
