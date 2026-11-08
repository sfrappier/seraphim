using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class SanctuaryHud : MonoBehaviour
    {
        static SanctuaryHud instance;
        SanctuaryGame game;
        Canvas canvas;
        RectTransform bottom,objective,mapPanel,headerPanel;
        CelestialOrbGraphic life,spirit;
        readonly CelestialSkillGraphic[] icons=new CelestialSkillGraphic[6];
        readonly Text[] timers=new Text[6];
        readonly Text[] padLabels=new Text[6];
        Text controls,aimHint;
        Text lifeValue,spiritValue,objectiveText,ultimatePhase,combatStatus,buffStatus,waveStatus,pickupStatus;
        public CathedralMinimap Minimap { get; private set; }
        CanvasGroup ultimateBanner;
        Font displayFont,bodyFont;
        float nextRefresh;
        readonly Color gold=new Color(.91f,.77f,.48f);
        readonly Color ivory=new Color(.94f,.94f,.88f);
        readonly Color muted=new Color(.52f,.62f,.7f);
        public bool HasResourceOrbs => life!=null && spirit!=null;
        public int SkillSlotCount => icons.Length;
        public Canvas HudCanvas => canvas;
        public void SetVisible(bool visible) { canvas.enabled=visible; }

        public static bool BlocksScreenPoint(Vector2 position)
        {
            if (instance==null || instance.canvas==null) return false;
            if (instance.game.FrontEnd!=null && instance.game.FrontEnd.BlocksWorldPointer) return true;
            var camera=instance.canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(instance.bottom,position,camera)
                || RectTransformUtility.RectangleContainsScreenPoint(instance.objective,position,camera)
                || RectTransformUtility.RectangleContainsScreenPoint(instance.mapPanel,position,camera)
                || RectTransformUtility.RectangleContainsScreenPoint(instance.headerPanel,position,camera);
        }

        public void Initialize(SanctuaryGame owner)
        {
            instance=this;
            game=owner;
            displayFont=game.models.hudDisplayFont;
            bodyFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (displayFont==null) displayFont=bodyFont;
            canvas=new GameObject("Angelic HUD",typeof(RectTransform)).AddComponent<Canvas>();
            canvas.transform.SetParent(transform,false);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=Camera.main;
            canvas.planeDistance=1;
            canvas.sortingOrder=100;
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900);
            scaler.matchWidthOrHeight=0;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("HUD input").AddComponent<EventSystem>();
                events.transform.SetParent(transform,false);
                events.sendNavigationEvents=false;
                events.gameObject.AddComponent<StandaloneInputModule>();
            }
            BuildHeader(); BuildBottom(); BuildUltimateBanner(); Refresh();
        }

        RectTransform Node(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var node=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent,false);
            node.anchorMin=node.anchorMax=anchor; node.anchoredPosition=position; node.sizeDelta=size;
            return node;
        }
        Text Label(Transform parent,string name,string text,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,Color color,bool heading=false,TextAnchor alignment=TextAnchor.MiddleCenter)
        {
            var label=Node(parent,name,anchor,position,size).gameObject.AddComponent<Text>();
            label.font=heading?displayFont:bodyFont; label.text=text; label.fontSize=fontSize; label.color=color; label.alignment=alignment;
            label.raycastTarget=false; label.horizontalOverflow=HorizontalWrapMode.Overflow; label.verticalOverflow=VerticalWrapMode.Overflow; label.supportRichText=false;
            var shadow=label.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.8f); shadow.effectDistance=new Vector2(0,-1);
            return label;
        }

        void BuildHeader()
        {
            var header=Node(canvas.transform,"Readout backdrop",new Vector2(0,1),new Vector2(230,-112),new Vector2(444,218));
            headerPanel=header;
            var headerImage=header.gameObject.AddComponent<Image>(); headerImage.color=new Color(.02f,.035f,.06f,.82f); headerImage.raycastTarget=false;
            Label(canvas.transform,"Title","SERAPHIM",new Vector2(0,1),new Vector2(230,-37),new Vector2(380,60),32,gold,true,TextAnchor.MiddleLeft);
            Label(canvas.transform,"Location","CATHEDRAL OF DAWN",new Vector2(0,1),new Vector2(217,-73),new Vector2(350,25),12,ivory,false,TextAnchor.MiddleLeft);
            Label(canvas.transform,"Movement hint","WASD / CLICK / LEFT STICK    ·    SCROLL TO ZOOM",new Vector2(0,1),new Vector2(262,-99),new Vector2(440,20),11,muted,false,TextAnchor.MiddleLeft);
            waveStatus=Label(canvas.transform,"Wave","",new Vector2(0,1),new Vector2(262,-124),new Vector2(440,22),14,gold,false,TextAnchor.MiddleLeft);
            combatStatus=Label(canvas.transform,"Encounter","",new Vector2(0,1),new Vector2(262,-149),new Vector2(440,22),13,ivory,false,TextAnchor.MiddleLeft);
            aimHint=Label(canvas.transform,"Aim hint","AIM SPEARS AND LIGHTNING WITH THE MOUSE",new Vector2(0,1),new Vector2(262,-173),new Vector2(440,20),10,muted,false,TextAnchor.MiddleLeft);
            pickupStatus=Label(canvas.transform,"Pickup","",new Vector2(0,1),new Vector2(262,-197),new Vector2(440,22),14,gold,false,TextAnchor.MiddleLeft);
            buffStatus=Label(canvas.transform,"Ascension duration","",new Vector2(.5f,1),new Vector2(0,-35),new Vector2(500,32),15,gold,true);
            mapPanel=Node(canvas.transform,"Minimap panel",new Vector2(1,1),new Vector2(-153,-176),new Vector2(270,310));
            var mapBackdrop=mapPanel.gameObject.AddComponent<Image>(); mapBackdrop.color=new Color(.02f,.035f,.06f,.9f); mapBackdrop.raycastTarget=false;
            Label(mapPanel,"Map title","CATHEDRAL OF DAWN",new Vector2(.5f,1),new Vector2(0,-12),new Vector2(260,24),12,gold,true);
            var map=Node(mapPanel,"Landscape and combatants",new Vector2(.5f,.5f),new Vector2(0,-2),new Vector2(244,254));
            Minimap=map.gameObject.AddComponent<CathedralMinimap>(); Minimap.Initialize(game);
            Label(mapPanel,"Map legend","N ↑   ·   YOU ◆   ·   DEMONS ●",new Vector2(.5f,0),new Vector2(0,10),new Vector2(260,22),10,muted);
            objective=Node(canvas.transform,"Quest",new Vector2(1,1),new Vector2(-153,-372),new Vector2(270,72));
            var panel=objective.gameObject.AddComponent<Image>(); panel.color=new Color(.025f,.045f,.067f,.84f); panel.raycastTarget=false;
            Label(objective,"Quest title","DEFEND THE CATHEDRAL",new Vector2(.5f,.5f),new Vector2(0,19),new Vector2(260,25),12,gold,true);
            objectiveText=Label(objective,"Quest progress","",new Vector2(.5f,.5f),new Vector2(0,-12),new Vector2(260,35),12,ivory);
        }

        void BuildBottom()
        {
            bottom=Node(canvas.transform,"Bottom HUD",new Vector2(.5f,0),new Vector2(0,103),new Vector2(1600,206));
            bottom.anchorMin=new Vector2(0,0); bottom.anchorMax=new Vector2(1,0); bottom.sizeDelta=new Vector2(0,206);
            var backdrop=bottom.gameObject.AddComponent<CelestialFrameGraphic>(); backdrop.fullWidth=true; backdrop.raycastTarget=false;
            var plate=Node(bottom,"Gilded skill frame",new Vector2(.5f,0),new Vector2(0,92),new Vector2(790,167));
            plate.gameObject.AddComponent<CelestialFrameGraphic>().raycastTarget=false;
            Label(plate,"Class","D A W N K E E P E R",new Vector2(.5f,1),new Vector2(0,-18),new Vector2(350,20),10,gold);
            for (var i=0;i<6;i++)
            {
                var slot=i;
                var holder=Node(plate,"Skill "+(i+1),new Vector2(.5f,0),new Vector2((i-2.5f)*112,75),new Vector2(100,92));
                icons[i]=holder.gameObject.AddComponent<CelestialSkillGraphic>(); icons[i].slot=i; icons[i].raycastTarget=false;
                var hit=Node(holder,"Cast skill",new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,92));
                var image=hit.gameObject.AddComponent<Image>(); image.color=new Color(1,1,1,.005f);
                var button=hit.gameObject.AddComponent<Button>(); button.targetGraphic=image; button.interactable=game.Skills.IsAssigned(i);
                button.onClick.AddListener(()=>game.Skills.TryUse(slot));
                Label(holder,"Key",(i+1).ToString(),new Vector2(.5f,.5f),new Vector2(-35,32),new Vector2(22,22),13,game.Skills.IsAssigned(i)?ivory:muted);
                padLabels[i]=Label(holder,"Controller button","",new Vector2(.5f,.5f),new Vector2(9,32),new Vector2(76,19),9,gold);
                Label(holder,"Skill name",game.Skills.Name(i),new Vector2(.5f,.5f),new Vector2(0,-58),new Vector2(112,22),12,game.Skills.IsAssigned(i)?ivory:muted);
                Label(holder,"Cost",game.Skills.CostLabel(i),new Vector2(.5f,.5f),new Vector2(0,-33),new Vector2(96,16),9,i==4?gold:muted);
                timers[i]=Label(holder,"Cooldown","",new Vector2(.5f,.5f),Vector2.zero,new Vector2(90,40),24,ivory);
            }
            var left=Node(bottom,"Life orb",new Vector2(0,0),new Vector2(112,103),new Vector2(182,182));
            life=left.gameObject.AddComponent<CelestialOrbGraphic>(); life.raycastTarget=false;
            Label(left,"Life label","LIFE",new Vector2(.5f,.5f),new Vector2(0,93),new Vector2(150,25),14,gold,true);
            lifeValue=Label(left,"Life amount","",new Vector2(.5f,.5f),new Vector2(0,-88),new Vector2(150,25),14,ivory);
            var right=Node(bottom,"Spirit orb",new Vector2(1,0),new Vector2(-112,103),new Vector2(182,182));
            spirit=right.gameObject.AddComponent<CelestialOrbGraphic>(); spirit.spirit=true; spirit.raycastTarget=false;
            Label(right,"Spirit label","SPIRIT",new Vector2(.5f,.5f),new Vector2(0,93),new Vector2(150,25),14,new Color(.64f,.85f,.96f),true);
            spiritValue=Label(right,"Spirit amount","",new Vector2(.5f,.5f),new Vector2(0,-88),new Vector2(150,25),14,ivory);
            controls=Label(bottom,"Control hint","",new Vector2(.5f,0),new Vector2(0,10),new Vector2(900,18),10,muted);
        }

        void BuildUltimateBanner()
        {
            var banner=Node(canvas.transform,"Word of God announcement",new Vector2(.5f,.75f),Vector2.zero,new Vector2(700,110));
            ultimateBanner=banner.gameObject.AddComponent<CanvasGroup>();
            ultimateBanner.blocksRaycasts=false; ultimateBanner.interactable=false; ultimateBanner.alpha=0;
            Label(banner,"Ultimate title","WORD OF GOD",new Vector2(.5f,.5f),new Vector2(0,10),new Vector2(700,60),38,new Color(1,.92f,.67f),true);
            ultimatePhase=Label(banner,"Ultimate phase","",new Vector2(.5f,.5f),new Vector2(0,-34),new Vector2(700,25),13,ivory);
        }

        void Refresh()
        {
            life.Fraction=game.Vitals.LifeFraction; spirit.Fraction=game.Vitals.SpiritFraction;
            lifeValue.text=$"{Mathf.CeilToInt(game.Vitals.Life)} / {Mathf.CeilToInt(game.Vitals.MaxLife)}";
            spiritValue.text=$"{Mathf.CeilToInt(game.Vitals.Spirit)} / {Mathf.CeilToInt(game.Vitals.MaxSpirit)}";
            var wave=game.Waves;
            objectiveText.text=wave.State==WaveDirector.EncounterState.Victory?"All ten waves defeated!\nR / Options / Start to replay":"Survive ten waves\nWaystones awakened   "+game.AttunedCount+" / 3";
            waveStatus.text=wave.State==WaveDirector.EncounterState.Victory?"VICTORY · CATHEDRAL DEFENDED":wave.State==WaveDirector.EncounterState.Resting?"WAVE "+(wave.Wave+1)+" / 10   IN "+Mathf.CeilToInt(wave.RestRemaining)+"s":wave.State==WaveDirector.EncounterState.Fighting?"WAVE "+wave.Wave+" / 10    ·    "+wave.Slain+" / "+wave.Total+" BANISHED":"THE CATHEDRAL AWAITS";
            if (wave.ReinforcementRemaining>0 && game.Enemies.ActiveCount==0)
                waveStatus.text="WAVE "+wave.Wave+" · REINFORCEMENTS IN "+Mathf.CeilToInt(wave.ReinforcementRemaining)+"s";
            combatStatus.text=Time.time-game.Enemies.LastRecoveryAt<3?"Grace restores you. This wave will retry.":"SCORE  "+game.Enemies.Score+"       DEMONS  "+game.Enemies.ActiveCount+" / "+(wave.State==WaveDirector.EncounterState.Fighting?wave.ConcurrentLimit:game.settings.enemyLimit);
            pickupStatus.text=Time.time-game.Drops.LastPickupAt<2?game.Drops.LastPickup:"";
            buffStatus.text=game.Ultimate.IsActive && game.Ultimate.Stage!=WordOfGodAbility.Phase.Charging?"WORD OF GOD  ·  "+game.Ultimate.ActiveRemaining.ToString("0.0")+"s":"";
            controls.text=game.Gamepads.PreferController?game.Gamepads.ControlHint:"1  RADIANCE     2  LANCE     3  CHAIN     4  INFERNO     5  WORD OF GOD";
            aimHint.text=game.Gamepads.PreferController?"RIGHT STICK AIM · AUTO-TARGET WHEN CENTERED":game.Gamepads.ConnectedCount>0?"MOUSE / RIGHT STICK AIM · "+game.Gamepads.ConnectedCount+" GAMEPAD(S) READY":"AIM SPEARS AND LIGHTNING WITH THE MOUSE";
            for (var i=0;i<icons.Length;i++)
            {
                var remaining=game.Skills.Remaining(i);
                icons[i].Cooldown=remaining/game.Skills.Duration(i);
                timers[i].text=remaining<=0?"":remaining.ToString(remaining>=10?"0":"0.0");
                padLabels[i].text=game.Gamepads.ConnectedCount>0 && game.Skills.IsAssigned(i)?game.Gamepads.BindingLabel(i):"";
            }
            ultimateBanner.alpha=game.Ultimate.IsActive?Mathf.Min(1,game.Ultimate.VisualPower*2)*Mathf.Clamp01(1-(game.Ultimate.Elapsed-2.2f)/.6f):0;
            ultimatePhase.text=game.Ultimate.Stage==WordOfGodAbility.Phase.Charging?"GATHERING THE LIGHT":"LET THERE BE LIGHT";
        }
        void Update()
        {
            if (game==null || Time.unscaledTime<nextRefresh) return;
            nextRefresh=Time.unscaledTime+.05f; Refresh();
        }
        void OnDestroy() { if (instance==this) instance=null; }
    }
}
