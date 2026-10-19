using System;
using Switchboard.Domain;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Switchboard.Game
{
    /// <summary>
    /// Saves in the browser's localStorage (9b, D26) through BrowserStorage.jslib. Unity's own Web PlayerPrefs live
    /// under a path tied to the page URL, and itch gives every upload a new URL. Outside Web players there is no
    /// browser: <see cref="IsAvailable"/> is false and nothing is stored.
    /// </summary>
    public sealed class BrowserStorage : ISaveStorage
    {
        private const int WriteWarning = 1;
        private const int DeleteWarning = 2;
        private const int FallbackWarning = 3;

        /// <summary>Whether localStorage can be written here (blocked site data and some private modes can't).</summary>
        public static bool IsAvailable() => NativeIsAvailable();

        /// <summary>True only the first time in this page load, so the PlayerPrefs fallback warns once.</summary>
        public static bool ClaimFallbackWarning() => NativeClaimWarning(FallbackWarning);

        public string Read(string key) => NativeRead(key);

        public void Write(string key, string value)
        {
            if (!NativeWrite(key, value) && NativeClaimWarning(WriteWarning))
                Debug.LogWarning("Save: the browser refused to store the game (its storage may be full or blocked), " +
                    "so progress since the last save may be lost when the page closes.");
        }

        public void Delete(string key)
        {
            if (!NativeDelete(key) && NativeClaimWarning(DeleteWarning))
                Debug.LogWarning("Save: the browser refused to delete the saved game.");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int OperatorStorage_IsAvailable();
        [DllImport("__Internal")] private static extern string OperatorStorage_Read(string key);
        [DllImport("__Internal")] private static extern int OperatorStorage_Write(string key, string value);
        [DllImport("__Internal")] private static extern int OperatorStorage_Delete(string key);
        [DllImport("__Internal")] private static extern int OperatorStorage_ClaimWarning(int id);

        private static bool NativeIsAvailable() => OperatorStorage_IsAvailable() == 1;
        private static string NativeRead(string key) => OperatorStorage_Read(key);
        private static bool NativeWrite(string key, string value) => OperatorStorage_Write(key, value) == 1;
        private static bool NativeDelete(string key) => OperatorStorage_Delete(key) == 1;
        private static bool NativeClaimWarning(int id) => OperatorStorage_ClaimWarning(id) == 1;
#else
        private static bool NativeIsAvailable() => false;
        private static string NativeRead(string key) => null;
        private static bool NativeWrite(string key, string value) => false;
        private static bool NativeDelete(string key) => false;
        private static bool NativeClaimWarning(int id) => false;
#endif
    }

    /// <summary>
    /// Calls a save when the browser page is hidden or closed (9b): a hidden tab runs no frames, so neither the 10 s
    /// autosave nor focus events may come. Does nothing outside Web players.
    /// </summary>
    public static class BrowserPageHidden
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private delegate void HiddenCallback();

        // The one static with state (Step 9b's documented exception): the browser calls back into a static method,
        // which has to find the session's Autosave.
        private static Action handler;

        [DllImport("__Internal")] private static extern void OperatorPage_SetHiddenCallback(HiddenCallback callback);

        public static void Register(Action save)
        {
            handler = save;
            OperatorPage_SetHiddenCallback(save != null ? OnHidden : (HiddenCallback)null);
        }

        public static void Unregister(Action save)
        {
            if (handler == null || handler != save) return;
            handler = null;
            OperatorPage_SetHiddenCallback(null);
        }

        [AOT.MonoPInvokeCallback(typeof(HiddenCallback))]
        private static void OnHidden() => handler?.Invoke();
#else
        public static void Register(Action save) { }

        public static void Unregister(Action save) { }
#endif
    }
}
