using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace Switchboard.UI
{
    /// <summary>A scalable socket face drawn directly in the UI canvas.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SocketGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        private const int Segments = 64;
        [SerializeField] private Sprite faceSprite;
        public Sprite FaceSprite { get => faceSprite; set { faceSprite = value; SetAllDirty(); } }
        public override Texture mainTexture => faceSprite != null ? faceSprite.texture : base.mainTexture;
        [SerializeField, Min(1f)] private float hitRadiusScale = 1f;
        public float HitRadiusScale
        {
            get => hitRadiusScale;
            set
            {
                hitRadiusScale = Mathf.Max(1f, value);
                // Expand the rectangular raycast gate as well as the circular hit filter.
                Vector2 padding = rectTransform.rect.size * ((hitRadiusScale - 1f) * -.5f);
                raycastPadding = new Vector4(padding.x, padding.y, padding.x, padding.y);
            }
        }
        private bool highlighted;
        private bool blockedHighlight;
        private bool callConnected;
        private SocketHaloGraphic halo;
        public bool IsHighlighted => highlighted;
        public bool IsBlockedHighlight => blockedHighlight;
        public bool IsCallConnected => callConnected;

        public void SetCallConnected(bool value)
        {
            if (callConnected == value) return;
            callConnected = value;
            RefreshHalo();
        }

        public void SetHighlighted(bool value, bool blocked = false)
        {
            blocked = value && blocked;
            if (highlighted == value && blockedHighlight == blocked) return;
            highlighted = value;
            blockedHighlight = blocked;
            RefreshHalo();
        }

        private void RefreshHalo()
        {
            bool visible = isActiveAndEnabled && (callConnected || highlighted);
            if (halo == null && visible)
            {
                var child = new GameObject("Socket halo", typeof(RectTransform), typeof(CanvasRenderer));
                child.hideFlags = HideFlags.DontSave;
                child.layer = gameObject.layer;
                var rect = (RectTransform)child.transform;
                rect.SetParent(transform, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                halo = child.AddComponent<SocketHaloGraphic>();
                halo.raycastTarget = false;
            }
            if (halo == null) return;
            halo.color = blockedHighlight ? new Color32(241, 87, 70, 255)
                : callConnected ? new Color32(125, 255, 155, 255) : new Color32(245, 210, 120, 255);
            halo.Intensity = callConnected && !blockedHighlight ? 1.4f : 1f;
            halo.gameObject.SetActive(visible);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            if (faceSprite != null)
            {
                Vector2 size = faceSprite.rect.size;
                size *= Mathf.Min(rect.width, rect.height) * .88f / Mathf.Max(size.x, size.y);
                Vector2 min = center - size * .5f, max = center + size * .5f;
                Vector4 uv = DataUtility.GetOuterUV(faceSprite);
                Color fill = color;
                mesh.AddVert(new Vector3(min.x, min.y), fill, new Vector2(uv.x, uv.y));
                mesh.AddVert(new Vector3(min.x, max.y), fill, new Vector2(uv.x, uv.w));
                mesh.AddVert(new Vector3(max.x, max.y), fill, new Vector2(uv.z, uv.w));
                mesh.AddVert(new Vector3(max.x, min.y), fill, new Vector2(uv.z, uv.y));
                mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(2, 3, 0);
                return;
            }
            float radius = Mathf.Min(rect.width, rect.height) * 0.40f;

            AddDisc(mesh, center + Vector2.down * 3f, radius + 3f, new Color32(35, 36, 35, 255));
            AddDisc(mesh, center, radius, new Color32(167, 170, 163, 255));
            AddDisc(mesh, center + Vector2.up, radius * 0.86f, new Color32(216, 216, 203, 255));
            AddDisc(mesh, center, radius * 0.70f, new Color32(77, 81, 77, 255));
            AddDisc(mesh, center + Vector2.down, radius * 0.57f, new Color32(22, 26, 24, 255));
        }

        private void AddDisc(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            int start = mesh.currentVertCount;
            Color fill = tint * color;
            mesh.AddVert(center, fill, Vector2.zero);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + direction * (radius - 0.65f), fill, Vector2.zero);
                Color edge = fill;
                edge.a = 0f;
                mesh.AddVert(center + direction * (radius + 0.65f), edge, Vector2.zero);
                if (i == 0) continue;
                int current = start + 1 + i * 2;
                mesh.AddTriangle(start, current - 2, current);
                mesh.AddTriangle(current - 2, current - 1, current);
                mesh.AddTriangle(current, current - 1, current + 1);
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
                return false;
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.44f * hitRadiusScale;
            return (localPoint - rect.center).sqrMagnitude <= radius * radius;
        }

        protected override void OnDisable()
        {
            highlighted = false;
            blockedHighlight = false;
            callConnected = false;
            if (halo != null) halo.gameObject.SetActive(false);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (halo != null)
            {
                if (Application.isPlaying) Destroy(halo.gameObject);
                else DestroyImmediate(halo.gameObject);
            }
            base.OnDestroy();
        }
    }
}
