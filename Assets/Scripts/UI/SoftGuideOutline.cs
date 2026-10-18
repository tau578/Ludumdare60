using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Soft silhouette glow with a bright inner edge and a transparent outer falloff.</summary>
    public sealed class SoftGuideOutline : BaseMeshEffect
    {
        private const int Samples = 24;
        private static readonly float[] Radii = { 3f, 6f, 10f, 15f };
        private static readonly float[] Opacities = { .10f, .07f, .035f, .012f };
        private readonly List<UIVertex> source = new List<UIVertex>();
        private readonly List<UIVertex> glow = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            source.Clear();
            glow.Clear();
            mesh.GetUIVertexStream(source);
            // Many faint copies blur the alpha silhouette without a hard offset border.
            // Geometry extends outside the image rect so the outer glow is not clipped.
            for (int ring = Radii.Length - 1; ring >= 0; ring--)
            for (int sample = 0; sample < Samples; sample++)
            {
                float angle = (sample + ring * .5f) * Mathf.PI * 2f / Samples;
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Radii[ring];
                foreach (var original in source)
                {
                    var vertex = original;
                    vertex.position += offset;
                    var tint = vertex.color;
                    tint.a = (byte)Mathf.RoundToInt(tint.a * Opacities[ring]);
                    vertex.color = tint;
                    glow.Add(vertex);
                }
            }
            glow.AddRange(source);
            mesh.Clear();
            mesh.AddUIVertexTriangleStream(glow);
        }
    }
}
