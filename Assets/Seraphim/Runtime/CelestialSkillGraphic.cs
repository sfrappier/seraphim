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
            var active=slot<5;
            var glyph=slot==4?1:slot>=1 && slot<=3?slot+1:slot;
            var primary=glyph==1 || glyph==2?Pale:glyph==4?new Color(1,.55f,.18f):new Color(.48f,.8f,.95f);
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
            if (glyph==0)
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
            else if (glyph==1)
            {
                Diamond(vh,Vector2.zero,7,25,primary);
                Line(vh,new Vector2(-22,8),new Vector2(22,8),2,primary);
                for (var side=-1;side<=1;side+=2)
                for (var i=0;i<3;i++)
                    Triangle(vh,new Vector2(side*9,2-i*4),new Vector2(side*(29-i*3),17-i*8),new Vector2(side*15,-5-i*4),primary);
                Arc(vh,new Vector2(0,23),9,1.2f,primary,primary,0,360,40);
            }
            else if (glyph==2)
            {
                Line(vh,new Vector2(-12,-24),new Vector2(10,18),2,primary);
                Triangle(vh,new Vector2(3,11),new Vector2(15,28),new Vector2(16,7),primary);
                Line(vh,new Vector2(-20,-13),new Vector2(-4,18),1,new Color(.4f,.8f,1));
                Line(vh,new Vector2(0,-24),new Vector2(15,2),1,new Color(.4f,.8f,1));
            }
            else if (glyph==3)
            {
                Line(vh,new Vector2(14,27),new Vector2(-8,4),3,primary);
                Line(vh,new Vector2(-8,4),new Vector2(9,4),3,primary);
                Line(vh,new Vector2(9,4),new Vector2(-15,-25),3,primary);
                Arc(vh,new Vector2(-20,15),4,1,primary,primary);
                Arc(vh,new Vector2(23,-12),4,1,primary,primary);
            }
            else
            {
                Arc(vh,new Vector2(0,-8),21,2,primary,primary,195,150,35);
                Triangle(vh,new Vector2(-15,-17),new Vector2(-6,20),new Vector2(2,-14),primary);
                Triangle(vh,new Vector2(-4,-17),new Vector2(6,29),new Vector2(15,-17),primary);
                Triangle(vh,new Vector2(7,-16),new Vector2(21,12),new Vector2(23,-17),primary);
            }
            Arc(vh,Vector2.zero,34,1,new Color(.15f,.2f,.25f),new Color(.15f,.2f,.25f));
            Arc(vh,Vector2.zero,34,1.7f,Gold,Pale,90,360*(1-Mathf.Clamp01(cooldown)));
            if (cooldown>0) Box(vh,rect,new Color(.005f,.01f,.02f,.22f),new Color(.005f,.01f,.02f,.15f));
        }
    }
}
