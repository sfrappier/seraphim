using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        IEnumerator CombatChecks()
        {
            game.Enemies.SpawnAutomatic=false;
            game.Enemies.DespawnAll();
            game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath();
            game.Player.Agent.Warp(new Vector3(0,0,-5));
            game.Vitals.RestoreLife(game.Vitals.MaxLife); game.Vitals.RestoreSpirit(game.Vitals.MaxSpirit);
            game.Enemies.Populate();
            var initial=game.Enemies.ActiveCount;
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive) enemy.Passive=true;
            var kinds=new HashSet<DemonKind>();
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive) kinds.Add(enemy.Kind);
            Check(initial==game.settings.initialEnemies && kinds.Count==DemonRoster.KindCount,"Random initial encounter includes all four imported demon classes");
            game.Enemies.SpawnAutomatic=true;
            yield return new WaitForSeconds(game.settings.enemySpawnInterval+.65f);
            game.Enemies.SpawnAutomatic=false;
            Check(game.Enemies.ActiveCount>initial && game.Enemies.ActiveCount<=game.settings.enemyLimit,"Timed random demon spawning respects the population cap");
            for (var i=0;i<game.settings.enemyLimit+8;i++) game.Enemies.SpawnRandom();
            Check(game.Enemies.ActiveCount==game.settings.enemyLimit,"Additional spawn requests cannot exceed the demon limit");
            var grounded=true;
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive)
            {
                enemy.Passive=true;
                grounded &= NavMesh.SamplePosition(enemy.transform.position,out _,.2f,NavMesh.AllAreas);
            }
            Check(grounded,"Every spawned demon stands on navigable ground");
            yield return new WaitForSeconds(.35f);
            yield return Capture("06-demon-encounter.png");
            game.Enemies.DespawnAll();
            game.Player.Agent.Warp(new Vector3(-5,0,-8));
            var target=game.Enemies.SpawnAt(new Vector3(-5,0,-5.5f),DemonKind.CinderImp,true);
            Check(target!=null,"A demon target can be spawned for combat");
            if (target==null) yield break;
            game.Vitals.RestoreSpirit(100);
            var kills=game.Enemies.KillCount;
            Check(game.Skills.TryUse(0),"Radiance fires in the combat encounter");
            Check(!game.Skills.TryUse(0),"Radiance still blocks same-frame repeat casts");
            yield return new WaitForSeconds(.4f);
            Check(Mathf.Abs(target.Health-(target.MaxHealth-game.settings.radianceDamage))<.1f,"An expanding Radiance pulse damages each demon only once");
            Check(target.HealthBar.DisplayedFraction<1 && target.HealthBar.IsVisible,"Overhead health bar reflects the damage dealt");
            yield return Capture("07-radiance-hit.png");
            while (game.Ability.CooldownRemaining>0) yield return null;
            Check(game.Skills.TryUse(0),"Radiance can fire again after its shorter cooldown");
            Check(game.settings.pulseCooldown<=.6f,"Radiance has a 0.6-second cooldown");
            yield return new WaitForSeconds(.45f);
            Check(!target.IsAlive && game.Enemies.KillCount>kills,"Two quick Radiance pulses banish a weak demon");
            var oldId=target.TargetId;
            yield return new WaitForSeconds(.85f);
            Check(!target.gameObject.activeSelf,"Defeated demon disperses and returns to its pool");
            target=game.Enemies.SpawnAt(new Vector3(-5,0,-4),DemonKind.CinderImp,true);
            Check(target!=null && target.TargetId!=oldId && target.Health==target.MaxHealth && !target.IsBurning,"Reused demons have fresh health and cleared status effects");
            game.Enemies.DespawnAll();
            var first=game.Enemies.SpawnAt(new Vector3(-5,0,-1),DemonKind.CinderImp,true);
            var second=game.Enemies.SpawnAt(new Vector3(-5,0,3),DemonKind.CinderImp,true);
            game.Vitals.RestoreSpirit(100);
            yield return new WaitForSeconds(.3f);
            Check(first!=null && Vector3.Distance(game.Powers.AimFromScreen(Camera.main.WorldToScreenPoint(first.transform.position+Vector3.up)),first.transform.position)<.5f,"Mouse aiming selects a demon's actual hitbox");
            Check(game.Powers.Cast(0,new Vector3(-5,0,10)),"Lance of Dawn launches its spear volley");
            yield return new WaitForSeconds(.16f);
            yield return Capture("08-lance-of-dawn.png");
            yield return new WaitForSeconds(.5f);
            Check(first!=null && second!=null && !first.IsAlive && !second.IsAlive,"Swept spear hits pierce through multiple demons without tunneling");
            yield return new WaitForSeconds(.8f);
            game.Enemies.DespawnAll();
            game.Player.Agent.Warp(new Vector3(0,0,-8));
            var chain=new List<DemonEnemy>();
            foreach (var x in new[]{-5f,-2f,1f,4f})
                chain.Add(game.Enemies.SpawnAt(new Vector3(x,0,-3),DemonKind.ObsidianGuard,true));
            game.Vitals.RestoreSpirit(100);
            Check(game.Powers.Cast(1,new Vector3(-5,0,-3)),"Chain of Heaven casts toward the aimed demon");
            var allChained=true;
            foreach (var enemy in chain) allChained &= enemy!=null && enemy.Health<enemy.MaxHealth && enemy.LastDamageSource==DivineDamage.Chain && enemy.IsStunned;
            Check(allChained,"Lightning chains through four distinct demons and stuns them");
            yield return new WaitForSeconds(.08f);
            yield return Capture("09-chain-of-heaven.png");
            yield return new WaitForSeconds(.55f);
            game.Enemies.DespawnAll();
            game.Player.Agent.Warp(new Vector3(-5,0,-8));
            var burning=game.Enemies.SpawnAt(new Vector3(-5,0,-4),DemonKind.ObsidianGuard,true);
            game.Vitals.RestoreSpirit(100);
            Check(game.Skills.TryUse(3),"Slot 4 casts Seraphic Inferno");
            yield return new WaitForSeconds(.5f);
            Check(burning!=null && burning.Health<burning.MaxHealth && burning.IsBurning,"Expanding Inferno hits, knocks back, and ignites a demon");
            yield return Capture("10-seraphic-inferno.png");
            yield return new WaitForSeconds(3.2f);
            Check(burning!=null && Mathf.Abs(burning.Health-(burning.MaxHealth-game.settings.infernoDamage-24))<.2f
                && burning.LastDamageSource==DivineDamage.Burning,"Inferno burn applies three damage ticks without duplicate wave hits");
            game.Enemies.DespawnAll();
            game.Player.Agent.Warp(new Vector3(0,0,-8));
            var attacker=game.Enemies.SpawnAt(new Vector3(0,0,-6.7f),DemonKind.CinderImp);
            var life=game.Vitals.Life;
            yield return new WaitForSeconds(2);
            Check(attacker!=null && game.Vitals.Life<life,"Nearby demons land telegraphed melee attacks");
            game.Enemies.DespawnAll();
            game.Vitals.TakeDamage(1000);
            yield return new WaitForSeconds(.25f);
            Check(game.Vitals.Life==game.Vitals.MaxLife && game.Enemies.ActiveCount==0
                && Vector3.Distance(game.Player.transform.position,game.settings.spawnPoint)<.5f,"Defeat safely restores the player at the sanctuary and disperses demons");
            var assigned=0; for (var i=0;i<6;i++) if (game.Skills.IsAssigned(i)) assigned++;
            Check(assigned==5,"The numbered skill bar exposes all five combat powers");
        }
    }
}
