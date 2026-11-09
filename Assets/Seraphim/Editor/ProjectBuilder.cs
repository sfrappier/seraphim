using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Seraphim.Editor
{
    public static class ProjectBuilder
    {
        const string Generated = "Assets/Seraphim/Generated";
        const string ScenePath = "Assets/Seraphim/Scenes/Sanctuary.unity";
        const string CharacterPath = "Assets/ThirdParty/CC0/KayKitAngel/Knight.fbx";

        [MenuItem("Seraphim/Prepare sanctuary")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory("Assets/Seraphim/Scenes");
            Directory.CreateDirectory("Assets/ThirdParty/AssetStore");
            ConfigureProject();
            ConfigureRenderer();
            var models = ConfigureModels();
            DownloadedPresentationBuilder.Configure();
            var settings = GetOrCreate<SanctuarySettings>(Generated+"/SanctuarySettings.asset");
            var atmosphere = ConfigureAtmosphere();
            // Create the bootstrap scene only once. Subsequent builds preserve authored scene changes.
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root = new GameObject("Sanctuary").AddComponent<SanctuaryGame>();
                root.settings = settings;
                root.models = models;
                root.atmosphere = atmosphere;
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Seraphim sanctuary prepared.");
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset,path);
            return asset;
        }

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "Seraphim";
            PlayerSettings.productName = "Seraphim";
            PlayerSettings.bundleVersion = "0.11.1";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[] {UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
            EditorSettings.serializationMode = SerializationMode.ForceText;
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = playerSettings.FindProperty("activeInputHandler");
            if (input != null) input.intValue = 2;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "Walkable";
            tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue = "Demon";
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureRenderer()
        {
            const string rendererPath = Generated+"/SanctuaryRenderer.asset";
            const string pipelinePath = Generated+"/SanctuaryPipeline.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(renderer,rendererPath);
                ResourceReloader.ReloadAllNullIn(renderer,"Packages/com.unity.render-pipelines.universal");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 1;
                pipeline.shadowDistance = 55;
                pipeline.maxAdditionalLightsCount = 8;
                AssetDatabase.CreateAsset(pipeline,pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        static PrototypeModels ConfigureModels()
        {
            if (!File.Exists(CharacterPath)) throw new BuildFailedException("Prototype assets are missing. Run Tools/Download-Assets.ps1 first.");
            foreach (var path in new[] {CharacterPath,"Assets/ThirdParty/CC0/KenneyCharacters/Animations/idle.fbx","Assets/ThirdParty/CC0/KenneyCharacters/Animations/run.fbx"})
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.animationType == ModelImporterAnimationType.Human) continue;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            var catalog = GetOrCreate<PrototypeModels>(Generated+"/PrototypeModels.asset");
            catalog.character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            catalog.characterTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/CC0/KayKitAngel/knight_texture.png");
            catalog.humanoidAvatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<Avatar>().FirstOrDefault(a => a.isValid && a.isHuman);
            catalog.dungeonGate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/CC0/KenneyDungeon/Models/FBX format/gate.fbx");
            catalog.idle = Animation("idle","Idle");
            catalog.run = Animation("run","Run");
            catalog.animatorController = ConfigureAnimator(catalog.idle,catalog.run);
            catalog.stone = Lit("Stone",new Color(.56f,.59f,.62f),.05f,.2f);
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/CC0/PolyHavenStone/stone_brick_wall_001_diff_1k.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/CC0/PolyHavenStone/stone_brick_wall_001_nor_gl_1k.jpg");
            catalog.stone.SetTexture("_BaseMap",diffuse);
            catalog.stone.SetTexture("_BumpMap",normal);
            catalog.stone.SetFloat("_BumpScale",.35f);
            catalog.stone.EnableKeyword("_NORMALMAP");
            catalog.ivory = Lit("Ivory",new Color(.83f,.87f,.89f),.15f,.35f);
            catalog.darkStone = Lit("Dark stone",new Color(.1f,.15f,.19f),.1f,.25f);
            catalog.gold = Lit("Gold",new Color(.78f,.52f,.18f),.7f,.65f);
            catalog.cyan = Lit("Cyan crystal",new Color(.15f,.6f,.85f),.35f,.7f,new Color(.1f,1.4f,2));
            catalog.glowGold = Glow("Gold radiance",new Color(2.8f,1.65f,.4f,1));
            catalog.glowCyan = Glow("Cyan radiance",new Color(.25f,1.3f,2.3f,1));
            catalog.celestialMote=Glow("Celestial motes",new Color(3,2.2f,1,1),"Seraphim/Celestial Mote");
            catalog.hudDisplayFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/CinzelDecorative/CinzelDecorative-Regular.ttf");
            catalog.marble=CathedralMaterial("Cathedral marble","marble_01",new Color(.88f,.89f,.86f),.5f);
            catalog.cathedralStone=CathedralMaterial("Cathedral limestone","stone_brick_wall_001",new Color(.68f,.7f,.73f),.22f);
            catalog.cathedralWood=CathedralMaterial("Cathedral oak","wood_table_001",new Color(.42f,.25f,.13f),.32f);
            catalog.vestment=Lit("Celestial vestment",new Color(.9f,.94f,1),.12f,.3f);
            catalog.angelArmor=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Seraphim armor.mat");
            if (catalog.angelArmor==null)
            {
                catalog.angelArmor=new Material(Shader.Find("Seraphim/Celestial Armor"));
                AssetDatabase.CreateAsset(catalog.angelArmor,Generated+"/Seraphim armor.mat");
            }
            catalog.angelArmor.SetTexture("_BaseMap",catalog.characterTexture); EditorUtility.SetDirty(catalog.angelArmor);
            EditorUtility.SetDirty(catalog.stone);
            CharacterContentBuilder.Configure(catalog);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static Material CathedralMaterial(string name,string texture,Color color,float smoothness)
        {
            var mat=Lit(name,color,.05f,smoothness);
            var folder="Assets/ThirdParty/CC0/CathedralTextures/"+texture;
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"_diff_2k.jpg"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"_nor_gl_2k.jpg"));
            mat.SetFloat("_BumpScale",.55f); mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat); return mat;
        }

        static AnimationClip Animation(string source,string name)
        {
            var path = Generated+"/"+name+".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/CC0/KenneyCharacters/Animations/"+source+".fbx")
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
            Debug.Log("SERAPHIM_CLIPS: "+source+" -> "+string.Join(", ",clips.Select(c => c.name+" ("+c.length.ToString("0.000")+"s)")));
            // FBX files also contain a short reference T-pose. Select the actual
            // motion take rather than relying on AssetDatabase subasset order.
            var imported = clips.OrderByDescending(c => c.length).FirstOrDefault();
            if (imported == null) throw new BuildFailedException("Animation not found: "+source);
            if (!imported.humanMotion) throw new BuildFailedException("Animation was not imported as humanoid: "+source);
            var prepared = UnityEngine.Object.Instantiate(imported);
            prepared.name = name;
            prepared.legacy = false;
            var settings = AnimationUtility.GetAnimationClipSettings(prepared);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(prepared,settings);
            if (clip == null) { clip=prepared; AssetDatabase.CreateAsset(clip,path); }
            else { EditorUtility.CopySerialized(prepared,clip); UnityEngine.Object.DestroyImmediate(prepared); EditorUtility.SetDirty(clip); }
            return clip;
        }

        static AnimatorController ConfigureAnimator(AnimationClip idleClip,AnimationClip runClip)
        {
            var path = Generated+"/Seraphim.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle"); idle.motion=idleClip;
            var run = machine.AddState("Run"); run.motion=runClip;
            machine.defaultState=idle;
            var move = idle.AddTransition(run); move.hasExitTime=false; move.duration=.15f;
            move.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
            var stop = run.AddTransition(idle); stop.hasExitTime=false; stop.duration=.15f;
            stop.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static Material Lit(string name,Color color,float metallic,float smoothness,Color? emission=null)
        {
            var path = Generated+"/"+name+".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) {name=name};
            material.SetColor("_BaseColor",color);
            material.SetFloat("_Metallic",metallic);
            material.SetFloat("_Smoothness",smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",emission.Value);
            }
            AssetDatabase.CreateAsset(material,path);
            return material;
        }
        static Material Glow(string name,Color color,string shader="Seraphim/Celestial Glow")
        {
            var path = Generated+"/"+name+".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(shader)) {name=name};
            material.SetColor("_BaseColor",color);
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        static VolumeProfile ConfigureAtmosphere()
        {
            var path = Generated+"/Atmosphere.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile,path);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.55f); bloom.threshold.Override(1.1f); bloom.scatter.Override(.6f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(.5f); color.contrast.Override(12); color.saturation.Override(-8);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.22f); vignette.smoothness.Override(.5f);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        [MenuItem("Seraphim/Validate project")]
        public static void Validate()
        {
            Prepare();
            var scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var game = scene.GetRootGameObjects().Select(o => o.GetComponent<SanctuaryGame>()).FirstOrDefault(c => c != null);
            if (game == null || game.settings == null || game.models == null || game.atmosphere == null)
                throw new BuildFailedException("Sanctuary scene dependencies are missing.");
            if (game.models.character == null || game.models.idle == null || game.models.run == null || game.models.dungeonGate == null)
                throw new BuildFailedException("Downloaded asset references are incomplete.");
            var avatar = game.models.humanoidAvatar;
            if (avatar==null || !avatar.isValid || !avatar.isHuman || game.models.animatorController==null)
                throw new BuildFailedException("Humanoid character rig or animation controller is invalid.");
            if (game.models.hudDisplayFont==null || game.models.celestialMote==null) throw new BuildFailedException("HUD font or particle material is missing.");
            if (game.settings.ultimateCooldown!=30 || game.settings.maxSpirit<=0 || game.settings.maxLife<=0)
                throw new BuildFailedException("Life, Spirit, or Word of God configuration is invalid.");
            if (game.settings.ultimateDuration!=15 || game.settings.pulseCooldown>.75f || game.settings.enemyLimit>32 || game.settings.enemyLimit<1)
                throw new BuildFailedException("Pulse duration, Radiance cooldown, or demon population configuration is invalid.");
            if (game.settings.menuEnemyLimit<32 || game.settings.menuEnemyLimit>128)
                throw new BuildFailedException("Main-menu showcase population must remain bounded between 32 and 128.");
            if (game.settings.menuMoveSpeed<6.5f || game.settings.menuMoveSpeed>12)
                throw new BuildFailedException("Main-menu running speed must be between 6.5 and 12 meters per second.");
            if (game.settings.bossHealth<=0 || game.settings.bossMoveSpeed<=0 || game.settings.bossMeteorDamage<=0
                || game.settings.bossSlashDamage<=0 || game.settings.bossFireDamage<=0 || game.settings.bossNovaDamage<=0 || game.settings.bossRestSeconds<3)
                throw new BuildFailedException("Final boss health, attacks, speed or arrival rest is invalid.");
            if (game.settings.bossRallySeconds<1 || game.settings.bossTransformationSeconds<2 || game.settings.bossAscendedScale<1 || game.settings.bossAscendedScale>2
                || game.settings.bossSummonScale<1 || game.settings.bossSummonScale>2 || game.settings.bossPhaseTwoSummonLimit<1
                || game.settings.bossPhaseThreeSummonLimit<game.settings.bossPhaseTwoSummonLimit || game.settings.bossPhaseThreeSummonLimit>=game.settings.enemyLimit)
                throw new BuildFailedException("Boss transitions, growth or bounded summons settings are invalid.");
            if (game.models.marble==null || game.models.marble.mainTexture==null || game.models.cathedralWood==null
                || game.settings.waveCount!=10 || game.settings.cathedralSize.x<80 || game.settings.cathedralSize.y<100)
                throw new BuildFailedException("Cathedral textures, dimensions, or ten-wave scenario are missing.");
            var shaders = new[] {Shader.Find("Universal Render Pipeline/Lit"),Shader.Find("Seraphim/Celestial Glow"),Shader.Find("Seraphim/Celestial Mote"),Shader.Find("Seraphim/Celestial Armor"),Shader.Find("Seraphim/Imported Particle"),Shader.Find("UI/Default")};
            if (shaders.Any(s => s == null || UnityEditor.ShaderUtil.ShaderHasError(s))) throw new BuildFailedException("A required shader failed to compile.");
            Debug.Log("SERAPHIM_VALIDATION: scene, settings, imported models, animations, shaders and renderer passed.");
        }

        [MenuItem("Seraphim/Build Windows")]
        public static void BuildWindows()
        {
            Validate();
            var output = Path.GetFullPath("Builds/Windows/Seraphim.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[] {ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Windows build failed: "+report.summary.result);
            if (ShaderUtil.ShaderHasError(Shader.Find("Seraphim/Imported Particle"))) throw new BuildFailedException("Imported particle shader failed during player compilation.");
            var outputDirectory=Path.GetDirectoryName(output);
            Directory.CreateDirectory(Path.Combine(outputDirectory,"Licenses"));
            File.Copy("Assets/ThirdParty/CinzelDecorative/OFL.txt",Path.Combine(outputDirectory,"Licenses","CinzelDecorative-OFL.txt"),true);
            var inputPackage=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.InputSystem.Gamepad).Assembly);
            if (inputPackage==null) throw new BuildFailedException("Input System package provenance is missing.");
            File.Copy(Path.Combine(inputPackage.resolvedPath,"LICENSE.md"),Path.Combine(outputDirectory,"Licenses","UnityInputSystem-LICENSE.md"),true);
            File.Copy("THIRD_PARTY_NOTICES.txt",Path.Combine(outputDirectory,"THIRD_PARTY_NOTICES.txt"),true);
            File.Copy("Assets/ThirdParty/CCBY/Characters/CC-BY-4.0.txt",Path.Combine(outputDirectory,"Licenses","Characters-CC-BY-4.0.txt"),true);
            File.Copy("Assets/ThirdParty/CCBY/Characters/CREDITS.txt",Path.Combine(outputDirectory,"Licenses","Character-Credits.txt"),true);
            var localMusic=Path.GetFullPath("LocalAudio/seraphim-theme.ogg");
            var musicDestination=Path.Combine(outputDirectory,"Music","seraphim-theme.ogg");
            if (File.Exists(localMusic))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(musicDestination)); File.Copy(localMusic,musicDestination,true);
                if (File.Exists("LocalAudio/source.json")) File.Copy("LocalAudio/source.json",Path.Combine(outputDirectory,"Music","source.json"),true);
                Debug.Log("SERAPHIM_MUSIC: licensed local loop packaged; audio remains excluded from source control.");
            }
            else
            {
                if (File.Exists(musicDestination)) File.Delete(musicDestination);
                var staleReceipt=Path.Combine(outputDirectory,"Music","source.json");
                if (File.Exists(staleReceipt)) File.Delete(staleReceipt);
                Debug.Log("SERAPHIM_MUSIC: clean-checkout original soundtrack fallback enabled.");
            }
            Debug.Log($"SERAPHIM_BUILD: {output}; {report.summary.totalSize} bytes; {report.summary.totalTime}.");
        }
    }
}
