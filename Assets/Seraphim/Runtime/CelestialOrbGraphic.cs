using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class CelestialOrbGraphic : HudMeshGraphic
    {
        public bool spirit;
        float fraction=1;
        public float Fraction { get => fraction; set { var next=Mathf.Clamp01(value); if (!Mathf.Approximately(fraction,next)) { fraction=next; SetVerticesDirty(); } } }
        void Update() { if (fraction<.999f && fraction>.001f) SetVerticesDirty(); }
        protected override void Draw(VertexHelper vh)
        {
            var center=rectTransform.rect.center;
            var radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.39f;
            var sign=spirit?-1:1;
            var metal=spirit?ResourcePalette.SpiritSilver:Gold;
            var pale=spirit?ResourcePalette.SpiritLight:Pale;
            for (var i=0;i<7;i++)
            {
                var start=center+new Vector2(sign*(radius-5),20-i*7);
                var tip=center+new Vector2(sign*(radius+34+i*11),49-i*12);
                var shoulder=Vector2.Lerp(start,tip,.55f)+Vector2.up*7;
                Quad(vh,start,Vector2.Lerp(start,tip,.6f)-Vector2.up*5,tip,shoulder,spirit?new Color(.23f,.25f,.28f):new Color(.28f,.23f,.14f),pale);
                Line(vh,start,tip,.65f,metal);
            }
            Disc(vh,center+Vector2.down*3,radius+12,new Color(0,0,0,.95f),new Color(0,0,0,0));
            Arc(vh,center,radius+9,7,spirit?new Color(.18f,.2f,.24f):new Color(.19f,.16f,.12f),metal);
            Arc(vh,center,radius+3,2,pale,metal);
            Disc(vh,center,radius,new Color(.035f,.06f,.09f),new Color(.005f,.01f,.022f));
            var main=ResourcePalette.Main(spirit);
            var light=ResourcePalette.Light(spirit);
            const int rows=70;
            var surface=-1+fraction*2;
            for (var row=0;row<rows;row++)
            {
                var y0=-1+2f*row/rows;
                if (y0>=surface) break;
                var y1=Mathf.Min(-1+2f*(row+1)/rows,surface);
                var x0=Mathf.Sqrt(Mathf.Max(0,1-y0*y0));
                var x1=Mathf.Sqrt(Mathf.Max(0,1-y1*y1));
                var lower=Color.Lerp(main*.24f,light,Mathf.Clamp01((y0+1)*.5f)); lower.a=1;
                var upper=Color.Lerp(main*.24f,light,Mathf.Clamp01((y1+1)*.5f)); upper.a=1;
                Quad(vh,center+new Vector2(-x0,y0)*radius,center+new Vector2(x0,y0)*radius,
                    center+new Vector2(x1,y1)*radius,center+new Vector2(-x1,y1)*radius,lower,upper);
            }
            if (fraction>.01f && fraction<.99f)
            {
                var half=Mathf.Sqrt(Mathf.Max(0,1-surface*surface))*radius;
                for (var i=0;i<24;i++)
                {
                    var x0=Mathf.Lerp(-half,half,i/24f); var x1=Mathf.Lerp(-half,half,(i+1)/24f);
                    var y0=surface*radius+Mathf.Sin(Time.unscaledTime*1.9f+x0*.07f)*1.5f;
                    var y1=surface*radius+Mathf.Sin(Time.unscaledTime*1.9f+x1*.07f)*1.5f;
                    Line(vh,center+new Vector2(x0,y0),center+new Vector2(x1,y1),1.2f,new Color(light.r,light.g,light.b,.7f));
                }
            }
            Disc(vh,center+new Vector2(-.25f,.34f)*radius,radius*.28f,new Color(1,1,1,.24f),new Color(1,1,1,0));
            Arc(vh,center,radius-5,2,new Color(1,1,1,.45f),new Color(1,1,1,0),60,82,30);
            Arc(vh,center,radius+11,1,metal,pale,15,150,40);
            Diamond(vh,center+Vector2.up*(radius+13),4,8,pale);
            Diamond(vh,center+Vector2.down*(radius+9),3,6,metal);
        }
    }
}
