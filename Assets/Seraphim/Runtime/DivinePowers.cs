using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    public sealed class DivinePowers : MonoBehaviour
    {
        sealed class Bolt
        {
            public LineRenderer outer,inner;
            public Vector3 from,to;
            public float age;
        }
        sealed class FireWave
        {
            public GameObject root;
            public LineRenderer inner,outer;
            public readonly List<LineRenderer> flames=new List<LineRenderer>();
            public ParticleSystem particles;
            public readonly HashSet<int> hit=new HashSet<int>();
            public float age,emission;
        }
        SanctuaryGame game;
        GameObject effectsRoot;
        ParticleSystem sparks;
        Material arcMaterial,fireMaterial;
        public Material WhiteMaterial { get; private set; }
        readonly float[] readyAt=new float[3];
        readonly List<DivineProjectile> projectiles=new List<DivineProjectile>();
        readonly List<Bolt> bolts=new List<Bolt>();
        readonly List<FireWave> waves=new List<FireWave>();
        readonly HashSet<int> chainHits=new HashSet<int>();
        public int LanceCasts { get; private set; }
        public int ChainCasts { get; private set; }
        public int InfernoCasts { get; private set; }
        public float Remaining(int index) => index>=0 && index<3?Mathf.Max(0,readyAt[index]-Time.time):0;
        public void Initialize(SanctuaryGame owner)
        {
            game=owner;
            effectsRoot=new GameObject("Pooled divine weapons");
            WhiteMaterial=new Material(game.models.glowGold); WhiteMaterial.SetColor("_BaseColor",new Color(3,3,3,1));
            arcMaterial=new Material(game.models.glowCyan); arcMaterial.SetColor("_BaseColor",new Color(.35f,2.4f,4,1));
            fireMaterial=new Material(game.models.glowGold); fireMaterial.SetColor("_BaseColor",new Color(4,1.1f,.1f,1));
            sparks=CombatArt.Motes(effectsRoot.transform,game.models.celestialMote,500,new Color(.7f,.9f,1),5,.65f);
        }
        public float Cost(int index) => index==0?game.settings.lanceCost:index==1?game.settings.chainCost:game.settings.infernoCost;
        public float Cooldown(int index) => index==0?game.settings.lanceCooldown:index==1?game.settings.chainCooldown:game.settings.infernoCooldown;
        public Vector3 AimPoint() => AimFromScreen(Input.mousePosition);
        public Vector3 AimFromScreen(Vector2 screen)
        {
            if (!SanctuaryHud.BlocksScreenPoint(screen))
            {
                var ray=Camera.main.ScreenPointToRay(screen);
                if (Physics.Raycast(ray,out var picked,120,1<<9,QueryTriggerInteraction.Collide))
                {
                    var enemy=picked.collider.GetComponent<DemonEnemy>();
                    if (enemy!=null && enemy.IsAlive) return enemy.transform.position;
                }
                if (Physics.Raycast(ray,out var ground,120,1<<8,QueryTriggerInteraction.Ignore)) return ground.point;
            }
            var target=game.Enemies.Nearest(transform.position,24);
            return target!=null?target.transform.position:transform.position+transform.forward*20;
        }
        public bool Cast(int index,Vector3 aim)
        {
            if (index<0 || index>2 || Remaining(index)>0 || !game.Vitals.SpendSpirit(Cost(index))) return false;
            readyAt[index]=Time.time+Cooldown(index);
            game.Audio?.Play(index==0?SoundCue.Spear:index==1?SoundCue.Lightning:SoundCue.Fire,transform.position);
            game.Presentation.Play(index==0?EffectCue.SpearCast:index==1?EffectCue.LightningCast:EffectCue.Inferno,transform.position+Vector3.up*.15f);
            if (index==0) CastLance(aim);
            else if (index==1) CastChain(aim);
            else CastInferno();
            return true;
        }
        public void SparksAt(Vector3 point,int amount,Color color)
        {
            sparks.transform.position=point;
            var main=sparks.main; main.startColor=color; sparks.Emit(amount);
        }
        void CastLance(Vector3 aim)
        {
            LanceCasts++;
            var direction=aim-transform.position; direction.y=0;
            if (direction.sqrMagnitude<.1f) direction=transform.forward;
            var target=game.Enemies.AimTarget(transform.position,aim,26);
            if (target!=null && Vector3.Angle(direction,target.transform.position-transform.position)<12) direction=target.transform.position-transform.position;
            direction.y=0; direction.Normalize();
            for (var i=-1;i<=1;i++)
            {
                DivineProjectile shot=null;
                foreach (var projectile in projectiles) if (!projectile.IsFlying) { shot=projectile; break; }
                if (shot==null)
                {
                    if (projectiles.Count>=12) break;
                    shot=new GameObject("Lance of Dawn").AddComponent<DivineProjectile>(); shot.transform.SetParent(effectsRoot.transform,false);
                    shot.Initialize(this,game.Enemies,game.models); projectiles.Add(shot);
                }
                shot.Launch(transform.position+Vector3.up*1.05f,Quaternion.Euler(0,i*5,0)*direction,game.settings.lanceDamage);
            }
            SparksAt(transform.position+Vector3.up,65,new Color(.75f,.95f,1));
        }
        Bolt CreateBolt(Vector3 from,Vector3 to)
        {
            var bolt=bolts.Find(b=>!b.outer.gameObject.activeSelf);
            if (bolt==null)
            {
                if (bolts.Count>=16) return null;
                bolt=new Bolt();
                bolt.outer=new GameObject("Chain of Heaven").AddComponent<LineRenderer>();
                bolt.outer.transform.SetParent(effectsRoot.transform,false); bolt.outer.sharedMaterial=arcMaterial; bolt.outer.widthMultiplier=.19f;
                bolt.inner=new GameObject("Lightning core").AddComponent<LineRenderer>();
                bolt.inner.transform.SetParent(bolt.outer.transform,false); bolt.inner.sharedMaterial=WhiteMaterial; bolt.inner.widthMultiplier=.045f;
                bolt.outer.positionCount=bolt.inner.positionCount=16;
                bolt.outer.useWorldSpace=bolt.inner.useWorldSpace=true;
                bolts.Add(bolt);
            }
            bolt.from=from; bolt.to=to; bolt.age=0; bolt.outer.gameObject.SetActive(true); return bolt;
        }
        void CastChain(Vector3 aim)
        {
            ChainCasts++; chainHits.Clear();
            var from=transform.position+Vector3.up*1.25f;
            var target=game.Enemies.AimTarget(transform.position,aim,19);
            if (target==null) { CreateBolt(from,aim+Vector3.up*.2f); SparksAt(aim,35,new Color(.3f,.8f,1)); return; }
            for (var hop=0;hop<4 && target!=null;hop++)
            {
                var point=target.transform.position;
                CreateBolt(from,point+Vector3.up*1.1f);
                chainHits.Add(target.TargetId);
                target.Hit(game.settings.chainDamage*Mathf.Pow(.9f,hop),DivineDamage.Chain,transform.position,.2f,.8f);
                SparksAt(point+Vector3.up,45,new Color(.4f,.85f,1));
                game.Presentation.Play(EffectCue.LightningHit,point+Vector3.up);
                from=point+Vector3.up;
                target=game.Enemies.Nearest(point,7,chainHits);
            }
        }
        void CastInferno()
        {
            InfernoCasts++;
            var wave=waves.Find(w=>!w.root.activeSelf);
            if (wave==null)
            {
                wave=new FireWave {root=new GameObject("Seraphic Inferno")}; wave.root.transform.SetParent(effectsRoot.transform,false);
                wave.inner=SceneArt.Ring("White fire",wave.root.transform,1,.06f,WhiteMaterial,128);
                wave.outer=SceneArt.Ring("Flame front",wave.root.transform,1,.18f,fireMaterial,128);
                wave.inner.transform.localPosition=Vector3.up*.08f; wave.outer.transform.localPosition=Vector3.up*.15f;
                for (var i=0;i<12;i++)
                {
                    var flame=new GameObject("Flame petal").AddComponent<LineRenderer>(); flame.transform.SetParent(wave.root.transform,false);
                    flame.useWorldSpace=false; flame.positionCount=3; flame.sharedMaterial=fireMaterial; flame.widthMultiplier=.24f;
                    flame.widthCurve=AnimationCurve.Linear(0,1,1,0); wave.flames.Add(flame);
                }
                wave.particles=CombatArt.Motes(wave.root.transform,game.models.celestialMote,500,new Color(1,.48f,.12f),3,.75f);
                wave.particles.transform.localPosition=Vector3.up*.12f; wave.particles.transform.localRotation=Quaternion.Euler(-90,0,0);
                var shape=wave.particles.shape; shape.shapeType=ParticleSystemShapeType.Circle;
                waves.Add(wave);
            }
            wave.age=wave.emission=0; wave.hit.Clear(); wave.root.transform.position=transform.position;
            wave.root.SetActive(true); wave.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            SparksAt(transform.position+Vector3.up,80,new Color(1,.65f,.25f));
        }
        void Update()
        {
            foreach (var bolt in bolts)
            {
                if (!bolt.outer.gameObject.activeSelf) continue;
                bolt.age+=Time.deltaTime;
                if (bolt.age>.55f) { bolt.outer.gameObject.SetActive(false); continue; }
                var side=Vector3.Cross((bolt.to-bolt.from).normalized,Vector3.up);
                for (var i=0;i<16;i++)
                {
                    var point=Vector3.Lerp(bolt.from,bolt.to,i/15f);
                    if (i>0 && i<15) point+=side*Mathf.Sin(i*9.71f+Time.time*53)*.22f+Vector3.up*Mathf.Cos(i*5.13f+Time.time*47)*.12f;
                    bolt.outer.SetPosition(i,point); bolt.inner.SetPosition(i,point);
                }
                bolt.outer.startColor=bolt.outer.endColor=bolt.inner.startColor=bolt.inner.endColor=new Color(1,1,1,1-bolt.age/.55f);
            }
            foreach (var wave in waves)
            {
                if (!wave.root.activeSelf) continue;
                wave.age+=Time.deltaTime;
                if (wave.age>1.95f) { wave.root.SetActive(false); continue; }
                var progress=Mathf.Clamp01(wave.age/1.15f); var radius=Mathf.Lerp(1,11,progress);
                if (wave.age<1.2f) game.Enemies.Radial(wave.root.transform.position,radius,game.settings.infernoDamage,DivineDamage.Inferno,wave.hit,1.4f,.25f,3);
                var alpha=Mathf.Clamp01(1-wave.age/1.5f);
                wave.inner.transform.localScale=wave.outer.transform.localScale=Vector3.one*radius;
                wave.inner.startColor=wave.inner.endColor=wave.outer.startColor=wave.outer.endColor=new Color(1,1,1,alpha);
                for (var i=0;i<wave.flames.Count;i++)
                {
                    var angle=i*Mathf.PI/6+wave.age;
                    var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var flame=wave.flames[i];
                    flame.SetPosition(0,radial*radius+Vector3.up*.1f);
                    flame.SetPosition(1,radial*(radius*.97f)+Vector3.up*(1+Mathf.Sin(wave.age*8+i)*.35f));
                    flame.SetPosition(2,radial*(radius*.9f)+Vector3.up*2);
                    flame.startColor=flame.endColor=new Color(1,1,1,alpha);
                }
                if (wave.age<1.15f)
                {
                    var shape=wave.particles.shape; shape.radius=radius;
                    wave.emission+=Time.deltaTime*280; var count=Mathf.FloorToInt(wave.emission);
                    if (count>0) { wave.particles.Emit(count); wave.emission-=count; }
                }
            }
        }
        public void ResetForRun()
        {
            for (var i=0;i<readyAt.Length;i++) readyAt[i]=0;
            foreach (var projectile in projectiles) projectile.ResetProjectile();
            foreach (var bolt in bolts) bolt.outer.gameObject.SetActive(false);
            foreach (var wave in waves) { wave.root.SetActive(false); wave.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
            sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            LanceCasts=ChainCasts=InfernoCasts=0;
        }
        void OnDestroy()
        {
            if (effectsRoot!=null) Destroy(effectsRoot);
            if (WhiteMaterial!=null) Destroy(WhiteMaterial);
            if (arcMaterial!=null) Destroy(arcMaterial);
            if (fireMaterial!=null) Destroy(fireMaterial);
        }
    }
}
