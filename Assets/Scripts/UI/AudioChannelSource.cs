using UnityEngine;

namespace Switchboard.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class AudioChannelSource : MonoBehaviour
    {
        [SerializeField] private AudioVolumeChannel channel = AudioVolumeChannel.Sfx;
        [SerializeField, Range(0, 1)] private float baseVolume = .6f;
        private AudioSource source;
        public AudioVolumeChannel Channel => channel;
        public float BaseVolume => baseVolume;
        public void Configure(AudioVolumeChannel bus, float volume)
        {
            channel = bus; baseVolume = volume;
            if (Application.isPlaying) Refresh();
        }
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            AudioVolumeSettings.Changed += Refresh;
            Refresh();
        }
        private void OnDisable() => AudioVolumeSettings.Changed -= Refresh;
        private void Refresh()
        {
            if (source == null) source = GetComponent<AudioSource>();
            // Master is applied by AudioListener, so it must not be multiplied here again.
            source.volume = baseVolume * (channel == AudioVolumeChannel.Master ? 1 : AudioVolumeSettings.GetVolume(channel));
        }
    }
}
