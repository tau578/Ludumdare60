using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class GameLoopRunner : MonoBehaviour
    {
        private GameSession session;

        public void Bind(GameSession gameSession) => session = gameSession;

        // Time.deltaTime is clamped by Time.maximumDeltaTime and stops while the tab is hidden (Design 8).
        private void Update()
        {
            // Paused (timeScale 0, D56): no tick at all, so not even a free slot deals a new call.
            if (session == null || Time.deltaTime <= 0f) return;
            session.Tick(Time.deltaTime);
        }

    }
}
