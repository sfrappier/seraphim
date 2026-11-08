using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class CelestialSkillGraphic : HudMeshGraphic
    {
        public int slot;
        float cooldown;
        public float Cooldown { get => cooldown; set { if (!Mathf.Approximately(value,cooldown)) { cooldown=value; SetVerticesDirty(); } } }
        protected override void Draw(VertexHelper vh)
        {
            var rect=rectTransform.rect;
            var active=slot<2;
            var primary=slot==1?Pale:new Color(.48f,.8f,.95f);
            var edge=active?Gold:new Color(.26f,.29f,.32f);
            Box(vh,rect,new Color(.015f,.024f,.04f),active?new Color(.08f,.11f,.15f):new Color(.035f,.045f,.058f));
            Line(vh,new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),2,edge);
            Line(vh,new Vector2(rect.xMin,rect.yMax),new Vector2(rect.xMax,rect.yMax),1,edge);
            Line(vh,new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMin,rect.yMax),1,edge);
            Line(vh,new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),1,edge);
            if (!active)
            {
                Arc(vh,Vector2.zero,15,1,edge,edge,0,360,32);
                Diamond(vh,Vector2.zero,3,7,edge);
                return;
            }
            if (cooldown>0) primary*=.38f;
            primary.a=1;
            Disc(vh,Vector2.zero,31,new Color(primary.r,primary.g,primary.b,.15f),new Color(primary.r,primary.g,primary.b,0));
            if (slot==0)
            {
                Arc(vh,Vector2.zero,12,2,primary,primary);
                for (var i=0;i<8;i++)
                {
                    var angle=i*Mathf.PI/4;
                    var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Line(vh,direction*16,direction*(i%2==0?26:22),1.4f,primary);
                }
                Diamond(vh,Vector2.zero,4,7,primary);
            }
            else
            {
                Diamond(vh,Vector2.zero,7,25,primary);
                Line(vh,new Vector2(-22,8),new Vector2(22,8),2,primary);
                for (var side=-1;side<=1;side+=2)
                for (var i=0;i<3;i++)
                    Triangle(vh,new Vector2(side*9,2-i*4),new Vector2(side*(29-i*3),17-i*8),new Vector2(side*15,-5-i*4),primary);
                Arc(vh,new Vector2(0,23),9,1.2f,primary,primary,0,360,40);
            }
            Arc(vh,Vector2.zero,34,1,new Color(.15f,.2f,.25f),new Color(.15f,.2f,.25f));
            Arc(vh,Vector2.zero,34,1.7f,Gold,Pale,90,360*(1-Mathf.Clamp01(cooldown)));
            if (cooldown>0) Box(vh,rect,new Color(.005f,.01f,.02f,.22f),new Color(.005f,.01f,.02f,.15f));
        }
    }
}
