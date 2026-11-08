using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        void ResetPressureFixture(Vector3 player)
        {
            game.Waves.StopScenario(); game.Enemies.DespawnAll(); game.Drops.Clear();
            game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(player);
            game.Vitals.RestoreLife(100); game.Vitals.RestoreSpirit(100);
        }
        IEnumerator WaitForSpecial(DemonEnemy enemy)
        {
            var deadline=Time.realtimeSinceStartup+5;
            while (enemy!=null && !enemy.Offense.SpecialReady && Time.realtimeSinceStartup<deadline) yield return null;
        }
        IEnumerator DifficultyChecks()
        {
            ResetPressureFixture(new Vector3(-5,0,-8));
            yield return new WaitForSeconds(4.1f);
            Check(game.Waves.TargetFor(10)==64 && game.Waves.LimitFor(10)==24 && game.Waves.BurstFor(10)==3
                && game.Waves.IntervalFor(10)<=.33f,"Wave ten schedules 64 demons, up to 24 alive, in fast three-enemy bursts");
            Check(game.Waves.SpawnBudgetFor(10,0)==22 && game.Waves.SpawnBudgetFor(10,11.9f)==22
                && game.Waves.SpawnBudgetFor(10,12)==43 && game.Waves.SpawnBudgetFor(10,24)==64,"Wave-ten reinforcements unlock in three timed groups instead of arriving under one ultimate");
            Check((game.Waves.GroupsFor(10)-1)*game.settings.reinforcementInterval>game.settings.ultimateDuration+1,"Final reinforcements arrive after a starting ultimate's active window");
            var basic=new DemonDifficulty(game.settings,DemonKind.CinderImp,1);
            var late=new DemonDifficulty(game.settings,DemonKind.CinderImp,10);
            Check(late.size>basic.size && late.health>2.5f && late.speed>1.5f && late.damage>2.5f && late.knockback<.5f,"Late demons grow larger, tougher, faster, more dangerous, and harder to push back");
            var melee=game.Enemies.SpawnAt(new Vector3(-5,0,-6.5f),DemonKind.CinderImp,false,10);
            var life=game.Vitals.Life;
            yield return new WaitForSeconds(2.1f);
            Check(melee!=null && game.Vitals.Life<life-10,"Standing still against a late-wave melee demon loses meaningful Life");

            ResetPressureFixture(new Vector3(-5,0,-8));
            var ash=game.Enemies.SpawnAt(new Vector3(-5,0,2),DemonKind.AshStalker,true,10);
            yield return WaitForSpecial(ash);
            Check(ash!=null && ash.Offense.TrySpecial(),"Late Ash Stalkers can mark the player's location from beyond Radiance range");
            Check(ash!=null && ash.Offense.Phase==DemonOffense.AttackPhase.Windup && game.Vitals.Life==100,"Marked ground warns before dealing damage");
            yield return new WaitForSeconds(1.2f);
            Check(ash!=null && game.Vitals.Life<100 && ash.Offense.Phase==DemonOffense.AttackPhase.Burning,"Remaining in an ember field causes repeated area damage");
            yield return Capture("19-ember-field.png");
            var until=Time.time+.7f;
            while (Time.time<until) { game.Player.MoveWithKeyboard(Vector2.right,Time.deltaTime); yield return null; }
            game.Player.StopKeyboardMovement(); var escapedLife=game.Vitals.Life; yield return new WaitForSeconds(.65f);
            Check(Mathf.Abs(game.Vitals.Life-escapedLife)<.1f && Vector3.Distance(game.Player.transform.position,ash.Offense.ThreatPoint)>2.75f,"Moving out of the marked field avoids its following damage ticks");

            ResetPressureFixture(new Vector3(-5,0,-8));
            ash=game.Enemies.SpawnAt(new Vector3(-5,0,2),DemonKind.AshStalker,true,10);
            yield return WaitForSpecial(ash);
            Check(ash!=null && ash.Offense.TrySpecial(),"A ranged threat can begin another telegraphed attack");
            var chained=game.Powers.Cast(1,ash.transform.position);
            Check(chained && ash.IsStunned && ash.Offense.Phase==DemonOffense.AttackPhase.Idle,"Chained lightning interrupts a ranged windup before it becomes a hazard");
            yield return new WaitForSeconds(1.2f);
            Check(game.Vitals.Life==100,"Interrupted windups never apply their pending damage");
            ash.Hit(10000,DivineDamage.Radiance,game.Player.transform.position); ash.ReturnToPool();
            ash=game.Enemies.SpawnAt(new Vector3(-5,0,2),DemonKind.AshStalker,true);
            Check(ash!=null && Mathf.Abs(ash.MaxHealth-32)<.1f && !ash.Offense.SpecialActive && !ash.IsStunned,"Reusing a late-wave demon resets scaling, status effects, and pending threats");

            ResetPressureFixture(new Vector3(-5,0,-8));
            var casters=new List<DemonEnemy>();
            for (var i=0;i<5;i++) casters.Add(game.Enemies.SpawnAt(new Vector3(-9+i*3,0,0),DemonKind.AshStalker,true,10));
            yield return new WaitForSeconds(3);
            var started=0; foreach (var caster in casters) if (caster!=null && caster.Offense.TrySpecial()) started++;
            Check(started==game.settings.maxConcurrentSpecialAttacks && game.Enemies.ActiveSpecialAttacks==started,"Simultaneous special attacks have a separate readable four-threat cap");
            game.Enemies.DespawnAll();
            Check(game.Enemies.ActiveSpecialAttacks==0,"Clearing an encounter also clears every warning and lingering field");

            ResetPressureFixture(new Vector3(0,0,-4));
            var guard=game.Enemies.SpawnAt(new Vector3(0,0,-12),DemonKind.ObsidianGuard,true,10);
            yield return WaitForSpecial(guard);
            Check(guard!=null && guard.Offense.TrySpecial(),"Late guards warn along a locked charge lane");
            yield return new WaitForSeconds(.2f); yield return Capture("20-guard-charge.png");
            until=Time.time+.65f;
            while (Time.time<until) { game.Player.MoveWithKeyboard(Vector2.right,Time.deltaTime); yield return null; }
            game.Player.StopKeyboardMovement(); yield return new WaitForSeconds(.7f);
            Check(game.Vitals.Life==100 && guard!=null && guard.transform.position.z>-8,"Sidestepping the locked lane avoids a moving guard's charge");
            Check(guard!=null && NavMesh.SamplePosition(guard.transform.position,out _,.15f,NavMesh.AllAreas),"Charging guards remain on navigable ground");
            ResetPressureFixture(new Vector3(0,0,8));
            guard=game.Enemies.SpawnAt(new Vector3(0,0,-2),DemonKind.ObsidianGuard,true,10);
            yield return WaitForSpecial(guard);
            Check(guard!=null && guard.Offense.TrySpecial(),"A charge aimed across architecture can be telegraphed safely");
            yield return new WaitForSeconds(1.7f);
            Check(guard!=null && guard.transform.position.z<1.1f && game.Vitals.Life==100,"Solid architecture blocks guard charges and their damage path");

            ResetPressureFixture(new Vector3(-5,0,-8));
            guard=game.Enemies.SpawnAt(new Vector3(-5,0,-4),DemonKind.ObsidianGuard,true,10);
            Check(guard!=null && guard.MaxHealth>game.settings.radianceDamage*6,"Late guards survive primary-only burst damage");
            var lightning=game.Powers.Cast(1,guard.transform.position);
            var fire=game.Powers.Cast(2,guard.transform.position);
            var spears=game.Powers.Cast(0,guard.transform.position);
            yield return new WaitForSeconds(.9f);
            Check(lightning && fire && spears && !guard.IsAlive,"Chaining lightning, fire, and spears quickly banishes a durable late guard");

            ResetPressureFixture(game.settings.spawnPoint);
            game.Waves.BeginScenario(); game.Waves.StartNextWave();
            var clock=Time.time+100;
            for (var step=0;step<500 && game.Waves.Wave<10;step++)
            {
                clock+=8; game.Waves.Tick(clock);
                foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive) { enemy.Hit(10000,DivineDamage.Radiance,game.Player.transform.position); enemy.ReturnToPool(); }
                if (step%10==0) yield return null;
            }
            for (var i=0;i<30;i++) { clock+=2; game.Waves.Tick(clock); }
            Check(game.Waves.Wave==10 && game.Enemies.ActiveCount==24 && game.Enemies.Enemies.Count<=game.Enemies.PoolCapacity,"A live wave-ten encounter fills the larger population while keeping its pool bounded");
            yield return new WaitForSeconds(1.8f);
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive) enemy.Passive=true;
            yield return new WaitForSeconds(.12f);
            Check(game.Hud.Minimap.EnemyMarkerCount==game.Enemies.ActiveCount,"The minimap tracks the larger late-wave population");
            var camera=Camera.main; var follow=camera.GetComponent<SanctuaryCamera>(); follow.enabled=false;
            camera.transform.position=game.Player.transform.position+Vector3.up*.6f-camera.transform.forward*26;
            yield return Capture("18-wave-ten-pressure.png"); follow.enabled=true;
            game.Waves.StopScenario(); game.Enemies.DespawnAll(); game.Drops.Clear();
        }
    }
}
