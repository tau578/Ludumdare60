using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource), typeof(AudioChannelSource))]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        private bool ownsPlayback;

        private void Awake()
        {
            // Each entry scene has the same prefab; keep the source already playing.
            foreach (BackgroundMusic music in FindObjectsByType<BackgroundMusic>(FindObjectsSortMode.None))
            {
                if (music == this || !music.ownsPlayback) continue;
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            ownsPlayback = true;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!ownsPlayback) return;
            AudioSource source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            if (source.clip != null) source.Play();
        }
    }
}
