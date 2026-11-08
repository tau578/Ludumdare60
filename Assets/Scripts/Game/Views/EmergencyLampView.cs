using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// The red warning lamp in the board's top frame, right of the column numbers, a spot that is free at every board
    /// size and clear of the call in hand's speech bubble (D60, V3). It flashes while any emergency waits for its
    /// connection and is dark otherwise. It is a sibling of the board's number lamps: their own frosted bulb art,
    /// tinted red, lit from inside with a soft glow on the frame. Under it, a matte brass plate set into the frame, like
    /// the name plates beside a real switchboard's lamps, reads "EMERGENCY" in dark engraved letters; while the lamp
    /// flashes, the letters stay dark and red light falls on and round the plate. The plate and its lettering are
    /// measured in bulb sizes, so they follow the bulb. It lies on the board's canvas, so the open book covers it; it covers no jack,
    /// number lamp or card and takes no clicks. Its flashing runs on game time, so it freezes while the game is paused,
    /// and it only fades prebuilt layers (alpha), so a flash rebuilds no mesh.
    /// </summary>
    public sealed class EmergencyLampView : MonoBehaviour
    {
        /// <summary>The EMERGENCY plate's measures, in bulb sizes.</summary>
        [System.Serializable]
        public sealed class SignProportions
        {
            [SerializeField, Tooltip("The plate's width, in bulb sizes.")]
            private float width = 2.6f;
            [SerializeField, Tooltip("The plate's height, in bulb sizes.")]
            private float height = .56f;
            [SerializeField, Tooltip("The capitals' height, in bulb sizes.")]
            private float capHeight = .25f;
            [SerializeField, Tooltip("The space between the bulb and the plate, in bulb sizes.")]
            private float gap = .16f;

            public SignProportions() { }

            public SignProportions(float width, float height, float capHeight, float gap)
            {
                this.width = width;
                this.height = height;
                this.capHeight = capHeight;
                this.gap = gap;
            }

            public float Width => width;
            public float Height => height;
            public float CapHeight => capHeight;
            public float Gap => gap;
        }

        public const float FlashesPerSecond = 2f;
        /// <summary>The bulb's default size in the board's units (the number lamps' bulbs are 96); the game passes its
        /// own serialized value.</summary>
        public const float DefaultBulbSize = 44f;
        /// <summary>The glow on the frame, in bulb sizes; it fades to nothing at its edge.</summary>
        public const float GlowScale = 1.45f;
        // Where the bulb sits, in board units from the last column number's centre: level with the numbers, on the
        // free frame between the number's bulb and the frame's gold curl.
        private static readonly Vector2 BulbOffset = new Vector2(99.5f, 0);
        // Unlit: dull, frosted deep red. Lit: a warm red lit from inside, hottest in the middle (the inner glow).
        private static readonly Color UnlitTint = new Color32(160, 62, 50, 255);
        private static readonly Color LitTint = new Color32(232, 74, 52, 255);
        private static readonly Color InnerGlowTint = new Color32(255, 150, 105, 135);
        private const float InnerGlowScale = .62f;
        private static readonly Color GlowTint = new Color32(255, 84, 52, 175);
        public const string SignText = "EMERGENCY";
        // Engraved: a dark brown-red cut into the brass, and a touch redder while the alarm's light is on the plate.
        // Both stay dark, so they read at 4.5:1 on the darkest brass under the full red wash (5.0:1 quiet, 4.6:1 lit).
        public static readonly Color32 SignQuiet = new Color32(58, 20, 12, 255);
        public static readonly Color32 SignLit = new Color32(72, 11, 13, 255);
        /// <summary>The light caught by each letter's cut lower edge: an offset copy of the lettering.</summary>
        public static readonly Color32 SignEngraveEdge = new Color32(255, 240, 200, 89);
        /// <summary>How far the light edge sits below the letters, in bulb sizes (0.6 units at 44).</summary>
        public const float SignEngraveDepth = .6f / 44f;
        /// <summary>The red light on the plate's face at full flash (its alpha is the strength).</summary>
        public static readonly Color32 SignWashTint = new Color32(232, 84, 58, 36);
        private static readonly Color32 SignHaloTint = new Color32(255, 84, 52, 150);
        /// <summary>Lit, the lit lettering never fades below this over the quiet one, so it stays lit at the flash's dim end.</summary>
        public const float SignLitFloor = .35f;
        /// <summary>The space between the rim and the lettering's box, in plate heights.</summary>
        public const float SignPadding = .12f;
        /// <summary>How much the engraved strokes are thickened (the SDF face dilation): at about 10 px tall Della
        /// Respira's thin strokes never reach their own colour, so the letters would read lighter than they are.</summary>
        public const float SignLetterWeight = .3f;
        private CallService calls;
        private Transform lastColumn;
        private RectTransform rect;
        private float bulbSize;
        private float clock;
        private float shownBrightness = -1;
        private float shownSign = -1;

        /// <summary>An emergency is waiting, so the lamp flashes.</summary>
        public bool IsWarning { get; private set; }
        /// <summary>How bright the lamp is now, 0 to 1.</summary>
        public float Brightness { get; private set; }
        /// <summary>How brightly the sign's red letters are lit, 0 (dark, nothing waits) to 1.</summary>
        public float SignBrightness { get; private set; }
        /// <summary>The plate's box, under the lamp.</summary>
        public RectTransform Sign { get; private set; }
        /// <summary>The brass plate, recessed into the frame, with its darker rim.</summary>
        public LabelStripGraphic SignPlate { get; private set; }
        /// <summary>The red light on the plate's face, faded in by <see cref="Brightness"/>.</summary>
        public LabelStripGraphic SignWash { get; private set; }
        /// <summary>The soft red light round the plate, faded in by <see cref="Brightness"/>.</summary>
        public LabelStripGraphic SignGlow { get; private set; }
        /// <summary>The frosted bulb while dark (the board's unlit bulb art, tinted red); always shown.</summary>
        public Image Bulb { get; private set; }
        /// <summary>The lit bulb (the board's lit bulb art), faded in over the dark one by <see cref="Brightness"/>.</summary>
        public Image LitBulb { get; private set; }
        /// <summary>The light inside the lit bulb, brightest in its middle.</summary>
        public Graphic InnerGlow { get; private set; }
        /// <summary>The soft glow on the frame around the lamp.</summary>
        public Graphic Glow { get; private set; }
        /// <summary>The light lower edge of the engraved letters, a little below them; always shown.</summary>
        public TMP_Text SignEdgeLabel { get; private set; }
        /// <summary>The engraved lettering, always shown.</summary>
        public TMP_Text SignLabel { get; private set; }
        /// <summary>The lit lettering, faded in over the quiet one by <see cref="SignBrightness"/>.</summary>
        public TMP_Text SignLitLabel { get; private set; }
        /// <summary>The bulb's size in the board's units.</summary>
        public float BulbSize => bulbSize;
        /// <summary>The lettering's own copy of the font's material, thickened by <see cref="SignLetterWeight"/>; made
        /// once, so the shared font material never changes.</summary>
        public Material SignMaterial { get; private set; }
        /// <summary>The plate's measures in bulb sizes.</summary>
        public SignProportions Proportions { get; private set; }

        /// <summary>
        /// Builds the lamp beside the last column number; null when the board has no column numbers.
        /// <paramref name="boardLamps"/> must be the board's lamps only (not the volume patch's). The column lamps of
        /// closed columns are hidden but keep their place, so the spot doesn't move as the board grows.
        /// </summary>
        public static EmergencyLampView Create(IReadOnlyList<SwitchboardLampView> boardLamps, CallService callService,
            TMP_FontAsset signFont, float bulbSize = DefaultBulbSize, SignProportions sign = null)
        {
            float top = float.NegativeInfinity;
            foreach (SwitchboardLampView lamp in boardLamps)
                if (lamp != null) top = Mathf.Max(top, lamp.transform.position.y);
            SwitchboardLampView last = null;
            foreach (SwitchboardLampView lamp in boardLamps)
            {
                if (lamp == null || Mathf.Abs(lamp.transform.position.y - top) > 2f) continue;
                if (last == null || lamp.transform.position.x > last.transform.position.x) last = lamp;
            }
            if (last == null) return null;
            Canvas canvas = last.GetComponentInParent<Canvas>(true);
            if (canvas == null) return null;
            var root = new GameObject("Emergency lamp", typeof(RectTransform), typeof(LayoutElement));
            root.GetComponent<LayoutElement>().ignoreLayout = true;
            var view = root.AddComponent<EmergencyLampView>();
            view.bulbSize = bulbSize > 0 ? bulbSize : DefaultBulbSize;
            view.Proportions = sign ?? new SignProportions();
            view.rect = (RectTransform)root.transform;
            view.rect.SetParent(canvas.rootCanvas.transform, false);
            view.rect.SetAsLastSibling();
            view.rect.anchorMin = view.rect.anchorMax = new Vector2(.5f, .5f);
            view.rect.sizeDelta = Vector2.one * view.bulbSize;
            // Bottom to top: the lamp's light on the frame, the dark bulb, the lit bulb, its inner light, then the
            // plate, drawn last so the lamp's light never tints it and its lettering is read against the plate alone.
            view.Glow = view.Part<LampGlowGraphic>(view.rect, "Glow", GlowScale, GlowTint);
            view.Bulb = view.Part<Image>(view.rect, "Bulb", 1, UnlitTint);
            view.Bulb.sprite = last.BulbOff;
            view.LitBulb = view.Part<Image>(view.rect, "Lit bulb", 1, LitTint);
            view.LitBulb.sprite = last.BulbOn;
            view.InnerGlow = view.Part<LampGlowGraphic>(view.rect, "Inner glow", InnerGlowScale, InnerGlowTint);
            view.BuildSign(signFont);
            view.lastColumn = last.transform;
            view.Follow();
            view.Bind(callService);
            return view;
        }

        public void Bind(CallService callService)
        {
            calls = callService;
            clock = 0;
            shownBrightness = shownSign = -1;
            Tick(0);
        }

        /// <summary>The plate's size in the board's units.</summary>
        public Vector2 SignSize => new Vector2(Proportions.Width, Proportions.Height) * bulbSize;

        /// <summary>The lettering's font size that gives capitals <see cref="SignProportions.CapHeight"/> bulbs tall.</summary>
        public static float FontSizeFor(TMP_FontAsset font, float capHeight)
        {
            FaceInfo face = font.faceInfo;
            float capLine = face.capLine * face.scale;
            return capLine > 0 ? capHeight * face.pointSize / capLine : capHeight * 1.4f;
        }

        // A brass plate centred under the bulb, a gap below it, with the lettering engraved in its face.
        private void BuildSign(TMP_FontAsset font)
        {
            Vector2 size = SignSize;
            var sign = new GameObject("Emergency sign", typeof(RectTransform)).GetComponent<RectTransform>();
            sign.SetParent(rect, false);
            sign.anchorMin = sign.anchorMax = new Vector2(.5f, .5f);
            sign.sizeDelta = size;
            sign.anchoredPosition = new Vector2(0, -(bulbSize * .5f + Proportions.Gap * bulbSize + size.y * .5f));
            Sign = sign;
            SignGlow = Strip(sign, "Sign glow", LabelStripGraphic.Layer.Halo, SignHaloTint);
            SignPlate = Strip(sign, "Sign plate", LabelStripGraphic.Layer.Plate, Color.white);
            SignPlate.Mounting = LabelStripGraphic.Mount.Recessed;
            SignWash = Strip(sign, "Sign wash", LabelStripGraphic.Layer.Wash, SignWashTint);
            if (font == null) font = TMP_Settings.defaultFontAsset;
            float fontSize = font != null ? FontSizeFor(font, Proportions.CapHeight * bulbSize) : 12;
            // Only the width limits the lettering; the box is tall enough for the font's whole line.
            var lettering = new Vector2(SignPlate.Face.width - size.y * SignPadding * 2, size.y * 2);
            SignEdgeLabel = Lettering(sign, "Sign engraved edge", SignEngraveEdge, font, fontSize, lettering);
            SignEdgeLabel.rectTransform.anchoredPosition = new Vector2(0, -SignEngraveDepth * bulbSize);
            SignLabel = Lettering(sign, "Sign text", SignQuiet, font, fontSize, lettering);
            SignLitLabel = Lettering(sign, "Sign lit text", SignLit, font, fontSize, lettering);
            if (font == null || font.material == null) return;
            SignMaterial = new Material(font.material) { name = "Emergency sign lettering" };
            SignMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, SignLetterWeight);
            SignEdgeLabel.fontSharedMaterial = SignLabel.fontSharedMaterial = SignLitLabel.fontSharedMaterial = SignMaterial;
        }

        private static LabelStripGraphic Strip(RectTransform parent, string name, LabelStripGraphic.Layer layer, Color tint)
        {
            var strip = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<LabelStripGraphic>();
            RectTransform stripRect = strip.rectTransform;
            stripRect.SetParent(parent, false);
            stripRect.anchorMin = Vector2.zero;
            stripRect.anchorMax = Vector2.one;
            stripRect.sizeDelta = Vector2.zero;
            strip.Part = layer;
            strip.color = tint;
            strip.raycastTarget = false;
            return strip;
        }

        private static TMP_Text Lettering(RectTransform parent, string name, Color tint, TMP_FontAsset font, float size,
            Vector2 box)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var labelRect = label.rectTransform;
            labelRect.SetParent(parent, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, .5f);
            labelRect.sizeDelta = box;
            if (font != null) label.font = font;
            label.text = SignText;
            // Only shrinks if the proportions ask for more letters than the plate holds.
            label.enableAutoSizing = true;
            label.fontSizeMin = 1;
            label.fontSizeMax = label.fontSize = size;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            // Centred on the capitals themselves, not on the font's line, so they sit in the middle of the plate.
            label.verticalAlignment = VerticalAlignmentOptions.Geometry;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.margin = Vector4.zero;
            label.color = tint;
            label.raycastTarget = false;
            return label;
        }

        private T Part<T>(RectTransform parent, string name, float scale, Color tint) where T : Graphic
        {
            var part = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<T>();
            var partRect = part.rectTransform;
            partRect.SetParent(parent, false);
            partRect.anchorMin = partRect.anchorMax = new Vector2(.5f, .5f);
            partRect.sizeDelta = Vector2.one * bulbSize * scale;
            part.color = tint;
            part.raycastTarget = false;
            return part;
        }

        /// <summary>Any emergency still waiting for its connection, on a card or not.</summary>
        public static bool AnyEmergencyWaiting(IReadOnlyList<Call> live)
        {
            if (live == null) return false;
            for (int i = 0; i < live.Count; i++)
            {
                Call call = live[i];
                if (call.HasResponseTime && (call.State == CallState.Incoming || call.State == CallState.Taken ||
                    call.State == CallState.Connecting)) return true;
            }
            return false;
        }

        /// <summary>Moves the flashing on by <paramref name="gameSeconds"/> (0 while paused). LateUpdate drives it.</summary>
        public void Tick(float gameSeconds)
        {
            IsWarning = calls != null && AnyEmergencyWaiting(calls.Live);
            if (IsWarning) clock += gameSeconds;
            else clock = 0;
            // Starts lit, so a new emergency shows at once.
            Brightness = IsWarning ? .5f + .5f * Mathf.Cos(clock * FlashesPerSecond * Mathf.PI * 2f) : 0f;
            if (Brightness != shownBrightness)
            {
                shownBrightness = Brightness;
                if (LitBulb != null) LitBulb.canvasRenderer.SetAlpha(Brightness);
                if (InnerGlow != null) InnerGlow.canvasRenderer.SetAlpha(Brightness);
                if (Glow != null) Glow.canvasRenderer.SetAlpha(Brightness);
            }
            SignBrightness = IsWarning ? SignLitFloor + (1 - SignLitFloor) * Brightness : 0f;
            if (SignBrightness == shownSign) return;
            shownSign = SignBrightness;
            if (SignLitLabel != null) SignLitLabel.canvasRenderer.SetAlpha(SignBrightness);
            if (SignGlow != null) SignGlow.canvasRenderer.SetAlpha(Brightness);
            if (SignWash != null) SignWash.canvasRenderer.SetAlpha(Brightness);
        }

        private void OnDestroy()
        {
            if (SignMaterial == null) return;
            if (Application.isPlaying) Destroy(SignMaterial);
            else DestroyImmediate(SignMaterial);
        }

        private void LateUpdate()
        {
            Follow();
            Tick(Time.deltaTime);
        }

        // The board moves and scales with the play area, so the lamp follows the last column number, in the
        // board's own scale.
        private void Follow()
        {
            if (lastColumn == null || rect.parent == null) return;
            float parentScale = rect.parent.lossyScale.x;
            float scale = parentScale != 0 ? lastColumn.lossyScale.x / parentScale : 1;
            if (rect.localScale.x != scale) rect.localScale = new Vector3(scale, scale, 1);
            Vector3 local = rect.parent.InverseTransformPoint(lastColumn.position) + (Vector3)(BulbOffset * scale);
            if ((Vector2)rect.localPosition != (Vector2)local) rect.localPosition = local;
        }
    }
}
