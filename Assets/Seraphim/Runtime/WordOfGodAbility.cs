using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seraphim
{
    public sealed class WordOfGodAbility : MonoBehaviour
    {
        public enum Phase { Ready,Charging,Revelation,Ascended,Fading }
        const float ChargeTime=1;
        const float FadeTime=.8f;
        SanctuarySettings settings;
        SeraphimVitals vitals;
        SeraphimWings wings;
        Transform visual;
        Vector3 originalVisualPosition;
        List<Waystone> stones;
        readonly List<Renderer> characterRenderers=new List<Renderer>();
        readonly List<Material[]> originalMaterials=new List<Material[]>();
        Material ascendedMaterial;
        GameObject effectRoot;
        Transform beam,chargeSphere;
        readonly List<LineRenderer> rings=new List<LineRenderer>();
        readonly List<LineRenderer> rays=new List<LineRenderer>();
        ParticleSystem chargeMotes,burstMotes,ascendingMotes;
        Light heavenLight,groundLight;
        Volume volume;
        VolumeProfile profile;
        Mesh beamMesh;
        float readyAt,startedAt;
        bool revealed;
        EnemyDirector enemies;
        float nextPulseAt;
        float chargeEmission,risingEmission;
        public int CastCount { get; private set; }
        public int PulseCount { get; private set; }
        public bool IsActive { get; private set; }
        public float VisualPower { get; private set; }
        public Phase Stage { get; private set; }
        public float CooldownRemaining => Mathf.Clamp(readyAt-Time.time,0,settings.ultimateCooldown);
        public bool CanCast => !IsActive && CooldownRemaining<=0;
        public float ChargeProgress => IsActive?Mathf.Clamp01((Time.time-startedAt)/ChargeTime):0;
        public float ActiveRemaining => IsActive?Mathf.Clamp(startedAt+ChargeTime+settings.ultimateDuration-Time.time,0,settings.ultimateDuration):0;
        public float Elapsed => IsActive?Time.time-startedAt:0;
        public Vector3 EffectPosition => effectRoot!=null?effectRoot.transform.position:transform.position;

        public void Initialize(SanctuarySettings config,PrototypeModels art,SeraphimVitals resources,List<Waystone> waystones,EnemyDirector director)
        {
            enemies=director;
            settings=config;
            vitals=resources;
            stones=waystones;
            wings=GetComponent<SeraphimWings>();
            visual=transform.Find("Character visual");
            originalVisualPosition=visual.localPosition;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                characterRenderers.Add(renderer);
                originalMaterials.Add(renderer.sharedMaterials);
            }
            ascendedMaterial=new Material(art.ivory) {name="Word of God ascended form"};
            ascendedMaterial.EnableKeyword("_EMISSION");
            ascendedMaterial.SetColor("_BaseColor",new Color(1,.92f,.65f));
            ascendedMaterial.SetColor("_EmissionColor",new Color(3,2.1f,.6f));
            BuildEffects(art);
        }

        void BuildEffects(PrototypeModels art)
        {
            effectRoot=new GameObject("Word of God · pooled effects");
            beam=new GameObject("Pillar of heaven").transform;
            beam.SetParent(effectRoot.transform,false);
            beamMesh=CreateBeam();
            beam.gameObject.AddComponent<MeshFilter>().sharedMesh=beamMesh;
            beam.gameObject.AddComponent<MeshRenderer>().sharedMaterial=art.glowGold;
            chargeSphere=SceneArt.Shape(PrimitiveType.Sphere,"Gathering light",effectRoot.transform,Vector3.up*1.5f,Vector3.one,art.celestialMote).transform;
            for (var i=0;i<5;i++)
            {
                var ring=SceneArt.Ring("Divine seal "+i,effectRoot.transform,1,i==0?.1f:.035f,i%2==0?art.glowGold:art.glowCyan,128);
                ring.transform.localPosition=Vector3.up*(.08f+i*.025f);
                if (i==1)
                {
                    ring.positionCount=12;
                    for (var j=0;j<12;j++)
                    {
                        var angle=(j*5%12)*Mathf.PI/6;
                        ring.SetPosition(j,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));
                    }
                }
                rings.Add(ring);
            }
            for (var i=0;i<12;i++)
            {
                var ray=new GameObject("Heaven ray").AddComponent<LineRenderer>();
                ray.transform.SetParent(effectRoot.transform,false);
                ray.useWorldSpace=false;
                ray.positionCount=3;
                ray.sharedMaterial=art.glowGold;
                ray.widthMultiplier=.045f;
                var angle=i*Mathf.PI/6;
                ray.SetPosition(0,new Vector3(Mathf.Cos(angle)*4,.12f,Mathf.Sin(angle)*4));
                ray.SetPosition(1,new Vector3(Mathf.Cos(angle)*2,8,Mathf.Sin(angle)*2));
                ray.SetPosition(2,new Vector3(Mathf.Cos(angle)*.3f,22,Mathf.Sin(angle)*.3f));
                rays.Add(ray);
            }
            chargeMotes=CreateParticles("Gathering motes",art.celestialMote,260,1.6f,.08f,2);
            burstMotes=CreateParticles("Revelation burst",art.celestialMote,settings.ultimateParticles,1.7f,.14f,13);
            ascendingMotes=CreateParticles("Ascending starlight",art.celestialMote,500,2.5f,.09f,7);
            var chargeShape=chargeMotes.shape; chargeShape.radius=1.5f;
            var riseShape=ascendingMotes.shape; riseShape.shapeType=ParticleSystemShapeType.Cone; riseShape.angle=9; riseShape.radius=3.5f;
            ascendingMotes.transform.localRotation=Quaternion.Euler(-90,0,0);
            heavenLight=SceneArt.PointLight("Heaven light",effectRoot.transform,Vector3.up*6,new Color(1,.87f,.6f),0,20);
            groundLight=SceneArt.PointLight("Revelation light",effectRoot.transform,Vector3.up*2,new Color(.55f,.8f,1),0,14);
            volume=effectRoot.AddComponent<Volume>();
            volume.isGlobal=true;
            volume.priority=50;
            volume.weight=0;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(1.4f); bloom.threshold.Override(.8f);
            var exposure=profile.Add<ColorAdjustments>(true); exposure.postExposure.Override(.95f);
            volume.sharedProfile=profile;
            effectRoot.SetActive(false);
        }

        ParticleSystem CreateParticles(string name,Material material,int maximum,float lifetime,float size,float speed)
        {
            var particles=new GameObject(name).AddComponent<ParticleSystem>();
            particles.transform.SetParent(effectRoot.transform,false);
            particles.transform.localPosition=Vector3.up*1.1f;
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;
            main.playOnAwake=false; main.loop=false;
            main.maxParticles=maximum;
            main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.6f,lifetime);
            main.startSize=new ParticleSystem.MinMaxCurve(size*.35f,size);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.45f,speed);
            main.startColor=new Color(1,.9f,.65f,.85f);
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission; emission.enabled=false;
            var shape=particles.shape; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=.5f;
            var fade=particles.colorOverLifetime;
            fade.enabled=true;
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.7f,.3f),1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});
            fade.color=gradient;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            return particles;
        }

        public bool Cast()
        {
            if (!CanCast) return false;
            readyAt=Time.time+settings.ultimateCooldown;
            startedAt=Time.time;
            IsActive=true;
            revealed=false;
            PulseCount=0;
            chargeEmission=risingEmission=0;
            Stage=Phase.Charging;
            CastCount++;
            enemies.Game.Audio?.Play(SoundCue.Ascend,transform.position);
            enemies.Game.Presentation.Play(EffectCue.Ascend,transform.position,transform);
            effectRoot.transform.position=transform.position;
            effectRoot.SetActive(true);
            chargeMotes.Emit(100);
            for (var i=0;i<characterRenderers.Count;i++)
            {
                var materials=new Material[originalMaterials[i].Length];
                for (var j=0;j<materials.Length;j++) materials[j]=ascendedMaterial;
                characterRenderers[i].sharedMaterials=materials;
            }
            return true;
        }

        void Reveal()
        {
            revealed=true;
            burstMotes.Emit(settings.ultimateParticles);
            ascendingMotes.Emit(180);
            vitals.RestoreLife(25);
            vitals.RestoreSpirit(35);
            foreach (var stone in stones) if (Vector3.Distance(transform.position,stone.transform.position)<=settings.ultimateRadius) stone.Attune();
            var camera=Camera.main.GetComponent<SanctuaryCamera>();
            if (camera!=null) camera.Impact();
            enemies.Game.Audio?.Play(SoundCue.Revelation,transform.position);
            enemies.Game.Presentation.Play(EffectCue.Revelation,transform.position+Vector3.up*.25f);
            enemies.Game.Presentation.Play(EffectCue.DivineAura,transform.position,transform,1,settings.ultimateDuration);
            Pulse();
            nextPulseAt=startedAt+ChargeTime+settings.ultimatePulseInterval;
        }

        void Pulse()
        {
            PulseCount++;
            if (PulseCount>1) enemies.Game.Audio?.Play(SoundCue.Pulse,transform.position);
            enemies.Game.Presentation.Play(EffectCue.Pulse,transform.position);
            enemies.Radial(transform.position,settings.ultimateRadius,settings.ultimateDamage,DivineDamage.WordOfGod,null,.35f,.15f);
            if (PulseCount>1) burstMotes.Emit(100);
            foreach (var stone in stones) if (Vector3.Distance(transform.position,stone.transform.position)<=settings.ultimateRadius) stone.Attune();
        }

        void Update()
        {
            if (!IsActive) return;
            var age=Time.time-startedAt;
            var activeEnd=ChargeTime+settings.ultimateDuration;
            if (age>=activeEnd+FadeTime) { RestoreForm(); return; }
            var charge=Mathf.Clamp01(age/ChargeTime);
            var after=Mathf.Max(0,age-ChargeTime);
            if (!revealed && age>=ChargeTime) Reveal();
            if (revealed) while (Time.time>=nextPulseAt && nextPulseAt<startedAt+activeEnd-.01f) { Pulse(); nextPulseAt+=settings.ultimatePulseInterval; }
            Stage=age<ChargeTime?Phase.Charging:age<1.45f?Phase.Revelation:age<activeEnd?Phase.Ascended:Phase.Fading;
            VisualPower=age<ChargeTime?Mathf.SmoothStep(0,.65f,charge):Mathf.SmoothStep(1,0,Mathf.Clamp01((age-activeEnd)/FadeTime));
            effectRoot.transform.position=transform.position;
            wings.Ascension=VisualPower;
            visual.localPosition=originalVisualPosition+Vector3.up*(VisualPower*.42f);
            ascendedMaterial.SetColor("_EmissionColor",new Color(1,.72f,.28f)*(1+VisualPower*4));
            chargeSphere.gameObject.SetActive(!revealed);
            chargeSphere.localScale=Vector3.one*(.2f+charge*.8f);
            var rhythm=.65f+.35f*Mathf.Cos(after*Mathf.PI*2/settings.ultimatePulseInterval);
            var revealFade=revealed?VisualPower*rhythm:charge*.15f;
            beam.localScale=new Vector3(Mathf.Lerp(.1f,1.3f,VisualPower),revealed?1:charge*.65f,Mathf.Lerp(.1f,1.3f,VisualPower));
            var flash=revealed?Mathf.Exp(-after*5):0;
            heavenLight.intensity=VisualPower*10+flash*14;
            groundLight.intensity=VisualPower*5+flash*6;
            volume.weight=age<ChargeTime?charge*.12f:Mathf.Max(flash*.65f,revealFade*.22f);
            var followCamera=Camera.main.GetComponent<SanctuaryCamera>();
            if (followCamera!=null) followCamera.DivinePower=VisualPower;
            for (var i=0;i<rings.Count;i++)
            {
                var wave=Mathf.Repeat(after/settings.ultimatePulseInterval+(i-2)/3f,1);
                var radius=i<2 ? Mathf.Lerp(.8f,5.2f,charge) : revealed?Mathf.Lerp(1,settings.ultimateRadius,wave):.3f;
                var alpha=i<2 ? VisualPower*.8f : revealed?(1-wave)*VisualPower*.7f:0;
                rings[i].transform.localScale=Vector3.one*radius;
                rings[i].transform.localRotation=Quaternion.Euler(0,(i%2==0?1:-1)*age*20,0);
                rings[i].startColor=rings[i].endColor=new Color(1,1,1,alpha);
            }
            foreach (var ray in rays) ray.startColor=ray.endColor=new Color(1,.96f,.8f,revealFade*.65f);
            if (age<ChargeTime)
            {
                chargeEmission+=Time.deltaTime*120;
                var count=Mathf.FloorToInt(chargeEmission);
                if (count>0) { chargeMotes.Emit(count); chargeEmission-=count; }
            }
            if (revealed && age<activeEnd)
            {
                risingEmission+=Time.deltaTime*100;
                var count=Mathf.FloorToInt(risingEmission);
                if (count>0) { ascendingMotes.Emit(count); risingEmission-=count; }
            }
        }

        void LateUpdate()
        {
            if (IsActive) effectRoot.transform.position=transform.position;
        }

        public void Cancel() { if (IsActive) RestoreForm(); }
        public void ResetForRun() { Cancel(); readyAt=0; CastCount=PulseCount=0; }

        void RestoreForm()
        {
            enemies.Game.Presentation.ClearUltimate();
            IsActive=false;
            Stage=Phase.Ready;
            VisualPower=0;
            if (wings!=null) wings.Ascension=0;
            if (visual!=null) visual.localPosition=originalVisualPosition;
            for (var i=0;i<characterRenderers.Count;i++) if (characterRenderers[i]!=null) characterRenderers[i].sharedMaterials=originalMaterials[i];
            if (Camera.main!=null)
            {
                var camera=Camera.main.GetComponent<SanctuaryCamera>();
                if (camera!=null) camera.DivinePower=0;
            }
            if (volume!=null) volume.weight=0;
            if (effectRoot!=null)
            {
                chargeMotes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                burstMotes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                ascendingMotes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                effectRoot.SetActive(false);
            }
        }

        static Mesh CreateBeam()
        {
            const int segments=64;
            var vertices=new Vector3[(segments+1)*5];
            var colors=new Color[vertices.Length];
            var triangles=new int[segments*4*6];
            for (var row=0;row<5;row++)
            for (var i=0;i<=segments;i++)
            {
                var angle=i*Mathf.PI*2/segments;
                var radius=row==0?.35f:row==1?.8f:row==2?.5f:row==3?1.4f:2.2f;
                var height=row==0?.1f:row==1?.5f:row==2?3:row==3?13:24;
                var index=row*(segments+1)+i;
                vertices[index]=new Vector3(Mathf.Cos(angle)*radius,height,Mathf.Sin(angle)*radius);
                colors[index]=new Color(1,1,1,row==0?0:row==1?.35f:row==2?.14f:row==3?.06f:0);
                if (row==4 || i==segments) continue;
                var t=(row*segments+i)*6;
                triangles[t]=index; triangles[t+1]=index+segments+1; triangles[t+2]=index+1;
                triangles[t+3]=index+1; triangles[t+4]=index+segments+1; triangles[t+5]=index+segments+2;
            }
            var mesh=new Mesh {name="Pillar of heaven",vertices=vertices,colors=colors,triangles=triangles};
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDisable() { if (effectRoot!=null) RestoreForm(); }
        void OnDestroy()
        {
            if (effectRoot!=null) Destroy(effectRoot);
            if (ascendedMaterial!=null) Destroy(ascendedMaterial);
            if (beamMesh!=null) Destroy(beamMesh);
            if (profile!=null) { foreach (var component in profile.components) Destroy(component); Destroy(profile); }
        }
    }
}
