using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MuteIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector2 center = rectTransform.rect.center;
            float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 48;
            Polygon(mesh, center, scale, new[] { new Vector2(-19, -7), new Vector2(-19, 7), new Vector2(-10, 7), new Vector2(-10, -7) });
            Polygon(mesh, center, scale, new[] { new Vector2(-10, -7), new Vector2(-10, 7), new Vector2(2, 17), new Vector2(2, -17) });
            Line(mesh, center, scale, new Vector2(9, -8), new Vector2(23, 8), 3);
            Line(mesh, center, scale, new Vector2(9, 8), new Vector2(23, -8), 3);
        }
        private void Polygon(VertexHelper mesh, Vector2 center, float scale, Vector2[] points)
        {
            int start = mesh.currentVertCount;
            foreach (var point in points) mesh.AddVert(center + point * scale, color, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
        private void Line(VertexHelper mesh, Vector2 center, float scale, Vector2 a, Vector2 b, float width)
        {
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            Polygon(mesh, center, scale, new[] { a - n, a + n, b + n, b - n });
        }
    }
}
