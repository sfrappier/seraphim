using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // Authored motion on the refined skeleton. Combat timings are owned by the
    // encounter; animation never moves its navigation root or applies damage.
    public sealed class InfernalKnightAnimation : MonoBehaviour
    {
        sealed class Bone {public Transform transform;public Quaternion rest;}
        readonly Dictionary<string,Bone> bones=new Dictionary<string,Bone>();
        DemonEnemy enemy;
        Transform hips;
        Vector3 hipRest;
        TrailRenderer bladeTrail;
        Material trailMaterial;
        float phase,began=-100,transformationAt=-100,transformationSeconds;
        BossAttack attack;
        public int AttackAnimations {get;private set;}
        public bool IsWalking {get;private set;}
        public bool IsAttacking => Time.time-began<Duration;
        float Duration => attack==BossAttack.Slash?1.65f:1.9f;
        public Transform SwordHand => BoneTransform("Hand.R");
        public Transform SwordTip => BoneTransform("BladeTip");
        public float ShoulderAngle => bones.TryGetValue("UpperArm.R",out var b)?Quaternion.Angle(b.rest,b.transform.localRotation):0;
        public float BladeLength => SwordHand!=null && SwordTip!=null?Vector3.Distance(SwordHand.position,SwordTip.position):0;
        Transform BoneTransform(string name) => bones.TryGetValue(name,out var bone)?bone.transform:null;
        public void Initialize(DemonEnemy owner,PrototypeModels art)
        {
            enemy=owner;
            foreach (var animator in GetComponentsInChildren<Animator>()) animator.enabled=false;
            var names=new HashSet<string>(new[]{"Hips","Spine","Chest","Neck","Head","UpperLeg.L","UpperLeg.R","LowerLeg.L","LowerLeg.R","Foot.L","Foot.R","UpperArm.L","UpperArm.R","Forearm.L","Forearm.R","Hand.L","Hand.R","Cape","BladeTip"});
            foreach (var t in GetComponentsInChildren<Transform>()) if (names.Contains(t.name)) bones[t.name]=new Bone {transform=t,rest=t.localRotation};
            hips=BoneTransform("Hips");if (hips!=null) hipRest=hips.localPosition;
            if (SwordTip!=null)
            {
                bladeTrail=new GameObject("Infernal greatsword · blade trail").AddComponent<TrailRenderer>();bladeTrail.transform.SetParent(SwordTip,false);
                trailMaterial=new Material(art.glowGold);trailMaterial.SetColor("_BaseColor",new Color(3,.35f,.025f));
                bladeTrail.sharedMaterial=trailMaterial;bladeTrail.time=.22f;bladeTrail.minVertexDistance=.05f;
                bladeTrail.widthMultiplier=.24f;bladeTrail.startColor=new Color(1,.6f,.1f,.85f);bladeTrail.endColor=new Color(1,.1f,.01f,0);
                bladeTrail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;bladeTrail.emitting=false;
            }
            ResetForEncounter();
        }
        public void PlayAttack(BossAttack value)
        {
            attack=value;began=Time.time;AttackAnimations++;
            if (bladeTrail!=null) {bladeTrail.emitting=false;bladeTrail.Clear();}
        }
        public void Transform(float seconds) {transformationAt=Time.time;transformationSeconds=seconds;}
        void Turn(string name,Vector3 angle)
        {
            if (!bones.TryGetValue(name,out var bone)) return;
            var parent=bone.transform.parent;
            bone.transform.localRotation=Quaternion.AngleAxis(angle.x,parent.InverseTransformDirection(transform.right))*Quaternion.AngleAxis(angle.y,parent.InverseTransformDirection(transform.up))*Quaternion.AngleAxis(angle.z,parent.InverseTransformDirection(transform.forward))*bone.rest;
        }
        public void ResetForEncounter()
        {
            began=transformationAt=-100;phase=0;AttackAnimations=0;IsWalking=false;
            foreach (var bone in bones.Values) bone.transform.localRotation=bone.rest;
            if (hips!=null) hips.localPosition=hipRest;
            if (bladeTrail!=null) {bladeTrail.emitting=false;bladeTrail.Clear();}
        }
        void LateUpdate()
        {
            if (enemy==null || !enemy.IsAlive) {if (bladeTrail!=null) bladeTrail.emitting=false;return;}
            var speed=enemy.Agent.enabled?enemy.Agent.velocity.magnitude:0;
            var motion=Mathf.Clamp01(speed/Mathf.Max(.1f,enemy.MoveSpeed));IsWalking=motion>.12f;
            phase+=Time.deltaTime*(1.8f+motion*6);
            var stride=Mathf.Sin(phase)*motion;
            foreach (var bone in bones.Values) bone.transform.localRotation=bone.rest;
            Turn("Hips",new Vector3(0,stride*3,0));
            Turn("UpperLeg.L",new Vector3(stride*18,0,0));Turn("UpperLeg.R",new Vector3(-stride*18,0,0));
            Turn("LowerLeg.L",new Vector3(Mathf.Max(0,-stride)*22,0,0));Turn("LowerLeg.R",new Vector3(Mathf.Max(0,stride)*22,0,0));
            Turn("Chest",new Vector3(Mathf.Sin(phase*.6f)*1.8f+motion*4,-stride*4,0));
            Turn("Head",new Vector3(Mathf.Sin(phase*.5f),Mathf.Sin(phase*.3f)*2,0));
            Turn("UpperArm.L",new Vector3(stride*9,0,0));Turn("UpperArm.R",new Vector3(-stride*6,0,0));
            Turn("Cape",new Vector3(Mathf.Sin(phase*.6f)*2+motion*4,Mathf.Sin(phase*.4f)*2,0));
            if (hips!=null) hips.localPosition=hipRest+hips.parent.InverseTransformVector(Vector3.up*(Mathf.Abs(stride)*.055f+Mathf.Sin(phase*.7f)*.012f));
            var age=Time.time-began;
            if (age>=0 && age<Duration)
            {
                if (attack==BossAttack.Slash)
                {
                    var windup=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/1.05f));
                    var strike=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-1.05f)/.24f));
                    var recovery=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-1.32f)/.33f));
                    Turn("Spine",new Vector3(8*strike,0,0));
                    Turn("Chest",new Vector3(Mathf.Lerp(-5,10,strike)*windup*recovery,Mathf.Lerp(-18,30,strike)*windup*recovery,0));
                    Turn("UpperArm.R",new Vector3(Mathf.Lerp(-45,35,strike)*windup*recovery,Mathf.Lerp(-20,40,strike)*windup*recovery,0));
                    Turn("Forearm.R",new Vector3(Mathf.Lerp(-18,20,strike)*windup*recovery,0,0));
                    Turn("UpperArm.L",new Vector3(-12*windup*recovery,0,18*windup*recovery));
                }
                else
                {
                    var cast=Mathf.Sin(Mathf.Clamp01(age/Duration)*Mathf.PI);
                    Turn("Chest",new Vector3(attack==BossAttack.Fire?8*cast:-4*cast,cast*10,0));
                    Turn("UpperArm.L",new Vector3(-45*cast,0,(attack==BossAttack.Nova?25:12)*cast));
                    Turn("Forearm.L",new Vector3(-22*cast,0,0));Turn("Head",new Vector3(-8*cast,0,0));
                    if (attack==BossAttack.Nova) Turn("UpperArm.R",new Vector3(-25*cast,0,-25*cast));
                }
            }
            if (transformationSeconds>0 && Time.time-transformationAt<transformationSeconds)
            {
                var power=Mathf.Sin(Mathf.Clamp01((Time.time-transformationAt)/transformationSeconds)*Mathf.PI);
                Turn("Chest",new Vector3(-10*power,0,0));Turn("UpperArm.L",new Vector3(-40*power,0,20*power));
                Turn("UpperArm.R",new Vector3(-35*power,0,-12*power));Turn("Head",new Vector3(-12*power,0,0));
            }
            if (bladeTrail!=null) bladeTrail.emitting=attack==BossAttack.Slash && age>.94f && age<1.5f;
        }
        void OnDisable() {ResetForEncounter();}
        void OnDestroy() {if (trailMaterial!=null) Destroy(trailMaterial);}
    }
}
