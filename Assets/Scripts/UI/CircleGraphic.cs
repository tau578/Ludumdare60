using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>A resolution-independent circular UI surface, also usable with a Mask.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CircleGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        private const int Segments = 96;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float innerRadius = Mathf.Max(0f, radius - 0.75f);
            Color edge = color;
            edge.a = 0f;

            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + direction * innerRadius, color, Vector2.zero);
                mesh.AddVert(center + direction * radius, edge, Vector2.zero);
                if (i == 0) continue;
                int current = 1 + i * 2;
                mesh.AddTriangle(0, current - 2, current);
                mesh.AddTriangle(current - 2, current - 1, current);
                mesh.AddTriangle(current, current - 1, current + 1);
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, eventCamera, out Vector2 localPoint)) return false;
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            return (localPoint - rect.center).sqrMagnitude <= radius * radius;
        }
    }
}
