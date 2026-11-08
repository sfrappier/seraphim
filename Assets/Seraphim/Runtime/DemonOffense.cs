using UnityEngine;

namespace Seraphim
{
    // Fixed warning positions reward dodging. Stuns interrupt windups and charges.
    public sealed class DemonOffense : MonoBehaviour
    {
        public enum AttackPhase { Idle,Windup,Burning,Charging,Recovery }
        enum AttackKind { Melee,EmberField,Charge }
        DemonEnemy owner;
        EnemyDirector director;
        GameObject effects;
        LineRenderer warning,progress,lane;
        ParticleSystem embers;
        AttackKind kind;
        Vector3 point,chargeEnd;
        float started,until,nextMelee,nextSpecial,nextBurn,nextEmission,radius;
        bool chargeHit;
        public AttackPhase Phase { get; private set; }
        public bool BlocksMovement => Phase==AttackPhase.Windup || Phase==AttackPhase.Charging || Phase==AttackPhase.Recovery;
        public bool SpecialActive => Phase!=AttackPhase.Idle && kind!=AttackKind.Melee;
        public bool SpecialReady => Phase==AttackPhase.Idle && !owner.IsStunned && Time.time>=nextSpecial
            && ((owner.Kind==DemonKind.AshStalker && owner.WaveNumber>=3) || (owner.Kind==DemonKind.ObsidianGuard && owner.WaveNumber>=5));
        public Vector3 ThreatPoint => point;
        public int SpecialCasts { get; private set; }
        public float DamageApplied { get; private set; }
        float ChargeRadius => .65f*owner.Difficulty.size+.35f;

        public void Initialize(DemonEnemy demon,EnemyDirector encounter)
        {
            owner=demon; director=encounter;
            effects=new GameObject("Demon attack · pooled warning"); effects.transform.SetParent(director.transform,false);
            warning=SceneArt.Ring("Danger boundary",effects.transform,1,.065f,director.Game.models.glowGold,64);
            progress=SceneArt.Ring("Windup progress",effects.transform,1,.035f,director.Game.models.glowGold,64);
            warning.transform.localPosition=Vector3.up*.09f; progress.transform.localPosition=Vector3.up*.11f;
            lane=new GameObject("Guard charge lane").AddComponent<LineRenderer>(); lane.transform.SetParent(effects.transform,false);
            lane.useWorldSpace=false; lane.positionCount=2; lane.widthMultiplier=1.3f; lane.sharedMaterial=director.Game.models.glowGold;
            lane.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            embers=CombatArt.Motes(effects.transform,director.Game.models.celestialMote,180,new Color(1,.2f,.25f),2,.7f);
            var shape=embers.shape; shape.shapeType=ParticleSystemShapeType.Circle;
            embers.transform.localPosition=Vector3.up*.2f;
            embers.transform.localRotation=Quaternion.Euler(-90,0,0);
            var rising=embers.velocityOverLifetime; rising.enabled=true; rising.space=ParticleSystemSimulationSpace.World;
            rising.x=0; rising.y=.8f; rising.z=0;
            effects.SetActive(false);
        }
        public void ResetForSpawn()
        {
            ClearThreat(); DamageApplied=0; SpecialCasts=0;
            nextMelee=Time.time+1.1f+(owner.TargetId%3)*.08f;
            nextSpecial=Time.time+2.2f+(owner.TargetId%4)*.2f;
        }
        public void Interrupt()
        {
            if (Phase==AttackPhase.Windup || Phase==AttackPhase.Charging) ClearThreat();
        }
        public void ClearThreat()
        {
            Phase=AttackPhase.Idle;
            if (effects!=null) effects.SetActive(false);
            if (embers!=null) embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public bool TrySpecial()
        {
            if (!owner.IsAlive || !SpecialReady || director.ActiveSpecialAttacks>=director.Game.settings.maxConcurrentSpecialAttacks) return false;
            var target=director.Game.Player.transform.position;
            var distance=FlatDistance(target,transform.position);
            if (owner.Kind==DemonKind.AshStalker)
            {
                if (distance>14) return false;
                Begin(AttackKind.EmberField,target,2.5f,1.05f);
            }
            else
            {
                if (distance<3 || distance>11) return false;
                var direction=target-transform.position; direction.y=0;
                var end=transform.position+direction.normalized*Mathf.Min(11,distance+2);
                if (owner.Agent.Raycast(end,out var boundary)) end=boundary.position-direction.normalized*.03f;
                chargeEnd=end; Begin(AttackKind.Charge,end,1.15f,.9f);
                lane.widthMultiplier=ChargeRadius*2;
                lane.SetPosition(0,transform.position-end+Vector3.up*.12f); lane.SetPosition(1,Vector3.up*.12f);
            }
            nextSpecial=Time.time+owner.Difficulty.specialInterval; SpecialCasts++; return true;
        }
        void Begin(AttackKind attack,Vector3 target,float size,float windup)
        {
            kind=attack; point=target; radius=size; started=Time.time; until=Time.time+windup;
            Phase=AttackPhase.Windup; chargeHit=false;
            effects.transform.position=point; effects.SetActive(true);
            warning.enabled=progress.enabled=true; lane.enabled=kind==AttackKind.Charge;
            var mat=kind==AttackKind.EmberField?director.Game.models.glowCyan:director.Game.models.glowGold;
            warning.sharedMaterial=progress.sharedMaterial=mat;
            var color=kind==AttackKind.EmberField?new Color(1,.18f,.72f,.85f):new Color(1,.19f,.06f,.85f);
            warning.startColor=warning.endColor=progress.startColor=progress.endColor=color;
            var fire=embers.main; fire.startColor=kind==AttackKind.EmberField?new Color(.8f,.18f,1):new Color(1,.55f,.15f);
            var emitter=embers.shape; emitter.radius=radius;
            lane.startColor=lane.endColor=new Color(1,.16f,.05f,.3f);
            warning.transform.localScale=Vector3.one*radius;
            progress.transform.localScale=Vector3.one*.1f;
        }
        public void Tick()
        {
            if (!owner.IsAlive || director.Game.Vitals.Life<=0) { ClearThreat(); return; }
            if (owner.IsStunned) Interrupt();
            if (Phase==AttackPhase.Windup)
            {
                progress.transform.localScale=Vector3.one*radius*Mathf.Clamp01((Time.time-started)/(until-started));
                if (Time.time<until) return;
                if (kind==AttackKind.EmberField)
                {
                    Phase=AttackPhase.Burning; until=Time.time+2.4f; nextBurn=Time.time; nextEmission=0;
                    var shape=embers.shape; shape.radius=radius;
                }
                else if (kind==AttackKind.Charge)
                {
                    Phase=AttackPhase.Charging; until=Time.time+.7f;
                    lane.startColor=lane.endColor=new Color(1,.4f,.07f,.8f);
                }
                else
                {
                    if (FlatDistance(director.Game.Player.transform.position,point)<radius+.25f)
                        Damage((owner.Kind==DemonKind.ObsidianGuard?6:4)*owner.Difficulty.damage);
                    embers.Emit(20); Phase=AttackPhase.Recovery; until=Time.time+.16f;
                }
            }
            if (Phase==AttackPhase.Burning)
            {
                if (Time.time>=until) { ClearThreat(); return; }
                if (Time.time>=nextEmission) { embers.Emit(12); nextEmission=Time.time+.12f; }
                if (Time.time>=nextBurn)
                {
                    nextBurn=Time.time+.6f;
                    if (FlatDistance(director.Game.Player.transform.position,point)<radius+.25f) Damage(4*owner.Difficulty.damage);
                }
                var pulse=.65f+.35f*Mathf.Sin(Time.time*10);
                progress.transform.localScale=Vector3.one*radius*pulse;
                return;
            }
            if (Phase==AttackPhase.Charging)
            {
                var from=transform.position; var desired=Vector3.MoveTowards(from,chargeEnd,18*Time.deltaTime);
                if (owner.Agent.Raycast(desired,out var edge)) desired=edge.position;
                owner.Agent.Move(desired-from);
                if (!chargeHit && CombatArt.SegmentDistance(director.Game.Player.transform.position,from,desired)<ChargeRadius)
                {
                    chargeHit=true; Damage(10*owner.Difficulty.damage);
                }
                if (Vector3.Distance(desired,chargeEnd)<.12f || Time.time>=until) { Phase=AttackPhase.Recovery; until=Time.time+.3f; }
                return;
            }
            if (Phase==AttackPhase.Recovery) { if (Time.time>=until) ClearThreat(); return; }
            if (Phase!=AttackPhase.Idle || owner.Passive || owner.IsStunned) return;
            if (TrySpecial()) return;
            if (Time.time>=nextMelee && FlatDistance(director.Game.Player.transform.position,transform.position)<2.05f)
            {
                nextMelee=Time.time+owner.Difficulty.meleeInterval;
                Begin(AttackKind.Melee,director.Game.Player.transform.position,1.15f,.4f);
            }
        }
        void Damage(float amount)
        {
            var before=director.Game.Vitals.Life;
            director.Game.Vitals.TakeDamage(amount); DamageApplied+=before-director.Game.Vitals.Life;
        }
        static float FlatDistance(Vector3 a,Vector3 b) { a.y=b.y=0; return Vector3.Distance(a,b); }
        void OnDisable() { ClearThreat(); }
        void OnDestroy() { if (effects!=null) Destroy(effects); }
    }
}
