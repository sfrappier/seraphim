using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public enum BossAttack { Meteor,Fire,Nova,Slash }

    // All damage uses the same locked warning geometry as the rendered attack.
    public sealed class BossHazardField : MonoBehaviour
    {
        public sealed class Hazard
        {
            public GameObject root,rock,sector;
            public LineRenderer edge,progress,lane;
            public ParticleSystem particles;
            public TrailRenderer trail;
            public BossAttack kind;
            public Vector3 point,direction,end;
            public float began,impact,expires,nextBurn,nextEmission,radius,damage;
            public bool hit,landed;
            public bool Active => root.activeSelf;
        }
        CathedralBoss owner;
        SanctuaryGame game;
        readonly List<Hazard> pool=new List<Hazard>();
        readonly List<Mesh> meshes=new List<Mesh>();
        Material fire,voidFire,warning;
        public IReadOnlyList<Hazard> Hazards => pool;
        public int ActiveCount { get { var n=0; foreach (var h in pool) if (h.Active) n++; return n; } }
        public int Capacity => 18;
        public float DamageApplied { get; private set; }
        public void Initialize(CathedralBoss boss,SanctuaryGame world)
        {
            owner=boss; game=world;
            fire=new Material(game.models.glowGold); fire.SetColor("_BaseColor",new Color(3.5f,.43f,.025f));
            voidFire=new Material(game.models.glowCyan); voidFire.SetColor("_BaseColor",new Color(1.8f,.12f,3));
            warning=new Material(game.models.glowGold); warning.SetColor("_BaseColor",new Color(2,.09f,.04f));
        }
        Hazard Rent()
        {
            var h=pool.Find(item=>!item.Active);
            if (h!=null) return h;
            if (pool.Count>=Capacity) return null;
            h=new Hazard { root=new GameObject("Boss attack · pooled") }; h.root.transform.SetParent(transform,false);
            h.edge=SceneArt.Ring("Locked danger boundary",h.root.transform,1,.085f,warning,96);
            h.progress=SceneArt.Ring("Warning progress / safe inner edge",h.root.transform,1,.045f,warning,96);
            h.edge.transform.localPosition=Vector3.up*.1f; h.progress.transform.localPosition=Vector3.up*.12f;
            h.lane=new GameObject("Flame path / scythe sweep").AddComponent<LineRenderer>(); h.lane.transform.SetParent(h.root.transform,false);
            h.lane.useWorldSpace=false; h.lane.sharedMaterial=warning; h.lane.widthMultiplier=.12f;
            h.lane.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            h.rock=SceneArt.Shape(PrimitiveType.Sphere,"Falling ember / hellfire core",h.root.transform,Vector3.zero,Vector3.one*.9f,fire);
            h.trail=h.rock.AddComponent<TrailRenderer>(); h.trail.sharedMaterial=fire; h.trail.time=.42f; h.trail.minVertexDistance=.1f;
            h.trail.widthMultiplier=.85f; h.trail.startColor=new Color(1,.6f,.15f,.9f); h.trail.endColor=new Color(1,.05f,.01f,0);
            h.particles=CombatArt.Motes(h.root.transform,game.models.celestialMote,220,new Color(1,.3f,.045f),4,.8f);
            var rising=h.particles.velocityOverLifetime; rising.enabled=true; rising.space=ParticleSystemSimulationSpace.World; rising.x=0; rising.y=1.5f; rising.z=0;
            h.sector=new GameObject("Scythe danger sector"); h.sector.transform.SetParent(h.root.transform,false);
            var vertices=new Vector3[34]; var triangles=new int[32*3]; var colors=new Color[34];
            colors[0]=new Color(1,.12f,.02f,.04f);
            for (var i=0;i<=32;i++)
            {
                var a=Mathf.Lerp(-65,65,i/32f)*Mathf.Deg2Rad;
                vertices[i+1]=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)); colors[i+1]=new Color(1,.1f,.02f,.16f);
                if (i<32) { triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=i+2; }
            }
            var mesh=new Mesh {name="Readable scythe sector",vertices=vertices,triangles=triangles,colors=colors}; mesh.RecalculateBounds(); meshes.Add(mesh);
            h.sector.AddComponent<MeshFilter>().sharedMesh=mesh; h.sector.AddComponent<MeshRenderer>().sharedMaterial=warning;
            pool.Add(h); return h;
        }
        public bool Cast(BossAttack kind,Vector3 point,Vector3 direction,float delay=0)
        {
            var h=Rent(); if (h==null) return false;
            direction.y=0; if (direction.sqrMagnitude<.01f) direction=Vector3.forward;
            h.kind=kind; h.point=point; h.direction=direction.normalized; h.began=Time.time+delay;
            h.radius=kind==BossAttack.Meteor?2.4f:kind==BossAttack.Nova?12:kind==BossAttack.Slash?5.8f:.8f;
            h.impact=h.began+(kind==BossAttack.Meteor?1.5f:kind==BossAttack.Nova?1.45f:kind==BossAttack.Slash?1.05f:1.15f);
            h.expires=h.impact+(kind==BossAttack.Meteor?4.2f:kind==BossAttack.Fire?1.7f:.55f);
            h.damage=kind==BossAttack.Meteor?game.settings.bossMeteorDamage:kind==BossAttack.Fire?game.settings.bossFireDamage*2.5f:kind==BossAttack.Nova?game.settings.bossNovaDamage:game.settings.bossSlashDamage;
            h.hit=h.landed=false; h.nextEmission=0; h.nextBurn=h.impact+.6f;
            h.root.SetActive(true); h.root.transform.SetPositionAndRotation(point,Quaternion.identity);
            h.root.name="Vorath · "+kind;
            h.edge.enabled=kind==BossAttack.Meteor || kind==BossAttack.Nova; h.progress.enabled=h.edge.enabled;
            h.edge.sharedMaterial=h.progress.sharedMaterial=kind==BossAttack.Nova?voidFire:warning;
            h.edge.transform.localScale=Vector3.one*h.radius;
            h.edge.startColor=h.edge.endColor=Color.white;
            h.progress.startColor=h.progress.endColor=Color.white;
            h.lane.enabled=kind==BossAttack.Fire || kind==BossAttack.Slash;
            h.lane.widthMultiplier=.12f;
            h.lane.sharedMaterial=kind==BossAttack.Fire?fire:warning; h.lane.startColor=h.lane.endColor=new Color(1,.5f,.3f,.7f);
            h.sector.SetActive(kind==BossAttack.Slash); h.sector.transform.localPosition=Vector3.up*.075f;
            h.sector.transform.localRotation=Quaternion.LookRotation(h.direction); h.sector.transform.localScale=Vector3.one*h.radius;
            h.rock.SetActive(false); h.trail.emitting=false; h.trail.Clear();
            h.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if (kind==BossAttack.Fire)
            {
                h.end=point+h.direction*18;
                if (NavMesh.Raycast(point,h.end,out var wall,NavMesh.AllAreas)) h.end=wall.position;
                h.lane.positionCount=2; h.lane.SetPosition(0,Vector3.up*.12f); h.lane.SetPosition(1,h.end-point+Vector3.up*.12f);
            }
            if (kind==BossAttack.Slash)
            {
                h.lane.positionCount=35; h.lane.SetPosition(0,Vector3.up*.14f);
                for (var i=0;i<=32;i++)
                {
                    var a=Mathf.Lerp(-65,65,i/32f);
                    h.lane.SetPosition(i+1,Quaternion.Euler(0,a,0)*h.direction*h.radius+Vector3.up*.14f);
                }
                h.lane.SetPosition(34,Vector3.up*.14f);
            }
            return true;
        }
        public void Clear()
        {
            foreach (var h in pool)
            {
                h.root.SetActive(false); h.trail.emitting=false; h.trail.Clear();
                h.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        public static bool InSlash(Vector3 player,Vector3 origin,Vector3 direction)
        {
            var delta=player-origin; delta.y=0;
            return delta.magnitude<=6.05f && (delta.magnitude<.1f || Vector3.Angle(delta,direction)<=67);
        }
        void Damage(float amount)
        {
            var before=game.Vitals.Life; game.Vitals.TakeDamage(amount); DamageApplied+=before-game.Vitals.Life;
        }
        void Update()
        {
            if (!owner.IsAlive) { if (ActiveCount>0) Clear(); return; }
            foreach (var h in pool)
            {
                if (!h.Active) continue;
                if (Time.time>=h.expires) { h.root.SetActive(false); continue; }
                var player=game.Player.transform.position;
                if (Time.time<h.impact)
                {
                    var t=Mathf.Clamp01((Time.time-h.began)/(h.impact-h.began));
                    h.progress.transform.localScale=Vector3.one*(h.kind==BossAttack.Nova?5:h.radius*t);
                    if (h.kind==BossAttack.Meteor && Time.time>=h.impact-.7f)
                    {
                        var was=h.rock.activeSelf; h.rock.SetActive(true);
                        h.rock.transform.localPosition=Vector3.up*Mathf.Lerp(23,.4f,Mathf.Clamp01((Time.time-h.impact+.7f)/.7f))+Vector3.back*(1-t)*4;
                        if (!was) h.trail.Clear(); h.trail.emitting=true;
                    }
                    continue;
                }
                if (!h.landed)
                {
                    h.landed=true; h.rock.SetActive(h.kind==BossAttack.Fire); h.lane.enabled=h.kind==BossAttack.Fire || h.kind==BossAttack.Slash;
                    h.particles.transform.localPosition=Vector3.up*.2f;
                    var shape=h.particles.shape; shape.shapeType=ParticleSystemShapeType.Circle; shape.radius=h.kind==BossAttack.Meteor?h.radius:1;
                    h.particles.Emit(h.kind==BossAttack.Meteor?100:40);
                    game.Audio.Play(h.kind==BossAttack.Meteor?SoundCue.Revelation:h.kind==BossAttack.Slash?SoundCue.Spear:SoundCue.Fire,h.point);
                    var distance=FlatDistance(player,h.point);
                    if (h.kind==BossAttack.Meteor && distance<=h.radius+.25f) Damage(h.damage);
                    if (h.kind==BossAttack.Nova && distance>=4.75f && distance<=12.25f) Damage(h.damage);
                    if (h.kind==BossAttack.Slash && InSlash(player,h.point,h.direction)) Damage(h.damage);
                    if (h.kind==BossAttack.Meteor) Camera.main.GetComponent<SanctuaryCamera>().Impact();
                    if (h.kind==BossAttack.Fire)
                    {
                        h.rock.transform.localPosition=Vector3.up*1.1f; h.trail.Clear(); h.trail.emitting=true;
                    }
                }
                if (h.kind==BossAttack.Meteor)
                {
                    h.edge.sharedMaterial=h.progress.sharedMaterial=fire;
                    h.progress.transform.localScale=Vector3.one*h.radius*(.8f+.1f*Mathf.Sin(Time.time*12));
                    if (Time.time>=h.nextBurn)
                    {
                        h.nextBurn=Time.time+.65f;
                        if (FlatDistance(player,h.point)<=h.radius+.25f) Damage(game.settings.bossFireDamage);
                    }
                    if (Time.time>=h.nextEmission) { h.nextEmission=Time.time+.1f; h.particles.Emit(12); }
                }
                else if (h.kind==BossAttack.Fire)
                {
                    var from=h.rock.transform.position; var to=Vector3.MoveTowards(from,new Vector3(h.end.x,1.1f,h.end.z),11*Time.deltaTime);
                    if (!h.hit && CombatArt.SegmentDistance(player,from,to)<h.radius+.25f) { h.hit=true; Damage(h.damage); }
                    h.rock.transform.position=to;
                    if (Time.time>=h.nextEmission) { h.nextEmission=Time.time+.08f; h.particles.transform.position=to; h.particles.Emit(8); }
                    if (Vector2.Distance(new Vector2(to.x,to.z),new Vector2(h.end.x,h.end.z))<.1f) h.root.SetActive(false);
                }
                else if (h.kind==BossAttack.Nova)
                {
                    h.progress.transform.localScale=Vector3.one*Mathf.Lerp(5,12,(Time.time-h.impact)/.55f);
                    h.edge.startColor=h.edge.endColor=new Color(1,1,1,1-(Time.time-h.impact)/.55f);
                }
                else
                {
                    var a=Mathf.Lerp(-65,65,(Time.time-h.impact)/.45f);
                    h.lane.positionCount=2; h.lane.SetPosition(0,Vector3.up*1.5f);
                    h.lane.SetPosition(1,Quaternion.Euler(0,a,0)*h.direction*h.radius+Vector3.up*1.5f);
                    h.lane.widthMultiplier=.4f;
                }
            }
        }
        static float FlatDistance(Vector3 a,Vector3 b) { a.y=b.y=0; return Vector3.Distance(a,b); }
        void OnDestroy()
        {
            foreach (var mesh in meshes) Destroy(mesh);
            if (fire!=null) Destroy(fire); if (voidFire!=null) Destroy(voidFire); if (warning!=null) Destroy(warning);
        }
    }
}
