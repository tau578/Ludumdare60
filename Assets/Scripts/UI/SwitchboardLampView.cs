using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Guides a held plug to the hovered socket's row and column.</summary>
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
                    if (socket != null && socket.IsNavigationHighlighted)
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
