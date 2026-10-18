using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// "To be continued..." when the secret upgrade is bought (Design 5.10). Its dialog blocks clicks behind it
    /// while open, and the game keeps running underneath (D20). The real effect is still open (D15).
    /// </summary>
    public sealed class GoalPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject dialog;
        [SerializeField] private TMP_Text goalName;
        [SerializeField] private Button keepPlaying;

        private GoalProgress goal;

        public void Bind(GoalProgress progress)
        {
            Unsubscribe();
            goal = progress;
            goal.GoalPurchased += OnGoalPurchased;
            keepPlaying.onClick.AddListener(Close);
            dialog.SetActive(false);
        }

        private void OnDisable() => Unsubscribe();

        private void Unsubscribe()
        {
            if (goal != null) goal.GoalPurchased -= OnGoalPurchased;
            if (keepPlaying != null) keepPlaying.onClick.RemoveListener(Close);
            goal = null;
        }

        private void OnGoalPurchased()
        {
            goalName.text = goal.Goal != null ? goal.Goal.Name : string.Empty;
            dialog.SetActive(true);
        }

        private void Close() => dialog.SetActive(false);
    }
}
