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
            BuildPlayer();
            BuildCamera();
            Skills=gameObject.AddComponent<SeraphimSkillBar>();
            Skills.Initialize(this);
            Hud=gameObject.AddComponent<SanctuaryHud>();
            Hud.Initialize(this);
            if (System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-seraphim-smoke"))
                gameObject.AddComponent<PrototypeSmokeTest>().Initialize(this);
        }

        void BuildSanctuary()
        {
            architecture = new GameObject("Sanctuary architecture").transform;
            var random = new System.Random(settings.worldSeed);
            var ground = SceneArt.Shape(PrimitiveType.Cube, "Walkable sanctuary ground", architecture,
                new Vector3(0,-.28f,0), new Vector3(42,.5f,38), models.darkStone, true);
            ground.layer = 8;
            AddNavBox(new Vector3(0,-.28f,0), new Vector3(42,.5f,38), 0);
            var tileMaterials = new Material[5];
            for (var i = 0; i < tileMaterials.Length; i++)
            {
                tileMaterials[i] = new Material(models.stone);
                tileMaterials[i].color = Color.Lerp(new Color(.32f,.37f,.43f), new Color(.6f,.62f,.63f), i/4f);
            }
            for (var x = -10; x <= 10; x++)
            for (var z = -9; z <= 9; z++)
            {
                SceneArt.Shape(PrimitiveType.Cube, "Stone paving", architecture, new Vector3(x*1.95f,-.055f,z*1.95f),
                    new Vector3(1.9f,.12f,1.9f), tileMaterials[random.Next(tileMaterials.Length)]);
            }
            for (var i = 0; i < 2; i++)
            {
                var x = i == 0 ? -20.6f : 20.6f;
                SceneArt.Shape(PrimitiveType.Cube, "Sanctuary edge", architecture, new Vector3(x,.15f,0), new Vector3(.3f,.4f,37), models.gold);
                SceneArt.Shape(PrimitiveType.Cube, "Boundary parapet", architecture, new Vector3(x,1.1f,0), new Vector3(.65f,2.2f,38), models.darkStone);
                AddNavBox(new Vector3(x,1.1f,0),new Vector3(.65f,2.2f,38),1);
            }
            foreach (var x in new[] {-9f,9f})
            foreach (var z in new[] {-12f,-5f,3f,11f}) Pillar(new Vector3(x,0,z));

            ObstacleBox("Central altar", new Vector3(0,.55f,3), new Vector3(3.6f,1.1f,3.6f), models.darkStone);
            SceneArt.Shape(PrimitiveType.Cube,"Altar cap",architecture,new Vector3(0,1.15f,3),new Vector3(4,.15f,4),models.gold);
            var altarRing = SceneArt.Ring("Altar sigil",architecture,1.45f,.035f,models.glowCyan);
            altarRing.transform.localPosition = new Vector3(0,1.25f,3);
            var relic = SceneArt.Shape(PrimitiveType.Cube,"Suspended relic",architecture,new Vector3(0,2.1f,3),Vector3.one*.7f,models.gold);
            relic.transform.localRotation = Quaternion.Euler(45,25,45);
            SceneArt.PointLight("Altar light",architecture,new Vector3(0,3,3),new Color(.3f,.7f,1),4,9);

            if (models.dungeonGate != null)
            {
                var gate = Instantiate(models.dungeonGate, architecture);
                gate.name = "Kenney sanctuary gateway";
                NormalizeModel(gate, 8.5f, false);
                gate.transform.localPosition += new Vector3(0,0,16);
                foreach (var renderer in gate.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = models.stone;
                AddNavBox(new Vector3(-3.6f,2,16),new Vector3(1.2f,4,1.4f),1);
                AddNavBox(new Vector3(3.6f,2,16),new Vector3(1.2f,4,1.4f),1);
            }
            else
            {
                Pillar(new Vector3(-4,0,16)); Pillar(new Vector3(4,0,16));
                SceneArt.Shape(PrimitiveType.Cube,"Gate lintel",architecture,new Vector3(0,5.2f,16),new Vector3(9,.65f,.8f),models.ivory);
            }
            var portal = SceneArt.Ring("Dawn gate",null,2.1f,.1f,models.glowGold);
            portal.transform.position = new Vector3(0,2.35f,16);
            portal.transform.rotation = Quaternion.Euler(90,0,0);
            SceneArt.PointLight("Gateway light",architecture,new Vector3(0,2,15),new Color(1,.65f,.25f),5,10);

            for (var i = 0; i < settings.decorativeRubble; i++)
            {
                var x = (i % 2 == 0 ? -1 : 1) * (15 + (float)random.NextDouble()*3.5f);
                var z = (float)random.NextDouble()*32-16;
                var rock = SceneArt.Shape(PrimitiveType.Cube,"Weathered masonry",architecture,new Vector3(x,.18f,z),
                    new Vector3(.3f+(float)random.NextDouble(),.3f,.4f+(float)random.NextDouble()),models.darkStone);
                rock.transform.localRotation = Quaternion.Euler(0,random.Next(360),random.Next(-12,12));
            }
            CreateWaystone(new Vector3(-13,0,6),"Mercy");
            CreateWaystone(new Vector3(12,0,6),"Courage");
            CreateWaystone(new Vector3(0,0,-13),"Hope");
            StaticBatchingUtility.Combine(architecture.gameObject);
        }

        void Pillar(Vector3 point)
        {
            ObstacleBox("Pillar foundation", point + Vector3.up*.18f,new Vector3(2,.36f,2),models.darkStone);
            SceneArt.Shape(PrimitiveType.Cylinder,"Ivory column",architecture,point+Vector3.up*2.1f,new Vector3(1.15f,1.9f,1.15f),models.ivory);
            SceneArt.Shape(PrimitiveType.Cube,"Capital",architecture,point+Vector3.up*4,new Vector3(1.7f,.3f,1.7f),models.stone);
            SceneArt.Shape(PrimitiveType.Cylinder,"Gilded collar",architecture,point+Vector3.up*.5f,new Vector3(1.25f,.08f,1.25f),models.gold);
            AddNavBox(point+Vector3.up*2,new Vector3(1.3f,4,1.3f),1);
        }

        void ObstacleBox(string name, Vector3 center, Vector3 size, Material material)
        {
            SceneArt.Shape(PrimitiveType.Cube,name,architecture,center,size,material,true);
            AddNavBox(center,size,1);
        }

        void AddNavBox(Vector3 center, Vector3 size, int area)
        {
            navSources.Add(new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(center,Quaternion.identity,Vector3.one),size=size,area=area });
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
            navData = NavMeshBuilder.BuildNavMeshData(config,navSources,new Bounds(Vector3.zero,new Vector3(46,16,44)),Vector3.zero,Quaternion.identity);
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
                var bodyMaterial = new Material(models.ivory);
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
            player.AddComponent<SeraphimWings>().Initialize(models,Player);
            var aura = SceneArt.Ring("Seraphim ground aura",player.transform,.6f,.025f,models.glowCyan);
            aura.transform.localPosition = Vector3.up*.05f;
            Vitals=player.AddComponent<SeraphimVitals>();
            Vitals.Initialize(settings);
            Ability = player.AddComponent<CelestialAbility>();
            Ability.Initialize(settings,models.glowGold,models.celestialMote,Waystones,Vitals);
            Ultimate=player.AddComponent<WordOfGodAbility>();
            Ultimate.Initialize(settings,models,Vitals,Waystones);
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
