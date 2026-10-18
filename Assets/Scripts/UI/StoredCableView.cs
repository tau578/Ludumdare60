using UnityEngine;
using UnityEngine.UI;
using Switchboard.Game;

namespace Switchboard.UI
{
    /// <summary>Tray preview uses the same wire and plug graphics as the connected cable.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public sealed class StoredCableView : MonoBehaviour
    {
        [SerializeField] private RectTransform preview;
        [SerializeField] private CableGraphic wire;
        [SerializeField] private PlugGraphic firstPlug;
        [SerializeField] private PlugGraphic secondPlug;
        [SerializeField, HideInInspector] private Image artwork;
        public Graphic Artwork => artwork;

        public void SetArtwork(Sprite sprite, Color tint)
        {
            if (sprite == null) return;
            if (artwork == null)
            {
                artwork = new GameObject("Coiled cable", typeof(RectTransform)).AddComponent<Image>();
                artwork.transform.SetParent(transform, false);
                artwork.rectTransform.anchorMin = Vector2.zero;
                artwork.rectTransform.anchorMax = Vector2.one;
                artwork.rectTransform.offsetMin = artwork.rectTransform.offsetMax = Vector2.zero;
                artwork.preserveAspect = true;
                artwork.raycastTarget = false;
            }
            artwork.sprite = sprite; artwork.color = tint;
            artwork.enabled = Available;
            if (preview != null) preview.gameObject.SetActive(false);
        }
        private Image hitArea;
        private Outline[] hoverOutlines;
        private SlowGuidePulse hoverGlow;
        private bool highlighted;
        public CableController Cable { get; private set; }
        public bool Available => HitArea.enabled && (Cable == null || Cable.IsInTray);
        public bool IsHighlighted => highlighted;
        private Image HitArea => hitArea != null ? hitArea : hitArea = GetComponent<Image>();

        public void Configure(RectTransform visual, CableGraphic line, PlugGraphic first, PlugGraphic second)
        {
            preview = visual;
            wire = line;
            firstPlug = first;
            secondPlug = second;
            UpdateWire();
        }

        public void Bind(CableController cable) => Cable = cable;
        private void OnEnable() => UpdateWire();

        private void UpdateWire()
        {
            if (wire != null && firstPlug != null && secondPlug != null)
                wire.SetEndpoints(firstPlug.rectTransform.anchoredPosition + firstPlug.TailOffset,
                    secondPlug.rectTransform.anchoredPosition + secondPlug.TailOffset);
        }

        public void SetCableColor(Color value)
        {
            if (wire != null) wire.InsulationColor = value;
            if (firstPlug != null) firstPlug.SleeveColor = value;
            if (secondPlug != null) secondPlug.SleeveColor = value;
        }

        public void SetAvailable(bool available)
        {
            if (!available) SetHighlighted(false);
            HitArea.enabled = available;
            if (preview != null) preview.gameObject.SetActive(available && artwork == null);
            if (artwork != null) artwork.enabled = available;
        }

        public void SetHighlighted(bool value)
        {
            value = value && Available && isActiveAndEnabled;
            if (highlighted == value) return;
            highlighted = value;
            if (artwork != null)
            {
                if (value && hoverGlow == null)
                {
                    hoverGlow = artwork.GetComponent<SlowGuidePulse>();
                    if (hoverGlow == null) hoverGlow = artwork.gameObject.AddComponent<SlowGuidePulse>();
                    hoverGlow.UseSilhouetteHalo();
                }
                if (hoverGlow != null) hoverGlow.SetHovered(value);
                if (hoverOutlines != null)
                    foreach (var outline in hoverOutlines) if (outline != null) outline.enabled = false;
                return;
            }
            if (value && hoverOutlines == null)
            {
                hoverOutlines = new[] { CreateOutline(wire), CreateOutline(firstPlug), CreateOutline(secondPlug) };
            }
            if (hoverOutlines != null)
                foreach (var outline in hoverOutlines) if (outline != null) outline.enabled = value;
        }

        private static Outline CreateOutline(Graphic graphic)
        {
            if (graphic == null) return null;
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = SocketHaloGraphic.HoverColor;
            outline.effectDistance = new Vector2(4, -4);
            outline.useGraphicAlpha = true;
            return outline;
        }

        private void OnDisable() => SetHighlighted(false);
    }
}
