using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        IEnumerator ScenarioChecks()
        {
            game.Waves.StopScenario(); game.Enemies.DespawnAll(); game.Drops.Clear();
            game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(game.settings.spawnPoint);
            Check(game.models.character.name=="Knight" && game.Player.GetComponent<AngelicVestments>().HasCrown,"Rigged armored seraphim has a celestial crown and vestments");
            Check(game.Player.GetComponent<SeraphimWings>().HasValidGeometry,"Sculpted seraphim wings have finite mesh geometry");
            Check(game.models.marble.mainTexture.width==2048 && game.models.cathedralStone.mainTexture.width==2048 && game.models.cathedralWood.mainTexture.width==2048,"Cathedral uses three high-resolution 2K texture sets");
            var path=new NavMeshPath();
            Check(NavMesh.CalculatePath(new Vector3(5,0,-45),new Vector3(5,0,42),NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete,"Expanded cathedral is navigable from entrance to apse");
            Check(NavMesh.SamplePosition(new Vector3(37,0,35),out _,.4f,NavMesh.AllAreas),"Cathedral side aisles are navigable beyond the original sanctuary");
            Check(SanctuaryHud.BlocksScreenPoint(new Vector2(Screen.width-150,Screen.height-175)) && !game.Player.MoveFromScreen(new Vector2(Screen.width-150,Screen.height-175)),"Minimap clicks are isolated from world movement");
            // The previous combat check ends with revival protection.
            yield return new WaitForSeconds(4.1f);
            game.Vitals.RestoreLife(100); game.Vitals.RestoreSpirit(100); game.Vitals.TakeDamage(35); game.Vitals.SpendSpirit(85);
            var lifeBefore=game.Vitals.Life; var collected=game.Drops.Collected; var score=game.Enemies.Score;
            var demon=game.Enemies.SpawnAt(game.Player.transform.position+Vector3.forward*4,DemonKind.CinderImp,true);
            Check(demon!=null,"A demon can be placed for the restoration-drop encounter");
            if (demon==null) yield break;
            var deathPoint=demon.transform.position;
            demon.Hit(1000,DivineDamage.Lance,game.Player.transform.position);
            var awarded=game.Enemies.Score;
            demon.Hit(1000,DivineDamage.Lance,game.Player.transform.position);
            Check(awarded==score+100 && game.Enemies.Score==awarded,"Every demon awards points exactly once");
            Check(game.Drops.ActiveCount==2,"Slain demons drop both Life and Spirit orbs");
            yield return new WaitForSeconds(.5f);
            yield return Capture("11-restoration-orbs.png");
            game.Player.MoveTo(deathPoint);
            var deadline=Time.realtimeSinceStartup+5;
            while (game.Drops.Collected<collected+2 && Time.realtimeSinceStartup<deadline) yield return null;
            Check(game.Drops.Collected>=collected+2 && game.Vitals.Life>=lifeBefore+game.settings.lifeDropRestore,"Walking to drops collects both orbs and restores resources");
            game.Player.Agent.ResetPath(); game.Player.StopKeyboardMovement();
            for (var i=0;i<60;i++) game.Drops.SpawnPair(new Vector3(5,0,5));
            Check(game.Drops.ActiveCount<=48,"Restoration drops use a bounded pool");
            game.Drops.Clear(); game.Vitals.RestoreLife(100); game.Vitals.RestoreSpirit(100);
            var preserved=game.Drops.Spawn(game.Player.transform.position,DropKind.Life);
            yield return new WaitForSeconds(.5f);
            Check(preserved!=null && preserved.Active,"Full resources leave matching orbs available for later");
            game.Drops.Clear(); game.Player.Agent.Warp(game.settings.spawnPoint);

            game.Waves.BeginScenario(); game.Waves.StartNextWave();
            var clock=Time.time+100;
            for (var i=0;i<7;i++) { clock+=4; game.Waves.Tick(clock); }
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive) enemy.Passive=true;
            Check(game.Waves.Wave==1 && game.Enemies.ActiveCount==3 && game.Waves.Spawned==3,"First wave increases spawn rate while limiting the encounter to three demons");
            yield return new WaitForSeconds(.6f);
            Check(game.Hud.Minimap.EnemyMarkerCount==game.Enemies.ActiveCount,"Minimap tracks living enemies as red markers");
            var beforeMap=game.Hud.Minimap.PlayerMapPosition;
            game.Player.Agent.Warp(game.settings.spawnPoint+Vector3.right*3);
            Check(Vector2.Distance(beforeMap,game.Hud.Minimap.PlayerMapPosition)>1,"Minimap player marker follows world movement");
            yield return new WaitForSeconds(.6f);
            yield return Capture("12-cathedral-wave.png");
            var capped=true; var allWaves=true; var maxHealth=0f;
            for (var guard=0;guard<500 && game.Waves.State!=WaveDirector.EncounterState.Victory;guard++)
            {
                clock+=8; game.Waves.Tick(clock);
                capped &= game.Enemies.ActiveCount<=game.Waves.ConcurrentLimit && game.Enemies.ActiveCount<=8;
                foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive)
                {
                    if (game.Waves.Wave==10) maxHealth=Mathf.Max(maxHealth,enemy.MaxHealth);
                    enemy.Hit(10000,DivineDamage.WordOfGod,game.Player.transform.position); enemy.ReturnToPool();
                }
                if (guard%8==0) yield return null;
            }
            allWaves=game.Waves.State==WaveDirector.EncounterState.Victory && game.Waves.Wave==10;
            Check(capped,"All ten waves obey their increasing concurrency limits and the eight-enemy safety cap");
            Check(allWaves && game.Enemies.KillCount==150,"Ten-wave scenario ends in victory after all 150 demons are slain");
            Check(maxHealth>42 && maxHealth<80 && game.Waves.IntervalFor(10)<game.Waves.IntervalFor(1),"Later waves increase demon durability and spawn rate gradually");
            Check(game.Enemies.Score>=15000,"Scenario score includes every demon and later-wave bonuses");
            yield return Capture("13-cathedral-victory.png");
            game.Waves.BeginScenario();
            Check(game.Enemies.Score==0 && game.Waves.Wave==0 && game.Drops.ActiveCount==0,"Replaying the scenario resets score, wave progress, and drops");
            game.Waves.StartNextWave(); clock+=20; game.Waves.Tick(clock);
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive) enemy.Hit(10000,DivineDamage.Radiance,game.Player.transform.position);
            var earned=game.Enemies.Score; game.Vitals.TakeDamage(10000);
            yield return new WaitForSeconds(.35f);
            Check(game.Waves.State==WaveDirector.EncounterState.Resting && game.Waves.Wave==0 && game.Enemies.ActiveCount==0 && game.Enemies.Score==earned,"Defeat retries the current wave without stranding its quota or losing earned points");
            game.Waves.StartNextWave();
            Check(game.Waves.Wave==1 && game.Waves.Slain==0 && game.Waves.Spawned==0,"Retried wave starts with a fresh enemy quota");
            game.Waves.StopScenario(); game.Enemies.DespawnAll(); game.Drops.Clear();

            game.Player.Agent.Warp(new Vector3(0,0,38));
            yield return new WaitForSeconds(1);
            yield return Capture("14-cathedral-apse.png");
            var overviewCamera=Camera.main; var overviewFollow=overviewCamera.GetComponent<SanctuaryCamera>(); var overviewRotation=overviewCamera.transform.rotation;
            overviewFollow.enabled=false; overviewCamera.transform.position=new Vector3(-24,16,-26); overviewCamera.transform.LookAt(new Vector3(3,4,23));
            yield return new WaitForSeconds(.25f); yield return Capture("16-cathedral-overview.png");
            overviewCamera.transform.rotation=overviewRotation; overviewFollow.enabled=true;
            game.Player.Agent.Warp(game.settings.spawnPoint);
            var camera=Camera.main; var follow=camera.GetComponent<SanctuaryCamera>(); var rotation=camera.transform.rotation; var fov=camera.fieldOfView;
            follow.enabled=false; camera.transform.position=game.Player.transform.position+new Vector3(-3.7f,2.5f,-4.3f);
            camera.transform.LookAt(game.Player.transform.position+Vector3.up*1.1f); camera.fieldOfView=32;
            game.Player.transform.rotation=Quaternion.LookRotation(new Vector3(-3.7f,0,-4.3f));
            yield return new WaitForSeconds(.25f); yield return Capture("15-angel-portrait.png");
            camera.fieldOfView=fov; camera.transform.rotation=rotation; follow.enabled=true;
        }
    }
}
