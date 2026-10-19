using System;
using Switchboard.Content;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>Picks where saves live on this platform. Stateless: every caller gets its own instance.</summary>
    public static class SaveStorage
    {
        public static ISaveStorage Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Choose(true, BrowserStorage.IsAvailable, () => new BrowserStorage(), WarnFallback);
#else
            return Choose(false, null, null, null);
#endif
        }

        public static SaveFile OpenFile() => new SaveFile(Create(), SaveMigrations.All);

        /// <summary>
        /// The choice itself (9b): Web players use the browser's localStorage while it works, otherwise PlayerPrefs
        /// with one warning; the editor and desktop builds use PlayerPrefs.
        /// </summary>
        public static ISaveStorage Choose(bool webPlayer, Func<bool> browserWorks, Func<ISaveStorage> browser,
            Action warnFallback)
        {
            if (!webPlayer) return new PlayerPrefsStorage();
            if (browserWorks != null && browser != null && browserWorks()) return browser();
            warnFallback?.Invoke();
            return new PlayerPrefsStorage();
        }

        private static void WarnFallback()
        {
            if (BrowserStorage.ClaimFallbackWarning())
                Debug.LogWarning("Save: this browser blocks its storage for this page (private mode or blocked site data), " +
                    "so progress may not survive a new upload of the game.");
        }
    }
}
