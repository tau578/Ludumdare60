using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace Switchboard.UI
{
    /// <summary>RCA-style plug; its top-center is the insertion/cursor hotspot.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlugGraphic : MaskableGraphic
    {
        [SerializeField] private Color sleeveColor = new Color32(166, 91, 48, 255);
        public Color SleeveColor { get => sleeveColor; set { sleeveColor = value; SetVerticesDirty(); } }
        [SerializeField] private Sprite heldSprite, insertedSprite;
        [SerializeField] private Color artworkTint = Color.white;
        private bool inserted;
        private Sprite CurrentSprite => inserted ? insertedSprite : heldSprite;
        public override Texture mainTexture => CurrentSprite != null ? CurrentSprite.texture : base.mainTexture;
        public Vector2 TailOffset => CurrentSprite != null ? new Vector2(0, inserted ? -54 : -128) : new Vector2(0, -64);

        public void SetArtwork(Sprite held, Sprite connected, Color tint)
        { heldSprite = held; insertedSprite = connected; artworkTint = tint; SetAllDirty(); }

        public void SetInserted(bool value)
        { if (inserted == value) return; inserted = value; SetAllDirty(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (CurrentSprite != null)
            {
                var sprite = CurrentSprite;
                float height = inserted ? 84 : 132;
                float width = height * sprite.rect.width / sprite.rect.height;
                // The inserted artwork includes transparent space above its socket centre.
                float top = inserted ? 30 : 0;
                Vector4 uv = DataUtility.GetOuterUV(sprite);
                Color tint = color * artworkTint;
                mesh.AddVert(new Vector2(-width / 2, top - height), tint, new Vector2(uv.x, uv.y));
                mesh.AddVert(new Vector2(-width / 2, top), tint, new Vector2(uv.x, uv.w));
                mesh.AddVert(new Vector2(width / 2, top), tint, new Vector2(uv.z, uv.w));
                mesh.AddVert(new Vector2(width / 2, top - height), tint, new Vector2(uv.z, uv.y));
                mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(2, 3, 0);
                return;
            }
            Box(mesh, -13, -52, 28, 37, new Color32(15, 18, 15, 150));
            Box(mesh, -4, -23, 8, 23, new Color32(171, 168, 139, 255));
            Box(mesh, -3, -21, 3, 20, new Color32(242, 230, 186, 255));
            Box(mesh, -11, -31, 22, 14, new Color32(145, 148, 136, 255));
            Box(mesh, -10, -29, 5, 11, new Color32(220, 221, 203, 255));
            Box(mesh, -14, -49, 28, 22, sleeveColor);
            Box(mesh, -12, -47, 4, 18, Color.Lerp(sleeveColor, Color.white, 0.3f));
            Box(mesh, 10, -49, 4, 22, Color.Lerp(sleeveColor, Color.black, 0.35f));
            Box(mesh, -9, -58, 18, 9, new Color32(52, 49, 39, 255));
            for (int i = 0; i < 3; i++) Box(mesh, -10, -51 - i * 3, 20, 1.2f, new Color32(116, 108, 85, 255));
            Box(mesh, -4, -65, 8, 8, Color.Lerp(sleeveColor, Color.black, 0.35f));
        }

        private void Box(VertexHelper mesh, float x, float y, float width, float height, Color tint)
        {
            Vector2 origin = new Vector2(rectTransform.rect.center.x, rectTransform.rect.yMax);
            int start = mesh.currentVertCount;
            Color shaded = tint * color;
            mesh.AddVert(origin + new Vector2(x, y), shaded, Vector2.zero);
            mesh.AddVert(origin + new Vector2(x, y + height), shaded, Vector2.zero);
            mesh.AddVert(origin + new Vector2(x + width, y + height), shaded, Vector2.zero);
            mesh.AddVert(origin + new Vector2(x + width, y), shaded, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
