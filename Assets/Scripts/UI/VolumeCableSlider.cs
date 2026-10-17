using UnityEngine;

namespace Switchboard.UI
{
    [DisallowMultipleComponent]
    public sealed class VolumeCableSlider : MonoBehaviour
    {
        [SerializeField] private AudioVolumeChannel channel;
        [SerializeField] private SocketGraphic fixedSocket;
        [SerializeField] private SocketGraphic[] sockets;
        [SerializeField] private CableGraphic wire;
        [SerializeField] private PlugGraphic fixedPlug;
        [SerializeField] private RectTransform wallAnchor;
        [SerializeField] private PlugGraphic movingPlug;
        [SerializeField] private HandCursorGraphic hand;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip insertSound;
        [SerializeField] private AudioClip removeSound;
        public AudioVolumeChannel Channel => channel;
        public SocketGraphic FixedSocket => fixedSocket;
        public SocketGraphic[] Sockets => sockets;
        public PlugGraphic FixedPlug => fixedPlug;
        public RectTransform WallAnchor => wallAnchor;
        public PlugGraphic MovingPlug => movingPlug;
        public CableGraphic Wire => wire;
        public HandCursorGraphic Hand => hand;
        public bool IsHeld => AudioVolumeSettings.Patch.HeldCable == (int)channel;
        public int SelectedIndex => AudioVolumeSettings.Patch.SocketOf((int)channel);

        public void Configure(SocketGraphic anchor, SocketGraphic[] choices, CableGraphic cable,
            PlugGraphic first, PlugGraphic second, HandCursorGraphic cursor, AudioSource audio,
            AudioClip insert, AudioClip remove)
        {
            fixedSocket = anchor; sockets = choices; wire = cable;
            fixedPlug = first; movingPlug = second; hand = cursor;
            audioSource = audio; insertSound = insert; removeSound = remove;
            Render(choices[1].transform.position);
        }
        public void SetChannel(AudioVolumeChannel value, Color color)
        {
            channel = value;
            wire.InsulationColor = color;
            if (fixedPlug != null) fixedPlug.SleeveColor = color;
            movingPlug.SleeveColor = color;
        }
        public void UseWallAnchor(RectTransform anchor)
        {
            wallAnchor = anchor;
            fixedSocket = null;
            fixedPlug = null;
        }
        public void Render(Vector3 endpoint)
        {
            if (wallAnchor == null) fixedPlug.transform.position = fixedSocket.transform.position;
            movingPlug.transform.position = endpoint;
            movingPlug.raycastTarget = !IsHeld;
            var firstTail = wallAnchor != null ? wallAnchor.position : fixedPlug.transform.TransformPoint(fixedPlug.TailOffset);
            var secondTail = movingPlug.transform.TransformPoint(movingPlug.TailOffset);
            wire.SetEndpoints(wire.rectTransform.InverseTransformPoint(firstTail), wire.rectTransform.InverseTransformPoint(secondTail));
        }
        public void PlayFeedback(bool inserted)
        {
            var clip = inserted ? insertSound : removeSound;
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
