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
    public sealed class PrototypeSmokeTest : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool success;
            public string engine;
            public float avatarHeight;
            public string[] passed;
            public string[] failures;
        }
        SanctuaryGame game;
        readonly List<string> passed = new List<string>();
        readonly List<string> failures = new List<string>();
        string output;
        float avatarHeight;
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
            yield return new WaitForSeconds(1);
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
            Check(game.Ability.Cast(),"Radiance can be cast");
            Check(!game.Ability.Cast(),"Ability cooldown prevents immediate recast");
            Check(stone.IsAttuned && game.AttunedCount==1,"Radiance attunes nearby waystone");
            yield return new WaitForSeconds(.18f);
            yield return Capture("02-radiance.png");
            yield return new WaitForSeconds(.7f);
            Check(File.Exists(Path.Combine(output,"01-sanctuary.png")),"Gameplay screenshot captured");
            var report = new Report { success=failures.Count==0,engine=Application.unityVersion,avatarHeight=avatarHeight,passed=passed.ToArray(),failures=failures.ToArray() };
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
