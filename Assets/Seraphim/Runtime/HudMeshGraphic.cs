using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    public abstract class HudMeshGraphic : MaskableGraphic
    {
        protected static readonly Color Gold=new Color(.78f,.61f,.31f);
        protected static readonly Color Pale=new Color(.98f,.91f,.71f);
        protected override void OnPopulateMesh(VertexHelper vh) { vh.Clear(); Draw(vh); }
        protected abstract void Draw(VertexHelper vh);
        protected static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color bottom,Color top)
        {
            var start=vh.currentVertCount;
            vh.AddVert(a,bottom,Vector2.zero); vh.AddVert(b,bottom,Vector2.zero);
            vh.AddVert(c,top,Vector2.zero); vh.AddVert(d,top,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
        protected static void Box(VertexHelper vh,Rect rect,Color bottom,Color top)
        {
            Quad(vh,new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),new Vector2(rect.xMin,rect.yMax),bottom,top);
        }
        protected static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            var direction=b-a;
            if (direction.sqrMagnitude<.0001f) return;
            var normal=new Vector2(-direction.y,direction.x).normalized*width*.5f;
            Quad(vh,a-normal,b-normal,b+normal,a+normal,color,color);
        }
        protected static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            var start=vh.currentVertCount;
            vh.AddVert(a,color,Vector2.zero); vh.AddVert(b,color,Vector2.zero); vh.AddVert(c,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);
        }
        protected static void Diamond(VertexHelper vh,Vector2 center,float width,float height,Color color)
        {
            Quad(vh,center+Vector2.down*height,center+Vector2.right*width,center+Vector2.up*height,center+Vector2.left*width,color,color);
        }
        protected static void Disc(VertexHelper vh,Vector2 center,float radius,Color middle,Color edge,int segments=64)
        {
            var start=vh.currentVertCount;
            vh.AddVert(center,middle,Vector2.zero);
            for (var i=0;i<=segments;i++)
            {
                var angle=i*Mathf.PI*2/segments;
                vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,edge,Vector2.zero);
                if (i>0) vh.AddTriangle(start,start+i,start+i+1);
            }
        }
        protected static void Arc(VertexHelper vh,Vector2 center,float radius,float width,Color inside,Color outside,float start=0,float sweep=360,int segments=80)
        {
            for (var i=0;i<segments;i++)
            {
                var a=(start+sweep*i/segments)*Mathf.Deg2Rad;
                var b=(start+sweep*(i+1)/segments)*Mathf.Deg2Rad;
                var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                var q=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Quad(vh,center+p*(radius-width),center+q*(radius-width),center+q*radius,center+p*radius,inside,outside);
            }
        }
    }
}
