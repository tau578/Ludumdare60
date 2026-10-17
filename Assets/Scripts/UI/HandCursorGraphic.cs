using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Resolution-independent glove cursors; the transform origin is the click hotspot.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HandCursorGraphic : MaskableGraphic
    {
        private bool grabbing;
        public bool Grabbing { get => grabbing; set { if (grabbing == value) return; grabbing = value; SetVerticesDirty(); } }
        private static readonly Vector2[] Pointing = {
            new Vector2(20,2), new Vector2(23,3), new Vector2(25,6), new Vector2(25,25),
            new Vector2(28,22), new Vector2(32,22), new Vector2(35,25), new Vector2(38,24),
            new Vector2(42,25), new Vector2(44,28), new Vector2(47,28), new Vector2(50,31),
            new Vector2(50,43), new Vector2(47,50), new Vector2(42,56), new Vector2(42,60),
            new Vector2(22,60), new Vector2(22,55), new Vector2(17,50), new Vector2(12,43),
            new Vector2(7,36), new Vector2(7,32), new Vector2(10,30), new Vector2(13,31),
            new Vector2(17,35), new Vector2(17,7), new Vector2(18,3)
        };
        private static readonly Vector2[] Grab = {
            new Vector2(17,12), new Vector2(20,9), new Vector2(24,9), new Vector2(27,12),
            new Vector2(27,20), new Vector2(29,9), new Vector2(32,7), new Vector2(35,8),
            new Vector2(37,11), new Vector2(37,22), new Vector2(39,13), new Vector2(42,12),
            new Vector2(45,14), new Vector2(46,18), new Vector2(46,28), new Vector2(48,21),
            new Vector2(51,22), new Vector2(53,26), new Vector2(52,41), new Vector2(49,49),
            new Vector2(44,54), new Vector2(44,59), new Vector2(24,59), new Vector2(24,54),
            new Vector2(18,51), new Vector2(12,46), new Vector2(6,36), new Vector2(6,32),
            new Vector2(9,30), new Vector2(12,31), new Vector2(17,37), new Vector2(17,18)
        };
        private static readonly int[] PointingTriangles = Triangulate(Pointing);
        private static readonly int[] GrabTriangles = Triangulate(Grab);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var shape = grabbing ? Grab : Pointing;
            var triangles = grabbing ? GrabTriangles : PointingTriangles;
            var shadow = new Color32(10, 15, 12, 110);
            var outline = new Color32(30, 35, 29, 255);
            Fill(mesh, shape, triangles, new Vector2(2, 3), shadow);
            Fill(mesh, shape, triangles, Vector2.zero, new Color32(238, 235, 214, 255));
            for (int i = 0; i < shape.Length; i++) Line(mesh, shape[i], shape[(i + 1) % shape.Length], 2.3f, outline);
            if (grabbing)
            {
                Line(mesh, new Vector2(27,20), new Vector2(27,32), 1.6f, outline);
                Line(mesh, new Vector2(37,22), new Vector2(36,33), 1.6f, outline);
                Line(mesh, new Vector2(46,28), new Vector2(44,35), 1.6f, outline);
                Line(mesh, new Vector2(17,37), new Vector2(26,42), 1.6f, outline);
                Line(mesh, new Vector2(24,54), new Vector2(44,54), 1.6f, outline);
            }
            else
            {
                Line(mesh, new Vector2(25,25), new Vector2(25,36), 1.6f, outline);
                Line(mesh, new Vector2(35,25), new Vector2(35,36), 1.6f, outline);
                Line(mesh, new Vector2(44,28), new Vector2(44,37), 1.6f, outline);
                Line(mesh, new Vector2(22,55), new Vector2(42,55), 1.6f, outline);
            }
        }

        private Vector2 Local(Vector2 p)
        {
            var hotspot = grabbing ? new Vector2(29,28) : new Vector2(20,2);
            return new Vector2(p.x - hotspot.x, hotspot.y - p.y);
        }
        private void Fill(VertexHelper mesh, Vector2[] shape, int[] triangles, Vector2 offset, Color tint)
        {
            int start = mesh.currentVertCount;
            foreach (var p in shape) mesh.AddVert(Local(p + offset), tint * color, Vector2.zero);
            for (int i = 0; i < triangles.Length; i += 3) mesh.AddTriangle(start + triangles[i], start + triangles[i + 1], start + triangles[i + 2]);
        }
        private void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint)
        {
            var a = Local(from); var b = Local(to);
            var direction = (b - a).normalized;
            var n = new Vector2(-direction.y, direction.x) * width * .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - n, tint * color, Vector2.zero);
            mesh.AddVert(a + n, tint * color, Vector2.zero);
            mesh.AddVert(b + n, tint * color, Vector2.zero);
            mesh.AddVert(b - n, tint * color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static int[] Triangulate(Vector2[] points)
        {
            var remaining = new List<int>();
            var triangles = new List<int>();
            for (int i = 0; i < points.Length; i++) remaining.Add(i);
            while (remaining.Count > 2)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count], b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (Cross(points[b] - points[a], points[c] - points[b]) <= 0) continue;
                    bool contains = false;
                    foreach (int p in remaining)
                    {
                        if (p == a || p == b || p == c) continue;
                        if (Cross(points[b] - points[a], points[p] - points[a]) >= 0 &&
                            Cross(points[c] - points[b], points[p] - points[b]) >= 0 &&
                            Cross(points[a] - points[c], points[p] - points[c]) >= 0) { contains = true; break; }
                    }
                    if (contains) continue;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            return triangles.ToArray();
        }
    }
}
