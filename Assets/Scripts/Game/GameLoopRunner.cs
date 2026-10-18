using Switchboard.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Switchboard.Game
{
    public sealed class GameLoopRunner : MonoBehaviour
    {
        private GameSession session;

        public void Bind(GameSession gameSession) => session = gameSession;

        // Time.deltaTime is clamped by Time.maximumDeltaTime and stops while the tab is hidden (Design 8).
        private void Update()
        {
            if (session == null) return;
            if (HadInput()) session.Idle.ReportInput();
            session.Tick(Time.deltaTime);
        }

        // Any press or pointer movement, or any key, means the player is still there (D16).
        private static bool HadInput()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && (pointer.press.wasPressedThisFrame || pointer.delta.ReadValue().sqrMagnitude > 0f)) return true;
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }
    }
}
