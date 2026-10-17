using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CableGraphic : MaskableGraphic
    {
        private const int Segments = 48;
        private readonly Vector2[] points = new Vector2[Segments + 1];
        private Vector2 first, second;
        [SerializeField] private Color insulationColor = new Color32(178, 99, 51, 255);
        public Color InsulationColor { get => insulationColor; set { insulationColor = value; SetVerticesDirty(); } }

        public void SetEndpoints(Vector2 a, Vector2 b)
        {
            if (first == a && second == b) return;
            first = a;
            second = b;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float sag = Mathf.Clamp(Vector2.Distance(first, second) * 0.35f, 28f, 155f);
            Vector2 p1 = first + Vector2.down * sag;
            Vector2 p2 = second + Vector2.down * sag;
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                float u = 1 - t;
                points[i] = u * u * u * first + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * second;
            }
            Strip(mesh, 6.5f, new Vector2(2, -3), new Color32(16, 19, 17, 150));
            Strip(mesh, 4.5f, Vector2.zero, Color.Lerp(insulationColor, Color.black, 0.65f));
            Strip(mesh, 2.6f, new Vector2(0, 0.5f), insulationColor);
            Strip(mesh, 0.7f, new Vector2(0, 1.5f), Color.Lerp(insulationColor, Color.white, 0.35f));
        }

        private void Strip(VertexHelper mesh, float halfWidth, Vector2 offset, Color tint)
        {
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
