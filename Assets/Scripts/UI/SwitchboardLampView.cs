using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>A row or column indicator; sockets remain the source of connection state.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SwitchboardLampView : MonoBehaviour
    {
        [SerializeField] private Image bulb;
        [SerializeField] private Image label;
        [SerializeField] private Sprite bulbOff;
        [SerializeField] private Sprite bulbOn;
        [SerializeField] private Sprite labelOff;
        [SerializeField] private Sprite labelOn;
        [SerializeField] private SocketView[] sockets;
        [SerializeField, Tooltip("Show the lit state in Edit Mode to preview the artwork.")]
        private bool previewLit;

        public bool IsLit { get; private set; }

        public void Configure(Image bulbImage, Image labelImage, Sprite off, Sprite on,
            Sprite textOff, Sprite textOn, SocketView[] observedSockets)
        {
            bulb = bulbImage;
            label = labelImage;
            bulbOff = off;
            bulbOn = on;
            labelOff = textOff;
            labelOn = textOn;
            sockets = observedSockets;
            Refresh();
        }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void OnValidate() => Refresh();

        public void Refresh()
        {
            bool lit = !Application.isPlaying && previewLit;
            if (sockets != null)
                foreach (SocketView socket in sockets)
                    if (socket != null && (socket.IsOccupied || socket.GetComponent<SocketGraphic>().IsHighlighted))
                    {
                        lit = true;
                        break;
                    }
            SetLit(lit);
        }

        private void OnDisable() => SetLit(false);

        private void SetLit(bool lit)
        {
            IsLit = lit;
            if (bulb != null) bulb.sprite = lit ? bulbOn : bulbOff;
            if (label != null) label.sprite = lit ? labelOn : labelOff;
        }
    }
}
