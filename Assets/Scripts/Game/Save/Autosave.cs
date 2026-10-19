using Switchboard.Content;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// Saves the session (5.12): right after a purchase, every 10 s of real time when the save changed, when the
    /// window loses focus or the app pauses, and when the game closes. It captures only then, never per frame.
    /// </summary>
    public sealed class Autosave : MonoBehaviour
    {
        private const float IntervalSeconds = 10f;

        private GameSession session;
        private SaveFile file;
        private string lastWritten;
        private float nextCheck;

        public void Bind(GameSession gameSession, SaveFile saveFile)
        {
            Stop();
            session = gameSession;
            file = saveFile;
            lastWritten = null;
            nextCheck = Time.unscaledTime + IntervalSeconds;
            if (session != null) session.Upgrades.UpgradeBought += OnUpgradeBought;
        }

        /// <summary>No more saves from this session, e.g. after New game deleted the save.</summary>
        public void Stop()
        {
            if (session != null) session.Upgrades.UpgradeBought -= OnUpgradeBought;
            session = null;
        }

        public void SaveNow()
        {
            // A new game with nothing done yet writes no save, so New game stays hidden in the menu.
            if (session == null || file == null || !session.HasProgress) return;
            string json = SaveFile.ToJson(session.CaptureSave());
            if (json == lastWritten) return;
            file.Write(json);
            lastWritten = json;
        }

        private void Update()
        {
            if (session == null || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + IntervalSeconds;
            SaveNow();
        }

        private void OnUpgradeBought(UpgradeBought _) => SaveNow();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnDestroy()
        {
            SaveNow();
            Stop();
        }
    }
}
