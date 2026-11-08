using UnityEngine;

namespace Seraphim
{
    public sealed class SanctuaryHud : MonoBehaviour
    {
        SanctuaryGame game;
        GUIStyle title, subtitle, body, small, number;
        readonly Color gold = new Color(.91f,.76f,.45f);
        readonly Color ivory = new Color(.9f,.93f,.96f);
        readonly Color muted = new Color(.56f,.65f,.73f);
        public void Initialize(SanctuaryGame owner) { game = owner; }

        void InitializeStyles()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title = Style(font,36,gold); title.fontStyle = FontStyle.Bold;
            subtitle = Style(font,12,muted);
            body = Style(font,17,ivory);
            small = Style(font,13,muted);
            number = Style(font,23,gold); number.alignment = TextAnchor.MiddleCenter;
        }
        static GUIStyle Style(Font font,int size,Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { font=font,fontSize=size };
            style.normal.textColor = color;
            return style;
        }
        static void Block(Rect rect, Color color)
        {
            var previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = previous;
        }
        void OnGUI()
        {
            if (game == null || game.Player == null) return;
            if (title == null) InitializeStyles();
            var scale = Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1));
            var width = Screen.width/scale;
            var height = Screen.height/scale;
            Block(new Rect(0,0,width,112),new Color(.015f,.025f,.04f,.7f));
            Block(new Rect(34,30,3,55),gold);
            GUI.Label(new Rect(53,23,360,47),"S E R A P H I M",title);
            GUI.Label(new Rect(56,76,400,22),"S A N C T U A R Y   /   T H E   F I R S T   L I G H T",subtitle);
            GUI.Label(new Rect(width-300,34,270,30),"SANCTUARY OF DAWN",body);
            GUI.Label(new Rect(width-300,65,270,24),"Windows prototype  ·  build 0.1",small);
            Block(new Rect(35,139,340,98),new Color(.025f,.045f,.065f,.86f));
            Block(new Rect(35,139,340,2),new Color(gold.r,gold.g,gold.b,.65f));
            GUI.Label(new Rect(52,151,320,24),game.AttunedCount == 3 ? "THE SANCTUARY AWAKENS" : "AWAKEN THE WAYSTONES",body);
            GUI.Label(new Rect(52,181,320,24),$"Radiance restores their light     {game.AttunedCount} / 3",small);
            Block(new Rect(0,height-110,width,110),new Color(.012f,.023f,.036f,.91f));
            Block(new Rect(0,height-110,width,1),new Color(gold.r,gold.g,gold.b,.35f));
            GUI.Label(new Rect(36,height-86,400,25),"LMB / HOLD     Move through the sanctuary",small);
            GUI.Label(new Rect(36,height-58,440,25),"SCROLL     Camera zoom        ESC     Exit",small);
            Block(new Rect(width/2-40,height-94,80,68),new Color(.1f,.16f,.19f,.95f));
            GUI.Label(new Rect(width/2-40,height-91,80,39),game.Ability.CooldownRemaining>0 ? game.Ability.CooldownRemaining.ToString("0.0") : "1",number);
            GUI.Label(new Rect(width/2-29,height-52,80,24),"RADIANCE",subtitle);
            GUI.Label(new Rect(width/2+62,height-82,330,25),"SPACE / RMB",body);
            GUI.Label(new Rect(width/2+62,height-55,330,25),"Release a pulse of celestial light",small);
            GUI.Label(new Rect(width-240,height-74,230,25),"SERAPH  /  DAWNKEEPER",subtitle);
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
