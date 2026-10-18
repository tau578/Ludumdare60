using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>An untextured, soft ring outside the socket face.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SocketHaloGraphic : MaskableGraphic
    {
        private const int Segments = 64;
        private static readonly float[] Radii = { .435f, .455f, .48f, .535f };
        private static readonly float[] Opacity = { 0f, .8f, .4f, 0f };
        private float intensity = 1f;
        public float Intensity { get => intensity; set { if (Mathf.Approximately(intensity, value)) return; intensity = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float diameter = Mathf.Min(rect.width, rect.height);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (int band = 0; band < Radii.Length; band++)
                {
                    Color tint = color;
                    tint.a *= Mathf.Clamp01(Opacity[band] * intensity);
                    mesh.AddVert(rect.center + direction * (diameter * Radii[band]), tint, Vector2.zero);
                    if (i == 0 || band == 0) continue;
                    int current = i * Radii.Length + band;
                    int previous = current - Radii.Length;
                    mesh.AddTriangle(previous - 1, previous, current - 1);
                    mesh.AddTriangle(current - 1, previous, current);
                }
            }
        }
    }
}
