using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // Bone-mounted additions stay aligned with the imported humanoid animation.
    public sealed class AngelicVestments : MonoBehaviour
    {
        readonly List<Mesh> meshes=new List<Mesh>();
        Material embroidery;
        public bool HasCrown { get; private set; }
        public void Initialize(Animator animator,PrototypeModels art)
        {
            if (animator==null || !animator.isHuman) return;
            var detailed=art.playerModel!=null;
            embroidery=art.gold;
            var head=Anchor(animator.GetBoneTransform(HumanBodyBones.Head),"Aureate crown",new Vector3(0,1.55f,0));
            var crown=SceneArt.Ring("Crown circlet",head,detailed?.23f:.4f,detailed?.012f:.027f,detailed?art.glowGold:art.gold,64);
            crown.transform.localPosition=Vector3.up*.28f;
            for (var i=0;i<7;i++)
            {
                var angle=i*Mathf.PI*2/7;
                var radius=detailed?.23f:.4f;
                var tip=SceneArt.Shape(PrimitiveType.Cube,"Crown ray",head,new Vector3(Mathf.Sin(angle)*radius,.34f,Mathf.Cos(angle)*radius),detailed?new Vector3(.015f,.07f,.015f):new Vector3(.045f,.18f,.045f),art.gold);
                tip.transform.localRotation=Quaternion.Euler(0,i*360/7,25);
            }
            HasCrown=true;
            // FallenAngel already includes authored armor, cloth and a helmet.
            if (detailed) return;
            var chestBone=animator.GetBoneTransform(HumanBodyBones.UpperChest)??animator.GetBoneTransform(HumanBodyBones.Chest)??animator.GetBoneTransform(HumanBodyBones.Spine);
            var chest=Anchor(chestBone,"Celestial mantle",new Vector3(0,1.02f,0));
            Cloth(chest,art.vestment,.4f,.12f,-.31f,.96f);
            var stole=Anchor(animator.GetBoneTransform(HumanBodyBones.Hips),"Ivory surcoat",new Vector3(0,.78f,0));
            Cloth(stole,art.vestment,.28f,0,.27f,.66f);
            foreach (var sign in new[]{-1,1})
            {
                SceneArt.Shape(PrimitiveType.Sphere,"Gilded pauldron",chest,new Vector3(sign*.34f,-.03f,0),new Vector3(.28f,.13f,.33f),art.gold);
                for (var i=0;i<4;i++)
                {
                    var feather=SceneArt.Shape(PrimitiveType.Sphere,"Mantle feather",chest,new Vector3(sign*(.3f+i*.065f),.12f-i*.028f,-.08f),new Vector3(.08f,.05f,.42f-i*.045f),art.ivory);
                    feather.transform.localRotation=Quaternion.Euler(-12,sign*(20+i*10),0);
                }
            }
            SceneArt.Shape(PrimitiveType.Cube,"Pectoral light",chest,new Vector3(0,.05f,.29f),new Vector3(.055f,.32f,.04f),art.gold);
            SceneArt.Shape(PrimitiveType.Cube,"Pectoral cross",chest,new Vector3(0,.09f,.3f),new Vector3(.22f,.05f,.04f),art.gold);
            SceneArt.Shape(PrimitiveType.Sphere,"Heart of dawn",chest,new Vector3(0,.09f,.33f),Vector3.one*.095f,art.glowCyan);
        }
        Transform Anchor(Transform bone,string name,Vector3 playerPoint)
        {
            if (bone==null) bone=transform;
            var root=new GameObject(name).transform;
            root.SetParent(bone,false); root.rotation=transform.rotation;
            root.position=transform.TransformPoint(playerPoint);
            var scale=bone.lossyScale;
            root.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            return root;
        }
        void Cloth(Transform parent,Material mat,float width,float top,float z,float length)
        {
            const int rows=8,columns=9;
            var vertices=new Vector3[rows*columns]; var uv=new Vector2[vertices.Length];
            var triangles=new List<int>();
            for (var row=0;row<rows;row++) for (var col=0;col<columns;col++)
            {
                var t=row/(float)(rows-1); var x=(col/(float)(columns-1)*2-1)*width*(1+t*.22f);
                var index=row*columns+col;
                vertices[index]=new Vector3(x,top-t*length,z+(z<0?-1:1)*t*.14f+Mathf.Cos(col*Mathf.PI*.75f)*.025f);
                uv[index]=new Vector2(col/(float)(columns-1),t);
                if (row==rows-1 || col==columns-1) continue;
                var a=index; var b=index+1; var c=index+columns; var d=c+1;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=CombatArt.DoubleSided("Pleated celestial silk",vertices,uv,triangles); meshes.Add(mesh);
            var cloth=new GameObject("Ivory silk"); cloth.transform.SetParent(parent,false);
            cloth.AddComponent<MeshFilter>().sharedMesh=mesh; cloth.AddComponent<MeshRenderer>().sharedMaterial=mat;
            foreach (var sign in new[]{-1,1})
                SceneArt.Shape(PrimitiveType.Cube,"Golden embroidered edge",parent,new Vector3(sign*width*.95f,top-length*.48f,z+.015f),new Vector3(.027f,length,.025f),embroidery);
        }
        void OnDestroy() { foreach (var mesh in meshes) Destroy(mesh); }
    }
}
