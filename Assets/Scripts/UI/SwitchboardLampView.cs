using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>
    /// Guides a held plug to the hovered socket's row and column. A lamp whose sockets are all closed (D42) is
    /// hidden, bulb and label alike (including a plain-text label), and never lights.
    /// </summary>
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
        [SerializeField] private SocketGraphic[] volumeSockets;
        [SerializeField, Tooltip("Show the lit state in Edit Mode to preview the artwork.")]
        private bool previewLit;
        private Graphic[] graphics;
        private Color[] baseColors;
        private bool dimmed;

        public bool IsLit { get; private set; }
        public bool IsDimmed => dimmed;

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
            bool open = sockets == null || sockets.Length == 0;
            if (sockets != null)
                foreach (SocketView socket in sockets)
                {
                    if (socket == null) continue;
                    if (socket.IsOpen) open = true;
                    if (socket.IsNavigationHighlighted) lit = true;
                }
            if (volumeSockets != null)
                foreach (SocketGraphic socket in volumeSockets)
                {
                    if (socket == null) continue;
                    open = true;
                    if (socket.IsHighlighted) lit = true;
                }
            SetLit(lit);
            SetDimmed(!open);
        }

        private void OnDisable()
        {
            SetLit(false);
            SetDimmed(false);
        }

        // The bulb, its glyph and any plain-text label are this object's graphics; their own colours are kept.
        private void SetDimmed(bool value)
        {
            if (dimmed == value) return;
            if (graphics == null)
            {
                graphics = GetComponentsInChildren<Graphic>(true);
                baseColors = new Color[graphics.Length];
                for (int i = 0; i < graphics.Length; i++) baseColors[i] = graphics[i].color;
            }
            dimmed = value;
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i] != null) graphics[i].color = dimmed ? Color.clear : baseColors[i];
        }

        private void SetLit(bool lit)
        {
            IsLit = lit;
            if (bulb != null) bulb.sprite = lit ? bulbOn : bulbOff;
            if (label != null) label.sprite = lit ? labelOn : labelOff;
        }
    }
}
