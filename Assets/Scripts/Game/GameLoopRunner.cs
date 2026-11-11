using System;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class GameLoopRunner : MonoBehaviour
    {
        private GameSession session;
        private Func<bool> holdClock;

        /// <param name="clockHeld">
        /// While true, the game's clock stands still with input live: a tutorial step is on screen (D81).
        /// </param>
        public void Bind(GameSession gameSession, Func<bool> clockHeld = null)
        {
            session = gameSession;
            holdClock = clockHeld;
        }

        /// <summary>The game's clock stands still for a tutorial step (D81); the pause menu stops Unity's time instead (D56).</summary>
        public bool IsClockHeld => holdClock != null && holdClock();

        // Time.deltaTime is clamped by Time.maximumDeltaTime and stops while the tab is hidden (Design 8).
        private void Update() => Advance(Time.deltaTime);

        /// <summary>Ticks the game by frame seconds, unless paused or held. Update drives it; tests call it.</summary>
        public void Advance(float seconds)
        {
            // Paused (timeScale 0, D56): no tick at all, so not even a free slot deals a new call. A tutorial step holds
            // the clock the same way, without stopping Unity's time, so cards, cords and the spotlight still move.
            if (session == null || !(seconds > 0f) || IsClockHeld) return;
            session.Tick(seconds);
        }
    }
}
