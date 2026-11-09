using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // Lightweight skeletal locomotion for the downloaded creature rigs, including
    // the two sources supplied without animations/armatures. Shared meshes and
    // materials plus two discrete LODs keep the menu's 96-enemy battle bounded.
    public sealed class DemonModelPresentation : MonoBehaviour
    {
        sealed class Bone
        {
            public Transform transform;
            public Quaternion rest;
            public float side;
        }
        readonly Dictionary<string,Bone> bones=new Dictionary<string,Bone>();
        DemonEnemy owner;
        Transform hips;
        Vector3 hipPosition;
        DemonRig rig;
        float phase;
        public bool HasAnimatedRig => bones.ContainsKey("leftThigh") && bones.ContainsKey("rightThigh") && bones.ContainsKey("chest");
        public int AnimatedBoneCount => bones.Count;
        public Vector3 HeadPosition => bones.TryGetValue("head",out var head)?head.transform.position:transform.position;

        public void Initialize(DemonEnemy enemy,CharacterModelDefinition definition)
        {
            owner=enemy; rig=definition.rig;
            foreach (var animator in GetComponentsInChildren<Animator>()) animator.enabled=false;
            string[] names;
            if (rig==DemonRig.Minotaur) names=new[]{"Bip01_L_Thigh","Bip01_R_Thigh","Bip01_L_Calf","Bip01_R_Calf","Bip01_L_UpperArm","Bip01_R_UpperArm","Bip01_L_Forearm","Bip01_R_Forearm","Bip01_Spine1","Bip01_Head","BN_Tail_01","Bip01_Pelvis"};
            else if (rig==DemonRig.Scourger) names=new[]{"Main Thigh.L","Main Thigh.R","Main Lower Leg.L","Main Lower Leg.R","UpperArm.L","UpperArm.R","Forearm.L","Forearm.R","Chest","Head","TailBase","Pelvis","Wing1.L","Wing1.R"};
            else names=new[]{"UpperLeg.L","UpperLeg.R","LowerLeg.L","LowerLeg.R","UpperArm.L","UpperArm.R","Forearm.L","Forearm.R","Chest","Head","","Hips"};
            var keys=new[]{"leftThigh","rightThigh","leftShin","rightShin","leftArm","rightArm","leftForearm","rightForearm","chest","head","tail","hips","leftWing","rightWing"};
            var hierarchy=GetComponentsInChildren<Transform>(true);
            for (var i=0;i<names.Length;i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;
                foreach (var bone in hierarchy) if (bone.name==names[i])
                {
                    bones[keys[i]]=new Bone {transform=bone,rest=bone.localRotation,side=Mathf.Sign(transform.InverseTransformPoint(bone.position).x)};
                    break;
                }
            }
            if (bones.TryGetValue("hips",out var pelvis)) { hips=pelvis.transform; hipPosition=hips.localPosition; }
            ResetPose();
        }
        public void ResetPose()
        {
            foreach (var bone in bones.Values) bone.transform.localRotation=bone.rest;
            if (hips!=null) hips.localPosition=hipPosition;
            phase=0;
            // The knight's source is one static mesh with the sword and cape
            // connected to its body. Keep its bind pose rigid: the boss presenter
            // animates the entire authored silhouette without bending the blade.
            if (!owner.IsBoss) Pose(0,0,0);
        }
        void Turn(string name,Vector3 angles)
        {
            if (!bones.TryGetValue(name,out var bone)) return;
            var parent=bone.transform.parent;
            var right=parent.InverseTransformDirection(transform.right);
            var up=parent.InverseTransformDirection(transform.up);
            var forward=parent.InverseTransformDirection(transform.forward);
            bone.transform.localRotation=Quaternion.AngleAxis(angles.x,right)*Quaternion.AngleAxis(angles.y,up)*Quaternion.AngleAxis(angles.z,forward)*bone.rest;
        }
        void Pose(float stride,float motion,float attack)
        {
            Turn("leftThigh",new Vector3(stride*28,0,0));
            Turn("rightThigh",new Vector3(-stride*28,0,0));
            Turn("leftShin",new Vector3(Mathf.Max(0,-stride)*32,0,0));
            Turn("rightShin",new Vector3(Mathf.Max(0,stride)*32,0,0));
            var drop=rig==DemonRig.GameRig?0:62;
            Arm("leftArm",-stride,attack,drop); Arm("rightArm",stride,attack,drop);
            Turn("leftForearm",new Vector3(-10-motion*12-attack*22,0,0));
            Turn("rightForearm",new Vector3(-10-motion*12-attack*22,0,0));
            Turn("chest",new Vector3(motion*6+attack*10,Mathf.Sin(phase*.5f)*motion*5,0));
            Turn("head",new Vector3(Mathf.Sin(phase*.45f)*2,0,0));
            Turn("tail",new Vector3(0,Mathf.Sin(phase*.7f)*12,0));
            var flap=Mathf.Sin(Time.time*3.8f+owner.TargetId)*9;
            Turn("leftWing",new Vector3(0,flap,0)); Turn("rightWing",new Vector3(0,-flap,0));
        }
        void Arm(string name,float swing,float attack,float drop)
        {
            if (bones.TryGetValue(name,out var bone)) Turn(name,new Vector3(swing*20-attack*72,attack*bone.side*18,-drop*bone.side));
        }
        void LateUpdate()
        {
            if (owner==null || !owner.IsAlive || owner.IsBoss) return;
            var speed=owner.Agent.enabled?owner.Agent.velocity.magnitude:0;
            var motion=owner.IsStunned?0:Mathf.Clamp01(speed/Mathf.Max(.1f,owner.MoveSpeed));
            phase+=Time.deltaTime*(2+motion*8);
            var attack=owner.Offense!=null && owner.Offense.Phase==DemonOffense.AttackPhase.Windup?.5f+.5f*Mathf.Sin(Time.time*12):0;
            Pose(Mathf.Sin(phase)*motion,motion,attack);
        }
    }
}
