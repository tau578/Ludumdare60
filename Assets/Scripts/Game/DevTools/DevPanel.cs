#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Switchboard.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Switchboard.Game
{
    /// <summary>
    /// Testing shortcuts, compiled only into the editor and development builds. F1 or the corner button toggles it.
    /// It sits in the strip right of the shop panel, clear of the board, the tray and the open book.
    /// </summary>
    public sealed class DevPanel : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ButtonWidth = 74f;
        private const float ButtonHeight = 34f;
        private const float Gap = 6f;
        private const float Margin = 3f;
        private const float FastTimeScale = 5f;

        private GameSession session;
        private CallQueueView callQueue;
        private bool open;
        private GUIStyle style;

        public void Bind(GameSession gameSession, CallQueueView queue)
        {
            session = gameSession;
            callQueue = queue;
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

            float x = Screen.width / scale - ButtonWidth - Margin;
            float y = Margin;
            bool fast = Time.timeScale > 1f;
            if (Button(x, ref y, open ? "Close" : fast ? "Dev ×5" : "Dev")) open = !open;
            if (!open) return;

            if (Button(x, ref y, "+ $10")) session.Wallet.Earn(1000);
            if (Button(x, ref y, "+ $100")) session.Wallet.Earn(10000);
            if (Button(x, ref y, fast ? "Speed ×1" : "Speed ×5")) Time.timeScale = fast ? 1f : FastTimeScale;
            if (Button(x, ref y, callQueue.ShowDurations ? "Hide times" : "Show times"))
                callQueue.ShowDurations = !callQueue.ShowDurations;
            // Clearing ends calls as Cleared, which never pays or fines; the slots refill from where the queue was.
            if (Button(x, ref y, "Clear calls")) session.ClearCalls();
            // Opens the next board size for free, as if bought (D49).
            if (Button(x, ref y, "Open board")) session.Upgrades.TryGrantNext(UpgradeKind.ExtraClients);
            // Forgets purchases without a refund, so the shop is back to its start. The board closes again, which
            // also clears live calls, sends back cords plugged into closing jacks and restarts startPool.
            if (Button(x, ref y, "Reset upgrades")) session.ResetUpgrades();
        }

        private bool Button(float x, ref float y, string label)
        {
            bool pressed = GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), label, style);
            y += ButtonHeight + Gap;
            return pressed;
        }
    }
}
#endif
