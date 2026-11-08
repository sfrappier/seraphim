using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    [DefaultExecutionOrder(200)]
    public sealed class DemonHealthBar : MonoBehaviour
    {
        DemonEnemy enemy;
        Canvas canvas;
        RectTransform root,fill,ghost;
        Text nameLabel,value,damage;
        float ghostFraction=1,lastHit=-10,hitAmount;
        public float DisplayedFraction { get; private set; }=1;
        public bool IsVisible => root!=null && root.gameObject.activeSelf;

        public void Initialize(DemonEnemy owner,Canvas hud)
        {
            enemy=owner; canvas=hud;
            root=Rect(hud.transform,"Demon health",new Vector2(118,48),Vector2.zero);
            nameLabel=Label(root,"",new Vector2(0,9),11,new Color(.95f,.82f,.65f));
            var back=Rect(root,"Health background",new Vector2(80,9),new Vector2(0,-4));
            back.gameObject.AddComponent<Image>().color=new Color(.015f,.015f,.025f,.95f);
            ghost=Rect(back,"Recent damage",new Vector2(76,5),new Vector2(-38,0)); ghost.pivot=new Vector2(0,.5f);
            ghost.gameObject.AddComponent<Image>().color=new Color(1,.75f,.25f);
            fill=Rect(back,"Remaining health",new Vector2(76,5),new Vector2(-38,0)); fill.pivot=new Vector2(0,.5f);
            fill.gameObject.AddComponent<Image>().color=new Color(.86f,.22f,.15f);
            value=Label(root,"",new Vector2(0,-17),10,new Color(.85f,.87f,.9f));
            damage=Label(root,"",new Vector2(0,29),18,new Color(1,.93f,.62f));
            foreach (var graphic in root.GetComponentsInChildren<Graphic>()) graphic.raycastTarget=false;
        }
        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=position; return rect;
        }
        static Text Label(Transform parent,string text,Vector2 position,int size,Color color)
        {
            var label=Rect(parent,"Demon readout",new Vector2(150,24),position).gameObject.AddComponent<Text>();
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=size; label.text=text;
            label.alignment=TextAnchor.MiddleCenter; label.color=color; label.horizontalOverflow=HorizontalWrapMode.Overflow;
            var shadow=label.gameObject.AddComponent<Shadow>(); shadow.effectColor=Color.black; return label;
        }
        public void ResetBar()
        {
            ghostFraction=1; lastHit=-10; hitAmount=0; nameLabel.text=enemy.DisplayName;
            root.gameObject.SetActive(true); Refresh();
        }
        public void Hit(float amount,DivineDamage source)
        {
            hitAmount=Time.time-lastHit<.15f?hitAmount+amount:amount;
            lastHit=Time.time;
            damage.text="−"+Mathf.CeilToInt(hitAmount);
            damage.color=source==DivineDamage.Chain?new Color(.55f,.9f,1):source==DivineDamage.Burning?new Color(1,.58f,.2f):new Color(1,.93f,.62f);
            Refresh();
        }
        void Refresh()
        {
            DisplayedFraction=enemy.HealthFraction;
            fill.sizeDelta=new Vector2(76*DisplayedFraction,5);
            ghost.sizeDelta=new Vector2(76*ghostFraction,5);
            value.text=Mathf.CeilToInt(enemy.Health)+" / "+Mathf.CeilToInt(enemy.MaxHealth);
        }
        public void Hide() { if (root!=null) root.gameObject.SetActive(false); }
        void LateUpdate()
        {
            if (enemy==null || !enemy.gameObject.activeInHierarchy || canvas==null || !canvas.isActiveAndEnabled || Camera.main==null) { Hide(); return; }
            var point=Camera.main.WorldToScreenPoint(enemy.transform.position+Vector3.up*enemy.BarHeight);
            var visible=point.z>0 && point.x>-60 && point.x<Screen.width+60 && point.y<Screen.height+40 && point.y>210*Screen.width/1600f
                && !SanctuaryHud.BlocksScreenPoint(new Vector2(point.x,point.y));
            root.gameObject.SetActive(visible);
            if (!visible) return;
            // Camera-space Canvas transforms are synchronized for rendering,
            // after LateUpdate. Pixel offsets avoid using their stale transform
            // while the follow camera is moving (or after a player warp).
            var center=canvas.worldCamera.pixelRect.center;
            root.anchoredPosition=(new Vector2(point.x,point.y)-center)/canvas.scaleFactor;
            if (Time.time-lastHit>.25f) ghostFraction=Mathf.MoveTowards(ghostFraction,DisplayedFraction,Time.deltaTime*1.5f);
            var age=Time.time-lastHit;
            var color=damage.color; color.a=Mathf.Clamp01(1-age/.8f); damage.color=color;
            damage.rectTransform.anchoredPosition=new Vector2(0,29+Mathf.Min(age,1)*28);
            Refresh();
        }
        void OnDestroy() { if (root!=null) Destroy(root.gameObject); }
    }
}
