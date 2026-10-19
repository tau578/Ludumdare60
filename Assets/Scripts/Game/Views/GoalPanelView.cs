using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// Presents the connected city when the secret upgrade is bought; restored saves never replay the sequence.
    /// </summary>
    public sealed class GoalPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject dialog;
        [SerializeField] private TMP_Text goalName;
        [SerializeField] private Button keepPlaying;
        [SerializeField] private VictorySequenceView victory;
        private GamePauseMenu pause;

        private GoalProgress goal;
        public void ConfigurePresentation(GamePauseMenu pauseMenu) => pause = pauseMenu;

        public void Bind(GoalProgress progress)
        {
            Unsubscribe();
            goal = progress;
            goal.GoalPurchased += OnGoalPurchased;
            keepPlaying.onClick.AddListener(Close);
            dialog.SetActive(false);
        }

        private void OnDisable()
        {
            if (victory != null) victory.Hide();
            if (pause != null) pause.EndPresentation();
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (goal != null) goal.GoalPurchased -= OnGoalPurchased;
            if (keepPlaying != null) keepPlaying.onClick.RemoveListener(Close);
            goal = null;
        }

        private void OnGoalPurchased()
        {
            if (victory != null)
            {
                if (victory.IsOpen) return;
                dialog.SetActive(false);
                if (pause != null) pause.BeginPresentation();
                victory.Play(Close, ReturnToMenu);
                return;
            }
            goalName.text = goal.Goal != null ? goal.Goal.Name : string.Empty;
            dialog.SetActive(true);
        }

        public void PreviewVictory() => OnGoalPurchased();

        private void Close()
        {
            dialog.SetActive(false);
            if (victory != null) victory.Hide();
            if (pause != null) pause.EndPresentation();
        }

        private void ReturnToMenu()
        {
            if (pause != null) pause.GoToMainMenu();
            else Close();
        }
    }
}
