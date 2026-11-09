using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seraphim.Editor
{
    public static class DownloadedPresentationBuilder
    {
        const string Store="Assets/ThirdParty/AssetStore/";
        const string Output=Store+"SeraphimPresentation/";
        const string Hovl=Store+"Hovl Studio/Magic effects pack/Prefabs/";
        const string UnityFx=Store+"UnityTechnologies/ParticlePack/EffectExamples/";
        const string Rpg=Store+"SFX Packs/General RPG SFX Pack/";
        const string Small=Store+"Small Sound Kit/";
        static readonly Color Gold=new Color(1,.82f,.42f),Silver=new Color(.72f,.88f,1),Blue=new Color(.28f,.75f,1),Purple=new Color(.8f,.25f,1),Fire=new Color(1,.32f,.07f);
        struct Recipe
        {
            public EffectCue cue;public string path;public float scale,seconds;public Color tint;public int limit;public bool boss;
            public Recipe(EffectCue c,string p,float s,float t,Color color,int cap=3,bool b=false) {cue=c;path=p;scale=s;seconds=t;tint=color;limit=cap;boss=b;}
        }
        public static void Configure()
        {
            NormalizeImportedFolders();
            if (!Directory.Exists(Hovl) && !Directory.Exists(Rpg) && !Directory.Exists(Small) && !Directory.Exists(UnityFx))
            {
                Debug.Log("SERAPHIM_PRESENTATION: imported packs absent; original audio/particles retained."); return;
            }
            Directory.CreateDirectory(Output+"Resources"); Directory.CreateDirectory(Output+"Effects"); Directory.CreateDirectory(Output+"Materials"); AssetDatabase.Refresh();
            var path=Output+"Resources/SeraphimPresentation.asset";
            var bank=AssetDatabase.LoadAssetAtPath<PresentationBank>(path);
            if (bank==null) {bank=ScriptableObject.CreateInstance<PresentationBank>(); AssetDatabase.CreateAsset(bank,path);}
            var sounds=new List<SoundBinding>();
            void Sound(SoundCue cue,float gain,params string[] sources)
            {
                var clips=new List<AudioClip>();
                foreach (var source in sources)
                {
                    if (!File.Exists(source)) continue;
                    var importer=(AudioImporter)AssetImporter.GetAtPath(source);
                    var settings=importer.defaultSampleSettings;
                    if (!importer.forceToMono || settings.loadType!=AudioClipLoadType.DecompressOnLoad)
                    {
                        importer.forceToMono=true; settings.loadType=AudioClipLoadType.DecompressOnLoad;
                        settings.compressionFormat=AudioCompressionFormat.Vorbis; settings.quality=.8f;
                        importer.defaultSampleSettings=settings; importer.SaveAndReimport();
                    }
                    clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(source));
                }
                if (clips.Count>0) sounds.Add(new SoundBinding {cue=cue,gain=gain,variants=clips.ToArray()});
            }
            Sound(SoundCue.Radiance,.85f,Rpg+"Magic SFX/Holy Magic.wav");
            Sound(SoundCue.Spear,.8f,Rpg+"Magic SFX/Wind Magic.wav");
            Sound(SoundCue.Lightning,.8f,Small+"Magic/Mgc_Electric_Throw_01.wav");
            Sound(SoundCue.Fire,.85f,Small+"Magic/Mgc_Fire_Cast_01.wav");
            Sound(SoundCue.Ascend,.8f,Rpg+"Magic SFX/Spell Casting 2.wav");
            Sound(SoundCue.Revelation,.85f,Rpg+"Magic SFX/Resurrection.wav");
            Sound(SoundCue.Pulse,.65f,Rpg+"Magic SFX/Heal 2.wav");
            Sound(SoundCue.Hit,.7f,Small+"Magic/Mgc_Electric_Hit_01.wav");
            Sound(SoundCue.Banish,.65f,Rpg+"Magic SFX/Dark Magic.wav");
            Sound(SoundCue.Hurt,.7f,Small+"Magic/Mgc_Electric_Impact_01.wav");
            Sound(SoundCue.Life,.8f,Rpg+"Magic SFX/Heal 1.wav");
            Sound(SoundCue.Spirit,.8f,Rpg+"UI SFX/Use Item.wav");
            Sound(SoundCue.Step,.8f,Enumerable.Range(1,4).Select(i=>Small+"Footsteps/S_Concrete_Mono_"+i+".wav").ToArray());
            Sound(SoundCue.Warning,.7f,Rpg+"Magic SFX/Rock Magic.wav");
            Sound(SoundCue.Charge,.8f,Small+"Magic/Mgc_Fire_Throw_01.wav");
            Sound(SoundCue.Wave,.8f,Rpg+"UI SFX/Save.wav");
            Sound(SoundCue.Victory,.9f,Rpg+"Magic SFX/Resurrection.wav");
            Sound(SoundCue.Ui,.8f,Directory.Exists(Rpg+"UI SFX")?Directory.GetFiles(Rpg+"UI SFX","Select*.wav").FirstOrDefault()??"":"");
            Sound(SoundCue.BossSwing,1,Rpg+"Magic SFX/Wind Magic.wav");
            Sound(SoundCue.Meteor,1,Small+"Magic/Mgc_Fire_Impact_01.wav");
            Sound(SoundCue.Hellfire,.8f,Small+"Magic/Mgc_Fire_Throw_01.wav");
            Sound(SoundCue.Nova,.85f,Rpg+"Magic SFX/Lightning Magic.wav");
            Sound(SoundCue.Roar,.9f,Rpg+"Magic SFX/Dark Magic.wav");
            bank.sounds=sounds.ToArray();
            var recipes=new[] {
                new Recipe(EffectCue.Radiance,Hovl+"Hits and explosions/Holy hit.prefab",1.2f,1,Gold,3),
                new Recipe(EffectCue.SpearCast,Hovl+"Slash effects/Charge slash blue.prefab",.55f,.65f,Gold),
                new Recipe(EffectCue.SpearHit,Hovl+"Hits and explosions/Star hit.prefab",.5f,.65f,Gold,4),
                new Recipe(EffectCue.LightningCast,Hovl+"Character auras/Lightning aura.prefab",.5f,.65f,Blue),
                new Recipe(EffectCue.LightningHit,Hovl+"Hits and explosions/Electro hit.prefab",.65f,.7f,Blue,5),
                new Recipe(EffectCue.Inferno,Hovl+"AoE effects/AoE slash orange.prefab",1.1f,1.4f,Gold,2),
                new Recipe(EffectCue.Ascend,Hovl+"Magic circles/Magic circle 2.prefab",1.2f,1.1f,Gold,1),
                new Recipe(EffectCue.Revelation,UnityFx+"Fire & Explosion Effects/Prefabs/EnergyExplosion.prefab",1.6f,2.1f,Gold,1),
                new Recipe(EffectCue.DivineAura,Hovl+"Character auras/Star aura.prefab",.65f,15,Gold,1),
                new Recipe(EffectCue.Pulse,Hovl+"Magic circles/Magic circle.prefab",1.6f,1.1f,Gold,2),
                new Recipe(EffectCue.Life,Hovl+"Character auras/Healing.prefab",.35f,.7f,Gold,2),
                new Recipe(EffectCue.Spirit,Hovl+"Character auras/Buff.prefab",.35f,.7f,Silver,2),
                new Recipe(EffectCue.EnemySpawn,Hovl+"Portals/Portal red.prefab",.35f,.85f,Purple,3),
                new Recipe(EffectCue.EnemyHit,Hovl+"Sparks/Sparks explode yellow.prefab",.45f,.45f,Gold,5),
                new Recipe(EffectCue.Banish,Hovl+"Environment/Teleport.prefab",.6f,.85f,Silver,4),
                new Recipe(EffectCue.Hurt,Hovl+"Magic shields/Magic shield yellow.prefab",.55f,.5f,Gold,2),
                new Recipe(EffectCue.EnemyRift,Hovl+"AoE effects/Plexus AoE.prefab",.5f,.7f,Purple,2),
                new Recipe(EffectCue.BossMeteor,UnityFx+"Fire & Explosion Effects/Prefabs/BigExplosion.prefab",1.1f,1.4f,Fire,5,true),
                new Recipe(EffectCue.BossFire,UnityFx+"Fire & Explosion Effects/Prefabs/FlameThrower.prefab",.4f,1.7f,Fire,5,true),
                new Recipe(EffectCue.BossNova,Hovl+"AoE effects/Red energy explosion.prefab",1.4f,1,Purple,2,true),
                new Recipe(EffectCue.BossSlash,Hovl+"Slash effects/Charge slash red.prefab",1.3f,.55f,Fire,2,true),
                new Recipe(EffectCue.BossTransform,Hovl+"AoE effects/Meteors AOE.prefab",.55f,3.5f,Fire,1,true),
                new Recipe(EffectCue.BossCast,Hovl+"Character auras/Plexus.prefab",.5f,1.3f,Fire,2,true),
                new Recipe(EffectCue.Ambient,UnityFx+"Misc Effects/Prefabs/DustMotesEffect.prefab",1,30,Gold,1)
            };
            var effects=new List<EffectBinding>();
            foreach (var recipe in recipes)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(recipe.path);
                if (source==null) { Debug.LogWarning("Optional effect absent: "+recipe.path); continue; }
                var instance=UnityEngine.Object.Instantiate(source); instance.name=recipe.cue.ToString();
                try
                {
                    foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
                    foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                    foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
                    foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(audio);
                    foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled=false;
                    var systems=instance.GetComponentsInChildren<ParticleSystem>(true);
                    var budget=recipe.cue==EffectCue.DivineAura?1100:recipe.boss?950:650;
                    foreach (var ps in systems)
                    {
                        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main=ps.main; main.playOnAwake=false; main.stopAction=ParticleSystemStopAction.None;
                        main.useUnscaledTime=false;
                        main.maxParticles=Mathf.Min(main.maxParticles,Mathf.Max(12,budget/Mathf.Max(1,systems.Length)));
                        var smoky=ps.name.IndexOf("Smoke",StringComparison.OrdinalIgnoreCase)>=0;
                        main.startColor=new Color(recipe.tint.r,recipe.tint.g,recipe.tint.b,smoky?.18f:.8f);
                        var color=ps.colorOverLifetime;
                        if (color.enabled)
                        {
                            var gradient=new Gradient();
                            var old=color.color.gradient;
                            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},old!=null?old.alphaKeys:new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});
                            color.color=gradient;
                        }
                        var collision=ps.collision;collision.enabled=false;var trigger=ps.trigger;trigger.enabled=false;var lights=ps.lights;lights.enabled=false;
                        var trails=ps.trails; if (trails.enabled) trails.ratio=Mathf.Min(trails.ratio,.3f);
                        var renderer=ps.GetComponent<ParticleSystemRenderer>();
                        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                        renderer.sharedMaterial=Convert(renderer.sharedMaterial,recipe.cue);
                        if (renderer.trailMaterial!=null) renderer.trailMaterial=Convert(renderer.trailMaterial,recipe.cue);
                    }
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        var materials=renderer.sharedMaterials;
                        for (var i=0;i<materials.Length;i++) materials[i]=Convert(materials[i],recipe.cue);
                        renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;
                    }
                    var prefab=PrefabUtility.SaveAsPrefabAsset(instance,Output+"Effects/"+recipe.cue+".prefab");
                    effects.Add(new EffectBinding {cue=recipe.cue,prefab=prefab,scale=recipe.scale,seconds=recipe.seconds,limit=recipe.limit,boss=recipe.boss});
                }
                finally {UnityEngine.Object.DestroyImmediate(instance);}
            }
            bank.effects=effects.ToArray(); EditorUtility.SetDirty(bank);
            if (Directory.Exists(Rpg) && Directory.Exists(Small) && sounds.Count!=Enum.GetValues(typeof(SoundCue)).Length) throw new BuildFailedException("Downloaded audio mapping is incomplete.");
            Debug.Log($"SERAPHIM_PRESENTATION: {sounds.Count} sampled sound cues; {effects.Count} adapted, bounded effect prefabs.");
        }
        static void NormalizeImportedFolders()
        {
            Directory.CreateDirectory(Store);AssetDatabase.Refresh();
            foreach (var name in new[]{"Hovl Studio","SFX Packs","Small Sound Kit","UnityTechnologies"})
            {
                var original="Assets/"+name;var destination=Store+name;
                if (!AssetDatabase.IsValidFolder(original)) continue;
                if (AssetDatabase.IsValidFolder(destination)) throw new BuildFailedException("Duplicate Asset Store import folders: "+original+" and "+destination+". Preserve the existing GUIDs when resolving duplicates.");
                var result=AssetDatabase.MoveAsset(original,destination);
                if (!string.IsNullOrEmpty(result)) throw new BuildFailedException("Could not preserve the imported pack under ignored AssetStore/: "+result);
            }
        }
        static Material Convert(Material original,EffectCue cue)
        {
            if (original==null) original=AssetDatabase.LoadAssetAtPath<Material>(Store+"Hovl Studio/Magic effects pack/Materials/GlowFree1.mat");
            if (original==null) throw new BuildFailedException("A downloaded effect has an empty material slot and no glow fallback.");
            if (original.shader!=null && original.shader.name=="Seraphim/Imported Particle") return original;
            var id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
            var path=Output+"Materials/"+cue+"-"+id+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material==null) {material=new Material(Shader.Find("Seraphim/Imported Particle"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("Seraphim/Imported Particle");
            var candidates=new[]{"_BaseMap","_MainTex"}.Concat(original.GetTexturePropertyNames()).Distinct();
            var property=candidates.FirstOrDefault(p=>original.HasProperty(p) && original.GetTexture(p)!=null);
            material.SetTexture("_BaseMap",property!=null?original.GetTexture(property):null);
            if (property!=null) {material.SetTextureScale("_BaseMap",original.GetTextureScale(property));material.SetTextureOffset("_BaseMap",original.GetTextureOffset(property));}
            var tint=original.HasProperty("_TintColor")?original.GetColor("_TintColor"):Color.white;
            material.SetColor("_TintColor",new Color(1,1,1,Mathf.Clamp(tint.a,.25f,1)));
            var alpha=original.name.IndexOf("Smoke",StringComparison.OrdinalIgnoreCase)>=0 || (original.HasProperty("_DstBlend") && Mathf.Approximately(original.GetFloat("_DstBlend"),10));
            material.SetFloat("_DstBlend",alpha?10:1);
            EditorUtility.SetDirty(material); return material;
        }
    }
}
