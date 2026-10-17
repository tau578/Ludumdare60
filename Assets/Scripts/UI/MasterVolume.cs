using UnityEngine;

namespace Switchboard.UI
{
    public static class MasterVolume
    {
        public static int SelectedIndex => AudioVolumeSettings.Patch.SocketOf(0);

        public static void Apply(int index)
        {
            if (index < 0 || index >= 5 || !AudioVolumeSettings.Patch.PickUp(0)) return;
            AudioVolumeSettings.Connect(AudioVolumeSettings.Patch.RowOf(0), index);
        }
    }
}
