using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class BossHealthHud : MonoBehaviour
    {
        SanctuaryGame game;
        CathedralBoss boss;
        GameObject root;
        RectTransform fill,ghost;
        Text title,health,hint;
        float ghostHealth=1;
        public bool Visible => root!=null && root.activeSelf;
        public float DisplayedFraction { get; private set; }
        public bool BlocksPoint(Vector2 point) => Visible && RectTransformUtility.RectangleContainsScreenPoint(root.GetComponent<RectTransform>(),point,Camera.main);
        RectTransform Node(Transform parent,string name,Vector2 size,Vector2 point)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=point; return rect;
        }
        Text Label(Transform parent,string name,Vector2 point,int size,Color color)
        {
            var label=Node(parent,name,new Vector2(700,26),point).gameObject.AddComponent<Text>();
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=size; label.color=color; label.alignment=TextAnchor.MiddleCenter;
            label.raycastTarget=false; return label;
        }
        public void Initialize(SanctuaryGame world,CathedralBoss encounter)
        {
            game=world; boss=encounter;
            var panel=Node(game.Hud.HudCanvas.transform,"Vorath boss health",new Vector2(700,112),Vector2.zero);
            panel.anchorMin=panel.anchorMax=new Vector2(.5f,1); panel.anchoredPosition=new Vector2(32,-96); root=panel.gameObject;
            var bg=root.AddComponent<Image>(); bg.color=new Color(.025f,.005f,.035f,.9f); bg.raycastTarget=false;
            title=Label(panel,"Boss name",new Vector2(0,34),20,new Color(1,.74f,.42f));
            title.font=game.models.hudDisplayFont; title.rectTransform.sizeDelta=new Vector2(700,42);
            var back=Node(panel,"Health track",new Vector2(652,15),new Vector2(0,8)); back.gameObject.AddComponent<Image>().color=new Color(.12f,.03f,.08f);
            ghost=Node(back,"Damage echo",new Vector2(648,11),new Vector2(-324,0)); ghost.pivot=new Vector2(0,.5f); ghost.gameObject.AddComponent<Image>().color=new Color(1,.63f,.2f);
            fill=Node(back,"Boss remaining health",new Vector2(648,11),new Vector2(-324,0)); fill.pivot=new Vector2(0,.5f); fill.gameObject.AddComponent<Image>().color=new Color(.75f,.13f,.56f);
            foreach (var threshold in new[]{.3f,.65f})
            {
                var mark=Node(back,"Phase boundary",new Vector2(2,19),new Vector2(-324+648*threshold,0));
                mark.gameObject.AddComponent<Image>().color=new Color(1,.88f,.57f,.9f);
            }
            health=Label(panel,"Boss health and phase",new Vector2(0,-14),12,new Color(.92f,.86f,.94f));
            hint=Label(panel,"Boss attack warning",new Vector2(0,-40),14,new Color(1,.8f,.45f));
            foreach (var graphic in panel.GetComponentsInChildren<Graphic>()) graphic.raycastTarget=false;
            root.SetActive(false);
        }
        void Update()
        {
            var visible=boss.IsAlive && game.Hud.HudCanvas.enabled; root.SetActive(visible);
            if (!visible) { ghostHealth=1; return; }
            DisplayedFraction=boss.Enemy.HealthFraction;
            ghostHealth=Mathf.MoveTowards(ghostHealth,DisplayedFraction,Time.deltaTime*.14f);
            fill.sizeDelta=new Vector2(648*DisplayedFraction,11); ghost.sizeDelta=new Vector2(648*ghostHealth,11);
            title.text=boss.Phase==3?"VORATH ASCENDANT":"VORATH, HERALD OF ASH";
            health.text=$"{Mathf.CeilToInt(boss.Enemy.Health)} / {Mathf.CeilToInt(boss.Enemy.MaxHealth)}    ·    PHASE {boss.Phase} / 3"+(boss.IsTransitioning?"   ·   WARDED":boss.Phase>=2?"   ·   HOST "+boss.Summons.ActiveCount+" / "+boss.Summons.Limit:"");
            hint.text=boss.AttackHint;
        }
    }
}
