using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class GameLoopRunner : MonoBehaviour
    {
        private GameSession session;

        public void Bind(GameSession gameSession) => session = gameSession;

        // Time.deltaTime is clamped by Time.maximumDeltaTime and stops while the tab is hidden (Design 8).
        private void Update() => session?.Tick(Time.deltaTime);
    }
}
