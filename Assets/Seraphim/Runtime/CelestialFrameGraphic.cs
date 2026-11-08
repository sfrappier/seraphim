using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class CelestialFrameGraphic : HudMeshGraphic
    {
        public bool fullWidth;
        protected override void Draw(VertexHelper vh)
        {
            var rect=rectTransform.rect;
            if (fullWidth)
            {
                Box(vh,rect,new Color(.006f,.012f,.025f,.98f),new Color(.02f,.035f,.055f,.5f));
                Line(vh,new Vector2(rect.xMin,rect.yMin+34),new Vector2(rect.xMax,rect.yMin+34),1,new Color(.6f,.47f,.24f,.35f));
                return;
            }
            var left=rect.xMin; var right=rect.xMax;
            var bottom=rect.yMin+8; var top=rect.yMax-11;
            Box(vh,new Rect(left,bottom,rect.width,top-bottom),new Color(.015f,.027f,.045f,.98f),new Color(.045f,.065f,.083f,.98f));
            Line(vh,new Vector2(left,bottom),new Vector2(right,bottom),2,Gold);
            Line(vh,new Vector2(left+10,top),new Vector2(-22,top),1.2f,Gold);
            Line(vh,new Vector2(22,top),new Vector2(right-10,top),1.2f,Gold);
            Line(vh,new Vector2(left,bottom),new Vector2(left,top-10),1,Gold);
            Line(vh,new Vector2(right,bottom),new Vector2(right,top-10),1,Gold);
            Diamond(vh,new Vector2(0,top),9,15,Pale);
            for (var side=-1;side<=1;side+=2)
            {
                for (var i=0;i<5;i++)
                {
                    var root=new Vector2(side*(20+i*9),top-3);
                    var tip=new Vector2(side*(52+i*11),top+12-i*2.8f);
                    Triangle(vh,root,tip,new Vector2(side*(38+i*9),top-6),new Color(.7f,.58f,.34f,.8f));
                }
                Diamond(vh,new Vector2(side*(rect.width*.5f),bottom+12),4,7,Gold);
            }
        }
    }
}
