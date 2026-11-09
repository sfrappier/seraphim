using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // Imported dragon-armored knight with the existing bounded transformation ritual.
    public sealed class CathedralBossArt : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        Transform halo;
        public InfernalKnightAnimation Knight {get;private set;}
        Material ember,voidGlow;
        Light furnace;
        int previousPhase;
        GameObject ritual;
        ParticleSystem ascension;
        LineRenderer seal,innerSeal;
        readonly List<LineRenderer> spires=new List<LineRenderer>();
        float transformBegan,transformSeconds,nextEmission;
        bool transforming;
        public Transform Body { get; private set; }
        public float Slash { get; set; }
        public int Phase { get; set; }=1;
        public bool Channeling { get; set; }
        public bool TransformationActive => transforming;
        Material Material(Material template,Color tint)
        {
            var mat=new Material(template); mat.SetColor("_BaseColor",tint); materials.Add(mat); return mat;
        }
        Transform Node(string name,Transform parent,Vector3 position)
        {
            var obj=new GameObject(name).transform; obj.SetParent(parent,false); obj.localPosition=position; return obj;
        }
        public void Initialize(EnemyDirector director)
        {
            ember=Material(director.Game.models.glowGold,new Color(3,.38f,.04f));
            voidGlow=Material(director.Game.models.glowCyan,new Color(1.9f,.12f,3));
            Body=Node("Infernal Wyrm · dragon knight",transform,Vector3.zero);
            var model=Instantiate(director.Game.models.bossModel.prefab,Body); model.name="DRAGON NEMESIS";
            Knight=Body.gameObject.AddComponent<InfernalKnightAnimation>(); Knight.Initialize(GetComponent<DemonEnemy>(),director.Game.models);
            // Copy only this unique boss's materials, retaining every authored texture.
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for (var i=0;i<slots.Length;i++) { slots[i]=new Material(slots[i]); materials.Add(slots[i]); }
                renderer.sharedMaterials=slots;
            }
            halo=Node("Infernal crown seal",Body,new Vector3(0,5.7f,-.5f)); halo.localRotation=Quaternion.Euler(90,0,0);
            SceneArt.Ring("Profane halo",halo,1.45f,.035f,voidGlow,96);
            furnace=SceneArt.PointLight("Ashen furnace light",Body,new Vector3(0,3.5f,1),new Color(1,.12f,.3f),4,12);
            ritual=new GameObject("Ascension of Ash · transformation"); ritual.transform.SetParent(transform,false);
            seal=SceneArt.Ring("Ascension seal",ritual.transform,4,.14f,voidGlow,128); seal.transform.localPosition=Vector3.up*.13f;
            innerSeal=SceneArt.Ring("Rising crown seal",ritual.transform,3,.07f,ember,96);
            ascension=CombatArt.Motes(ritual.transform,director.Game.models.celestialMote,450,new Color(1,.2f,.55f),5,1.2f);
            var shape=ascension.shape; shape.shapeType=ParticleSystemShapeType.Circle; shape.radius=3.5f;
            ascension.transform.localRotation=Quaternion.Euler(-90,0,0);
            var rising=ascension.velocityOverLifetime; rising.enabled=true; rising.space=ParticleSystemSimulationSpace.World;
            rising.x=0; rising.y=4; rising.z=0;
            for (var i=0;i<8;i++)
            {
                var line=new GameObject("Ritual lightning spire").AddComponent<LineRenderer>(); line.transform.SetParent(ritual.transform,false);
                line.useWorldSpace=false; line.positionCount=7; line.widthMultiplier=.07f; line.sharedMaterial=voidGlow;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; spires.Add(line);
            }
            ritual.SetActive(false);
        }
        public void BeginTransformation(float seconds)
        {
            Knight.Transform(seconds);
            transforming=true; transformBegan=Time.time; transformSeconds=seconds; nextEmission=0;
            ritual.SetActive(true); ascension.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); ascension.Emit(120);
        }
        public void ResetForEncounter()
        {
            transforming=false; Slash=0; Phase=1; Channeling=false;
            if (Knight!=null) Knight.ResetForEncounter();
            if (Body!=null) Body.localRotation=Quaternion.identity;
            if (ritual!=null) ritual.SetActive(false);
            if (ascension!=null) ascension.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
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
            if (transforming)
            {
                var t=Mathf.Clamp01((Time.time-transformBegan)/transformSeconds);
                var alpha=Mathf.Clamp01((transformSeconds+.65f-(Time.time-transformBegan))/.65f);
                Body.localPosition+=Vector3.up*Mathf.Sin(t*Mathf.PI)*.9f;
                seal.transform.localScale=Vector3.one*(1+t*.6f);
                seal.transform.localRotation=Quaternion.Euler(0,Time.time*85,0);
                innerSeal.transform.localPosition=Vector3.up*(.3f+t*8);
                innerSeal.transform.localScale=Vector3.one*(1+t*.5f);
                seal.startColor=seal.endColor=innerSeal.startColor=innerSeal.endColor=new Color(1,1,1,alpha);
                for (var i=0;i<spires.Count;i++)
                {
                    var angle=i*Mathf.PI/4; var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                    for (var p=0;p<7;p++)
                        spires[i].SetPosition(p,radial*(3.5f+t*2)+Vector3.up*(p*1.6f)+Vector3.right*Mathf.Sin(Time.time*40+p*7+i)*.2f);
                    spires[i].startColor=spires[i].endColor=new Color(1,1,1,alpha);
                }
                if (Time.time>=nextEmission) { nextEmission=Time.time+.08f; ascension.Emit(14); }
                if (Time.time-transformBegan>=transformSeconds+.65f) { transforming=false; ritual.SetActive(false); }
            }
            halo.localRotation=Quaternion.Euler(90,Time.time*(9+Phase*3),0);
            halo.localScale=Vector3.one*(1+(Phase-1)*.06f);
            Body.localRotation=Quaternion.identity;
        }
        void OnDestroy() { foreach (var mat in materials) Destroy(mat); }
    }
}
