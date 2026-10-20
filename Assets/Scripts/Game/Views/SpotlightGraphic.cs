using System;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// The tutorial's dark layer (D57): one quad over its whole rect with up to <see cref="MaxHoles"/> soft rounded holes,
    /// drawn by the TutorialSpotlight shader. The tutorial is mandatory (fix 1): while <see cref="Blocking"/>, it is the
    /// top raycast hit everywhere except its click areas, so a click there reaches nothing beneath it. It only reports
    /// the press (<see cref="Pressed"/>), which some steps take as "got it" (fix 2).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpotlightGraphic : Graphic, ICanvasRaycastFilter, IPointerBlocker, IPointerDownHandler
    {
        public const int MaxHoles = 8;
        public const int MaxClickAreas = 8;
        private static readonly int HolesId = Shader.PropertyToID("_Holes");
        private static readonly int ShapesId = Shader.PropertyToID("_HoleShapes");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

        [SerializeField] private Shader shader;
        [SerializeField, Min(1)] private float softness = 28f;
        private readonly Vector4[] holes = new Vector4[MaxHoles];
        private readonly Vector4[] shapes = new Vector4[MaxHoles];
        private readonly Rect[] clickAreas = new Rect[MaxClickAreas];
        private readonly bool[] clickAreaRound = new bool[MaxClickAreas];
        private Material runtimeMaterial;
        private bool holesDirty = true;

        /// <summary>Whether clicks outside the click areas are swallowed; off as soon as the guide hides or is skipped.</summary>
        public bool Blocking { get; set; }

        /// <summary>A press this layer swallowed: outside every click area, while blocking.</summary>
        public event Action Pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Blocking) Pressed?.Invoke();
        }
        public int ClickAreaCount { get; private set; }
        public Rect ClickArea(int index) => clickAreas[index];

        public void ClearClickAreas() => ClickAreaCount = 0;

        /// <param name="rect">In this graphic's local units; a round area is the ellipse inside it.</param>
        public void AddClickArea(Rect rect, bool round)
        {
            if (ClickAreaCount >= MaxClickAreas) return;
            clickAreas[ClickAreaCount] = rect;
            clickAreaRound[ClickAreaCount++] = round;
        }

        /// <summary>Whether a click at this local point is swallowed.</summary>
        public bool BlocksLocal(Vector2 local)
        {
            if (!Blocking) return false;
            for (int i = 0; i < ClickAreaCount; i++)
            {
                Rect area = clickAreas[i];
                if (!clickAreaRound[i])
                {
                    if (area.Contains(local)) return false;
                    continue;
                }
                Vector2 offset = local - area.center;
                float rx = area.width * .5f, ry = area.height * .5f;
                if (rx > 0 && ry > 0 && (offset.x * offset.x) / (rx * rx) + (offset.y * offset.y) / (ry * ry) <= 1) return false;
            }
            return true;
        }

        // The raycast hits this layer only where it blocks; inside a click area the target beneath gets the click.
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!Blocking) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local))
                return true;
            return BlocksLocal(local);
        }

        public Shader SpotlightShader => shader;
        public bool HasMaterial => runtimeMaterial != null;

        /// <param name="rect">In this graphic's local units.</param>
        /// <param name="strength">0 closes the hole, 1 opens it fully.</param>
        public void SetHole(int index, Rect rect, float cornerRadius, float strength)
        {
            if (index < 0 || index >= MaxHoles) return;
            var hole = new Vector4(rect.center.x, rect.center.y, rect.width * .5f, rect.height * .5f);
            var shape = new Vector4(Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * .5f), Mathf.Clamp01(strength), 0, 0);
            if (hole == holes[index] && shape == shapes[index]) return;
            holes[index] = hole;
            shapes[index] = shape;
            holesDirty = true;
        }

        public Rect HoleRect(int index) => new Rect(holes[index].x - holes[index].z, holes[index].y - holes[index].w,
            holes[index].z * 2, holes[index].w * 2);
        public float HoleStrength(int index) => shapes[index].y;

        /// <summary>Creates the material on first use and sends the holes once per change.</summary>
        public void Apply()
        {
            if (runtimeMaterial == null)
            {
                if (shader == null) return;
                runtimeMaterial = new Material(shader) { name = "Tutorial spotlight", hideFlags = HideFlags.HideAndDontSave };
                material = runtimeMaterial;
                holesDirty = true;
            }
            if (!holesDirty) return;
            holesDirty = false;
            runtimeMaterial.SetFloat(SoftnessId, softness);
            runtimeMaterial.SetVectorArray(HolesId, holes);
            runtimeMaterial.SetVectorArray(ShapesId, shapes);
        }

        /// <summary>Frees the material once the guide is over; Apply makes a new one if it shows again.</summary>
        public void Release()
        {
            if (runtimeMaterial == null) return;
            material = null;
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
            runtimeMaterial = null;
        }

        protected override void OnDestroy()
        {
            Release();
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Color32 tint = color;
            // The UVs are the local position, the space the holes are given in.
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, new Vector4(rect.xMin, rect.yMin));
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, new Vector4(rect.xMin, rect.yMax));
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, new Vector4(rect.xMax, rect.yMax));
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, new Vector4(rect.xMax, rect.yMin));
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(2, 3, 0);
        }
    }
}
