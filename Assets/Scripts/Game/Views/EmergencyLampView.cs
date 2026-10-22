using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// A small red warning lamp in the board's top frame, one column step right of the last column number, a spot
    /// that is free at every board size and clear of the call in hand's speech bubble (D60, V3). It flashes while any emergency waits for its connection and is dark otherwise. It lies on the
    /// board's canvas, so the open book covers it; it covers no jack, label or card and takes no clicks. Drawn from
    /// UI circles until a drawn lamp exists. Its flashing runs on game time, so it freezes while the game is paused.
    /// A small "EMERGENCY" sign on the frame just under the lamp explains it (11c playtest): dark red on a dark plate
    /// while nothing waits, bright red and glowing, flashing with the lamp, while an emergency waits.
    /// </summary>
    public sealed class EmergencyLampView : MonoBehaviour
    {
        public const float FlashesPerSecond = 2f;
        public const float Diameter = 44f;
        private static readonly Color BulbDark = new Color32(92, 24, 20, 255);
        private static readonly Color BulbLit = new Color32(255, 70, 48, 255);
        private static readonly Color GlowTint = new Color32(255, 60, 40, 255);
        private static readonly Color Rim = new Color32(58, 40, 26, 255);
        public const string SignText = "EMERGENCY";
        public static readonly Vector2 SignSize = new Vector2(92, 20);
        private const float SignGap = 3f;
        private const float SignFontSize = 13f;
        private static readonly Color SignPlate = new Color32(30, 20, 16, 255);
        private static readonly Color SignUnlit = new Color32(132, 40, 32, 255);
        private static readonly Color SignLitText = new Color32(255, 96, 70, 255);
        // Lit, the sign never drops below this, so it stays readable at the lamp's dark moments.
        private const float SignLitFloor = .35f;
        private CallService calls;
        private Transform lastColumn;
        private Transform previousColumn;
        private RectTransform rect;
        private Graphic lit;
        private Graphic glow;
        private Graphic signLit;
        private Graphic signGlow;
        private float clock;
        private float shownBrightness = -1;
        private float shownSign = -1;

        /// <summary>An emergency is waiting, so the lamp flashes.</summary>
        public bool IsWarning { get; private set; }
        /// <summary>How bright the lamp is now, 0 to 1.</summary>
        public float Brightness { get; private set; }
        /// <summary>How brightly the sign's red letters are lit, 0 (dark, nothing waits) to 1.</summary>
        public float SignBrightness { get; private set; }
        /// <summary>The sign's plate, under the lamp.</summary>
        public RectTransform Sign { get; private set; }

        /// <summary>
        /// Builds the lamp beside the board's column numbers; null when the board has fewer than two.
        /// <paramref name="boardLamps"/> must be the board's lamps only (not the volume patch's). The column lamps of
        /// closed columns are hidden but keep their place, so the spot doesn't move as the board grows.
        /// </summary>
        public static EmergencyLampView Create(IReadOnlyList<SwitchboardLampView> boardLamps, CallService callService)
        {
            float top = float.NegativeInfinity;
            foreach (SwitchboardLampView lamp in boardLamps)
                if (lamp != null) top = Mathf.Max(top, lamp.transform.position.y);
            Transform last = null, previous = null;
            foreach (SwitchboardLampView lamp in boardLamps)
            {
                if (lamp == null || Mathf.Abs(lamp.transform.position.y - top) > 2f) continue;
                Transform column = lamp.transform;
                if (last == null || column.position.x > last.position.x)
                {
                    previous = last;
                    last = column;
                }
                else if (previous == null || column.position.x > previous.position.x) previous = column;
            }
            if (last == null || previous == null) return null;
            Canvas canvas = last.GetComponentInParent<Canvas>(true);
            if (canvas == null) return null;
            var root = new GameObject("Emergency lamp", typeof(RectTransform), typeof(LayoutElement));
            root.GetComponent<LayoutElement>().ignoreLayout = true;
            var view = root.AddComponent<EmergencyLampView>();
            view.rect = (RectTransform)root.transform;
            view.rect.SetParent(canvas.rootCanvas.transform, false);
            view.rect.SetAsLastSibling();
            view.rect.anchorMin = view.rect.anchorMax = new Vector2(.5f, .5f);
            view.rect.sizeDelta = Vector2.one * Diameter;
            view.glow = Part<LampGlowGraphic>(view.rect, "Glow", 2.6f, GlowTint);
            Part<CircleGraphic>(view.rect, "Rim", 1f, Rim);
            Part<CircleGraphic>(view.rect, "Bulb", .74f, BulbDark);
            view.lit = Part<CircleGraphic>(view.rect, "Lit bulb", .74f, BulbLit);
            view.BuildSign();
            view.lastColumn = last;
            view.previousColumn = previous;
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

        // Its own plate under the lamp, on the frame between the lamp and the jacks' panel.
        private void BuildSign()
        {
            var sign = new GameObject("Emergency sign", typeof(RectTransform)).GetComponent<RectTransform>();
            sign.SetParent(rect, false);
            sign.anchorMin = sign.anchorMax = new Vector2(.5f, .5f);
            sign.sizeDelta = SignSize;
            sign.anchoredPosition = new Vector2(0, -(Diameter * .5f + SignGap + SignSize.y * .5f));
            Sign = sign;
            float glowSize = SignSize.y * 3.6f;
            signGlow = Part<LampGlowGraphic>(sign, "Sign glow", 0, GlowTint);
            signGlow.rectTransform.sizeDelta = Vector2.one * glowSize;
            signGlow.rectTransform.localScale = new Vector3(SignSize.x * 1.6f / glowSize, 1, 1);
            Part<Image>(sign, "Sign rim", 0, Rim).rectTransform.sizeDelta = SignSize + Vector2.one * 4;
            Part<Image>(sign, "Sign plate", 0, SignPlate).rectTransform.sizeDelta = SignSize;
            SignLabel(sign, "Sign text", SignUnlit);
            signLit = SignLabel(sign, "Sign lit text", SignLitText);
        }

        private static TMP_Text SignLabel(RectTransform parent, string name, Color tint)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var labelRect = label.rectTransform;
            labelRect.SetParent(parent, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            // LiberationSans SDF, the static default atlas: the sign is plain ASCII.
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.text = SignText;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8;
            label.fontSizeMax = label.fontSize = SignFontSize;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 6;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.margin = Vector4.zero;
            label.color = tint;
            label.raycastTarget = false;
            return label;
        }

        private static T Part<T>(RectTransform parent, string name, float scale, Color tint) where T : Graphic
        {
            var part = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<T>();
            var rect = part.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = Vector2.one * Diameter * scale;
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
                if (lit != null) lit.canvasRenderer.SetAlpha(Brightness);
                if (glow != null) glow.canvasRenderer.SetAlpha(Brightness * .8f);
            }
            SignBrightness = IsWarning ? SignLitFloor + (1 - SignLitFloor) * Brightness : 0f;
            if (SignBrightness == shownSign) return;
            shownSign = SignBrightness;
            if (signLit != null) signLit.canvasRenderer.SetAlpha(SignBrightness);
            if (signGlow != null) signGlow.canvasRenderer.SetAlpha(SignBrightness * .9f);
        }

        private void LateUpdate()
        {
            Follow();
            Tick(Time.deltaTime);
        }

        // The board moves through resolution changes, so the lamp follows the last two column lamps.
        private void Follow()
        {
            if (lastColumn == null || previousColumn == null || rect.parent == null) return;
            Vector3 spot = lastColumn.position + (lastColumn.position - previousColumn.position);
            Vector3 local = rect.parent.InverseTransformPoint(spot);
            if ((Vector2)rect.localPosition != (Vector2)local) rect.localPosition = local;
        }
    }
}
