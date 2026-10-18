using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// A short UI click. Its volume follows the team's Sfx slider through their AudioChannelSource
    /// on the same object, so only the pitch is set here.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class UiClickSound : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private AudioSource source;
        [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.94f;
        [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.08f;

        public void Play()
        {
            if (clip == null || source == null) return;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(clip);
        }
    }
}
