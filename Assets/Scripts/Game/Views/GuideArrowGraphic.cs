using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>A flat arrow pointing along its rect's +x axis: a shaft and a head, tip at the right edge. Any length.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GuideArrowGraphic : MaskableGraphic
    {
        [SerializeField, Range(.1f, 1)] private float shaftThickness = .34f;
        [SerializeField, Min(1)] private float headSize = 26f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float midY = rect.center.y;
            float halfShaft = rect.height * shaftThickness * .5f;
            float headStart = rect.xMax - Mathf.Min(headSize, rect.width * .8f);
            Color32 tint = color;
            mesh.AddVert(new Vector3(rect.xMin, midY - halfShaft), tint, Vector4.zero);
            mesh.AddVert(new Vector3(rect.xMin, midY + halfShaft), tint, Vector4.zero);
            mesh.AddVert(new Vector3(headStart, midY + halfShaft), tint, Vector4.zero);
            mesh.AddVert(new Vector3(headStart, midY - halfShaft), tint, Vector4.zero);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(2, 3, 0);
            mesh.AddVert(new Vector3(headStart, rect.yMin), tint, Vector4.zero);
            mesh.AddVert(new Vector3(headStart, rect.yMax), tint, Vector4.zero);
            mesh.AddVert(new Vector3(rect.xMax, midY), tint, Vector4.zero);
            mesh.AddTriangle(4, 5, 6);
        }
    }
}
