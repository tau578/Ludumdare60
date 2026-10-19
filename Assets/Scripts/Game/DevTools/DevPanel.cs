#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Switchboard.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Switchboard.Game
{
    /// <summary>
    /// Testing shortcuts, compiled only into the editor and development builds. F1 or the corner button toggles it.
    /// It sits left of the pause button in the free strip along the top edge and opens as a row going left, clear
    /// of the board, the shop slots (IMGUI clicks would reach a slot under them too), the tray and the open book.
    /// </summary>
    public sealed class DevPanel : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ButtonWidth = 74f;
        private const float ButtonHeight = 34f;
        private const float Gap = 6f;
        private const float Margin = 6f;
        // The pause button in the top-right corner: its 6 px margin, 72 px width and a 6 px gap, in reference pixels.
        private const float RightOffset = 84f;
        private const float FastTimeScale = 5f;

        private GameSession session;
        private CallQueueView callQueue;
        private Action newGame;
        private bool open;
        private GUIStyle style;

        public void Bind(GameSession gameSession, CallQueueView queue, Action startNewGame = null)
        {
            session = gameSession;
            callQueue = queue;
            newGame = startNewGame;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) open = !open;
        }

        private void OnDisable() => Time.timeScale = 1f;

        private void OnGUI()
        {
            if (session == null) return;
            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (style == null) style = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true };

            float x = Screen.width / scale - RightOffset - ButtonWidth;
            bool fast = Time.timeScale > 1f;
            if (Button(ref x, open ? "Close" : fast ? "Dev ×5" : "Dev")) open = !open;
            if (!open) return;

            if (Button(ref x, "+ $10")) session.Wallet.Earn(1000);
            if (Button(ref x, "+ $100")) session.Wallet.Earn(10000);
            if (Button(ref x, fast ? "Speed ×1" : "Speed ×5")) Time.timeScale = fast ? 1f : FastTimeScale;
            if (Button(ref x, callQueue.ShowDurations ? "Hide times" : "Show times"))
                callQueue.ShowDurations = !callQueue.ShowDurations;
            // Clearing ends calls as Cleared, which never pays or fines; the slots refill from where the queue was.
            if (Button(ref x, "Clear calls")) session.ClearCalls();
            // Opens the next board size for free, as if bought (D49).
            if (Button(ref x, "Open board")) session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            // Forgets purchases without a refund, so the shop is back to its start. The board closes again, which
            // also clears live calls, sends back cords plugged into closing jacks and restarts startPool.
            if (Button(ref x, "Reset upgrades")) session.ResetUpgrades();
            // Deletes the save, career included, and reloads this scene as a new game (D55).
            if (newGame != null && Button(ref x, "New game")) newGame();
        }

        private bool Button(ref float x, string label)
        {
            bool pressed = GUI.Button(new Rect(x, Margin, ButtonWidth, ButtonHeight), label, style);
            x -= ButtonWidth + Gap;
            return pressed;
        }
    }
}
#endif
