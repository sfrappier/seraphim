using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed class DemonEnemy : MonoBehaviour
    {
        EnemyDirector director;
        NavMeshAgent agent;
        Transform visual,leftLeg,rightLeg,leftArm,rightArm,tail;
        Renderer[] renderers;
        Color[] baseColors;
        MaterialPropertyBlock hitBlock;
        ParticleSystem sparks;
        LineRenderer spawnRing;
        CapsuleCollider targetCollider;
        float appearedAt,deathAt,nextPath,flashUntil,burnUntil,nextBurn,stunUntil;
        bool marked;
        public bool IsAlive { get; private set; }
        public bool IsBoss { get; private set; }
        public bool Passive { get; set; }
        public int TargetId { get; private set; }
        public int WaveNumber { get; private set; }
        float baseHealth,baseSpeed;
        public DemonDifficulty Difficulty { get; private set; }
        public DemonOffense Offense { get; private set; }
        public NavMeshAgent Agent => agent;
        public float VisualScale => BaseScale;
        public float MoveSpeed => agent.speed;
        public float HitRadius => IsBoss?1.7f:.35f*BaseScale;
        public DemonKind Kind { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float HealthFraction => Mathf.Clamp01(Health/Mathf.Max(1,MaxHealth));
        public float BarHeight => IsBoss?8.5f:(Kind==DemonKind.ObsidianGuard?2.8f:2.45f)*Difficulty.size;
        public string DisplayName => IsBoss?"VORATH, HERALD OF ASH":Kind==DemonKind.CinderImp?"CINDER IMP":Kind==DemonKind.AshStalker?"ASH STALKER":"OBSIDIAN GUARD";
        public DemonHealthBar HealthBar { get; private set; }
        public DivineDamage LastDamageSource { get; private set; }
        public bool IsBurning => IsAlive && Time.time<burnUntil;
        public bool IsStunned => IsAlive && Time.time<stunUntil;

        public void Initialize(EnemyDirector owner,DemonKind kind,bool boss=false)
        {
            director=owner; Kind=kind; IsBoss=boss;
            MaxHealth=kind==DemonKind.CinderImp?42:kind==DemonKind.AshStalker?32:60;
            baseHealth=MaxHealth;
            agent=gameObject.AddComponent<NavMeshAgent>();
            agent.radius=.38f; agent.height=1.8f; agent.speed=kind==DemonKind.AshStalker?3.2f:kind==DemonKind.ObsidianGuard?2:2.6f;
            agent.acceleration=12; agent.angularSpeed=360; agent.stoppingDistance=1.35f;
            baseSpeed=agent.speed;
            agent.avoidancePriority=35+(int)kind*8;
            gameObject.layer=9;
            targetCollider=gameObject.AddComponent<CapsuleCollider>();
            targetCollider.isTrigger=true; targetCollider.height=2; targetCollider.radius=.45f; targetCollider.center=Vector3.up;
            if (IsBoss) BuildBossVisuals(); else BuildVisuals();
            HealthBar=gameObject.AddComponent<DemonHealthBar>(); HealthBar.Initialize(this,director.Game.Hud.HudCanvas);
            Offense=gameObject.AddComponent<DemonOffense>(); Offense.Initialize(this,director);
            agent.enabled=false;
            gameObject.SetActive(false);
        }
        void BuildBossVisuals()
        {
            var art=gameObject.AddComponent<CathedralBossArt>(); art.Initialize(director);
            visual=art.Body;
            renderers=visual.GetComponentsInChildren<Renderer>(); baseColors=new Color[renderers.Length];
            for (var i=0;i<renderers.Length;i++) baseColors[i]=renderers[i].sharedMaterial.GetColor("_BaseColor");
            hitBlock=new MaterialPropertyBlock();
            sparks=CombatArt.Motes(transform,director.Game.models.celestialMote,350,new Color(1,.35f,.65f),5,1);
            sparks.transform.localPosition=Vector3.up*3;
            spawnRing=SceneArt.Ring("Herald arrival",transform,3,.12f,director.Game.models.glowGold,96);
            spawnRing.transform.localPosition=Vector3.up*.09f;
        }
        void BuildVisuals()
        {
            visual=new GameObject("Demon silhouette").transform; visual.SetParent(transform,false);
            var body=director.BodyMaterials[(int)Kind]; var horn=director.HornMaterial; var eye=director.EyeMaterials[(int)Kind];
            SceneArt.Shape(PrimitiveType.Capsule,"Obsidian body",visual,new Vector3(0,.8f,0),new Vector3(.68f,.59f,.48f),body);
            SceneArt.Shape(PrimitiveType.Sphere,"Horned head",visual,new Vector3(0,1.45f,.02f),new Vector3(.58f,.52f,.5f),body);
            for (var side=-1;side<=1;side+=2)
            {
                SceneArt.Shape(PrimitiveType.Sphere,"Ember eye",visual,new Vector3(side*.14f,1.5f,.258f),new Vector3(.115f,.065f,.05f),eye);
                CombatArt.Spike(visual,new Vector3(side*.16f,1.64f,0),new Vector3(side*.34f,2.05f,-.12f),.13f,horn,director.ConeMesh);
                CombatArt.Spike(visual,new Vector3(side*.34f,2.02f,-.12f),new Vector3(side*.27f,2.25f,.08f),.065f,horn,director.ConeMesh);
                var arm=new GameObject("Claw arm").transform; arm.SetParent(visual,false); arm.localPosition=new Vector3(side*.4f,1.1f,0);
                SceneArt.Shape(PrimitiveType.Capsule,"Arm",arm,new Vector3(side*.08f,-.23f,.02f),new Vector3(.2f,.29f,.2f),body);
                for (var claw=0;claw<3;claw++) CombatArt.Spike(arm,new Vector3(side*.1f+(claw-1)*.055f,-.45f,.04f),new Vector3(side*.1f+(claw-1)*.075f,-.6f,.25f),.035f,horn,director.ConeMesh);
                var leg=new GameObject("Demon leg").transform; leg.SetParent(visual,false); leg.localPosition=new Vector3(side*.2f,.5f,0);
                SceneArt.Shape(PrimitiveType.Capsule,"Leg",leg,new Vector3(0,-.16f,0),new Vector3(.22f,.24f,.22f),body);
                SceneArt.Shape(PrimitiveType.Cube,"Cloven foot",leg,new Vector3(0,-.4f,.08f),new Vector3(.23f,.16f,.34f),horn);
                if (side<0) { leftArm=arm; leftLeg=leg; } else { rightArm=arm; rightLeg=leg; }
                if (Kind==DemonKind.ObsidianGuard)
                    CombatArt.Spike(visual,new Vector3(side*.4f,1.2f,0),new Vector3(side*.8f,1.6f,-.03f),.2f,horn,director.ConeMesh);
                if (Kind==DemonKind.AshStalker)
                {
                    CombatArt.Spike(visual,new Vector3(side*.25f,1.1f,-.1f),new Vector3(side*1.1f,1.5f,-.5f),.07f,horn,director.ConeMesh);
                    CombatArt.Spike(visual,new Vector3(side*.25f,1.1f,-.1f),new Vector3(side*.85f,.65f,-.5f),.06f,body,director.ConeMesh);
                    SceneArt.Shape(PrimitiveType.Cube,"Shadow wing",visual,new Vector3(side*.65f,1.12f,-.3f),new Vector3(.7f,.48f,.07f),body).transform.localRotation=Quaternion.Euler(0,side*20,side*27);
                }
            }
            tail=new GameObject("Barbed tail").transform; tail.SetParent(visual,false); tail.localPosition=new Vector3(0,.55f,-.2f);
            CombatArt.Spike(tail,Vector3.zero,new Vector3(.14f,-.15f,-.65f),.1f,body,director.ConeMesh);
            CombatArt.Spike(tail,new Vector3(.14f,-.15f,-.6f),new Vector3(.1f,.1f,-.9f),.1f,eye,director.ConeMesh);
            renderers=visual.GetComponentsInChildren<Renderer>(); baseColors=new Color[renderers.Length];
            for (var i=0;i<renderers.Length;i++) baseColors[i]=renderers[i].sharedMaterial.GetColor("_BaseColor");
            hitBlock=new MaterialPropertyBlock();
            sparks=CombatArt.Motes(transform,director.Game.models.celestialMote,160,new Color(1,.7f,.28f),3,.65f);
            sparks.transform.localPosition=Vector3.up;
            spawnRing=SceneArt.Ring("Demon arrival",transform,.8f,.045f,director.Game.models.glowGold,48);
            spawnRing.transform.localPosition=Vector3.up*.05f;
        }
        public bool Activate(Vector3 point,int id,bool passive,int wave=0)
        {
            WaveNumber=wave;
            Difficulty=new DemonDifficulty(director.Game.settings,Kind,wave);
            MaxHealth=baseHealth*Difficulty.health; agent.speed=baseSpeed*Difficulty.speed;
            agent.radius=Mathf.Clamp(.38f*Difficulty.size,.38f,.6f); agent.height=1.8f*Difficulty.size;
            targetCollider.height=2*BaseScale; targetCollider.radius=.45f*BaseScale; targetCollider.center=Vector3.up*BaseScale;
            if (IsBoss)
            {
                MaxHealth=director.Game.settings.bossHealth; agent.speed=director.Game.settings.bossMoveSpeed;
                agent.radius=1.1f; agent.height=6; agent.stoppingDistance=4.4f;
                targetCollider.height=6.5f; targetCollider.radius=1.7f; targetCollider.center=Vector3.up*3.1f;
            }
            TargetId=id; Health=MaxHealth; IsAlive=true; Passive=passive;
            deathAt=0; appearedAt=Time.time; nextPath=0;
            burnUntil=stunUntil=flashUntil=0;
            visual.localScale=Vector3.one*BaseScale;
            gameObject.SetActive(true);
            targetCollider.enabled=true;
            transform.position=point;
            agent.enabled=true;
            if (!agent.Warp(point)) { ReturnToPool(); return false; }
            agent.ResetPath(); agent.isStopped=false;
            foreach (var renderer in renderers) renderer.SetPropertyBlock(null);
            HealthBar.ResetBar(); Offense.ResetForSpawn(); return true;
        }
        float BaseScale => IsBoss?1:(Kind==DemonKind.ObsidianGuard?1.13f:Kind==DemonKind.AshStalker?.92f:1)*Difficulty.size;
        public void Hit(float amount,DivineDamage source,Vector3 origin,float push=0,float stun=0,float burn=0)
        {
            if (!IsAlive || amount<=0) return;
            var actual=Mathf.Min(Health,amount); Health=Mathf.Max(0,Health-amount); LastDamageSource=source;
            director.RecordDamage(actual); HealthBar.Hit(actual,source);
            director.Game.Audio?.Play(SoundCue.Hit,transform.position);
            if (IsBoss) { stun=0; push=0; }
            flashUntil=Time.time+.12f; stunUntil=Mathf.Max(stunUntil,Time.time+stun);
            if (stun>0) Offense.Interrupt();
            if (burn>0) { burnUntil=Time.time+burn; nextBurn=Time.time+1; }
            sparks.Emit(12);
            if (Health<=0)
            {
                IsAlive=false; deathAt=Time.time; targetCollider.enabled=false; if (agent.enabled) agent.enabled=false;
                Offense.ClearThreat(); sparks.Emit(45); director.RecordKill(this); return;
            }
            if (push>0 && agent.enabled && agent.isOnNavMesh)
            {
                var away=transform.position-origin; away.y=0;
                if (away.sqrMagnitude>.01f)
                {
                    var desired=transform.position+away.normalized*push*Difficulty.knockback;
                    if (agent.Raycast(desired,out var edge)) desired=edge.position;
                    agent.Move((desired-transform.position)*.8f);
                }
            }
        }
        void Update()
        {
            if (!IsAlive)
            {
                if (deathAt<=0) return;
                var t=(Time.time-deathAt)/.75f;
                visual.localScale=Vector3.one*BaseScale*Mathf.Max(0,1-t);
                if (t>=1) ReturnToPool();
                return;
            }
            if (burnUntil>0 && nextBurn<=burnUntil+.01f && Time.time>=nextBurn) { nextBurn+=1; Hit(8,DivineDamage.Burning,transform.position); if (!IsAlive) return; sparks.Emit(5); }
            if (!IsBoss) Offense.Tick();
            var flashing=Time.time<flashUntil;
            if (flashing || IsBurning || marked)
            {
                for (var i=0;i<renderers.Length;i++)
                {
                    hitBlock.Clear();
                    if (flashing || IsBurning)
                    {
                        hitBlock.SetColor("_BaseColor",flashing?Color.Lerp(baseColors[i],Color.white,.8f):baseColors[i]);
                        hitBlock.SetColor("_EmissionColor",flashing?new Color(2,1.2f,.4f):new Color(1.1f,.23f,.03f));
                    }
                    renderers[i].SetPropertyBlock(hitBlock);
                }
                marked=flashing || IsBurning;
            }
            var arrival=Mathf.Clamp01((Time.time-appearedAt)/.8f);
            spawnRing.enabled=arrival<1;
            spawnRing.transform.localScale=Vector3.one*(.3f+arrival);
            spawnRing.startColor=spawnRing.endColor=new Color(1,.3f,.08f,1-arrival);
            if (IsBoss) return; // Dedicated encounter controls navigation, attacks and animation.
            if (!Passive && director.Game.Vitals.Life>0 && agent.enabled && agent.isOnNavMesh)
            {
                var distance=Vector3.Distance(transform.position,director.Game.Player.transform.position);
                var keepRange=Kind==DemonKind.AshStalker && WaveNumber>=3;
                agent.isStopped=IsStunned || Offense.BlocksMovement || (keepRange?distance>=5 && distance<=12:distance<1.55f);
                if (!agent.isStopped && Time.time>=nextPath)
                {
                    var destination=director.Game.Player.transform.position;
                    if (keepRange && distance<5)
                    {
                        var retreat=transform.position+(transform.position-destination).normalized*4;
                        if (NavMesh.SamplePosition(retreat,out var safe,1.5f,NavMesh.AllAreas)) destination=safe.position;
                    }
                    agent.SetDestination(destination); nextPath=Time.time+.3f;
                }
            }
            var swing=Mathf.Sin(Time.time*8+TargetId)*Mathf.Clamp01(agent.enabled?agent.velocity.magnitude/2:0)*28;
            leftLeg.localRotation=Quaternion.Euler(swing,0,0); rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            leftArm.localRotation=Quaternion.Euler(-swing*.7f,0,-12); rightArm.localRotation=Quaternion.Euler(swing*.7f,0,12);
            tail.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*3+TargetId)*18,0);
        }
        public void ReturnToPool()
        {
            IsAlive=false; deathAt=0; if (agent!=null) agent.enabled=false;
            if (Offense!=null) Offense.ClearThreat();
            if (targetCollider!=null) targetCollider.enabled=false;
            if (HealthBar!=null) HealthBar.Hide();
            if (sparks!=null) sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }
}
