using UnityEngine;

namespace Seraphim
{
    public static class ResourcePalette
    {
        public static readonly Color LifeGold=new Color(.94f,.69f,.24f);
        public static readonly Color LifeLight=new Color(1,.91f,.57f);
        public static readonly Color SpiritSilver=new Color(.67f,.72f,.79f);
        public static readonly Color SpiritLight=new Color(.97f,.98f,1);
        public static Color Main(bool spirit) => spirit?SpiritSilver:LifeGold;
        public static Color Light(bool spirit) => spirit?SpiritLight:LifeLight;
        public static Color Glow(bool spirit) { var color=Main(spirit)*2; color.a=1; return color; }
    }
}
