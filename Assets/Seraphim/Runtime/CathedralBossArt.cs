using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // Original procedural silhouette: floating ossuary, crown, ribs and hooked glaive.
    public sealed class CathedralBossArt : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<Transform> tendrils=new List<Transform>();
        Mesh robe;
        Mesh cone;
        Transform weapon,halo,leftArm;
        Material stone,bone,ember,voidGlow;
        Light furnace;
        int previousPhase;
        float channelPose;
        public Transform Body { get; private set; }
        public float Slash { get; set; }
        public int Phase { get; set; }=1;
        public bool Channeling { get; set; }
        Material Material(Material template,Color tint)
        {
            var mat=new Material(template); mat.SetColor("_BaseColor",tint); materials.Add(mat); return mat;
        }
        Transform Node(string name,Transform parent,Vector3 position)
        {
            var obj=new GameObject(name).transform; obj.SetParent(parent,false); obj.localPosition=position; return obj;
        }
        Transform Shape(PrimitiveType type,string name,Transform parent,Vector3 p,Vector3 scale,Material mat)
            => SceneArt.Shape(type,name,parent,p,scale,mat).transform;
        void Bone(Transform parent,Vector3 from,Vector3 to,float radius,Material mat=null)
            => CombatArt.Spike(parent,from,to,radius,mat??bone,cone);
        public void Initialize(EnemyDirector director)
        {
            cone=director.ConeMesh;
            stone=Material(director.Game.models.cathedralStone,new Color(.12f,.075f,.16f));
            bone=Material(director.Game.models.ivory,new Color(.58f,.53f,.62f));
            bone.SetFloat("_Metallic",.65f); bone.SetFloat("_Smoothness",.7f);
            ember=Material(director.Game.models.glowGold,new Color(3,.38f,.04f));
            voidGlow=Material(director.Game.models.glowCyan,new Color(1.9f,.12f,3));
            Body=Node("Herald of Ash · original sculpt",transform,Vector3.zero);
            var torso=Node("Floating rib cage",Body,Vector3.zero);
            Shape(PrimitiveType.Sphere,"Obsidian breastplate",torso,new Vector3(0,3.65f,0),new Vector3(2.2f,2.6f,1.15f),stone);
            Shape(PrimitiveType.Sphere,"Furnace heart",torso,new Vector3(0,3.7f,.65f),new Vector3(.55f,.85f,.25f),ember);
            for (var row=0;row<6;row++)
            {
                var y=2.6f+row*.35f; var width=.7f+Mathf.Sin(row*.5f)*.6f;
                for (var side=-1;side<=1;side+=2)
                {
                    Bone(torso,new Vector3(0,y+.24f,.72f),new Vector3(side*width,y+.1f,.36f),.13f);
                    Bone(torso,new Vector3(side*width,y+.1f,.36f),new Vector3(side*(width*.75f),y+.28f,-.5f),.15f);
                    Bone(torso,new Vector3(side*width,y+.25f,-.4f),new Vector3(side*(width+1),y+.65f,-1.4f),.17f,stone);
                }
            }
            for (var i=0;i<8;i++)
                Shape(PrimitiveType.Cube,"Spinal vertebra",Body,new Vector3(0,2.3f+i*.43f,-.45f),new Vector3(.34f,.25f,.45f),bone);
            Shape(PrimitiveType.Sphere,"Long crowned skull",Body,new Vector3(0,5.8f,.12f),new Vector3(1.12f,1.55f,.95f),bone);
            Shape(PrimitiveType.Sphere,"Black face plate",Body,new Vector3(0,5.8f,.53f),new Vector3(.9f,1.05f,.32f),stone);
            Bone(Body,new Vector3(0,5.6f,.75f),new Vector3(0,4.95f,.95f),.23f);
            for (var side=-1;side<=1;side+=2)
            {
                Shape(PrimitiveType.Sphere,"Sunken eye socket",Body,new Vector3(side*.26f,5.97f,.7f),new Vector3(.48f,.31f,.2f),stone);
                var eye=Shape(PrimitiveType.Cube,"Burning eye slit",Body,new Vector3(side*.26f,5.97f,.81f),new Vector3(.3f,.065f,.08f),voidGlow);
                eye.localRotation=Quaternion.Euler(0,0,side*-17);
                Bone(Body,new Vector3(side*.48f,5.72f,.52f),new Vector3(side*.2f,5.45f,.9f),.18f);
                Bone(Body,new Vector3(side*.4f,5.45f,.6f),new Vector3(side*.18f,5.12f,.72f),.13f);
                for (var h=0;h<3;h++)
                {
                    var from=new Vector3(side*(.4f+h*.15f),6.15f-h*.23f,-h*.13f);
                    var bend=from+new Vector3(side*(.35f+h*.35f),.8f,-.45f);
                    Bone(Body,from,bend,.23f-h*.035f);
                    Bone(Body,bend,bend+new Vector3(-side*.23f,1.1f-h*.2f,.3f),.13f-h*.02f);
                }
                var arm=Node(side<0?"Spell hand":"Glaive arm",Body,new Vector3(side*1.35f,4.35f,0));
                Shape(PrimitiveType.Sphere,"Armored shoulder",arm,Vector3.zero,new Vector3(1.25f,.9f,1.15f),stone);
                for (var spike=0;spike<3;spike++)
                    Bone(arm,new Vector3(side*.2f,0,spike*.25f-.3f),new Vector3(side*1.2f,1+spike*.15f,spike*.3f-.4f),.24f);
                Bone(arm,new Vector3(0,0,0),new Vector3(side*.85f,-.75f,.2f),.26f);
                Bone(arm,new Vector3(side*.85f,-.75f,.2f),new Vector3(side*.7f,-1.4f,.8f),.2f);
                for (var finger=0;finger<4;finger++)
                    Bone(arm,new Vector3(side*.7f+(finger-1.5f)*.15f,-1.3f,.8f),new Vector3(side*.7f+(finger-1.5f)*.22f,-1.9f,1.25f),.07f);
                if (side<0) leftArm=arm;
                else
                {
                    weapon=Node("Hooked dusk scythe",arm,new Vector3(.7f,-1.25f,.8f));
                    Shape(PrimitiveType.Cylinder,"Scythe haft",weapon,new Vector3(0,1,0),new Vector3(.12f,2.5f,.12f),bone);
                    Shape(PrimitiveType.Sphere,"Scythe furnace",weapon,new Vector3(0,3.35f,0),Vector3.one*.52f,ember);
                    for (var seg=0;seg<8;seg++)
                    {
                        var a=seg*Mathf.PI/10; var b=(seg+1)*Mathf.PI/10;
                        var p=new Vector3(-Mathf.Sin(a)*2.7f,3.3f+Mathf.Cos(a)*1.6f,0);
                        var q=new Vector3(-Mathf.Sin(b)*2.7f,3.3f+Mathf.Cos(b)*1.6f,0);
                        Bone(weapon,p,q,Mathf.Lerp(.35f,.035f,seg/7f),bone);
                        Bone(weapon,p+Vector3.forward*.14f,q+Vector3.forward*.14f,.05f,ember);
                    }
                }
            }
            for (var tooth=0;tooth<5;tooth++)
                Bone(Body,new Vector3((tooth-2)*.12f,5.45f,.8f),new Vector3((tooth-2)*.1f,5.2f,.87f),.055f);
            // Torn, two-sided mantle with a pointed hem and visible folds.
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>();
            for (var row=0;row<4;row++) for (var col=0;col<=20;col++)
            {
                var angle=col/20f*Mathf.PI*1.7f+Mathf.PI*.15f;
                var radius=1+row*.28f; var y=4.4f-row*1.15f-(row==3 && col%2==0?.6f:0);
                vertices.Add(new Vector3(Mathf.Sin(angle)*radius,y,Mathf.Cos(angle)*radius-.1f)); uv.Add(new Vector2(col/5f,row));
                if (row<3 && col<20)
                {
                    var n=row*21+col; indices.AddRange(new[]{n,n+21,n+1,n+1,n+21,n+22});
                }
            }
            robe=CombatArt.DoubleSided("Torn obsidian mantle",vertices.ToArray(),uv.ToArray(),indices);
            var cloak=Node("Cathedral mantle",Body,Vector3.zero); cloak.gameObject.AddComponent<MeshFilter>().sharedMesh=robe;
            cloak.gameObject.AddComponent<MeshRenderer>().sharedMaterial=stone;
            for (var i=0;i<7;i++)
            {
                var tail=Node("Spectral bone train",Body,new Vector3((i-3)*.36f,2.1f,-.55f)); tendrils.Add(tail);
                for (var joint=0;joint<5;joint++)
                    Bone(tail,new Vector3(Mathf.Sin(joint+i)*.15f,-joint*.4f,-joint*.28f),new Vector3(Mathf.Sin(joint+1+i)*.15f,-(joint+1)*.4f,-(joint+1)*.28f),.13f-joint*.018f,joint%2==0?bone:voidGlow);
            }
            halo=Node("Broken sigil crown",Body,new Vector3(0,5.2f,-.8f)); halo.localRotation=Quaternion.Euler(90,0,0);
            var ring=SceneArt.Ring("Profane halo",halo,2.25f,.06f,voidGlow,96);
            for (var i=0;i<12;i++)
            {
                var angle=i*Mathf.PI/6; var p=new Vector3(Mathf.Sin(angle)*2.3f,0,Mathf.Cos(angle)*2.3f);
                Bone(halo,p,p*1.18f,.09f,ember);
            }
            furnace=SceneArt.PointLight("Ashen furnace light",Body,new Vector3(0,4,1),new Color(1,.12f,.3f),4,12);
        }
        void Update()
        {
            if (Body==null) return;
            if (Phase!=previousPhase)
            {
                previousPhase=Phase;
                voidGlow.SetColor("_BaseColor",Phase==3?new Color(3,.1f,.55f):Phase==2?new Color(2,.4f,2.5f):new Color(1.9f,.12f,3));
                furnace.color=Phase==3?new Color(1,.3f,.05f):new Color(1,.12f,.3f);
            }
            Body.localPosition=Vector3.up*(.2f+Mathf.Sin(Time.time*1.6f)*.18f);
            halo.localRotation=Quaternion.Euler(90,Time.time*(9+Phase*3),0);
            halo.localScale=Vector3.one*(1+(Phase-1)*.06f);
            weapon.parent.localRotation=Quaternion.Euler(-10+Mathf.Sin(Time.time)*4,Mathf.Lerp(-40,100,Slash),Mathf.Lerp(-10,-60,Slash));
            channelPose=Mathf.MoveTowards(channelPose,Channeling?1:0,Time.deltaTime*3);
            leftArm.localRotation=Quaternion.Euler(-15-channelPose*55+Mathf.Sin(Time.time*2)*12,0,-12);
            for (var i=0;i<tendrils.Count;i++) tendrils[i].localRotation=Quaternion.Euler(Mathf.Sin(Time.time*2+i)*12,Mathf.Sin(Time.time+i)*20,0);
        }
        void OnDestroy() { if (robe!=null) Destroy(robe); foreach (var mat in materials) Destroy(mat); }
    }
}
