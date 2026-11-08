using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // All placement and collision definitions share world coordinates with the map.
    public sealed class CathedralBuilder : MonoBehaviour
    {
        PrototypeModels art;
        Action<Vector3,Vector3,int> nav;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Material> materials=new List<Material>();
        Material carpet,blueGlass,amberGlass,violetGlass;
        public static CathedralBuilder Build(PrototypeModels models,SanctuarySettings settings,Action<Vector3,Vector3,int> navigation,Action<Vector3,string> waystone)
        {
            var builder=new GameObject("Cathedral of Dawn · cutaway architecture").AddComponent<CathedralBuilder>();
            builder.art=models; builder.nav=navigation; builder.Construct(settings,waystone); return builder;
        }
        Material Tint(Material source,string name,Color color)
        {
            var mat=new Material(source) {name=name}; mat.SetColor("_BaseColor",color); materials.Add(mat); return mat;
        }
        GameObject Shape(PrimitiveType type,string name,Vector3 point,Vector3 size,Material material,bool obstacle=false)
        {
            var obj=SceneArt.Shape(type,name,transform,point,size,material,obstacle);
            if ((material==art.marble || material==art.cathedralStone || material==art.cathedralWood) && (type==PrimitiveType.Cube || type==PrimitiveType.Cylinder))
            {
                var filter=obj.GetComponent<MeshFilter>(); var mesh=Instantiate(filter.sharedMesh); var uv=mesh.uv;
                if (type==PrimitiveType.Cube)
                {
                    var vertices=mesh.vertices; var normals=mesh.normals;
                    for (var i=0;i<uv.Length;i++)
                    {
                        var p=Vector3.Scale(vertices[i],size); var n=normals[i];
                        uv[i]=(Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y))/3;
                    }
                }
                else for (var i=0;i<uv.Length;i++) uv[i]=Vector2.Scale(uv[i],new Vector2(Mathf.PI*size.x/3,size.y*2/3));
                mesh.uv=uv; filter.sharedMesh=mesh; meshes.Add(mesh);
            }
            if (obstacle) nav(point,size,1); return obj;
        }
        void Box(string name,Vector3 point,Vector3 size,Material mat,bool obstacle=false) => Shape(PrimitiveType.Cube,name,point,size,mat,obstacle);
        void Construct(SanctuarySettings settings,Action<Vector3,string> waystone)
        {
            var size=settings.cathedralSize;
            var floor=Shape(PrimitiveType.Cube,"Navigable cathedral foundation",new Vector3(0,-.3f,0),new Vector3(size.x,.5f,size.y),art.darkStone);
            floor.layer=8; floor.AddComponent<BoxCollider>();
            nav(new Vector3(0,-.3f,0),new Vector3(size.x,.5f,size.y),0);
            carpet=Tint(art.vestment,"Midnight ceremonial runner",new Color(.055f,.13f,.24f));
            blueGlass=Tint(art.glowCyan,"Sapphire stained glass",new Color(.08f,.55f,1.9f));
            amberGlass=Tint(art.glowGold,"Amber stained glass",new Color(1.7f,.65f,.12f));
            violetGlass=Tint(art.glowCyan,"Amethyst stained glass",new Color(.65f,.15f,1.4f));
            Floor(new Rect(-size.x/2,-size.y/2,size.x,size.y),art.marble,0,3);
            Floor(new Rect(-3.2f,-size.y/2+2,6.4f,size.y-4),carpet,.018f,4);
            foreach (var x in new[]{-3.35f,3.35f}) Floor(new Rect(x,-size.y/2+2,.09f,size.y-4),art.gold,.025f,3);
            for (var z=-49;z<=49;z+=7) foreach (var x in new[]{-7f,7f})
            {
                var seal=SceneArt.Ring("Marble inlay",transform,1.35f,.035f,art.gold,48);
                seal.transform.localPosition=new Vector3(x,.03f,z);
            }
            // Near walls are cut low so the overhead camera keeps the combat legible.
            foreach (var sign in new[]{-1,1})
            {
                var x=sign*(size.x/2-.5f); var height=sign<0?1.1f:9;
                Box("Cathedral side wall",new Vector3(x,height/2,0),new Vector3(.9f,height,size.y),art.cathedralStone,true);
                Box("Gilded side plinth",new Vector3(x,.25f,0),new Vector3(1.3f,.5f,size.y),art.marble);
                for (var z=-42;z<=42;z+=14)
                {
                    Column(new Vector3(sign*11,0,z),sign<0 && z<0?6:9);
                    Column(new Vector3(sign*35,0,z),sign<0?4:8);
                    if (sign>0) Window(new Vector3(x-.55f,3.2f,z),true);
                    CandleStand(new Vector3(sign*8,0,z+3));
                }
            }
            Box("Western cutaway threshold",new Vector3(0,.5f,-size.y/2+.5f),new Vector3(size.x,1,1),art.cathedralStone,true);
            Box("Apse wall",new Vector3(0,5,size.y/2-.5f),new Vector3(size.x,10,1),art.cathedralStone,true);
            for (var x=-28;x<=28;x+=14) Window(new Vector3(x,3.2f,size.y/2-1.1f),false);
            for (var z=14;z<=42;z+=14) Arch(new Vector3(-11,6,z),new Vector3(11,6,z),6.5f);
            foreach (var sign in new[]{-1,1}) for (var z=-42;z<42;z+=14)
                Arch(new Vector3(sign*11,6,z),new Vector3(sign*11,6,z+14),4);
            for (var z=-38;z<=32;z+=7) foreach (var sign in new[]{-1,1})
            {
                Pew(new Vector3(sign*22,0,z)); Pew(new Vector3(sign*31,0,z));
            }
            // Preserve the central altar and waystone quest anchors for existing play.
            Box("Central altar",new Vector3(0,.55f,3),new Vector3(3.6f,1.1f,3.6f),art.marble,true);
            Box("Altar gilded cornice",new Vector3(0,1.15f,3),new Vector3(4,.15f,4),art.gold);
            Box("Altar cloth",new Vector3(0,1.24f,3),new Vector3(1.5f,.025f,4.05f),art.vestment);
            foreach (var x in new[]{-1.4f,1.4f}) CandleStand(new Vector3(x,1.23f,3.5f),.55f);
            var relic=Shape(PrimitiveType.Cube,"Suspended dawn relic",new Vector3(0,2.4f,3),Vector3.one*.55f,art.gold);
            relic.transform.rotation=Quaternion.Euler(45,25,45);
            SceneArt.PointLight("Altar radiance",transform,new Vector3(0,3,3),new Color(.55f,.78f,1),4,10);
            RoseWindow(new Vector3(0,7.8f,size.y/2-1.2f));
            for (var i=0;i<3;i++)
                Box("Apse dais",new Vector3(0,.12f+i*.16f,47+i*.8f),new Vector3(14-i*2,.22f,4-i*.4f),art.marble,true);
            Box("High altar",new Vector3(0,1.2f,49),new Vector3(6,1.4f,1.5f),art.marble,true);
            Box("High altar gold",new Vector3(0,1.96f,49),new Vector3(6.4f,.12f,1.7f),art.gold);
            foreach (var x in new[]{-5f,5f}) CandleStand(new Vector3(x,.6f,48),1.8f);
            foreach (var x in new[]{-32f,32f}) foreach (var z in new[]{-46f,44f}) Memorial(new Vector3(x,0,z));
            var portal=SceneArt.Ring("Dawn gate",transform,2.1f,.1f,art.glowGold);
            portal.transform.localPosition=new Vector3(0,2.8f,53.6f); portal.transform.localRotation=Quaternion.Euler(90,0,0);
            waystone(new Vector3(-13,0,6),"Mercy"); waystone(new Vector3(12,0,6),"Courage"); waystone(new Vector3(0,0,-13),"Hope");
            StaticBatchingUtility.Combine(gameObject);
        }
        void Floor(Rect rect,Material mat,float height,float repeat)
        {
            var mesh=new Mesh {name="World-scaled cathedral surface"};
            mesh.vertices=new[]{new Vector3(rect.xMin,height,rect.yMin),new Vector3(rect.xMin,height,rect.yMax),new Vector3(rect.xMax,height,rect.yMax),new Vector3(rect.xMax,height,rect.yMin)};
            mesh.uv=new[]{new Vector2(rect.xMin,rect.yMin)/repeat,new Vector2(rect.xMin,rect.yMax)/repeat,new Vector2(rect.xMax,rect.yMax)/repeat,new Vector2(rect.xMax,rect.yMin)/repeat};
            mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateNormals(); meshes.Add(mesh);
            var part=new GameObject("Marble nave surface"); part.transform.SetParent(transform,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh; part.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        void Column(Vector3 point,float height)
        {
            Box("Column pedestal",point+Vector3.up*.25f,new Vector3(2.1f,.5f,2.1f),art.marble,true);
            Shape(PrimitiveType.Cylinder,"Clustered limestone shaft",point+Vector3.up*(height/2),new Vector3(1.1f,height/2,1.1f),art.cathedralStone);
            nav(point+Vector3.up*(height/2),new Vector3(1.5f,height,1.5f),1);
            for (var i=0;i<4;i++)
            {
                var angle=i*Mathf.PI/2;
                Shape(PrimitiveType.Cylinder,"Engaged column",point+new Vector3(Mathf.Cos(angle)*.5f,height/2,Mathf.Sin(angle)*.5f),new Vector3(.35f,height/2,.35f),art.marble);
            }
            foreach (var y in new[]{.6f,height-.5f})
                Shape(PrimitiveType.Cylinder,"Gilded capital",point+Vector3.up*y,new Vector3(1.6f,.09f,1.6f),art.gold);
            Box("Carved capital",point+Vector3.up*(height-.2f),new Vector3(1.9f,.4f,1.9f),art.marble);
        }
        void Arch(Vector3 left,Vector3 right,float rise)
        {
            var previous=left;
            for (var i=1;i<=20;i++)
            {
                var t=i/20f; var point=Vector3.Lerp(left,right,t)+Vector3.up*(rise*(1-Mathf.Pow(Mathf.Abs(t*2-1),1.3f)));
                var delta=point-previous;
                var rib=Shape(PrimitiveType.Cube,"Pointed Gothic rib",(point+previous)/2,new Vector3(.34f,.34f,delta.magnitude+.025f),art.cathedralStone);
                rib.transform.rotation=Quaternion.LookRotation(delta);
                previous=point;
            }
        }
        void Pew(Vector3 point)
        {
            Box("Oak pew bench",point+Vector3.up*.55f,new Vector3(6,.16f,1.05f),art.cathedralWood,true);
            Box("Pew carved back",point+new Vector3(0,1,.5f),new Vector3(6,.95f,.15f),art.cathedralWood);
            foreach (var x in new[]{-2.7f,2.7f})
            {
                Box("Pew end",point+new Vector3(x,.6f,0),new Vector3(.22f,1.2f,1.3f),art.cathedralWood);
                Shape(PrimitiveType.Sphere,"Pew finial",point+new Vector3(x,1.3f,.45f),Vector3.one*.23f,art.gold);
            }
        }
        void CandleStand(Vector3 point,float scale=1)
        {
            Shape(PrimitiveType.Cylinder,"Gilded candelabrum",point+Vector3.up*.65f*scale,new Vector3(.14f, .65f,.14f)*scale,art.gold);
            Shape(PrimitiveType.Cylinder,"Lamp foot",point+Vector3.up*.08f,new Vector3(.6f,.08f,.6f)*scale,art.gold);
            for (var i=-1;i<=1;i++)
            {
                var p=point+new Vector3(i*.23f,1.4f,0)*scale;
                Shape(PrimitiveType.Cylinder,"Ivory candle",p,new Vector3(.08f,.16f,.08f)*scale,art.vestment);
                Shape(PrimitiveType.Sphere,"Candle flame",p+Vector3.up*.22f*scale,new Vector3(.055f,.14f,.055f)*scale,art.glowGold);
            }
            SceneArt.PointLight("Candle glow",transform,point+Vector3.up*1.7f*scale,new Color(1,.66f,.3f),1.3f,5);
        }
        void Window(Vector3 point,bool side)
        {
            var rotation=side?Quaternion.Euler(0,90,0):Quaternion.identity;
            var root=new GameObject("Lancet stained-glass window").transform; root.SetParent(transform,false); root.localPosition=point; root.localRotation=rotation;
            for (var i=-1;i<=1;i++)
                SceneArt.Shape(PrimitiveType.Cube,"Colored glass",root,new Vector3(i*.72f,1,0),new Vector3(.65f,3.2f,.06f),i==0?amberGlass:blueGlass);
            for (var i=-2;i<=2;i++)
                SceneArt.Shape(PrimitiveType.Cube,"Stone mullion",root,new Vector3(i*.72f,1,.08f),new Vector3(.1f,3.5f,.16f),art.marble);
            var line=SceneArt.Ring("Lancet tracery",root,1,.06f,art.gold,64); line.loop=false; line.positionCount=33;
            for (var i=0;i<33;i++) { var t=i/32f; line.SetPosition(i,new Vector3((t*2-1)*1.45f,2.6f+1.6f*(1-Mathf.Abs(t*2-1)),.1f)); }
            SceneArt.PointLight("Stained-glass glow",root,new Vector3(0,1,-1),new Color(.28f,.5f,1),2,8);
        }
        void RoseWindow(Vector3 point)
        {
            var root=new GameObject("Rose of the seraphim").transform; root.SetParent(transform,false); root.localPosition=point;
            var ring=SceneArt.Ring("Rose gilded rim",root,3.8f,.14f,art.gold,128); ring.transform.localRotation=Quaternion.Euler(90,0,0);
            for (var i=0;i<12;i++)
            {
                var angle=i*Mathf.PI/6; var p=new Vector3(Mathf.Cos(angle)*2.3f,Mathf.Sin(angle)*2.3f,0);
                var glass=SceneArt.Shape(PrimitiveType.Sphere,"Rose glass petal",root,p,new Vector3(1.4f,2.4f,.08f),i%3==0?amberGlass:i%3==1?blueGlass:violetGlass);
                glass.transform.localRotation=Quaternion.Euler(0,0,i*30-90);
            }
            SceneArt.Shape(PrimitiveType.Sphere,"Rose center",root,Vector3.zero,new Vector3(1.3f,1.3f,.12f),amberGlass);
        }
        void Memorial(Vector3 point)
        {
            Box("Angel memorial plinth",point+Vector3.up*.4f,new Vector3(2.6f,.8f,2.6f),art.marble,true);
            Shape(PrimitiveType.Capsule,"Robed memorial",point+Vector3.up*2.1f,new Vector3(.9f,1.3f,.75f),art.marble);
            Shape(PrimitiveType.Sphere,"Memorial head",point+Vector3.up*3.55f,Vector3.one*.65f,art.marble);
            for (var tier=0;tier<3;tier++) foreach (var sign in new[]{-1,1})
            {
                var wing=Shape(PrimitiveType.Sphere,"Carved angel wing",point+new Vector3(sign*1.1f,2.6f-tier*.45f,-.3f),new Vector3(1.8f,.45f,.16f),art.marble);
                wing.transform.rotation=Quaternion.Euler(0,0,sign*(45-tier*40));
            }
            CandleStand(point+new Vector3(2,0,0));
        }
        void OnDestroy() { foreach (var mesh in meshes) Destroy(mesh); foreach (var mat in materials) Destroy(mat); }
    }
}
