using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Cutout brick city: storefronts, cornices, fire escapes and rooftop water tanks.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VictoryTownGraphic : MaskableGraphic
    {
        public enum Shape { Sky, Skyline, Roads, Building }
        public Shape Artwork;
        public int Variant;
        public float Reveal = 1;
        private static Color Ink(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            switch (Artwork)
            {
                case Shape.Sky:
                    mesh.AddVert(new Vector2(r.xMin, r.yMin), Ink("#E9C38F"), Vector2.zero);
                    mesh.AddVert(new Vector2(r.xMin, r.yMax), Ink("#7D9697"), Vector2.zero);
                    mesh.AddVert(new Vector2(r.xMax, r.yMax), Ink("#7D9697"), Vector2.zero);
                    mesh.AddVert(new Vector2(r.xMax, r.yMin), Ink("#E9C38F"), Vector2.zero);
                    mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
                    break;
                case Shape.Skyline: Skyline(mesh, r); break;
                case Shape.Roads: Streets(mesh); break;
                case Shape.Building: Building(mesh, r); break;
            }
        }

        private void Skyline(VertexHelper m, Rect r)
        {
            Quad(m, r.xMin, r.yMin - 700, r.width, 700, color);
            for (int i = 0; i < 24; i++)
            {
                float x = r.xMin + i * r.width / 24;
                float height = 95 + (i * 53 + Variant * 31) % 175;
                float width = r.width / 24 + 2;
                Quad(m, x, r.yMin, width, height, color);
                if (i % 4 == 0)
                {
                    Quad(m, x + width * .16f, r.yMin + height, width * .68f, 18, color);
                    Quad(m, x + width * .31f, r.yMin + height + 18, width * .38f, 20, color);
                }
                else if (i % 5 == 0)
                {
                    Quad(m, x + 20, r.yMin + height, 3, 25, color);
                    Quad(m, x + 48, r.yMin + height, 3, 25, color);
                    Quad(m, x + 16, r.yMin + height + 20, 39, 26, color);
                }
            }
        }

        private void Streets(VertexHelper m)
        {
            Color pavement = Ink("#ADA28B"), asphalt = Ink("#6F7670"), rail = Ink("#BCB39D");
            float t = Mathf.Clamp01(Reveal);
            // Orthogonal avenues, sidewalks and tram rails replace winding suburban paths.
            foreach (float y in new[] { 80f, -100f, -280f })
            {
                Quad(m, -870, y - 22, 1740 * t, 48, pavement);
                Quad(m, -870, y - 14, 1740 * t, 31, asphalt);
                Quad(m, -870, y - 7, 1740 * t, 1.2f, rail);
                Quad(m, -870, y + 7, 1740 * t, 1.2f, rail);
            }
            foreach (float x in new[] { -820f, -410f, 0f, 410f, 820f })
            {
                Quad(m, x - 20, -330, 40, 660 * t, pavement);
                Quad(m, x - 12, -330, 24, 660 * t, asphalt);
            }
        }

        private void Building(VertexHelper m, Rect r)
        {
            float w = r.width, h = r.height, x = r.xMin, y = r.yMin;
            Color brick;
            switch (Variant % 5)
            {
                case 0: brick = Ink("#A36A50"); break;
                case 1: brick = Ink("#B88861"); break;
                case 2: brick = Ink("#8A5344"); break;
                case 3: brick = Ink("#B8AA88"); break;
                default: brick = Ink("#7D8070"); break;
            }
            Color stone = Ink("#D0BE96"), iron = Ink("#424B46"), glass = Ink("#F1DDA4");
            float face = w * .84f;
            Quad(m, x - 5, y - 4, w + 18, 10, new Color(.18f,.19f,.15f,.22f));
            Quad(m, x, y, w, h - 10, brick);
            Quad(m, x + face, y, w - face, h - 10, Color.Lerp(brick, Color.black, .24f));
            // Sparse mortar courses retain the small-scale readability of the existing game art.
            Color mortar = Color.Lerp(brick, stone, .24f);
            for (float by = y + 45; by < y + h - 16; by += 9)
            {
                Quad(m, x + 3, by, face - 6, .7f, mortar);
                for (int b = 0; b < 5; b++)
                    Quad(m, x + 6 + b * 26 + ((int)(by / 9) % 2) * 10, by, .6f, 8, mortar);
            }
            Quad(m, x - 4, y + h - 15, w + 8, 6, iron);
            Quad(m, x - 7, y + h - 9, w + 14, 6, stone);
            Quad(m, x - 3, y + h - 3, w + 6, 3, Color.Lerp(stone, brick, .4f));
            for (int corbel = 0; corbel < 8; corbel++)
                Quad(m, x + 4 + corbel * face / 8, y + h - 20, 5, 5, stone);

            int floors = Variant % 3 + 3;
            float floorHeight = (h - 60) / floors;
            for (int floor = 0; floor < floors; floor++)
            {
                float wy = y + 48 + floor * floorHeight;
                for (int column = 0; column < 3; column++)
                {
                    float wx = x + 12 + column * (face - 17) / 3;
                    float wh = floorHeight - 9;
                    Quad(m, wx - 2, wy - 2, 23, wh + 4, iron);
                    Quad(m, wx, wy, 19, wh, (floor + column + Variant) % 4 == 0 ? Ink("#9BA698") : glass);
                    Quad(m, wx + 8.5f, wy, 2, wh, iron);
                    Quad(m, wx, wy + wh * .52f, 19, 2, iron);
                    Quad(m, wx - 4, wy - 4, 27, 3, stone);
                    Quad(m, wx - 3, wy + wh + 2, 25, 3, stone);
                }
            }
            // Ground-floor storefront, broad display windows and a recessed entrance.
            Quad(m, x + 5, y + 4, face - 10, 31, iron);
            Quad(m, x + 10, y + 8, face * .29f, 23, Ink("#90AAA1"));
            Quad(m, x + face * .37f, y + 8, face * .28f, 23, Ink("#90AAA1"));
            Quad(m, x + face * .7f, y + 4, face * .22f, 27, Ink("#59675E"));
            Quad(m, x + face * .73f, y + 16, face * .15f, 13, glass);
            Quad(m, x + 3, y + 36, face - 6, 8, stone);
            if (Variant % 3 == 0)
            {
                Quad(m, x + 3, y + 29, face - 6, 9, Ink("#E3CCA0"));
                for (int stripe = 0; stripe < 7; stripe++)
                    Quad(m, x + 3 + stripe * (face - 6) / 7, y + 29, (face - 6) / 14, 9, Ink("#53695D"));
            }
            else
            {
                // Painted signboard and stone entrance steps.
                Quad(m, x + 8, y + 36, face - 16, 7, Ink("#59675E"));
                Quad(m, x + face * .68f, y, face * .26f, 3, stone);
            }
            if (Variant % 2 == 1) FireEscape(m, x + face - 30, y + 46, floorHeight, floors, iron);
            if (Variant % 6 == 2) WaterTank(m, x + w * .65f, y + h, iron);
            else Quad(m, x + w * .72f, y + h, 12, 14, Color.Lerp(brick, Color.black, .15f));
            if (Variant % 6 == 4)
            {
                Quad(m, x + 12, y + h, face * .55f, 12, brick);
                Quad(m, x + 24, y + h + 12, face * .35f, 10, stone);
            }
        }

        private static void FireEscape(VertexHelper m, float x, float y, float step, int floors, Color iron)
        {
            for (int floor = 0; floor < floors; floor++)
            {
                float bottom = y + floor * step;
                Quad(m, x - 5, bottom, 34, 2.5f, iron);
                Quad(m, x - 5, bottom + 9, 34, 1.5f, iron);
                for (int bar = 0; bar < 6; bar++) Quad(m, x - 5 + bar * 6, bottom, 1, 10, iron);
                if (floor < floors - 1)
                {
                    Segment(m, new Vector2(x + 2, bottom + 11), new Vector2(x + 25, bottom + step), 2, iron);
                    Segment(m, new Vector2(x + 7, bottom + 11), new Vector2(x + 30, bottom + step), 2, iron);
                }
            }
        }

        private static void WaterTank(VertexHelper m, float x, float y, Color iron)
        {
            Quad(m, x - 14, y, 3, 17, iron); Quad(m, x + 11, y, 3, 17, iron);
            Segment(m, new Vector2(x - 14,y), new Vector2(x + 14,y + 16), 2, iron);
            Quad(m, x - 20, y + 13, 40, 28, Ink("#796553"));
            Quad(m, x - 21, y + 17, 42, 2, iron); Quad(m, x - 21, y + 35, 42, 2, iron);
            int s = m.currentVertCount;
            m.AddVert(new Vector2(x - 24,y + 41), iron, Vector2.zero);
            m.AddVert(new Vector2(x,y + 51), iron, Vector2.zero);
            m.AddVert(new Vector2(x + 24,y + 41), iron, Vector2.zero);
            m.AddTriangle(s,s + 1,s + 2);
        }

        private static void Segment(VertexHelper m, Vector2 a, Vector2 b, float thickness, Color c)
        {
            Vector2 d = (b-a).normalized, n = new Vector2(-d.y,d.x) * thickness * .5f;
            int s = m.currentVertCount;
            foreach (var p in new[] { a-n, a+n, b+n, b-n }) m.AddVert(p,c,Vector2.zero);
            m.AddTriangle(s,s+1,s+2); m.AddTriangle(s,s+2,s+3);
        }

        private static void Quad(VertexHelper m, float x, float y, float w, float h, Color c)
        {
            int s = m.currentVertCount;
            foreach (var p in new[] { new Vector2(x,y),new Vector2(x,y+h),new Vector2(x+w,y+h),new Vector2(x+w,y) })
                m.AddVert(p,c,Vector2.zero);
            m.AddTriangle(s,s+1,s+2); m.AddTriangle(s,s+2,s+3);
        }
    }
}
