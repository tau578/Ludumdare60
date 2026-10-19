using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Switchboard.UI
{
    /// <summary>
    /// Shows or hides the system pointer next to the drawn hand; all pointer code goes through here. It writes only on a
    /// change. In Web players it hides the pointer with a transparent image (SystemPointer.jslib), not Unity's
    /// cursor: none, which Chrome on some Mac displays shows again on every mouse move (9b).
    /// </summary>
    public static class SystemPointer
    {
        private static bool? shown;

        public static bool Visible
        {
            get => shown ?? Cursor.visible;
            set
            {
                if (shown == value) return;
                shown = value;
#if UNITY_WEBGL && !UNITY_EDITOR
                SystemPointer_SetShown(value ? 1 : 0);
#else
                Cursor.visible = value;
#endif
            }
        }

        // Each Play session starts with the pointer Unity shows, even when statics survive (no domain reload).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget() => shown = null;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void SystemPointer_SetShown(int shown);
#endif
    }
}
