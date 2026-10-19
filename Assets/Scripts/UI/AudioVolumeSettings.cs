using System;
using UnityEngine;

namespace Switchboard.UI
{
    public enum AudioVolumeChannel { Master, Music, Sfx }
    public static class AudioVolumeSettings
    {
        // Saved patches store these indices, so mute stays index 4 although it is drawn leftmost (9a fix).
        private static readonly float[] Levels = { .25f, .5f, .75f, 1f, 0f };
        private const string SaveKey = "Switchboard.AudioPatch.v1";
        [Serializable]
        private sealed class SavedPatch
        {
            public int version = 1;
            public int[] rows;
            public int[] sockets;
        }
        public static VolumePatchState Patch { get; private set; } = new VolumePatchState();
        public static event Action Changed;
        public static float GetVolume(AudioVolumeChannel channel) => Levels[Patch.SocketOf((int)channel)];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Patch = new VolumePatchState();
            Changed = null;
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedPatch>(PlayerPrefs.GetString(SaveKey));
                if (saved != null && saved.version == 1) Patch.Restore(saved.rows, saved.sockets);
            }
            catch (ArgumentException) { /* Invalid saves fall back to the default connections. */ }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => Apply();
        public static bool Connect(int row, int socket)
        {
            if (!Patch.Connect(row, socket)) return false;
            var saved = new SavedPatch { rows = new int[3], sockets = new int[3] };
            for (int i = 0; i < 3; i++)
            {
                saved.rows[i] = Patch.RowOf(i);
                saved.sockets[i] = Patch.SocketOf(i);
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
            Apply();
            return true;
        }
        private static void Apply()
        {
            AudioListener.volume = GetVolume(AudioVolumeChannel.Master);
            Changed?.Invoke();
        }
    }
}
