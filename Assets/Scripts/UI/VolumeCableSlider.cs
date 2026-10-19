using UnityEngine;

namespace Switchboard.UI
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
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
        [SerializeField] private CableArtwork artwork;
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

        private void OnEnable() => ApplyArtwork();

#if UNITY_EDITOR
        private void Update()
        {
            // Preview the default 50% connection without reading or changing saved volume settings.
            if (Application.IsPlaying(gameObject) || sockets == null || sockets.Length < 2 || sockets[1] == null ||
                movingPlug == null || wire == null || (wallAnchor == null && (fixedSocket == null || fixedPlug == null))) return;
            RenderConnection(sockets[1].transform.position, false);
        }
#endif

        public void SetArtwork(CableArtwork value) { artwork = value; ApplyArtwork(); }

        private void ApplyArtwork()
        {
            if (artwork == null || movingPlug == null) return;
            var art = artwork.ForColor(movingPlug.SleeveColor);
            movingPlug.SetArtwork(art.held, art.inserted, Color.white);
            if (fixedPlug != null) fixedPlug.SetArtwork(art.held, art.inserted, Color.white);
            if (wire != null)
            {
                wire.InsulationColor = art.color;
                wire.ThicknessScale = 1.5f;
            }
            // The inserted sprite extends 30 pixels above the socket centre, matching PlugGraphic.
            if (art.inserted != null)
            {
                movingPlug.rectTransform.sizeDelta = new Vector2(84 * art.inserted.rect.width / art.inserted.rect.height, 84);
                movingPlug.rectTransform.pivot = new Vector2(.5f, 54f / 84f);
            }
        }

        public void Configure(SocketGraphic anchor, SocketGraphic[] choices, CableGraphic cable,
            PlugGraphic first, PlugGraphic second, HandCursorGraphic cursor, AudioSource audio,
            AudioClip insert, AudioClip remove)
        {
            fixedSocket = anchor; sockets = choices; wire = cable;
            fixedPlug = first; movingPlug = second; hand = cursor;
            audioSource = audio; insertSound = insert; removeSound = remove;
            ApplyArtwork();
            Render(choices[1].transform.position);
        }
        public void SetChannel(AudioVolumeChannel value, Color color)
        {
            channel = value;
            wire.InsulationColor = color;
            if (fixedPlug != null) fixedPlug.SleeveColor = color;
            movingPlug.SleeveColor = color;
            ApplyArtwork();
        }
        public void UseWallAnchor(RectTransform anchor)
        {
            wallAnchor = anchor;
            fixedSocket = null;
            fixedPlug = null;
        }
        public void Render(Vector3 endpoint) => RenderConnection(endpoint, IsHeld);

        private void RenderConnection(Vector3 endpoint, bool held)
        {
            if (wallAnchor == null)
            {
                if (fixedPlug.transform.position != fixedSocket.transform.position)
                    fixedPlug.transform.position = fixedSocket.transform.position;
                fixedPlug.SetInserted(true);
            }
            if (movingPlug.transform.position != endpoint) movingPlug.transform.position = endpoint;
            movingPlug.SetInserted(!held);
            movingPlug.raycastTarget = !held;
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
