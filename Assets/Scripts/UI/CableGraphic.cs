using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CableGraphic : MaskableGraphic
    {
        private const int Segments = 48;
        /// <summary>How many points <see cref="CopyShape"/> writes.</summary>
        public const int ShapePoints = Segments + 1;
        /// <summary>How many wavelengths the connection wiggle's wave spans plug to plug.</summary>
        public const float SmoothWaveLengths = 1.5f;
        private readonly Vector2[] points = new Vector2[Segments + 1];
        private readonly Vector2[] restingPoints = new Vector2[Segments + 1];
        private Vector2 first, second;
        private float waveAmplitude;
        private float wavePhase;
        private float waveLengths = SmoothWaveLengths;
        [SerializeField] private Color insulationColor = new Color32(178, 99, 51, 255);
        [SerializeField] private float maximumSag = 155f;
        [SerializeField] private float thicknessScale = 1f;
        public float ThicknessScale { get => thicknessScale; set { thicknessScale = value; SetVerticesDirty(); } }
        public float MaximumSag { get => maximumSag; set { maximumSag = Mathf.Max(28, value); SetVerticesDirty(); } }
        public Color InsulationColor { get => insulationColor; set { insulationColor = value; SetVerticesDirty(); } }

        public void SetEndpoints(Vector2 a, Vector2 b)
        {
            if (first == a && second == b) return;
            first = a;
            second = b;
            SetVerticesDirty();
        }

        /// <summary>The wave across the wire right now; 0 when it hangs still.</summary>
        public float WaveAmplitude => waveAmplitude;
        /// <summary>How many wavelengths the current wave fits plug to plug.</summary>
        public float WaveLengths => waveLengths;

        /// <summary>
        /// Bends the wire with a wave pinned at both plugs (D60): <paramref name="amplitude"/> in canvas units across the
        /// wire, <paramref name="phase"/> in wavelengths moving it along, <paramref name="wavelengths"/> fitting plug to
        /// plug (more of them make tighter kinks, as the wrong-number jolt does). An amplitude of 0 draws the plain
        /// hanging wire, exactly as without a wave. The mesh is rebuilt only when a value changes.
        /// </summary>
        public void SetWave(float amplitude, float phase, float wavelengths = SmoothWaveLengths)
        {
            if (amplitude == 0)
            {
                phase = 0;
                wavelengths = SmoothWaveLengths;
            }
            if (waveAmplitude == amplitude && wavePhase == phase && waveLengths == wavelengths) return;
            waveAmplitude = amplitude;
            wavePhase = phase;
            waveLengths = wavelengths;
            SetVerticesDirty();
        }

        /// <summary>The drawn centre line, plug to plug, into an array of <see cref="ShapePoints"/>.</summary>
        public void CopyShape(Vector2[] into)
        {
            BuildPoints();
            System.Array.Copy(points, into, ShapePoints);
        }

        private void BuildPoints()
        {
            float sag = Mathf.Clamp(Vector2.Distance(first, second) * 0.35f, 28f, maximumSag);
            Vector2 p1 = first + Vector2.down * sag;
            Vector2 p2 = second + Vector2.down * sag;
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                float u = 1 - t;
                points[i] = u * u * u * first + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * second;
            }
            if (waveAmplitude == 0) return;
            System.Array.Copy(points, restingPoints, ShapePoints);
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                Vector2 tangent = restingPoints[Mathf.Min(i + 1, Segments)] - restingPoints[Mathf.Max(0, i - 1)];
                Vector2 normal = tangent.sqrMagnitude < 0.001f ? Vector2.up : new Vector2(-tangent.y, tangent.x).normalized;
                float across = waveAmplitude * Mathf.Sin(Mathf.PI * t) * Mathf.Sin(2 * Mathf.PI * (waveLengths * t - wavePhase));
                points[i] = restingPoints[i] + normal * across;
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            BuildPoints();
            Strip(mesh, 6.5f, new Vector2(2, -3), new Color32(16, 19, 17, 150));
            Strip(mesh, 4.5f, Vector2.zero, Color.Lerp(insulationColor, Color.black, 0.65f));
            Strip(mesh, 2.6f, new Vector2(0, 0.5f), insulationColor);
            Strip(mesh, 0.7f, new Vector2(0, 1.5f), Color.Lerp(insulationColor, Color.white, 0.35f));
        }

        private void Strip(VertexHelper mesh, float halfWidth, Vector2 offset, Color tint)
        {
            halfWidth *= thicknessScale;
            int start = mesh.currentVertCount;
            for (int i = 0; i <= Segments; i++)
            {
                Vector2 tangent = points[Mathf.Min(i + 1, Segments)] - points[Mathf.Max(0, i - 1)];
                Vector2 normal = tangent.sqrMagnitude < 0.001f ? Vector2.right : new Vector2(-tangent.y, tangent.x).normalized;
                mesh.AddVert(points[i] + offset - normal * halfWidth, tint * color, Vector2.zero);
                mesh.AddVert(points[i] + offset + normal * halfWidth, tint * color, Vector2.zero);
                if (i == 0) continue;
                int current = start + i * 2;
                mesh.AddTriangle(current - 2, current - 1, current);
                mesh.AddTriangle(current, current - 1, current + 1);
            }
        }
    }
}
