using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource), typeof(AudioChannelSource))]
    public sealed class ShopAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip purchased;
        [SerializeField] private AudioClip insufficientFunds;
        [SerializeField] private AudioClip secretUnlocked;
        private AudioSource source;
        private float nextRefusal;

        public void PlayInsufficientFunds()
        {
            if (Time.unscaledTime < nextRefusal || insufficientFunds == null) return;
            nextRefusal = Time.unscaledTime + .3f;
            Source.PlayOneShot(insufficientFunds);
        }

        public void PlayPurchased(bool secret)
        {
            AudioClip clip = secret ? secretUnlocked : purchased;
            if (clip == null) return;
            if (secret)
            {
                Source.Stop();
                // A muted SFX channel must not cause an unexplained dip in music.
                if (AudioVolumeSettings.GetVolume(AudioVolumeChannel.Sfx) > 0 && AudioListener.volume > 0)
                    AudioChannelSource.DuckMusic(clip.length);
            }
            Source.PlayOneShot(clip);
        }

        private AudioSource Source => source != null ? source : source = GetComponent<AudioSource>();
        private void OnDisable() { if (source != null) source.Stop(); }
    }
}
