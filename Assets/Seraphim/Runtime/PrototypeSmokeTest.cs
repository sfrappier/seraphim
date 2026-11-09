using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool success;
            public string engine;
            public float avatarHeight;
            public string[] detectedGamepads;
            public string music;
            public float musicSeconds;
            public string[] passed;
            public string[] failures;
        }
        SanctuaryGame game;
        readonly List<string> passed = new List<string>();
        readonly List<string> failures = new List<string>();
        string output;
        float avatarHeight;
        string[] detectedGamepads;
        public void Initialize(SanctuaryGame owner) { game = owner; }
        void OnEnable() { Application.logMessageReceived += ObserveLog; }
        void OnDisable() { Application.logMessageReceived -= ObserveLog; }
        void ObserveLog(string message,string stack,LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failures.Add(message);
        }
        void Check(bool valid,string label) { if (valid) passed.Add(label); else failures.Add(label); }

        IEnumerator Start()
        {
            output = Path.Combine(Application.dataPath,"..","SmokeTests");
            Directory.CreateDirectory(output);
            game.Player.InputEnabled = false;
            game.Skills.InputEnabled=false;
            game.Gamepads.InputEnabled=false;
            game.FrontEnd.InputEnabled=false;
            yield return new WaitForSeconds(1);
            var hardware=new List<string>();
            foreach (var pad in UnityEngine.InputSystem.Gamepad.all) hardware.Add(pad.displayName+" ["+pad.layout+"]");
            detectedGamepads=hardware.ToArray();
            Debug.Log("SERAPHIM_GAMEPADS: "+string.Join(", ",detectedGamepads));
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-characters-only"))
            {
                yield return CharacterModelChecks(); SaveReport(); yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-presentation-only"))
            {
                yield return PresentationChecks();SaveReport();yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-boss-only"))
            {
                yield return BossChecks(); SaveReport(); yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-front-end-only"))
            {
                yield return FrontEndChecks(); SaveReport(); yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-difficulty-only"))
            {
                yield return DifficultyChecks(); SaveReport(); yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-controller-only"))
            {
                yield return ControllerChecks(); SaveReport(); yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-scenario-only"))
            {
                yield return ScenarioChecks(); SaveReport(); yield break;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-combat-only"))
            {
                yield return CombatChecks();
                SaveReport();
                yield break;
            }
            Check(game.Player.Agent.isOnNavMesh,"Player spawns on navigation mesh");
            Check(game.Player.GetComponent<SeraphimWings>().WingCount == 6,"Six animated wings exist");
            Check(Camera.main != null && Camera.main.transform.position.y>8,"Overhead camera initialized");
            Check(game.Waystones.Count==3,"Three waystones generated");
            Check(game.models.character!=null && game.models.idle!=null && game.models.run!=null,"Downloaded character and animation clips assigned");
            var avatarRenderers = game.Player.transform.Find("Character visual").GetComponentsInChildren<Renderer>();
            if (avatarRenderers.Length>0)
            {
                var bounds = SanctuaryGame.VisualBounds(avatarRenderers);
                avatarHeight = bounds.size.y;
                Check(avatarHeight>1.3f && avatarHeight<2.5f,"Animated character keeps its intended scale");
                Check(bounds.size.x<2.5f && bounds.size.z<2.5f,"Character animation preserves upright proportions");
                Check(Vector2.Distance(new Vector2(bounds.center.x,bounds.center.z),new Vector2(game.Player.transform.position.x,game.Player.transform.position.z))<.5f,"Animated character stays aligned with wings and navigation");
                Check(game.Player.IsAnimationPlaying("Idle"),"Humanoid idle animation is playing");
                Debug.Log("SERAPHIM_AVATAR_BOUNDS: "+bounds);
            }
            yield return Capture("01-sanctuary.png");
            Check(game.Hud.HasResourceOrbs && game.Hud.SkillSlotCount==6,"Angelic HUD has Life and Spirit orbs and six numbered slots");
            Check(game.Hud.HudCanvas.worldCamera==Camera.main,"HUD renders with the gameplay camera");
            Check(SanctuaryHud.BlocksScreenPoint(new Vector2(Screen.width*.5f,60))
                && !game.Player.MoveFromScreen(new Vector2(Screen.width*.5f,60)),"HUD clicks cannot issue movement commands");
            Check(!game.Skills.TryUse(5),"Unassigned skill slots cannot cast");
            var keyboardStart=game.Player.transform.position;
            Check(game.Player.MoveTo(keyboardStart+Vector3.right*5),"Click destination accepted before WASD takeover");
            var keyboardUntil=Time.time+.55f;
            while (Time.time<keyboardUntil) { game.Player.MoveWithKeyboard(Vector2.up,Time.deltaTime); yield return null; }
            var straightDistance=Vector3.Distance(keyboardStart,game.Player.transform.position);
            Debug.Log($"SERAPHIM_KEYBOARD: distance={straightDistance:F2}; frame={Time.deltaTime:F3}s; speed={game.Player.Agent.speed:F2}");
            Check(straightDistance>2,"WASD steering physically moves the character");
            Check(game.Player.IsKeyboardMoving && !game.Player.HasDestination,"WASD cancels the previous click path");
            var flatForward=Camera.main.transform.forward; flatForward.y=0; flatForward.Normalize();
            Check(Vector3.Dot((game.Player.transform.position-keyboardStart).normalized,flatForward)>.95f,"W moves toward the top of the camera view");
            var diagonalStart=game.Player.transform.position;
            keyboardUntil=Time.time+.55f;
            while (Time.time<keyboardUntil) { game.Player.MoveWithKeyboard(Vector2.one,Time.deltaTime); yield return null; }
            var diagonalDistance=Vector3.Distance(diagonalStart,game.Player.transform.position);
            Check(diagonalDistance<straightDistance*1.15f && diagonalDistance>straightDistance*.8f,"Diagonal WASD movement has no speed boost");
            game.Player.StopKeyboardMovement();
            var releasedAt=game.Player.transform.position;
            yield return new WaitForSeconds(.25f);
            Check(Vector3.Distance(releasedAt,game.Player.transform.position)<.12f,"Releasing WASD stops movement without resuming the old path");
            game.Player.Agent.Warp(new Vector3(0,0,-1));
            yield return null;
            var right=Vector3.Cross(Vector3.up,flatForward);
            var againstAltar=new Vector2(Vector3.Dot(Vector3.forward,right),Vector3.Dot(Vector3.forward,flatForward));
            keyboardUntil=Time.time+.8f;
            while (Time.time<keyboardUntil) { game.Player.MoveWithKeyboard(againstAltar,Time.deltaTime); yield return null; }
            Check(game.Player.transform.position.z<1.1f && NavMesh.SamplePosition(game.Player.transform.position,out _,.15f,NavMesh.AllAreas),"WASD respects the altar and stays on navigable ground");
            game.Player.StopKeyboardMovement();
            game.Player.Agent.Warp(game.settings.spawnPoint);
            yield return new WaitForSeconds(.6f);
            var start = game.Player.transform.position;
            var destination = new Vector3(6,0,-1);
            Check(game.Player.MoveFromScreen(Camera.main.WorldToScreenPoint(destination)),"Screen click raycast accepts reachable destination");
            var deadline = Time.realtimeSinceStartup+14;
            while (Vector3.Distance(game.Player.transform.position,destination)>.6f && Time.realtimeSinceStartup<deadline) yield return null;
            Check(Vector3.Distance(game.Player.transform.position,destination)<.6f,"Player arrives at requested destination");
            Check(Vector3.Distance(game.Player.transform.position,start)>5,"Player physically moves through world");
            var obstaclePath = new NavMeshPath();
            Check(NavMesh.CalculatePath(new Vector3(0,0,-2),new Vector3(0,0,8),NavMesh.AllAreas,obstaclePath)
                && obstaclePath.status==NavMeshPathStatus.PathComplete && obstaclePath.corners.Length>=3,"Navigation routes around central altar");
            var stone = game.Waystones[1];
            var nearStone = stone.transform.position+Vector3.back*2;
            Check(game.Player.MoveTo(nearStone),"Waystone destination is reachable");
            deadline = Time.realtimeSinceStartup+12;
            while (Vector3.Distance(game.Player.transform.position,nearStone)>.6f && Time.realtimeSinceStartup<deadline) yield return null;
            Check(Vector3.Distance(game.Player.transform.position,nearStone)<.6f,"Player reaches waystone");
            var spiritBefore=game.Vitals.Spirit;
            Check(game.Skills.TryUse(0),"Slot 1 casts Radiance");
            Check(Mathf.Abs(game.Vitals.Spirit-(spiritBefore-game.settings.radianceSpiritCost))<.1f,"Radiance spends Spirit");
            Check(!game.Ability.Cast(),"Ability cooldown prevents immediate recast");
            Check(stone.IsAttuned && game.AttunedCount==1,"Radiance attunes nearby waystone");
            yield return new WaitForSeconds(.18f);
            yield return Capture("02-radiance.png");
            yield return new WaitForSeconds(.7f);
            Check(File.Exists(Path.Combine(output,"01-sanctuary.png")),"Gameplay screenshot captured");
            Check(game.Vitals.Spirit>spiritBefore-game.settings.radianceSpiritCost+4,"Spirit regenerates after casting");
            game.Player.Agent.ResetPath();
            game.Player.Agent.Warp(new Vector3(0,0,-4));
            yield return new WaitForSeconds(.6f);
            game.Vitals.TakeDamage(40);
            game.Vitals.SpendSpirit(35);
            var lifeBeforeUltimate=game.Vitals.Life;
            var spiritBeforeUltimate=game.Vitals.Spirit;
            var originalMaterial=avatarRenderers[0].sharedMaterial;
            var originalSets=new Material[avatarRenderers.Length][];
            for (var i=0;i<avatarRenderers.Length;i++) originalSets[i]=avatarRenderers[i].sharedMaterials;
            var originalVisualHeight=game.Player.transform.Find("Character visual").localPosition.y;
            var ultimateStarted=Time.time;
            Check(game.Skills.TryUse(4),"Slot 5 casts Word of God");
            Check(game.Ultimate.CooldownRemaining>29.8f && game.Ultimate.CooldownRemaining<=30,"Word of God starts a 30-second cooldown");
            Check(!game.Skills.TryUse(4),"Ultimate cannot be spammed while recharging");
            yield return new WaitForSeconds(.65f);
            Check(game.Ultimate.Stage==WordOfGodAbility.Phase.Charging,"Ultimate has a visible charge transition");
            Check(game.Player.GetComponent<SeraphimWings>().Ascension>.2f && avatarRenderers[0].sharedMaterial!=originalMaterial,"Ultimate transforms the character and spreads its wings");
            yield return Capture("03-word-of-god-charge.png");
            yield return new WaitForSeconds(.65f);
            Check(game.AttunedCount==3,"Word of God awakens waystones across its larger radius");
            Check(Mathf.Abs(game.Vitals.Life-Mathf.Min(game.Vitals.MaxLife,lifeBeforeUltimate+25))<.1f
                && game.Vitals.Spirit>spiritBeforeUltimate+30,"Ultimate restores Life and Spirit at revelation");
            Check(game.Player.transform.Find("Character visual").localPosition.y>originalVisualHeight+.3f
                && Camera.main.GetComponent<SanctuaryCamera>().DivinePower>.9f,"Ascended form levitates and changes the camera presentation");
            yield return Capture("04-word-of-god.png");
            var travelingStart=game.Player.transform.position;
            var movingUntil=Time.time+1.2f;
            while (Time.time<movingUntil) { game.Player.MoveWithKeyboard(Vector2.right,Time.deltaTime); yield return null; }
            game.Player.StopKeyboardMovement(); yield return null;
            yield return new WaitForEndOfFrame();
            Debug.Log($"SERAPHIM_MOVING_AURA: distance={Vector3.Distance(travelingStart,game.Player.transform.position):F2}; offset={Vector3.Distance(game.Ultimate.EffectPosition,game.Player.transform.position):F3}");
            Check(Vector3.Distance(travelingStart,game.Player.transform.position)>4
                && Vector3.Distance(game.Ultimate.EffectPosition,game.Player.transform.position)<.35f,"Word of God travels with the moving seraphim");
            var pulseTarget=game.Enemies.SpawnAt(new Vector3(17,0,-15),DemonKind.ObsidianGuard,true);
            Check(pulseTarget!=null && Vector3.Distance(pulseTarget.transform.position,travelingStart)>game.settings.ultimateRadius,"A demon can enter the aura beyond its original casting position");
            yield return new WaitForSeconds(1.15f);
            Check(pulseTarget!=null && pulseTarget.Health<pulseTarget.MaxHealth && pulseTarget.LastDamageSource==DivineDamage.WordOfGod,"Traveling ultimate pulses damage demons at the new location");
            Check(game.Ultimate.IsActive && game.Ultimate.PulseCount>=3,"Ultimate repeatedly pulses while the cooldown is running");
            var finishDeadline=Time.realtimeSinceStartup+game.settings.ultimateDuration+4;
            while (game.Ultimate.IsActive && Time.realtimeSinceStartup<finishDeadline) yield return null;
            Check(!game.Ultimate.IsActive && avatarRenderers[0].sharedMaterial==originalMaterial
                && game.Player.GetComponent<SeraphimWings>().Ascension==0,"Ultimate restores the original character materials and wings");
            Check(Mathf.Abs(game.Player.transform.Find("Character visual").localPosition.y-originalVisualHeight)<.01f
                && Camera.main.GetComponent<SanctuaryCamera>().DivinePower==0,"Levitating model and camera return to normal");
            Check(!game.Ultimate.CanCast && game.Ultimate.CooldownRemaining>10,"Ending the visual effect does not bypass the cooldown");
            Check(game.Ultimate.PulseCount==15 && Time.time-ultimateStarted>=16,"Ultimate delivers fifteen pulses over fifteen active seconds");
            yield return Capture("05-ultimate-recovery.png");
            var rechargeDeadline=Time.realtimeSinceStartup+35;
            while (game.Ultimate.CooldownRemaining>0 && Time.realtimeSinceStartup<rechargeDeadline) yield return null;
            Check(game.Ultimate.CanCast && Time.time-ultimateStarted>=29.95f,"Ultimate becomes ready after the full 30 seconds");
            Check(game.Skills.TryUse(4) && game.Ultimate.CastCount==2,"Ultimate effects can be reused after recharge");
            finishDeadline=Time.realtimeSinceStartup+game.settings.ultimateDuration+4;
            while (game.Ultimate.IsActive && Time.realtimeSinceStartup<finishDeadline) yield return null;
            Check(!game.Ultimate.IsActive && avatarRenderers[0].sharedMaterial==originalMaterial,"A second ultimate also restores the original character");
            var allMaterialsRestored=true;
            for (var i=0;i<avatarRenderers.Length;i++)
            {
                var restored=avatarRenderers[i].sharedMaterials;
                allMaterialsRestored &= restored.Length==originalSets[i].Length;
                for (var j=0;j<Mathf.Min(restored.Length,originalSets[i].Length);j++) allMaterialsRestored &= restored[j]==originalSets[i][j];
            }
            Check(allMaterialsRestored,"Repeated ultimates restore every FallenAngel material slot, including multi-material skin");
            yield return CharacterModelChecks();
            yield return PresentationChecks();
            yield return CombatChecks();
            yield return ScenarioChecks();
            yield return ControllerChecks();
            yield return DifficultyChecks();
            yield return BossChecks();
            yield return FrontEndChecks();
            SaveReport();
        }

        void SaveReport()
        {
            var hardware=new List<string>(detectedGamepads??Array.Empty<string>());
            foreach (var pad in UnityEngine.InputSystem.Gamepad.all)
            {
                var description=pad.displayName+" ["+pad.layout+"]";
                if (!hardware.Contains(description)) hardware.Add(description);
            }
            detectedGamepads=hardware.ToArray();
            var report = new Report { success=failures.Count==0,engine=Application.unityVersion,avatarHeight=avatarHeight,detectedGamepads=detectedGamepads,
                music=game.Audio.MusicCredit,musicSeconds=game.Audio.MusicReady?game.Audio.MusicSource.clip.length:0,passed=passed.ToArray(),failures=failures.ToArray() };
            File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"SERAPHIM_SMOKE: {passed.Count} passed; {failures.Count} failed. Results: {output}");
            Application.Quit(report.success?0:1);
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            // A hidden Windows player may skip presenting its backbuffer. Request
            // a real URP render explicitly so unattended checks validate GPU output.
            var target = RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination=target };
            RenderPipeline.SubmitRenderRequest(Camera.main,request);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var screenshot = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            screenshot.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            screenshot.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            var min = 1f;
            var max = 0f;
            for (var x = 3; x <= 7; x++)
            for (var y = 3; y <= 7; y++)
            {
                var luminance = screenshot.GetPixel(screenshot.width*x/10,screenshot.height*y/10).grayscale;
                min = Mathf.Min(min,luminance);
                max = Mathf.Max(max,luminance);
            }
            Check(max-min>.08f,"Rendered gameplay is visible in "+name);
            File.WriteAllBytes(Path.Combine(output,name),screenshot.EncodeToPNG());
            Destroy(screenshot);
        }
    }
}
