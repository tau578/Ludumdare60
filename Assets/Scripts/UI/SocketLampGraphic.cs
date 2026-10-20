using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>How a socket's lamp shows (D58). Explicit values: views and tests compare them.</summary>
    public enum SocketLamp
    {
        Off = 0,
        /// <summary>A waiting caller (fix 3): dark, but hovering its card flares it for a moment.</summary>
        Waiting = 1,
        /// <summary>Only the jack marked from the book: a steady glow (fix 1: blinking always means a caller).</summary>
        Steady = 2,
        /// <summary>The call in hand's caller: the only blinking jack (fix 3).</summary>
        Blinking = 3
    }

    /// <summary>
    /// A warm lamp glow in and around a jack, like the lamps of a real switchboard (D58). Only the call in hand's caller
    /// blinks, at about half brightness, with a bigger halo; waiting callers stay dark (fix 3), but hovering a waiting
    /// caller's card flares its lamp once, as a preview. Steady means the jack marked from the book. It lies on the
    /// board, under every plug and wire, and takes no clicks. The blink only changes the renderer's alpha; the mesh
    /// changes only between the normal and the bigger halo.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SocketLampGraphic : MaskableGraphic
    {
        // Tuning (lead, 2026-10-20): every lamp constant lives here.
        public static readonly Color LampColor = new Color32(255, 208, 110, 255);
        /// <summary>The call in hand: a clear blink that peaks at about half brightness (fix 3).</summary>
        public const float BlinkSeconds = .5f;
        public const float BlinkLow = .06f;
        public const float BlinkHigh = .55f;
        /// <summary>The book mark.</summary>
        public const float SteadyAlpha = 1f;
        /// <summary>Hovering a waiting caller's card: one pulse as bright as the blink's peak, then dark again.</summary>
        public const float FlarePeak = BlinkHigh;
        public const float FlareHoldSeconds = .3f;
        public const float FlareFadeSeconds = .5f;
        private const int Segments = 48;
        // Fractions of the socket's size: a bright lamp in the hole, the rim left readable, then a soft glow on the
        // board around it (to 0.64; jacks sit about 1.0-1.2 sockets apart).
        private static readonly float[] Radii = { 0f, .12f, .22f, .3f, .38f, .45f, .52f, .64f };
        private static readonly float[] Opacity = { 1f, .95f, .75f, .2f, .12f, .55f, .3f, 0f };
        // The call in hand's bigger halo: the same lamp, with the glow reaching most of the way to the next jack.
        private static readonly float[] BrightRadii = { 0f, .12f, .22f, .3f, .38f, .45f, .55f, .7f, .92f };
        private static readonly float[] BrightOpacity = { 1f, .95f, .75f, .2f, .12f, .7f, .5f, .22f, 0f };
        private SocketLamp mode = SocketLamp.Waiting;
        private float flareStart = float.NegativeInfinity;

        public SocketLamp Mode
        {
            get => mode;
            set
            {
                if (HasBigHalo(mode) != HasBigHalo(value)) SetVerticesDirty();
                mode = value;
                ApplyAlpha();
            }
        }

        /// <summary>The call in hand's lamp has the bigger halo.</summary>
        public bool ShowsBigHalo => HasBigHalo(mode);

        /// <summary>The outer edge of the glow, as a fraction of the socket's size.</summary>
        public float GlowRadius => HasBigHalo(mode) ? BrightRadii[BrightRadii.Length - 1] : Radii[Radii.Length - 1];

        /// <summary>The call in hand's caller's brightness at a time.</summary>
        public static float BlinkAlpha(float time) => Mathf.Lerp(BlinkLow, BlinkHigh,
            .5f - .5f * Mathf.Cos(time * Mathf.PI * 2f / BlinkSeconds));

        public static float AlphaOf(SocketLamp lamp, float time) =>
            lamp == SocketLamp.Steady ? SteadyAlpha : lamp == SocketLamp.Blinking ? BlinkAlpha(time) : 0f;

        /// <summary>A flare started at <paramref name="start"/>: <see cref="FlarePeak"/> while it holds, then fading to 0.</summary>
        public static float FlareAlpha(float start, float time)
        {
            float age = time - start;
            if (age < 0 || age >= FlareHoldSeconds + FlareFadeSeconds) return 0f;
            return FlarePeak * (age <= FlareHoldSeconds ? 1f : 1f - (age - FlareHoldSeconds) / FlareFadeSeconds);
        }

        private static bool HasBigHalo(SocketLamp lamp) => lamp == SocketLamp.Blinking;

        public float CurrentAlpha => canvasRenderer.GetAlpha();
        /// <summary>A hover flare is still showing.</summary>
        public bool IsFlaring => FlareAlpha(flareStart, Time.unscaledTime) > 0;

        /// <summary>Lights the lamp for a moment (hovering a waiting caller's card, fix 2).</summary>
        public void Flare()
        {
            flareStart = Time.unscaledTime;
            ApplyAlpha();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyAlpha();
        }

        protected override void OnDisable()
        {
            flareStart = float.NegativeInfinity;
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (mode != SocketLamp.Steady) ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            float now = Time.unscaledTime;
            float alpha = AlphaOf(mode, now);
            if (mode != SocketLamp.Off) alpha = Mathf.Max(alpha, FlareAlpha(flareStart, now));
            canvasRenderer.SetAlpha(alpha);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float size = Mathf.Min(rect.width, rect.height);
            float[] radii = HasBigHalo(mode) ? BrightRadii : Radii;
            float[] opacity = HasBigHalo(mode) ? BrightOpacity : Opacity;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (int band = 0; band < radii.Length; band++)
                {
                    Color tint = color;
                    tint.a *= opacity[band];
                    mesh.AddVert(rect.center + direction * (size * radii[band]), tint, Vector2.zero);
                    if (i == 0 || band == 0) continue;
                    int current = i * radii.Length + band;
                    int previous = current - radii.Length;
                    mesh.AddTriangle(previous - 1, previous, current - 1);
                    mesh.AddTriangle(current - 1, previous, current);
                }
            }
        }
    }
}
