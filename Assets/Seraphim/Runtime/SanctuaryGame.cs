using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seraphim
{
    public sealed class SanctuaryGame : MonoBehaviour
    {
        public SanctuarySettings settings;
        public PrototypeModels models;
        public VolumeProfile atmosphere;
        public SeraphimMotor Player { get; private set; }
        public CelestialAbility Ability { get; private set; }
        public WordOfGodAbility Ultimate { get; private set; }
        public SeraphimVitals Vitals { get; private set; }
        public SeraphimSkillBar Skills { get; private set; }
        public SanctuaryHud Hud { get; private set; }
        public EnemyDirector Enemies { get; private set; }
        public DivinePowers Powers { get; private set; }
        public WaveDirector Waves { get; private set; }
        public CelestialDrops Drops { get; private set; }
        public SeraphimGamepadInput Gamepads { get; private set; }
        public readonly List<Rect> MapObstacles=new List<Rect>();
        public List<Waystone> Waystones { get; } = new List<Waystone>();
        public int AttunedCount => Waystones.FindAll(s => s.IsAttuned).Count;
        readonly List<NavMeshBuildSource> navSources = new List<NavMeshBuildSource>();
        NavMeshData navData;
        NavMeshDataInstance navInstance;
        Transform architecture;

        void Awake()
        {
            if (settings == null || models == null) { Debug.LogError("Sanctuary content is missing. Run Tools/Build.ps1."); enabled = false; return; }
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            BuildSanctuary();
            BuildNavigation();
            Enemies=new GameObject("Demon encounter").AddComponent<EnemyDirector>();
            Enemies.transform.SetParent(transform,false);
            BuildPlayer();
            BuildCamera();
            Skills=gameObject.AddComponent<SeraphimSkillBar>();
            Skills.Initialize(this);
            Gamepads=gameObject.AddComponent<SeraphimGamepadInput>(); Gamepads.Initialize(this);
            Player.Gamepads=Gamepads;
            Waves=gameObject.AddComponent<WaveDirector>(); Waves.Initialize(this);
            Drops=gameObject.AddComponent<CelestialDrops>(); Drops.Initialize(this);
            Hud=gameObject.AddComponent<SanctuaryHud>();
            Hud.Initialize(this);
            var smoke=System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-seraphim-smoke");
            Enemies.Initialize(this,false);
            if (!smoke) Waves.BeginScenario();
            if (smoke)
                gameObject.AddComponent<PrototypeSmokeTest>().Initialize(this);
        }

        void BuildSanctuary()
        {
            architecture=CathedralBuilder.Build(models,settings,AddNavBox,CreateWaystone).transform;
        }
        void AddNavBox(Vector3 center, Vector3 size, int area)
        {
            navSources.Add(new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(center,Quaternion.identity,Vector3.one),size=size,area=area });
            if (area==1) MapObstacles.Add(new Rect(center.x-size.x/2,center.z-size.z/2,size.x,size.z));
        }

        void CreateWaystone(Vector3 point, string name)
        {
            var stone = new GameObject(name + " waystone").AddComponent<Waystone>();
            stone.transform.position = point;
            SceneArt.Shape(PrimitiveType.Cylinder,"Pedestal",stone.transform,Vector3.up*.25f,new Vector3(1.6f,.25f,1.6f),models.darkStone);
            stone.crystal = SceneArt.Shape(PrimitiveType.Cube,"Light crystal",stone.transform,Vector3.up*1.8f,new Vector3(.38f,.65f,.38f),models.cyan).transform;
            stone.crystal.localRotation = Quaternion.Euler(0,0,45);
            stone.beacon = SceneArt.PointLight("Waystone light",stone.transform,Vector3.up*2,new Color(.25f,.7f,1),2,6);
            stone.ring = SceneArt.Ring("Waystone sigil",stone.transform,1.1f,.04f,models.glowCyan);
            stone.ring.transform.localPosition = Vector3.up*.07f;
            AddNavBox(point+Vector3.up*.4f,new Vector3(1.4f,.8f,1.4f),1);
            Waystones.Add(stone);
        }

        void BuildNavigation()
        {
            var config = NavMesh.GetSettingsByIndex(0);
            config.agentRadius = .4f;
            config.agentHeight = 1.8f;
            config.agentClimb = .35f;
            config.overrideVoxelSize = true;
            config.voxelSize = .12f;
            navData = NavMeshBuilder.BuildNavMeshData(config,navSources,new Bounds(Vector3.zero,new Vector3(settings.cathedralSize.x+4,24,settings.cathedralSize.y+4)),Vector3.zero,Quaternion.identity);
            if (navData == null) throw new System.InvalidOperationException("Sanctuary navigation could not be built.");
            navInstance = NavMesh.AddNavMeshData(navData);
        }

        void BuildPlayer()
        {
            var player = new GameObject("Seraphim");
            player.transform.position = settings.spawnPoint;
            var agent = player.AddComponent<NavMeshAgent>();
            if (!NavMesh.SamplePosition(settings.spawnPoint,out var spawn,2,NavMesh.AllAreas)) throw new System.InvalidOperationException("Spawn is not navigable.");
            agent.Warp(spawn.position);
            var visual = new GameObject("Character visual").transform;
            visual.SetParent(player.transform,false);
            Animator anim = null;
            if (models.character != null)
            {
                var normalizedAvatar = new GameObject("Normalized avatar");
                normalizedAvatar.transform.SetParent(visual,false);
                var avatar = Instantiate(models.character,normalizedAvatar.transform);
                var bodyMaterial = new Material(models.angelArmor);
                if (models.characterTexture != null)
                {
                    bodyMaterial.SetTexture("_BaseMap",models.characterTexture);
                    bodyMaterial.SetColor("_BaseColor",Color.white);
                }
                foreach (var renderer in avatar.GetComponentsInChildren<Renderer>())
                {
                    var skinned = renderer as SkinnedMeshRenderer;
                    var mesh = skinned != null ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    var materials = new Material[mesh != null ? Mathf.Max(1,mesh.subMeshCount) : 1];
                    for (var i=0;i<materials.Length;i++) materials[i]=bodyMaterial;
                    renderer.sharedMaterials = materials;
                    if (skinned != null) skinned.updateWhenOffscreen = true;
                }
                anim = avatar.GetComponent<Animator>();
                if (anim == null) anim = avatar.AddComponent<Animator>();
                anim.avatar = models.humanoidAvatar;
                anim.runtimeAnimatorController = models.animatorController;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.Rebind();
                anim.Update(0);
                NormalizeModel(normalizedAvatar,1.8f,true);
            }
            else SceneArt.Shape(PrimitiveType.Capsule,"Placeholder armor",visual,Vector3.up*.9f,new Vector3(.65f,.9f,.65f),models.ivory);
            Player = player.AddComponent<SeraphimMotor>();
            Player.Initialize(settings,anim,models.glowGold);
            player.AddComponent<AngelicVestments>().Initialize(anim,models);
            player.AddComponent<SeraphimWings>().Initialize(models,Player);
            var aura = SceneArt.Ring("Seraphim ground aura",player.transform,.6f,.025f,models.glowCyan);
            aura.transform.localPosition = Vector3.up*.05f;
            Vitals=player.AddComponent<SeraphimVitals>();
            Vitals.Initialize(settings);
            Ability = player.AddComponent<CelestialAbility>();
            Ability.Initialize(settings,models.glowGold,models.celestialMote,Waystones,Vitals,Enemies);
            Ultimate=player.AddComponent<WordOfGodAbility>();
            Ultimate.Initialize(settings,models,Vitals,Waystones,Enemies);
            Powers=player.AddComponent<DivinePowers>(); Powers.Initialize(this);
        }

        internal static void NormalizeModel(GameObject model, float targetSize, bool byHeight)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = VisualBounds(renderers);
            var measured = byHeight ? bounds.size.y : bounds.size.x;
            var scale = targetSize / Mathf.Max(measured,.001f);
            var bottomCenter = new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var offset = bottomCenter-model.transform.position;
            if (model.transform.parent != null) offset=model.transform.parent.InverseTransformVector(offset);
            model.transform.localScale *= scale;
            model.transform.localPosition -= offset*scale;
        }

        internal static Bounds VisualBounds(Renderer[] renderers)
        {
            var bounds = new Bounds();
            var initialized = false;
            foreach (var renderer in renderers)
            {
                var skinned = renderer as SkinnedMeshRenderer;
                if (skinned == null)
                {
                    if (!initialized) { bounds=renderer.bounds; initialized=true; } else bounds.Encapsulate(renderer.bounds);
                    continue;
                }
                var baked = new Mesh();
                skinned.BakeMesh(baked,true);
                foreach (var vertex in baked.vertices)
                {
                    var point = skinned.transform.TransformPoint(vertex);
                    if (!initialized) { bounds=new Bounds(point,Vector3.zero); initialized=true; } else bounds.Encapsulate(point);
                }
                Destroy(baked);
            }
            return bounds;
        }

        void BuildCamera()
        {
            var cameraObject = new GameObject("Sanctuary camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 42;
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 150;
            camera.backgroundColor = new Color(.025f,.04f,.065f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraObject.AddComponent<SanctuaryCamera>().Initialize(Player.transform,settings);
            var volume = new GameObject("Sanctuary atmosphere").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = atmosphere;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.24f,.32f,.46f);
            RenderSettings.ambientEquatorColor = new Color(.16f,.2f,.28f);
            RenderSettings.ambientGroundColor = new Color(.08f,.1f,.15f);
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(.32f,.4f,.52f));
            RenderSettings.ambientProbe = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.07f,.12f,.19f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .013f;
            var sun = new GameObject("Dawn sunlight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(45,-30,0);
            sun.color = new Color(1,.85f,.67f);
            sun.intensity = 1.8f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .8f;
            sun.shadowBias = .04f;
            RenderSettings.sun = sun;
        }

        void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit(); }
        void OnDestroy()
        {
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
        }
    }
}
