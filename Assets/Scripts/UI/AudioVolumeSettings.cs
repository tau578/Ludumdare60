using System;
using Switchboard.Domain;
using Switchboard.Game;
using UnityEngine;

namespace Switchboard.UI
{
    public enum AudioVolumeChannel { Master, Music, Sfx }
    public static class AudioVolumeSettings
    {
        // Saved patches store these indices, so mute stays index 4 although it is drawn leftmost (9a fix).
        private static readonly float[] Levels = { .25f, .5f, .75f, 1f, 0f };
        // Stored with the saves (9b): localStorage in Web players, PlayerPrefs elsewhere. The key predates 9b and stays.
        public const string SaveKey = "Switchboard.AudioPatch.v1";
        [Serializable]
        private sealed class SavedPatch
        {
            public int version = 1;
            public int[] rows;
            public int[] sockets;
            // Bottom to top; saves from before 2026-10-20 have none and get the default stack.
            public int[] order;
        }
        public static VolumePatchState Patch { get; private set; } = new VolumePatchState();
        public static event Action Changed;
        public static float GetVolume(AudioVolumeChannel channel) => Levels[Patch.SocketOf((int)channel)];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Changed = null;
            Patch = Load(SaveStorage.Create());
        }
        /// <summary>The saved patch, or the default one when nothing usable is stored.</summary>
        public static VolumePatchState Load(ISaveStorage storage)
        {
            var patch = new VolumePatchState();
            string json = storage.Read(SaveKey);
            if (string.IsNullOrEmpty(json)) return patch;
            try
            {
                var saved = JsonUtility.FromJson<SavedPatch>(json);
                if (saved != null && saved.version == 1) patch.Restore(saved.rows, saved.sockets, saved.order);
            }
            catch (ArgumentException) { /* Invalid saves fall back to the default connections. */ }
            return patch;
        }
        public static void Save(VolumePatchState patch, ISaveStorage storage)
        {
            var saved = new SavedPatch { rows = new int[3], sockets = new int[3], order = new int[3] };
            for (int i = 0; i < 3; i++)
            {
                saved.rows[i] = patch.RowOf(i);
                saved.sockets[i] = patch.SocketOf(i);
                saved.order[i] = patch.CableInLayer(i);
            }
            storage.Write(SaveKey, JsonUtility.ToJson(saved));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => Apply();
        public static bool Connect(int row, int socket)
        {
            if (!Patch.Connect(row, socket)) return false;
            Save(Patch, SaveStorage.Create());
            Apply();
            return true;
        }
        // Putting a jack back where it was still lifts its wire to the top, so the order is saved too.
        public static void Cancel()
        {
            if (Patch.HeldCable < 0) return;
            Patch.Cancel();
            Save(Patch, SaveStorage.Create());
        }
        private static void Apply()
        {
            AudioListener.volume = GetVolume(AudioVolumeChannel.Master);
            Changed?.Invoke();
        }
    }
}
