using System.Collections;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Switchboard.Game
{
    /// <summary>
    /// Lifts the Web page's loading cover once the main menu is really on screen (2026-10-20): the page used to drop it
    /// when Unity's loader finished, which could show a frame or two of black first. It waits for the end of the frame
    /// after the first one, then calls the page once per page load (GameReady.jslib). Outside Web players it does nothing.
    /// </summary>
    public sealed class PageReady : MonoBehaviour
    {
        private static bool signalled;

        private IEnumerator Start()
        {
            if (signalled) yield break;
            yield return null;
            yield return new WaitForEndOfFrame();
            Signal();
        }

        private static void Signal()
        {
            if (signalled) return;
            signalled = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            OperatorPage_GameReady();
#endif
        }

        // Each Play session starts unsignalled, even when statics survive (no domain reload).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget() => signalled = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void OperatorPage_GameReady();
#endif
    }
}
