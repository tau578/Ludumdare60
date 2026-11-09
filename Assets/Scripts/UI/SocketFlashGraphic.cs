using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>What a socket's short flash shows (D60). Explicit values: tests compare them.</summary>
    public enum SocketFlashKind
    {
        /// <summary>A circuit closed on a call: a light ring expands from the jack (V6).</summary>
        ConnectRing = 0,
        /// <summary>A wrong number: the jack flashes red (V5).</summary>
        WrongNumber = 1,
        /// <summary>A wrong number's short circuit at the plug: a hot flash, falling streak sparks, a flickering arc and
        /// a puff of smoke (V5). Drawn over the plug and the wires.</summary>
        ShortCircuit = 2
    }

    /// <summary>
    /// A short effect over a socket, drawn as its own child so it never touches the socket's halo, lamp or dimming:
    /// when it ends the jack looks exactly as before (D60, V5 and V6). It takes no clicks and runs on unscaled time,
    /// so one already started finishes even if the game pauses.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SocketFlashGraphic : MaskableGraphic
    {
        public const float RingSeconds = .5f;
        public const float WrongSeconds = .5f;
        /// <summary>The short circuit lasts as long as its smoke.</summary>
        public const float ShortSeconds = .6f;
        public const float HotFlashSeconds = .12f;
        public const float ArcSeconds = .2f;
        public const int MinStreaks = 6;
        public const int MaxStreaks = 10;
        /// <summary>Nothing of the short circuit reaches further from the jack's centre, in jack widths.</summary>
        public const float MaxReach = 1.5f;
        /// <summary>The wrong-number ring's inner edge, in jack widths.</summary>
        public const float WrongRingInner = .22f;
        // Close to the connected jacks' green halo (11c playtest: the first mint was too pale).
        public static readonly Color RingColor = new Color32(104, 240, 134, 255);
        private static readonly Color HotWhite = new Color32(255, 255, 242, 255);
        private static readonly Color HotYellow = new Color32(255, 226, 104, 255);
        private static readonly Color HotOrange = new Color32(255, 128, 36, 255);
        private static readonly Color ArcCore = new Color32(232, 240, 255, 255);
        private static readonly Color ArcGlow = new Color32(150, 186, 255, 255);
        private static readonly Color Smoke = new Color32(36, 34, 33, 255);
        private const int Segments = 48;
        private const int DiscSegments = 18;
        private const int Puffs = 5;
        private const int ArcPoints = 7;
        private const float ArcFlickerSeconds = .03f;
        private const float StreakDrag = 3f;
        private const float Gravity = 8f;
        private const float StreakCore = .032f;
        // Scratch buffers: the mesh is rebuilt every frame while an effect plays (single-threaded, so shared is safe).
        private static readonly float[] RingRadii = new float[4];
        private static readonly Color[] RingColours = new Color[4];
        private static readonly Vector2[] ArcPath = new Vector2[ArcPoints];
        // One burst's random shape, drawn once per play; the frames only read it.
        private readonly float[] streakAngle = new float[MaxStreaks];
        private readonly float[] streakSpeed = new float[MaxStreaks];
        private readonly float[] streakLife = new float[MaxStreaks];
        private readonly float[] streakDelay = new float[MaxStreaks];
        private readonly float[] streakStart = new float[MaxStreaks];
        private readonly float[] arcAngle = new float[2];
        private readonly float[] arcTurn = new float[2];
        private readonly float[] arcReach = new float[2];
        private readonly float[] puffX = new float[Puffs];
        private readonly float[] puffRise = new float[Puffs];
        private readonly float[] puffDelay = new float[Puffs];
        private int streaks;
        private int arcs;
        private int seed;
        private float age = -1;

        public SocketFlashKind Kind { get; private set; }
        /// <summary>The wrong-number flash's colour, set by the socket from the game's palette before it plays.</summary>
        public Color WrongNumberColor { get; set; } = Color.white;
        /// <summary>The wrong-number ring's outer edge in jack widths, set by the socket from the palette before it plays.</summary>
        public float WrongNumberRingOuter { get; set; } = .54f;
        /// <summary>The wrong-number ring's strength, 0 to 1, set by the socket from the palette before it plays.</summary>
        public float WrongNumberStrength { get; set; } = .85f;
        public bool IsPlaying => age >= 0;
        public float Age => age;
        public float Duration => Kind == SocketFlashKind.ConnectRing ? RingSeconds
            : Kind == SocketFlashKind.WrongNumber ? WrongSeconds : ShortSeconds;
        /// <summary>The streak sparks of the current short circuit (6 to 10).</summary>
        public int StreakCount => Kind == SocketFlashKind.ShortCircuit ? streaks : 0;
        /// <summary>The zigzag arcs of the current short circuit (1 or 2).</summary>
        public int ArcCount => Kind == SocketFlashKind.ShortCircuit ? arcs : 0;
        /// <summary>The jack's width the effect is sized by, in its own units.</summary>
        public float JackSize => Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);

        public void Play(SocketFlashKind kind)
        {
            Kind = kind;
            if (kind == SocketFlashKind.ShortCircuit) NewBurst();
            age = 0;
            raycastTarget = false;
            gameObject.SetActive(true);
            SetVerticesDirty();
        }

        /// <summary>Moves the effect on; Update drives it, tests call it directly.</summary>
        public void Advance(float seconds)
        {
            if (age < 0) return;
            age += Mathf.Max(0, seconds);
            if (age >= Duration) Stop();
            else SetVerticesDirty();
        }

        public void Stop()
        {
            age = -1;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Where streak <paramref name="index"/> is now, relative to the jack's centre: its tail and its head (the
        /// leading end) and its full width. False while it hasn't started or has burnt out.
        /// </summary>
        public bool TryGetStreak(int index, out Vector2 tail, out Vector2 head, out float width)
        {
            tail = head = Vector2.zero;
            width = 0;
            if (age < 0 || Kind != SocketFlashKind.ShortCircuit || index < 0 || index >= streaks) return false;
            float size = JackSize;
            float t = age - streakDelay[index];
            float life = streakLife[index];
            if (t < 0 || t >= life) return false;
            var direction = new Vector2(Mathf.Cos(streakAngle[index]), Mathf.Sin(streakAngle[index]));
            float speed = streakSpeed[index] * size;
            float drag = Mathf.Exp(-StreakDrag * t);
            head = direction * (streakStart[index] * size + speed * (1 - drag) / StreakDrag) + Vector2.down * (.5f * Gravity * size * t * t);
            Vector2 velocity = direction * (speed * drag) + Vector2.down * (Gravity * size * t);
            float length = Mathf.Clamp(velocity.magnitude * .06f, .24f * size, .5f * size);
            tail = head - (velocity.sqrMagnitude < .0001f ? direction : velocity.normalized) * length;
            width = StreakCore * 2 * size * (1f - .5f * t / life);
            return true;
        }

        private void Update() => Advance(Mathf.Min(Time.unscaledDeltaTime, .05f));

        protected override void OnDisable()
        {
            age = -1;
            base.OnDisable();
        }

        // Uneven on purpose: a random count, jittered directions, speeds and starts, so it never reads as a symmetric
        // burst. They spray up and sideways, then gravity pulls them down, all within MaxReach.
        private void NewBurst()
        {
            seed = Random.Range(1, 1 << 20);
            streaks = Random.Range(MinStreaks, MaxStreaks + 1);
            const float lowest = -25f * Mathf.Deg2Rad, spread = 230f * Mathf.Deg2Rad;
            float gap = spread / streaks;
            for (int i = 0; i < streaks; i++)
            {
                streakAngle[i] = lowest + (i + .5f) * gap + Random.Range(-.45f, .45f) * gap;
                streakSpeed[i] = Random.Range(1.6f, 3.8f);
                streakLife[i] = Random.Range(.28f, .4f);
                streakDelay[i] = Random.Range(0f, .08f);
                streakStart[i] = Random.Range(.08f, .2f);
            }
            arcs = Random.value < .5f ? 1 : 2;
            for (int i = 0; i < arcs; i++)
            {
                arcAngle[i] = Random.value * Mathf.PI * 2f;
                arcTurn[i] = Random.Range(.7f, 1.5f) * (Random.value < .5f ? -1 : 1);
                arcReach[i] = Random.Range(.6f, .95f);
            }
            for (int i = 0; i < Puffs; i++)
            {
                puffX[i] = Random.Range(-.25f, .25f);
                puffRise[i] = Random.Range(.45f, .7f);
                puffDelay[i] = i * .02f + Random.Range(0f, .03f);
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (age < 0) return;
            Rect rect = rectTransform.rect;
            float size = Mathf.Min(rect.width, rect.height);
            float t = Mathf.Clamp01(age / Duration);
            if (Kind == SocketFlashKind.ConnectRing)
            {
                float grow = 1f - (1f - t) * (1f - t) * (1f - t);
                // It spreads only a little past the jack's rim, so it stays with its jack (11c playtest).
                float radius = size * (.4f + .28f * grow);
                float width = size * (.04f + .07f * (1f - t));
                Ring(mesh, rect.center, radius - width, radius + width, Tint(RingColor, 1f - t * t));
                return;
            }
            if (Kind == SocketFlashKind.WrongNumber)
            {
                // Two quick red flashes that fade, around the hole and over the rim.
                float flash = (.55f + .45f * Mathf.Cos(t * Mathf.PI * 4f)) * (1f - t * .7f) * Mathf.Clamp01(WrongNumberStrength);
                float outer = Mathf.Max(WrongRingInner + .02f, WrongNumberRingOuter);
                Ring(mesh, rect.center, size * WrongRingInner, size * outer, Tint(WrongNumberColor, flash));
                return;
            }
            DrawSmoke(mesh, rect.center, size);
            DrawHotFlash(mesh, rect.center, size);
            DrawStreaks(mesh, rect.center);
            DrawArcs(mesh, rect.center, size);
        }

        // A puff of dark smoke rises from the plug and thins out.
        private void DrawSmoke(VertexHelper mesh, Vector2 centre, float size)
        {
            for (int i = 0; i < Puffs; i++)
            {
                float u = (age - puffDelay[i]) / (ShortSeconds - puffDelay[i]);
                if (u <= 0 || u >= 1) continue;
                float alpha = .85f * (u < .15f ? u / .15f : (1 - u) / .85f);
                var at = centre + new Vector2(puffX[i] * size * (1 + u * .4f), (.05f + puffRise[i] * u) * size);
                Disc(mesh, at, size * (.24f + .34f * u), Tint(Smoke, alpha), Tint(Smoke, alpha * .85f), Tint(Smoke, 0));
            }
        }

        // A short, hot white-to-yellow flash right at the plug.
        private void DrawHotFlash(VertexHelper mesh, Vector2 centre, float size)
        {
            if (age >= HotFlashSeconds) return;
            float k = age / HotFlashSeconds;
            float alpha = (1 - k) * Mathf.Sqrt(1 - k);
            Disc(mesh, centre, size * (.4f + .35f * k), Tint(HotWhite, alpha), Tint(HotYellow, alpha * .9f), Tint(HotOrange, 0));
        }

        // Thin streaks with a white core that turns yellow then orange as they fly out, fall and burn out.
        private void DrawStreaks(VertexHelper mesh, Vector2 centre)
        {
            for (int i = 0; i < streaks; i++)
            {
                if (!TryGetStreak(i, out Vector2 tail, out Vector2 head, out float width)) continue;
                float u = (age - streakDelay[i]) / streakLife[i];
                float alpha = 1 - u * u * u;
                Color hot = u < .35f ? Color.Lerp(HotWhite, HotYellow, u / .35f) : Color.Lerp(HotYellow, HotOrange, (u - .35f) / .65f);
                Line(mesh, centre + tail, centre + head, width * .4f, width * 1.3f, Tint(HotOrange, 0), Tint(hot, alpha * .5f));
                Line(mesh, centre + tail, centre + head, width * .15f, width * .55f, Tint(hot, alpha * .6f), Tint(HotWhite, alpha));
            }
        }

        // Jagged arcs that jump around the plug, redrawn every few hundredths of a second and blinking out now and then.
        private void DrawArcs(VertexHelper mesh, Vector2 centre, float size)
        {
            if (age >= ArcSeconds) return;
            int flicker = Mathf.FloorToInt(age / ArcFlickerSeconds);
            float fade = 1 - age / ArcSeconds * .5f;
            for (int a = 0; a < arcs; a++)
            {
                if (flicker > 0 && Noise(flicker, a, 99) < .3f) continue;
                for (int p = 0; p < ArcPoints; p++)
                {
                    float s = (float)p / (ArcPoints - 1);
                    float angle = arcAngle[a] + arcTurn[a] * s;
                    float radius = Mathf.Lerp(.12f, arcReach[a], s);
                    float jitter = p == 0 || p == ArcPoints - 1 ? 0 : (Noise(flicker, a, p) - .5f) * .26f;
                    ArcPath[p] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ((radius + jitter) * size);
                }
                for (int p = 1; p < ArcPoints; p++)
                {
                    Line(mesh, ArcPath[p - 1], ArcPath[p], size * .045f, size * .045f, Tint(ArcGlow, .45f * fade), Tint(ArcGlow, .45f * fade));
                    Line(mesh, ArcPath[p - 1], ArcPath[p], size * .016f, size * .016f, Tint(ArcCore, fade), Tint(ArcCore, fade));
                }
            }
        }

        // A repeatable 0..1 per flicker step, so a frame allocates nothing and a step draws the same until it changes.
        private float Noise(int step, int arc, int point)
        {
            uint h = (uint)seed * 374761393u + (uint)step * 668265263u + (uint)arc * 2246822519u + (uint)point * 3266489917u;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        private Color Tint(Color tint, float alpha) => new Color(tint.r, tint.g, tint.b, tint.a * alpha * color.a);

        // A quad from a to b, its half widths and colours set at each end.
        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float halfA, float halfB, Color colourA, Color colourB)
        {
            Vector2 along = b - a;
            if (along.sqrMagnitude < .0001f) return;
            var normal = new Vector2(-along.y, along.x).normalized;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - normal * halfA, colourA, Vector2.zero);
            mesh.AddVert(a + normal * halfA, colourA, Vector2.zero);
            mesh.AddVert(b + normal * halfB, colourB, Vector2.zero);
            mesh.AddVert(b - normal * halfB, colourB, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        // A soft disc: the centre colour, the middle colour halfway out, the edge colour at the rim.
        private static void Disc(VertexHelper mesh, Vector2 centre, float radius, Color inner, Color middle, Color edge)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(centre, inner, Vector2.zero);
            for (int i = 0; i <= DiscSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / DiscSegments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(centre + direction * (radius * .5f), middle, Vector2.zero);
                mesh.AddVert(centre + direction * radius, edge, Vector2.zero);
                if (i == 0) continue;
                int mid = start + 1 + i * 2, previous = mid - 2;
                mesh.AddTriangle(start, previous, mid);
                mesh.AddTriangle(previous, previous + 1, mid);
                mesh.AddTriangle(mid, previous + 1, mid + 1);
            }
        }

        // A band with soft edges: transparent, full, full, transparent.
        private static void Ring(VertexHelper mesh, Vector2 centre, float inner, float outer, Color tint)
        {
            Color clear = tint;
            clear.a = 0;
            float soft = (outer - inner) * .3f;
            float[] radii = RingRadii;
            radii[0] = Mathf.Max(0, inner - soft); radii[1] = inner; radii[2] = outer; radii[3] = outer + soft;
            Color[] colours = RingColours;
            colours[0] = colours[3] = clear;
            colours[1] = colours[2] = tint;
            int start = mesh.currentVertCount;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (int band = 0; band < 4; band++)
                {
                    mesh.AddVert(centre + direction * radii[band], colours[band], Vector2.zero);
                    if (i == 0 || band == 0) continue;
                    int current = start + i * 4 + band;
                    int previous = current - 4;
                    mesh.AddTriangle(previous - 1, previous, current - 1);
                    mesh.AddTriangle(current - 1, previous, current);
                }
            }
        }
    }
}
