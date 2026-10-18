using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>A small gold UI coin; no imported texture or raycast surface is needed.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IncomeCoinGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            Disc(mesh, radius, new Color32(126, 75, 27, 255));
            Disc(mesh, radius * .87f, new Color32(255, 218, 112, 255));
            Disc(mesh, radius * .65f, new Color32(212, 146, 49, 255));
            // A raised central glint keeps the silhouette legible while it spins.
            int start = mesh.currentVertCount;
            Color glint = new Color32(255, 230, 148, 255);
            mesh.AddVert(new Vector2(-radius * .12f, -radius * .45f), glint, Vector2.zero);
            mesh.AddVert(new Vector2(-radius * .12f, radius * .45f), glint, Vector2.zero);
            mesh.AddVert(new Vector2(radius * .12f, radius * .45f), glint, Vector2.zero);
            mesh.AddVert(new Vector2(radius * .12f, -radius * .45f), glint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        private static void Disc(VertexHelper mesh, float radius, Color tint)
        {
            const int segments = 32;
            int start = mesh.currentVertCount;
            mesh.AddVert(Vector2.zero, tint, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
