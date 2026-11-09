using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed class CathedralBoss : MonoBehaviour
    {
        SanctuaryGame game;
        DemonEnemy enemy;
        CathedralBossArt art;
        float nextAttack,nextSlash,busyUntil,nextPath,slashAt;
        float transitionUntil;
        int rotation,phase,pendingPhase;
        public bool AutomaticAttacks { get; set; }=true;
        public bool IsAlive => enemy!=null && enemy.IsAlive && running;
        bool running;
        bool transitionPresentation;
        public DemonEnemy Enemy => enemy;
        public BossHazardField Field { get; private set; }
        public BossHealthHud Hud { get; private set; }
        public BossSummons Summons { get; private set; }
        public int Phase => IsAlive?phase:0;
        public bool IsTransitioning => IsAlive && (pendingPhase>0 || Time.time<transitionUntil);
        public float TransitionRemaining => Mathf.Max(0,transitionUntil-Time.time);
        public float ScytheScale => 1+(enemy!=null?enemy.EncounterScale-1:0)*.6f;
        public string AttackHint { get; private set; }="";
        public int Casts { get; private set; }
        public void Initialize(SanctuaryGame owner)
        {
            game=owner; Field=new GameObject("Vorath · bounded hazards").AddComponent<BossHazardField>();
            Field.transform.SetParent(transform,false); Field.Initialize(this,game);
            Hud=gameObject.AddComponent<BossHealthHud>(); Hud.Initialize(game,this);
            Summons=gameObject.AddComponent<BossSummons>(); Summons.Initialize(game,this);
        }
        public bool Begin()
        {
            Clear();
            var center=game.Player.transform.position;
            var forward=game.Player.KeyboardDirection(Vector2.up);
            var found=false; var spawn=center;
            for (var i=0;i<12;i++)
            {
                var desired=center+Quaternion.Euler(0,i*30,0)*forward*12;
                if (!NavMesh.SamplePosition(desired,out var sample,2,NavMesh.AllAreas)) continue;
                var path=new NavMeshPath();
                if (NavMesh.CalculatePath(center,sample.position,NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete)
                { spawn=sample.position; found=true; break; }
            }
            if (!found) return false;
            enemy=game.Enemies.SpawnBoss(spawn); if (enemy==null) return false;
            running=true; AutomaticAttacks=true; Casts=0; rotation=0; phase=1; pendingPhase=0; transitionUntil=0; transitionPresentation=false;
            Summons.ResetForEncounter();
            nextAttack=Time.time+2.5f; nextSlash=Time.time+3; busyUntil=Time.time+1.8f; nextPath=0; slashAt=-100;
            art=enemy.GetComponent<CathedralBossArt>(); art.ResetForEncounter(); art.Slash=0;
            var direction=center-spawn; direction.y=0; if (direction.sqrMagnitude>.01f) enemy.transform.rotation=Quaternion.LookRotation(direction);
            var camera=Camera.main.GetComponent<SanctuaryCamera>(); camera.BossTarget=enemy.transform; camera.BossZoom=7;
            AttackHint="THE HERALD DESCENDS"; game.Audio.Play(SoundCue.Ascend,spawn);
            game.Powers.SparksAt(spawn+Vector3.up*3,180,new Color(1,.15f,.7f));
            return true;
        }
        public bool Cast(BossAttack attack)
        {
            if (!IsAlive || IsTransitioning) return false;
            var target=game.Player.transform.position; var origin=enemy.transform.position;
            var direction=target-origin; direction.y=0; if (direction.sqrMagnitude<.01f) direction=enemy.transform.forward; direction.Normalize();
            enemy.transform.rotation=Quaternion.LookRotation(direction); enemy.Agent.ResetPath();
            busyUntil=Time.time+(attack==BossAttack.Meteor?1.8f:attack==BossAttack.Slash?1.65f:1.9f);
            Casts++; game.Audio.Play(SoundCue.Warning,origin);
            if (attack==BossAttack.Meteor)
            {
                AttackHint="METEOR RAIN · leave the marked circles";
                var count=Phase+2;
                for (var i=0;i<count;i++)
                {
                    // The first impact locks the current player position; others leave distinct escape routes.
                    var desired=i==0?target:target+Quaternion.Euler(0,(i-1)*137.5f,0)*Vector3.forward*(5+(i%2)*1.5f);
                    if (NavMesh.SamplePosition(desired,out var point,2,NavMesh.AllAreas)) Field.Cast(attack,point.position,direction,i*.2f);
                }
            }
            else if (attack==BossAttack.Fire)
            {
                AttackHint="HELLFIRE FAN · dodge between the burning trails";
                var count=Phase==1?5:7;
                for (var i=0;i<count;i++) Field.Cast(attack,origin,Quaternion.Euler(0,(i-(count-1)*.5f)*17,0)*direction);
            }
            else if (attack==BossAttack.Nova)
            {
                AttackHint="HOLLOW NOVA · inside 5m or beyond 12m";
                Field.Cast(attack,origin,direction);
                if (Phase==3) Field.Cast(attack,origin,direction,1.9f);
            }
            else
            {
                AttackHint="INFERNAL GREATSWORD · get behind him or retreat";
                Field.Cast(attack,origin,direction); slashAt=Time.time+1.05f;
            }
            return true;
        }
        void Update()
        {
            if (!IsAlive) { if (running) { running=false; Field.Clear(); Summons.Clear(); ResetCamera(); } return; }
            if (pendingPhase>0) EnterPhase(pendingPhase);
            if (transitionPresentation && !IsTransitioning)
            {
                transitionPresentation=false;
                AttackHint=phase==3?"ASCENDANT · the host grows stronger":"ASHEN HOST · slay summons to recover resources";
            }
            var current=Phase;
            art.Phase=current; art.Slash=Time.time>=slashAt && Time.time<slashAt+.5f?(Time.time-slashAt)/.5f:0;
            art.Channeling=Time.time<busyUntil || IsTransitioning;
            var camera=Camera.main.GetComponent<SanctuaryCamera>();
            camera.BossZoom=7+(enemy.EncounterScale-1)*9; camera.BossElevation=(enemy.EncounterScale-1)*4.5f;
            if (IsTransitioning) { enemy.Agent.isStopped=true; return; }
            if (!AutomaticAttacks || game.Vitals.Life<=0) { enemy.Agent.isStopped=true; return; }
            if (Time.time<busyUntil) { enemy.Agent.isStopped=true; return; }
            var distance=Vector3.Distance(game.Player.transform.position,enemy.transform.position);
            if (distance<5.5f*ScytheScale && Time.time>=nextSlash)
            {
                Cast(BossAttack.Slash); nextSlash=Time.time+(current==3?3.5f:4.5f); return;
            }
            if (Time.time>=nextAttack)
            {
                var step=rotation++%3;
                Cast(step==0?BossAttack.Meteor:step==1?BossAttack.Fire:BossAttack.Nova); nextAttack=Time.time+(current==3?3.1f:current==2?3.7f:4.3f); return;
            }
            enemy.Agent.isStopped=distance<4.5f;
            if (!enemy.Agent.isStopped && Time.time>=nextPath)
            { enemy.Agent.SetDestination(game.Player.transform.position); nextPath=Time.time+.35f; }
            if (distance>.2f)
            {
                var facing=game.Player.transform.position-enemy.transform.position; facing.y=0;
                enemy.transform.rotation=Quaternion.RotateTowards(enemy.transform.rotation,Quaternion.LookRotation(facing),80*Time.deltaTime);
            }
        }
        // Clamp before health changes, including multiple hits in the same frame.
        // EnterPhase runs in Update so area-damage iteration never mutates the demon pool.
        public float FilterDamage(float amount)
        {
            if (IsTransitioning) return 0;
            var floor=phase==1?.65f:phase==2?.3f:0;
            return Mathf.Min(amount,Mathf.Max(0,enemy.Health-enemy.MaxHealth*floor));
        }
        public void HealthChanged()
        {
            var threshold=phase==1?.65f:.3f;
            if (phase<3 && enemy.Health<=enemy.MaxHealth*threshold+.01f) pendingPhase=phase+1;
        }
        void EnterPhase(int next)
        {
            phase=next; pendingPhase=0; transitionPresentation=true;
            var duration=phase==3?game.settings.bossTransformationSeconds:game.settings.bossRallySeconds;
            transitionUntil=Time.time+duration; busyUntil=transitionUntil+.35f;
            nextAttack=busyUntil; nextSlash=busyUntil+1; Field.Clear();
            game.Audio.Play(SoundCue.Revelation,enemy.transform.position);
            game.Powers.SparksAt(enemy.transform.position+Vector3.up*3,150,new Color(1,.18f,.6f));
            game.Drops.SpawnPair(game.Player.transform.position+Vector3.left*4);
            game.Drops.SpawnPair(game.Player.transform.position+Vector3.right*4);
            if (phase==3)
            {
                enemy.GrowTo(game.settings.bossAscendedScale,duration); art.BeginTransformation(duration);
                AttackHint="ASCENSION OF ASH · transforming / warded";
            }
            else AttackHint="THE ASHEN HOST · summoning / warded";
            Summons.EnterPhase(phase);
        }
        public void Defeated()
        {
            if (!running) return;
            running=false; Field.Clear(); Summons.Clear(); ResetCamera();
            game.Powers.SparksAt(enemy.transform.position+Vector3.up*3,250,new Color(1,.75f,.3f));
            game.Waves.BossDefeated();
        }
        public void Clear()
        {
            running=false; pendingPhase=0; transitionUntil=0; transitionPresentation=false; AttackHint="";
            if (Field!=null) Field.Clear();
            if (Summons!=null) Summons.Clear();
            if (art!=null) art.ResetForEncounter();
            if (enemy!=null) enemy.ReturnToPool();
            ResetCamera();
        }
        void ResetCamera()
        {
            if (Camera.main==null) return;
            var camera=Camera.main.GetComponent<SanctuaryCamera>(); camera.BossTarget=null; camera.BossZoom=0; camera.BossElevation=0;
        }
    }
}
