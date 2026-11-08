using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// A small label plate for the board's frame, like the name plates beside a real switchboard's lamps: a matte face
    /// shaded with light from above inside a thin darker rim. It is either set into the frame (recessed: a soft inner
    /// shadow under the top edge and a faint light line along the bottom, nothing outside the plate) or raised on it
    /// (a dark-brown hairline and a soft contact shadow falling down). The same box can draw only the plate's face (a
    /// wash of <see cref="Graphic.color"/>) or a soft halo round it, so the three layers line up. Every measure
    /// follows the box's height, so the plate scales as a whole. The mesh is built once; fading it only changes the
    /// renderer's alpha.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LabelStripGraphic : MaskableGraphic
    {
        public enum Layer { Plate = 0, Wash = 1, Halo = 2 }
        public enum Mount { Recessed = 0, Raised = 1 }

        // Matte brass, the frame's gold line in a plate: lighter on top, lit from above.
        public static readonly Color32 BrassTop = new Color32(198, 169, 94, 255);
        public static readonly Color32 BrassBottom = new Color32(168, 138, 66, 255);
        public static readonly Color32 BrassRim = new Color32(110, 90, 30, 255);
        // Recessed: the top edge throws a soft shadow onto the face; the bottom edge catches a little light.
        public static readonly Color32 InnerShadow = new Color32(28, 18, 6, 140);
        public static readonly Color32 InnerLight = new Color32(255, 240, 210, 46);
        // Raised: a dark-brown hairline and a soft shadow on the wood below.
        public static readonly Color32 Hairline = new Color32(51, 36, 26, 255);
        public static readonly Color32 Shadow = new Color32(36, 20, 12, 120);
        public const float CornerRatio = .09f;
        public const float RimRatio = .05f;
        public const float HairlineRatio = .025f;
        public const float InnerShadowRatio = .12f;
        public const float InnerLightRatio = .04f;
        public const float ShadowDropRatio = .06f;
        public const float ShadowSoftRatio = .12f;
        public const float HaloRatio = .18f;
        private const int CornerSegments = 5;

        [SerializeField] private Layer layer;
        [SerializeField] private Mount mount;
        [SerializeField] private Color faceTop = BrassTop;
        [SerializeField] private Color faceBottom = BrassBottom;
        [SerializeField] private Color rim = BrassRim;

        private readonly List<Vector2> outer = new List<Vector2>();
        private readonly List<Vector2> inner = new List<Vector2>();
        private readonly Vector2[] centres = new Vector2[4];

        public Layer Part
        {
            get => layer;
            set { layer = value; SetVerticesDirty(); }
        }

        public Mount Mounting
        {
            get => mount;
            set { mount = value; SetVerticesDirty(); }
        }

        public Color FaceTop
        {
            get => faceTop;
            set { faceTop = value; SetVerticesDirty(); }
        }

        public Color FaceBottom
        {
            get => faceBottom;
            set { faceBottom = value; SetVerticesDirty(); }
        }

        public Color Rim
        {
            get => rim;
            set { rim = value; SetVerticesDirty(); }
        }

        private float Height => rectTransform.rect.height;
        public float RimWidth => Height * RimRatio;
        public float InnerShadowDepth => Height * InnerShadowRatio;
        public float InnerLightDepth => Height * InnerLightRatio;
        public float ShadowDrop => Height * ShadowDropRatio;
        public float ShadowSoftness => Height * ShadowSoftRatio;
        public float HaloWidth => Height * HaloRatio;
        /// <summary>The plate's face inside the rim.</summary>
        public Rect Face => Inset(rectTransform.rect, RimWidth);
        /// <summary>Everything this layer draws, in local units: the halo and a raised plate's shadow reach past the box.</summary>
        public Rect DrawnRect
        {
            get
            {
                Rect box = rectTransform.rect;
                switch (layer)
                {
                    case Layer.Halo: return Inset(box, -HaloWidth);
                    case Layer.Wash: return Face;
                    default:
                        if (mount == Mount.Recessed) return box;
                        Rect shadow = Inset(box, -ShadowSoftness);
                        shadow.y -= ShadowDrop;
                        return Rect.MinMaxRect(shadow.xMin, shadow.yMin, shadow.xMax, box.yMax);
                }
            }
        }

        /// <summary>The face's own colour at a height (0 bottom, 1 top), before its edges' shadow and light.</summary>
        public Color FaceAt(float t) => Color.Lerp(faceBottom, faceTop, t);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect box = rectTransform.rect;
            float corner = Height * CornerRatio;
            switch (layer)
            {
                case Layer.Halo:
                    // Eased out, so it reads as light falling off rather than a border.
                    Color edge = color, faint = color, gone = color;
                    faint.a *= .3f;
                    gone.a = 0;
                    Ring(mesh, box, corner, 0, -HaloWidth * .45f, edge, edge, faint, faint, box);
                    Ring(mesh, box, corner, -HaloWidth * .45f, -HaloWidth, faint, faint, gone, gone, box);
                    Fill(mesh, box, corner, 0, edge, edge, box);
                    break;
                case Layer.Wash:
                    Fill(mesh, box, corner, RimWidth, color, color, box);
                    break;
                default:
                    if (mount == Mount.Raised) Raised(mesh, box, corner);
                    else Recessed(mesh, box, corner);
                    break;
            }
        }

        private void Raised(VertexHelper mesh, Rect box, float corner)
        {
            Rect shadowBox = box;
            shadowBox.y -= ShadowDrop;
            Color shadow = Shadow, clear = Shadow;
            clear.a = 0;
            Fill(mesh, shadowBox, corner, 0, shadow, shadow, box);
            Ring(mesh, shadowBox, corner, 0, -ShadowSoftness, shadow, shadow, clear, clear, box);
            float hairline = Height * HairlineRatio;
            Ring(mesh, box, corner, 0, hairline, Hairline, Hairline, Hairline, Hairline, box);
            Ring(mesh, box, corner, hairline, RimWidth, rim, rim, rim, rim, box);
            Fill(mesh, box, corner, RimWidth, faceBottom, faceTop, box);
        }

        // Set into the frame: the edges' shadow and light lie on the face, inside the rim, and nothing falls outside.
        private void Recessed(VertexHelper mesh, Rect box, float corner)
        {
            Ring(mesh, box, corner, 0, RimWidth, rim, rim, rim, rim, box);
            Fill(mesh, box, corner, RimWidth, faceBottom, faceTop, box);
            Rect face = Face;
            Color shadow = InnerShadow, shadowGone = InnerShadow;
            shadowGone.a = 0;
            Band(mesh, face.xMin, face.xMax, face.yMax - InnerShadowDepth, face.yMax, shadowGone, shadow);
            Color light = InnerLight;
            Band(mesh, face.xMin, face.xMax, face.yMin, face.yMin + InnerLightDepth, light, light);
        }

        private static Rect Inset(Rect rect, float by) =>
            Rect.MinMaxRect(rect.xMin + by, rect.yMin + by, rect.xMax - by, rect.yMax - by);

        // A rounded rectangle's outline, inset by `by` (negative grows it), its corners staying concentric.
        private void Outline(List<Vector2> points, Rect box, float corner, float by)
        {
            points.Clear();
            Rect rect = Inset(box, by);
            float radius = Mathf.Max(0, corner - by);
            centres[0] = new Vector2(rect.xMax - radius, rect.yMax - radius);
            centres[1] = new Vector2(rect.xMin + radius, rect.yMax - radius);
            centres[2] = new Vector2(rect.xMin + radius, rect.yMin + radius);
            centres[3] = new Vector2(rect.xMax - radius, rect.yMin + radius);
            for (int c = 0; c < 4; c++)
            for (int i = 0; i <= CornerSegments; i++)
            {
                float angle = (c + i / (float)CornerSegments) * Mathf.PI * .5f;
                points.Add(centres[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        // Colours run from bottom to top of `shade`, so the light comes from above.
        private static Color Shade(Vector2 at, Rect shade, Color bottom, Color top) =>
            Color.Lerp(bottom, top, Mathf.InverseLerp(shade.yMin, shade.yMax, at.y));

        private static void Band(VertexHelper mesh, float xMin, float xMax, float yMin, float yMax, Color bottom, Color top)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(xMin, yMin), bottom, Vector4.zero);
            mesh.AddVert(new Vector3(xMin, yMax), top, Vector4.zero);
            mesh.AddVert(new Vector3(xMax, yMax), top, Vector4.zero);
            mesh.AddVert(new Vector3(xMax, yMin), bottom, Vector4.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        private void Fill(VertexHelper mesh, Rect box, float corner, float by, Color bottom, Color top, Rect shade)
        {
            Outline(outer, box, corner, by);
            int start = mesh.currentVertCount;
            Vector2 centre = Inset(box, by).center;
            mesh.AddVert(centre, Shade(centre, shade, bottom, top), Vector4.zero);
            for (int i = 0; i < outer.Count; i++) mesh.AddVert(outer[i], Shade(outer[i], shade, bottom, top), Vector4.zero);
            for (int i = 0; i < outer.Count; i++)
                mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % outer.Count);
        }

        private void Ring(VertexHelper mesh, Rect box, float corner, float from, float to, Color fromBottom,
            Color fromTop, Color toBottom, Color toTop, Rect shade)
        {
            Outline(outer, box, corner, from);
            Outline(inner, box, corner, to);
            int start = mesh.currentVertCount, count = outer.Count;
            for (int i = 0; i < count; i++)
            {
                mesh.AddVert(outer[i], Shade(outer[i], shade, fromBottom, fromTop), Vector4.zero);
                mesh.AddVert(inner[i], Shade(inner[i], shade, toBottom, toTop), Vector4.zero);
            }
            for (int i = 0; i < count; i++)
            {
                int a = start + i * 2, b = start + (i + 1) % count * 2;
                mesh.AddTriangle(a, a + 1, b);
                mesh.AddTriangle(b, a + 1, b + 1);
            }
        }
    }
}
