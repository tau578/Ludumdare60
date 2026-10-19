using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>A broad, feathered pool of light behind the glass, without a bright ring.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LampGlowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            const int segments = 64;
            const int bands = 12;
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            for (int band = 0; band <= bands; band++)
            {
                float t = band / (float)bands;
                Color tint = color;
                tint.a *= 1f - Mathf.SmoothStep(0f, 1f, t);
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * t),
                        tint, Vector2.zero);
                    if (band == 0 || i == 0) continue;
                    int current = band * (segments + 1) + i;
                    int previous = current - segments - 1;
                    mesh.AddTriangle(previous - 1, current - 1, previous);
                    mesh.AddTriangle(previous, current - 1, current);
                }
            }
        }
    }
}
