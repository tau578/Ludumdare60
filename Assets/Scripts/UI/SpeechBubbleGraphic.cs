using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>A continuous rounded speech balloon, including its left-facing tail.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpeechBubbleGraphic : MaskableGraphic
    {
        [SerializeField, Min(1)] private float cornerRadius = 18f;
        [SerializeField, Min(0)] private float rimWidth = 2f;
        [SerializeField, Min(0)] private float outlineWidth = 2.5f;
        [SerializeField] private Color rimColor = new Color32(255, 241, 210, 255);
        [SerializeField] private Color outlineColor = new Color32(79, 67, 48, 255);
        private readonly List<Vector2> contour = new List<Vector2>(40);
        private readonly List<Vector2> inward = new List<Vector2>(40);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width < 1 || rect.height < 1) return;
            float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * .25f);
            float tail = Mathf.Min(18f, rect.height * .12f);
            contour.Clear();
            // Clockwise outline; the tail replaces the middle of the left edge.
            Arc(new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 180);
            Arc(new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 90);
            Arc(new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 0);
            Arc(new Vector2(rect.xMin + radius, rect.yMin + radius), radius, -90);
            contour.Add(new Vector2(rect.xMin, rect.center.y - tail));
            contour.Add(new Vector2(rect.xMin - tail, rect.center.y));
            contour.Add(new Vector2(rect.xMin, rect.center.y + tail));

            inward.Clear();
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 before = (contour[i] - contour[(i + contour.Count - 1) % contour.Count]).normalized;
                Vector2 after = (contour[(i + 1) % contour.Count] - contour[i]).normalized;
                Vector2 a = new Vector2(before.y, -before.x);
                Vector2 b = new Vector2(after.y, -after.x);
                // Offset edge intersections keep the outline equally thick around the tail.
                inward.Add((a + b) / Mathf.Max(.01f, 1f + Vector2.Dot(a, b)));
            }

            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float rim = Mathf.Min(rimWidth, radius * .2f);
            float ink = Mathf.Min(outlineWidth, radius * .25f);
            float aa = Mathf.Min(.75f / Mathf.Max(.01f, scale), Mathf.Max(.01f, ink * .5f));
            Color edge = rimColor * color;
            Color dark = outlineColor * color;
            Color clear = edge; clear.a = 0;
            Ring(mesh, -aa, clear, 0, edge);
            Ring(mesh, 0, edge, rim, edge);
            Ring(mesh, rim, edge, rim + aa, dark);
            Ring(mesh, rim + aa, dark, rim + ink, dark);
            Ring(mesh, rim + ink, dark, rim + ink + aa, color);
            Fill(mesh, rim + ink + aa, rect.center);
        }

        private void Arc(Vector2 center, float radius, float startDegrees)
        {
            for (int step = 0; step <= 8; step++)
            {
                float angle = (startDegrees - step * 90f / 8f) * Mathf.Deg2Rad;
                contour.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private void Ring(VertexHelper mesh, float outerInset, Color outer, float innerInset, Color inner)
        {
            int start = mesh.currentVertCount;
            for (int i = 0; i < contour.Count; i++)
            {
                mesh.AddVert(contour[i] + inward[i] * outerInset, outer, Vector2.zero);
                mesh.AddVert(contour[i] + inward[i] * innerInset, inner, Vector2.zero);
            }
            for (int i = 0; i < contour.Count; i++)
            {
                int a = start + i * 2, b = start + (i + 1) % contour.Count * 2;
                mesh.AddTriangle(a, b, a + 1);
                mesh.AddTriangle(b, b + 1, a + 1);
            }
        }

        private void Fill(VertexHelper mesh, float inset, Vector2 center)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i < contour.Count; i++)
                mesh.AddVert(contour[i] + inward[i] * inset, color, Vector2.zero);
            for (int i = 0; i < contour.Count; i++)
                mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % contour.Count);
        }
    }
}
