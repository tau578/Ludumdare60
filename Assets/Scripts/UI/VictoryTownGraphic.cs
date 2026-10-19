using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Lightweight cutout scenery for the first playable victory sequence.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VictoryTownGraphic : MaskableGraphic
    {
        public enum Shape { Sky, Hills, Roads, House, Tree }
        public Shape Artwork;
        public int Variant;
        public float Reveal = 1;
        private static Color Ink(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            if (Artwork == Shape.Sky)
            {
                mesh.AddVert(new Vector2(r.xMin, r.yMin), Ink("#F2C58F"), Vector2.zero);
                mesh.AddVert(new Vector2(r.xMin, r.yMax), Ink("#849D99"), Vector2.zero);
                mesh.AddVert(new Vector2(r.xMax, r.yMax), Ink("#849D99"), Vector2.zero);
                mesh.AddVert(new Vector2(r.xMax, r.yMin), Ink("#F2C58F"), Vector2.zero);
                mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
            }
            else if (Artwork == Shape.Hills)
            {
                for (int i = 0; i < 18; i++)
                {
                    float x = r.xMin + i * r.width / 17;
                    float h = r.height * (.42f + .2f * Mathf.Sin(i * 1.7f + Variant));
                    Ellipse(mesh, new Vector2(x, r.yMin), new Vector2(r.width / 8, h), color);
                }
                Quad(mesh, r.xMin, r.yMin - 300, r.width, 300, color);
            }
            else if (Artwork == Shape.Roads)
            {
                Path(mesh, new[] { new Vector2(-830,-210), new Vector2(-520,-150), new Vector2(-180,-180),
                    new Vector2(100,-100), new Vector2(400,-140), new Vector2(830,-55) });
                Path(mesh, new[] { new Vector2(-830,100), new Vector2(-530,170), new Vector2(-200,120),
                    new Vector2(160,180), new Vector2(530,130), new Vector2(830,210) });
                Path(mesh, new[] { new Vector2(-480,-290), new Vector2(-400,-110), new Vector2(-220,40),
                    new Vector2(-200,120), new Vector2(-60,320) });
                Path(mesh, new[] { new Vector2(440,-280), new Vector2(400,-140), new Vector2(230,30),
                    new Vector2(160,180), new Vector2(250,320) });
            }
            else if (Artwork == Shape.Tree)
            {
                Quad(mesh, -3, r.yMin, 6, r.height * .55f, Ink("#5D4932"));
                Ellipse(mesh, new Vector2(0, r.yMin + r.height * .67f), new Vector2(r.width * .48f, r.height * .34f), color);
                Ellipse(mesh, new Vector2(-r.width * .13f, r.yMin + r.height * .74f),
                    new Vector2(r.width * .28f, r.height * .24f), Color.Lerp(color, Ink("#D3C78D"), .16f));
            }
            else House(mesh, r);
        }

        private void House(VertexHelper m, Rect r)
        {
            float w = r.width, h = r.height, x = r.xMin, y = r.yMin;
            Color wall = Variant % 3 == 0 ? Ink("#EBD4A3") : Variant % 3 == 1 ? Ink("#D6BB8E") : Ink("#C3CCAB");
            Color roof = Variant % 2 == 0 ? Ink("#A66344") : Ink("#6E7970");
            Ellipse(m, new Vector2(7, y + 2), new Vector2(w * .65f, 12), new Color(0.15f,.17f,.12f,.16f));
            Quad(m, x, y, w, h * .68f, wall);
            Quad(m, x + w * .79f, y, w * .21f, h * .68f, Color.Lerp(wall, Color.black, .16f));
            Triangle(m, new Vector2(x - 10,y+h*.67f), new Vector2(0,y+h), new Vector2(x+w+10,y+h*.67f), roof);
            Quad(m, x + w*.7f, y+h*.8f, 9, h*.2f, Ink("#8B644C"));
            Quad(m, -9, y, 18, h*.28f, Ink("#506558"));
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 2; col++)
                {
                    float wx=x+w*(.17f+col*.44f), wy=y+h*(.34f+row*.19f);
                    Quad(m, wx-2, wy-2, 17, 20, Ink("#8C7653"));
                    Quad(m, wx, wy, 13, 16, Ink("#F6DEA1"));
                    Quad(m, wx+6, wy, 1.5f, 16, Ink("#726E51"));
                }
            if (Variant % 4 == 0)
            {
                Quad(m, x-3, y+h*.25f, w+6, 13, Ink("#F1DBAD"));
                for (int i=0;i<5;i++) Quad(m,x+i*w/5,y+h*.25f,w/10,13,Ink("#A3664D"));
            }
        }

        private void Path(VertexHelper m, Vector2[] points)
        {
            float length = (points.Length-1) * Mathf.Clamp01(Reveal);
            for(int i=0;i<points.Length-1;i++)
            {
                float t=Mathf.Clamp01(length-i); if(t<=0) break;
                Vector2 a=points[i], b=Vector2.Lerp(a,points[i+1],t);
                Vector2 d=(b-a).normalized, n=new Vector2(-d.y,d.x)*14;
                int s=m.currentVertCount;
                foreach(var p in new[]{a-n,a+n,b+n,b-n}) m.AddVert(p,Ink("#DED1AA"),Vector2.zero);
                m.AddTriangle(s,s+1,s+2);m.AddTriangle(s,s+2,s+3);
                Ellipse(m,b,new Vector2(14,14),Ink("#DED1AA"));
            }
        }

        private static void Quad(VertexHelper m,float x,float y,float w,float h,Color c)
        {
            int s=m.currentVertCount;
            foreach(var p in new[]{new Vector2(x,y),new Vector2(x,y+h),new Vector2(x+w,y+h),new Vector2(x+w,y)})
                m.AddVert(p,c,Vector2.zero);
            m.AddTriangle(s,s+1,s+2);m.AddTriangle(s,s+2,s+3);
        }
        private static void Triangle(VertexHelper m,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            int s=m.currentVertCount; m.AddVert(a,tint,Vector2.zero);m.AddVert(b,tint,Vector2.zero);m.AddVert(c,tint,Vector2.zero);
            m.AddTriangle(s,s+1,s+2);
        }
        private static void Ellipse(VertexHelper m,Vector2 center,Vector2 radius,Color tint)
        {
            int s=m.currentVertCount;m.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<=32;i++)
            {
                float a=-i*Mathf.PI*2/32;
                m.AddVert(center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),radius),tint,Vector2.zero);
                if(i>0)m.AddTriangle(s,s+i,s+i+1);
            }
        }
    }
}
