using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PageFoldGraphic : MaskableGraphic
    {
        [SerializeField] private bool leftCorner;
        public bool LeftCorner { get => leftCorner; set { leftCorner = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r = rectTransform.rect;
            Vector2 bottom = new Vector2(r.xMin, r.yMin);
            Vector2 top = new Vector2(r.xMax, r.yMax);
            Triangle(mesh, bottom, new Vector2(r.xMax, r.yMin), top,
                new Color32(65, 65, 62, 255), new Color32(65, 65, 62, 255), new Color32(65, 65, 62, 255));
            Triangle(mesh, bottom + new Vector2(3, -2), top + new Vector2(2, -3), new Vector2(r.xMin + 7, r.yMax - 7),
                new Color32(90, 90, 84, 180), new Color32(90, 90, 84, 180), new Color32(90, 90, 84, 180));
            Triangle(mesh, bottom, top, new Vector2(r.xMin + 4, r.yMax - 4),
                new Color32(231, 228, 212, 255), new Color32(235, 232, 218, 255), new Color32(255, 253, 240, 255));
        }

        private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
        {
            if (leftCorner)
            {
                float axis = rectTransform.rect.center.x * 2;
                a.x = axis - a.x; b.x = axis - b.x; c.x = axis - c.x;
            }
            int start = mesh.currentVertCount;
            mesh.AddVert(a, ca * color, Vector2.zero);
            mesh.AddVert(b, cb * color, Vector2.zero);
            mesh.AddVert(c, cc * color, Vector2.zero);
            if (leftCorner) mesh.AddTriangle(start, start + 2, start + 1);
            else mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
