using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// One lit place for the tutorial spotlight: a UI rect, grown by some padding, drawn as a rounded rect or a circle.
    /// A clickable one lets clicks through to the target (its own rect, not the padding); the rest only show the way.
    /// </summary>
    public readonly struct SpotlightTarget
    {
        public SpotlightTarget(RectTransform rect, float padding = 12f, bool round = false, bool clickable = true)
        {
            Rect = rect;
            Padding = padding;
            Round = round;
            Clickable = clickable;
        }

        public RectTransform Rect { get; }
        public float Padding { get; }
        public bool Round { get; }
        public bool Clickable { get; }
    }

    /// <summary>The guide's words, one per step (D57). Plain ASCII only: the static fonts hold nothing else.</summary>
    [Serializable]
    public sealed class TutorialTexts
    {
        [TextArea] public string clickCaller = "A call! Click the caller to answer.";
        [TextArea] public string readMessage = "Read who they want to talk to.";
        [TextArea] public string readMessageDone = "Read who they want to talk to. Click to continue.";
        [TextArea] public string openBook = "Open the phone book.";
        [TextArea] public string findJacks = "Find both jacks: the caller's and the one they want. Then click the book to close it.";
        [TextArea] public string takeCord = "Take a cord.";
        [TextArea] public string plugJacks = "Plug one end into each jack.";
        [TextArea] public string connected = "Connected! You earn money while they talk.";
        [TextArea] public string buyCord = "You can afford another cord. Buy it to handle two calls at once.";
        [TextArea] public string mystery = "Save up for the mystery upgrade!";
        [TextArea] public string buyProfit = "More Profit: every call pays more. Buy it to earn faster.";
        [TextArea] public string buyClients = "More Clients: more callers at once, and a bigger board.";
    }

    /// <summary>
    /// The tutorial spotlight (D57): a dark layer over the whole screen with soft holes on the current targets, a
    /// short hint and an arrow beside them, and a Skip button. Holes follow their targets every frame and slide to
    /// the next step's targets. The tutorial is mandatory (fix 1): the dark layer swallows every click outside the
    /// step's click areas; Skip, and the pause menu above this canvas, always work.
    /// </summary>
    public sealed class TutorialSpotlight : MonoBehaviour
    {
        private const int MaxHoles = SpotlightGraphic.MaxHoles;

        [SerializeField] private SpotlightGraphic dim;
        [SerializeField] private RectTransform hint;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private Button skipButton;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TutorialTexts texts = new TutorialTexts();
        [Header("Motion")]
        [SerializeField, Min(0), Tooltip("How fast holes and the hint slide to a new place.")] private float slideSpeed = 9f;
        [SerializeField, Min(.01f)] private float fadeSeconds = .3f;
        [SerializeField, Min(0), Tooltip("How far the holes breathe in and out.")] private float pulse = 5f;
        [SerializeField, Min(.1f)] private float pulseSeconds = 1.6f;
        [Header("Layout")]
        [SerializeField, Min(0)] private float cornerRadius = 18f;
        [SerializeField, Min(50)] private float hintMaxWidth = 520f;
        [SerializeField] private Vector2 hintPadding = new Vector2(26, 16);
        [SerializeField, Min(0)] private float gap = 12f;
        [SerializeField, Min(10), Tooltip("Room for the arrow between the hint and the light.")] private float arrowSpace = 64f;
        [SerializeField, Min(10)] private float shortestArrow = 40f;
        [SerializeField, Min(10), Tooltip("A jack's arrow: short and diagonal, so it lies between the other jacks.")]
        private float jackArrowLength = 36f;
        [SerializeField, Min(0)] private float jackArrowGap = 6f;
        [SerializeField, Min(0)] private float margin = 24f;

        private readonly SpotlightTarget[] targets = new SpotlightTarget[MaxHoles];
        private readonly Camera[] targetCameras = new Camera[MaxHoles];
        private readonly Rect[] goals = new Rect[MaxHoles];
        private readonly bool[] goalValid = new bool[MaxHoles];
        private readonly Rect[] holes = new Rect[MaxHoles];
        private readonly float[] strengths = new float[MaxHoles];
        private readonly Vector3[] corners = new Vector3[4];
        private int targetCount;
        private RectTransform hintAnchor;
        private Camera hintAnchorCamera;
        private RectTransform clickArea;
        private Camera clickAreaCamera;
        private Side side;
        private bool showing;
        private bool listening;
        private bool hintPlaced;
        private Vector2 hintPosition;
        private float fade;
        private float time;

        public event Action SkipClicked;
        /// <summary>A click the dark layer swallowed (outside the step's click areas, Skip and the pause menu).</summary>
        public event Action DimPressed;
        public TutorialTexts Texts => texts;
        public bool IsShowing => showing;
        public string HintText => hintLabel != null ? hintLabel.text : string.Empty;
        public int TargetCount => targetCount;
        public SpotlightGraphic Dim => dim;
        public RectTransform Hint => hint;
        public RectTransform Arrow => arrow;
        public Button SkipButton => skipButton;
        public SpotlightTarget TargetAt(int index) => targets[index];
        public bool HasGoal(int index) => index >= 0 && index < MaxHoles && goalValid[index];
        public Rect Goal(int index) => goals[index];

        /// <summary>Shows a step: its words and up to 8 targets, copied, so the caller may reuse its array.</summary>
        /// <param name="anchor">Optional: the hint goes beside this instead of beside the lit places (the open book).</param>
        /// <param name="extraClickArea">Optional: one more place that takes clicks without being lit (the open book).</param>
        public void Show(string text, SpotlightTarget[] spots, int count, RectTransform anchor = null,
            RectTransform extraClickArea = null)
        {
            if (!listening)
            {
                if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
                if (dim != null) dim.Pressed += OnDimPressed;
                listening = true;
            }
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                fade = 0;
                hintPlaced = false;
                for (int i = 0; i < MaxHoles; i++) strengths[i] = 0;
            }
            showing = true;
            if (group != null) group.interactable = true;
            SetText(text);
            targetCount = Mathf.Clamp(count, 0, MaxHoles);
            hintAnchor = anchor;
            hintAnchorCamera = EventCameraOf(anchor);
            for (int i = 0; i < MaxHoles; i++)
            {
                targets[i] = i < targetCount ? spots[i] : default;
                targetCameras[i] = i < targetCount ? EventCameraOf(spots[i].Rect) : null;
            }
            clickArea = extraClickArea;
            clickAreaCamera = EventCameraOf(extraClickArea);
            if (dim != null)
            {
                dim.raycastTarget = true;
                dim.Blocking = true;
            }
            // At once, not next frame: the step's first click must already land where it should.
            UpdateClickAreas();
        }

        /// <summary>Whether a click at this screen point is swallowed right now.</summary>
        public bool BlocksScreenPoint(Vector2 screenPoint) => dim != null && dim.isActiveAndEnabled &&
            dim.IsRaycastLocationValid(screenPoint, null);

        /// <summary>Fades out, then switches itself off.</summary>
        public void Hide()
        {
            showing = false;
            // Unblocked at once; the fade is only for the eye.
            if (dim != null) dim.Blocking = false;
            if (group != null) group.interactable = false;
            if (!gameObject.activeSelf) Finish();
        }

        private void OnSkipClicked()
        {
            if (showing) SkipClicked?.Invoke();
        }

        private void OnDimPressed()
        {
            if (showing) DimPressed?.Invoke();
        }

        private void OnDestroy()
        {
            if (!listening) return;
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkipClicked);
            if (dim != null) dim.Pressed -= OnDimPressed;
        }

        private void LateUpdate() => Advance(Time.unscaledDeltaTime);

        /// <summary>One frame of motion; tests call it directly.</summary>
        public void Advance(float seconds)
        {
            seconds = Mathf.Max(0, seconds);
            time += seconds;
            fade = Mathf.MoveTowards(fade, showing ? 1 : 0, seconds / fadeSeconds);
            if (group != null) group.alpha = fade;
            if (!showing && fade <= 0)
            {
                Finish();
                return;
            }
            float follow = 1 - Mathf.Exp(-slideSpeed * seconds);
            float breathe = pulse * (.5f - .5f * Mathf.Cos(time * Mathf.PI * 2 / pulseSeconds));
            Rect union = default;
            bool any = false;
            for (int i = 0; i < MaxHoles; i++)
            {
                goalValid[i] = i < targetCount &&
                    TryLocalRect(targets[i].Rect, targetCameras[i], targets[i].Padding, out goals[i]);
                if (goalValid[i])
                {
                    if (strengths[i] <= .001f) holes[i] = SpawnRect(i);
                    holes[i] = Lerp(holes[i], goals[i], follow);
                    strengths[i] = Mathf.MoveTowards(strengths[i], 1, seconds / fadeSeconds);
                    union = any ? Union(union, goals[i]) : goals[i];
                    any = true;
                }
                else strengths[i] = Mathf.MoveTowards(strengths[i], 0, seconds / fadeSeconds);
                Rect shown = Grow(holes[i], breathe * strengths[i]);
                float radius = i < targetCount && targets[i].Round ? float.MaxValue : cornerRadius;
                dim.SetHole(i, shown, radius, strengths[i]);
            }
            dim.Apply();
            if (showing) UpdateClickAreas();
            Rect anchorRect = default;
            bool anchored = any && hintAnchor != null && TryLocalRect(hintAnchor, hintAnchorCamera, 0, out anchorRect);
            if (any) PlaceHintAndArrow(union, anchored ? anchorRect : union, anchored, follow);
            else
            {
                if (hint != null) hint.gameObject.SetActive(false);
                if (arrow != null) arrow.gameObject.SetActive(false);
            }
        }

        private void UpdateClickAreas()
        {
            if (dim == null) return;
            dim.ClearClickAreas();
            for (int i = 0; i < targetCount; i++)
                if (targets[i].Clickable && TryLocalRect(targets[i].Rect, targetCameras[i], 0, out Rect area))
                    dim.AddClickArea(area, targets[i].Round);
            if (clickArea != null && TryLocalRect(clickArea, clickAreaCamera, 0, out Rect extra)) dim.AddClickArea(extra, false);
        }

        private void Finish()
        {
            showing = false;
            targetCount = 0;
            hintAnchor = null;
            hintAnchorCamera = null;
            clickArea = null;
            clickAreaCamera = null;
            for (int i = 0; i < MaxHoles; i++) { targets[i] = default; targetCameras[i] = null; goalValid[i] = false; }
            if (dim != null)
            {
                dim.Blocking = false;
                dim.ClearClickAreas();
                dim.Release();
            }
            gameObject.SetActive(false);
        }

        private void SetText(string text)
        {
            if (hintLabel == null || hint == null) return;
            hintLabel.text = text ?? string.Empty;
            Vector2 preferred = hintLabel.GetPreferredValues(hintLabel.text, hintMaxWidth - hintPadding.x * 2, 0);
            float width = Mathf.Min(preferred.x, hintMaxWidth - hintPadding.x * 2);
            preferred = hintLabel.GetPreferredValues(hintLabel.text, width, 0);
            hint.sizeDelta = new Vector2(width, preferred.y) + hintPadding * 2;
        }

        // A new hole grows out of the nearest open one, so the light seems to move rather than blink.
        private Rect SpawnRect(int index)
        {
            Rect goal = goals[index];
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < MaxHoles; i++)
            {
                if (i == index || strengths[i] < .5f) continue;
                float distance = (holes[i].center - goal.center).sqrMagnitude;
                if (distance < best) { best = distance; nearest = i; }
            }
            return nearest >= 0 ? holes[nearest] : goal;
        }

        private bool TryLocalRect(RectTransform target, Camera camera, float padding, out Rect rect)
        {
            rect = default;
            if (target == null || !target.gameObject.activeInHierarchy || dim == null) return false;
            target.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dim.rectTransform, screen, null, out Vector2 local))
                    return false;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            rect = Grow(Rect.MinMaxRect(min.x, min.y, max.x, max.y), padding);
            return rect.width > 1 && rect.height > 1;
        }

        private static Camera EventCameraOf(RectTransform target)
        {
            Canvas canvas = target != null ? target.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return null;
            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private enum Side { Right, Left, Above, Below }

        /// <param name="lit">Every lit place together.</param>
        /// <param name="outer">What the hint keeps clear of: the lit places, or the anchor (the open book) around them.</param>
        private void PlaceHintAndArrow(Rect lit, Rect outer, bool anchored, float follow)
        {
            if (hint == null) return;
            hint.gameObject.SetActive(true);
            Rect bounds = dim.rectTransform.rect;
            Vector2 size = hint.sizeDelta;
            float clearance = gap * 2 + arrowSpace;
            float right = bounds.xMax - margin - outer.xMax - clearance - size.x;
            float left = outer.xMin - bounds.xMin - margin - clearance - size.x;
            float above = bounds.yMax - margin - outer.yMax - clearance - size.y;
            float below = outer.yMin - bounds.yMin - margin - clearance - size.y;
            // Beside the light first, on the roomier side; above or below only when neither side fits.
            if (right >= 0 || left >= 0) side = right >= left ? Side.Right : Side.Left;
            else if (above >= 0 || below >= 0) side = above >= below ? Side.Above : Side.Below;
            else side = Mathf.Max(right, left) >= Mathf.Max(above, below) ? (right >= left ? Side.Right : Side.Left)
                : (above >= below ? Side.Above : Side.Below);
            // Beside the anchor, but level with the lit places, so a short straight arrow points at them.
            Vector2 goal;
            switch (side)
            {
                case Side.Right: goal = new Vector2(outer.xMax + clearance + size.x * .5f, lit.center.y); break;
                case Side.Left: goal = new Vector2(outer.xMin - clearance - size.x * .5f, lit.center.y); break;
                case Side.Above: goal = new Vector2(lit.center.x, outer.yMax + clearance + size.y * .5f); break;
                default: goal = new Vector2(lit.center.x, outer.yMin - clearance - size.y * .5f); break;
            }
            goal.x = Mathf.Clamp(goal.x, bounds.xMin + margin + size.x * .5f, bounds.xMax - margin - size.x * .5f);
            goal.y = Mathf.Clamp(goal.y, bounds.yMin + margin + size.y * .5f, bounds.yMax - margin - size.y * .5f);
            hintPosition = hintPlaced ? Vector2.Lerp(hintPosition, goal, follow) : goal;
            hintPlaced = true;
            hint.anchoredPosition = hintPosition;
            PlaceArrow(size, anchored ? outer : (Rect?)null);
        }

        // The arrow runs from the hint's edge to just outside the nearest lit place; beside an anchor it stops at the
        // anchor's edge (it never crosses the book's text), and a jack gets a short diagonal arrow that lies between
        // its neighbours instead of on them.
        private void PlaceArrow(Vector2 hintSize, Rect? anchor)
        {
            if (arrow == null) return;
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < MaxHoles; i++)
            {
                if (!goalValid[i]) continue;
                float distance = (goals[i].center - hintPosition).sqrMagnitude;
                if (distance < best) { best = distance; nearest = i; }
            }
            if (nearest < 0) { arrow.gameObject.SetActive(false); return; }
            Rect hole = holes[nearest];
            Vector2 toHole = hole.center - hintPosition;
            if (toHole.sqrMagnitude < 1) { arrow.gameObject.SetActive(false); return; }
            Vector2 direction = toHole.normalized;
            float bob = pulse * (.5f - .5f * Mathf.Cos(time * Mathf.PI * 2 / pulseSeconds));
            Vector2 tip;
            float length;
            if (anchor.HasValue)
            {
                Rect outer = anchor.Value;
                direction = side == Side.Right ? Vector2.left : side == Side.Left ? Vector2.right
                    : side == Side.Above ? Vector2.down : Vector2.up;
                tip = side == Side.Right ? new Vector2(outer.xMax + gap, hintPosition.y)
                    : side == Side.Left ? new Vector2(outer.xMin - gap, hintPosition.y)
                    : side == Side.Above ? new Vector2(hintPosition.x, outer.yMax + gap)
                    : new Vector2(hintPosition.x, outer.yMin - gap);
                Vector2 tail = hintPosition + direction * (Exit(hintSize, direction) + gap);
                length = Vector2.Dot(tip - tail, direction) - pulse;
            }
            else if (targets[nearest].Round)
            {
                // Diagonal, from the hint's quarter: the jacks sit in a grid, so its diagonals are the free gaps.
                direction = new Vector2(toHole.x > 0 ? 1 : -1, toHole.y > 0 ? 1 : -1).normalized;
                tip = hole.center - direction * (Mathf.Min(hole.width, hole.height) * .5f + jackArrowGap);
                length = jackArrowLength;
            }
            else
            {
                tip = hole.center - direction * (Exit(hole.size, direction) + gap);
                Vector2 tail = hintPosition + direction * (Exit(hintSize, direction) + gap);
                length = Vector2.Dot(tip - tail, direction) - pulse;
            }
            // Whole units, so the arrow's mesh is rebuilt only when its length really changes; the bob only moves it.
            length = Mathf.Round(Mathf.Max(targets[nearest].Round && !anchor.HasValue ? jackArrowLength : shortestArrow, length));
            arrow.gameObject.SetActive(true);
            if (arrow.sizeDelta.x != length) arrow.sizeDelta = new Vector2(length, arrow.sizeDelta.y);
            arrow.anchoredPosition = tip - direction * (length * .5f + bob);
            arrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        // How far from a box's centre a ray in this direction leaves the box.
        private static float Exit(Vector2 size, Vector2 direction) => Mathf.Min(
            direction.x != 0 ? size.x * .5f / Mathf.Abs(direction.x) : float.MaxValue,
            direction.y != 0 ? size.y * .5f / Mathf.Abs(direction.y) : float.MaxValue);

        private static Rect Grow(Rect rect, float amount) =>
            Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);

        private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        private static Rect Lerp(Rect from, Rect to, float t) => Rect.MinMaxRect(
            Mathf.Lerp(from.xMin, to.xMin, t), Mathf.Lerp(from.yMin, to.yMin, t),
            Mathf.Lerp(from.xMax, to.xMax, t), Mathf.Lerp(from.yMax, to.yMax, t));
    }
}
