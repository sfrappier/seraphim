using System.Collections;
using UnityEngine;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        void BossFixture(Vector3 player,Vector3? boss=null)
        {
            game.Boss.AutomaticAttacks=false; game.Boss.Summons.AutoReplenish=false; game.Boss.Summons.Clear(); game.Boss.Field.Clear();
            game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(player);
            game.Boss.Enemy.Agent.ResetPath(); game.Boss.Enemy.Agent.Warp(boss??new Vector3(-5,0,-3));
            game.Vitals.RestoreLife(100); game.Vitals.RestoreSpirit(100);
            game.Ability.ResetForRun(); game.Ultimate.ResetForRun(); game.Powers.ResetForRun();
        }
        void FreezeBossSummons()
        {
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive && enemy.IsBossSummon)
            { enemy.Passive=true; enemy.Agent.ResetPath(); }
        }
        IEnumerator WaitBossTransition(bool clearSummons=false)
        {
            var deadline=Time.realtimeSinceStartup+10;
            while (game.Boss.IsTransitioning && Time.realtimeSinceStartup<deadline)
            {
                if (clearSummons) game.Boss.Summons.Clear();
                yield return null;
            }
        }
        IEnumerator DefeatBossThroughPhases()
        {
            for (var i=0;i<3 && game.Boss.IsAlive;i++)
            {
                game.Boss.Enemy.Hit(100000,DivineDamage.WordOfGod,game.Player.transform.position);
                yield return WaitBossTransition(true);
            }
        }
        IEnumerator BossChecks()
        {
            ResetPressureFixture(new Vector3(-5,0,-9));
            yield return new WaitForSeconds(4.1f);
            game.Waves.BeginBossTrial(); game.Waves.Tick(Time.time+game.settings.bossRestSeconds+.1f);
            Check(game.Waves.State==WaveDirector.EncounterState.BossBattle && game.Boss.IsAlive,"Boss trial enters the same final encounter used after wave ten");
            if (!game.Boss.IsAlive) yield break;
            var boss=game.Boss.Enemy;
            BossFixture(new Vector3(-5,0,-9)); yield return new WaitForSeconds(.5f);
            var bounds=SanctuaryGame.VisualBounds(boss.GetComponent<CathedralBossArt>().Body.GetComponentsInChildren<Renderer>());
            Check(boss.IsBoss && bounds.size.y>7 && bounds.size.x>5,"Vorath has an original towering skeletal model, crown, robe and scythe");
            Check(boss.MaxHealth==game.settings.bossHealth && boss.MaxHealth>=4000 && game.Boss.Phase==1,"Final boss starts with a full substantial health pool and phase one");
            Check(game.Boss.Hud.Visible && game.Boss.Hud.DisplayedFraction==1,"Final boss displays a dedicated health bar");
            Check(SanctuaryHud.BlocksScreenPoint(new Vector2(Screen.width*.5f,Screen.height-90)),"Boss health HUD cannot issue world movement");
            Check(game.Hud.Minimap.EnemyMarkerCount==1 && Camera.main.GetComponent<SanctuaryCamera>().BossZoom>0,"Minimap and wider camera track the boss encounter");
            yield return Capture("24-vorath.png");

            BossFixture(new Vector3(-5,0,-7));
            var health=boss.Health; var position=boss.transform.position;
            Check(game.Skills.TryUseDirected(0,position),"Radiance can be cast against Vorath");
            yield return new WaitForSeconds(.65f);
            Check(boss.Health<health && boss.LastDamageSource==DivineDamage.Radiance,"Radiance damages the boss");
            health=boss.Health; game.Skills.TryUseDirected(1,position); yield return new WaitForSeconds(.65f);
            Check(boss.Health<health && boss.LastDamageSource==DivineDamage.Lance,"Swept laser spears hit the boss silhouette");
            health=boss.Health; game.Skills.TryUseDirected(2,position);
            Check(boss.Health<health && !boss.IsStunned && Vector3.Distance(position,boss.transform.position)<.1f,"Lightning damages Vorath while boss poise resists stun and knockback");
            health=boss.Health; game.Skills.TryUseDirected(3,position); yield return new WaitForSeconds(.6f);
            Check(boss.Health<health && boss.IsBurning,"Inferno damages and burns the boss");
            health=boss.Health; game.Skills.TryUseDirected(4,position); yield return new WaitForSeconds(2.1f);
            Check(game.Ultimate.PulseCount>=1 && boss.Health<health,"Word of God pulses damage the boss with its normal cooldown");
            game.Ultimate.ResetForRun();
            boss.Hit(100000,DivineDamage.Lance,position);
            boss.Hit(100000,DivineDamage.WordOfGod,position);
            Check(boss.IsAlive && Mathf.Abs(boss.HealthFraction-.65f)<.001f,"Lethal same-frame burst damage cannot skip the first boss phase boundary");
            yield return null; FreezeBossSummons();
            Check(game.Boss.Phase==2 && game.Boss.IsTransitioning && game.Boss.Hud.DisplayedFraction<=.651f,"Phase two begins with an explicit visible transition ward");
            var wardHealth=boss.Health; boss.Hit(100000,DivineDamage.Chain,position);
            Check(boss.Health==wardHealth,"Transition ward prevents damage from immediately skipping phase two");
            Check(game.Boss.Summons.ActiveCount>=3,"Phase two summons a real attacking demon host");
            for (var i=0;i<5;i++) game.Boss.Summons.SpawnWave(); FreezeBossSummons();
            Check(game.Boss.Summons.ActiveCount==game.settings.bossPhaseTwoSummonLimit && game.Enemies.ActiveCount<=game.settings.enemyLimit,"Phase-two summons stop at their separate living cap");
            yield return Capture("30-ashen-host.png");
            game.Drops.Clear(); DemonEnemy minion=null;
            foreach (var item in game.Enemies.Enemies) if (item.IsAlive && item.IsBossSummon) { minion=item; break; }
            minion.Hit(100000,DivineDamage.Radiance,game.Player.transform.position);
            Check(!minion.IsAlive && game.Drops.ActiveCount==2,"Slain boss summons drop both Life and Spirit orbs");
            var gold=false; var silver=false;
            foreach (var orb in game.Drops.Orbs) if (orb.Active)
            {
                var color=orb.visual.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
                if (orb.kind==DropKind.Life) gold=color.r>color.b*2 && color.g>color.b*2;
                else silver=Mathf.Max(color.r,Mathf.Max(color.g,color.b))-Mathf.Min(color.r,Mathf.Min(color.g,color.b))<.3f;
            }
            Check(gold && silver,"Boss summons drop gold Life and silver Spirit resources");
            yield return WaitBossTransition();
            Check(!game.Boss.IsTransitioning,"Phase-two ward ends and boss damage becomes available again");
            boss.Hit(100000,DivineDamage.Lance,position); boss.Hit(100000,DivineDamage.Radiance,position);
            Check(boss.IsAlive && Mathf.Abs(boss.HealthFraction-.3f)<.001f,"Second damage boundary guarantees the third phase occurs before death");
            yield return null; FreezeBossSummons();
            Check(game.Boss.Phase==3 && boss.GetComponent<CathedralBossArt>().TransformationActive,"Phase three starts a dedicated ascension animation");
            var startScale=boss.EncounterScale;
            yield return new WaitForSeconds(game.settings.bossTransformationSeconds*.45f);
            Check(boss.EncounterScale>startScale+.05f && boss.EncounterScale<game.settings.bossAscendedScale,"Boss growth animates progressively during transformation");
            yield return Capture("31-ascension-of-ash.png");
            yield return WaitBossTransition(); yield return null;
            var enlarged=true;
            foreach (var item in game.Enemies.Enemies) if (item.IsAlive && item.IsBossSummon) enlarged &= item.EncounterScale>=game.settings.bossSummonScale-.01f;
            Check(Mathf.Abs(boss.EncounterScale-game.settings.bossAscendedScale)<.01f && enlarged,"Ascendant boss and surviving summons reach their larger third-phase sizes");
            game.Boss.Summons.SpawnWave(); FreezeBossSummons();
            yield return new WaitForSeconds(game.settings.bossTransformationSeconds+.1f);
            enlarged=true;
            foreach (var item in game.Enemies.Enemies) if (item.IsAlive && item.IsBossSummon) enlarged &= item.EncounterScale>=game.settings.bossSummonScale-.01f;
            Check(enlarged && game.Boss.Summons.ActiveCount==game.settings.bossPhaseThreeSummonLimit,"New phase-three summons also grow and obey the larger living cap");
            Check(!boss.GetComponent<CathedralBossArt>().TransformationActive,"Temporary transformation lightning and particles finish cleanly");
            yield return Capture("32-vorath-ascendant.png");
            DemonEnemy attacker=null;
            foreach (var item in game.Enemies.Enemies) if (item.IsAlive && item.IsBossSummon && item.Kind==DemonKind.CinderImp) { attacker=item; break; }
            game.Vitals.RestoreLife(100);
            attacker.Agent.Warp(game.Player.transform.position+Vector3.forward*1.5f); attacker.Passive=false;
            yield return new WaitForSeconds(1.3f);
            Check(game.Vitals.Life<100,"Summoned demons actively attack and damage the player");
            attacker.Passive=true; attacker.Agent.ResetPath();
            var collected=game.Drops.Collected; var summoned=game.Boss.Summons.TotalSummoned;
            attacker.Hit(100000,DivineDamage.Radiance,game.Player.transform.position);
            game.Boss.Summons.AutoReplenish=true; yield return new WaitForSeconds(.2f); FreezeBossSummons();
            Check(game.Boss.Summons.TotalSummoned>summoned && game.Boss.Summons.ActiveCount==game.settings.bossPhaseThreeSummonLimit,"Automatic reinforcements replace fallen summons without exceeding the cap");
            yield return new WaitForSeconds(.5f);
            Check(game.Drops.Collected>collected && game.Vitals.Life==game.Vitals.MaxLife,"Collecting a slain summon Life orb restores player health");
            game.Boss.Summons.AutoReplenish=false;

            BossFixture(new Vector3(-5,0,-9));
            game.Boss.Cast(BossAttack.Meteor);
            Check(game.Boss.Field.ActiveCount==5,"Final-phase meteor rain marks five bounded impact areas");
            var locked=game.Boss.Field.Hazards[0].point;
            Check(Vector3.Distance(locked,game.Player.transform.position)<.1f,"First meteor locks the player's position during its warning");
            yield return new WaitForSeconds(1.15f); yield return Capture("25-meteor-rain.png");
            yield return new WaitForSeconds(.5f);
            Check(Mathf.Abs(game.Vitals.Life-(100-game.settings.bossMeteorDamage))<.1f,"Standing in a warned meteor takes impact damage once");
            var life=game.Vitals.Life; yield return new WaitForSeconds(.7f);
            Check(game.Vitals.Life<life,"Meteor craters leave damaging persistent fire");
            yield return Capture("26-burning-craters.png");
            BossFixture(new Vector3(-5,0,-9)); game.Boss.Cast(BossAttack.Meteor);
            game.Player.Agent.Warp(new Vector3(-5,0,-23)); yield return new WaitForSeconds(2);
            Check(game.Vitals.Life==100,"Leaving locked meteor circles avoids their impact and fire");

            BossFixture(new Vector3(-5,0,-9)); game.Boss.Cast(BossAttack.Fire);
            yield return new WaitForSeconds(1.8f); yield return Capture("27-hellfire-fan.png");
            Check(game.Vitals.Life<100,"Traveling boss fire damages a player remaining in its warned path");
            BossFixture(new Vector3(0,0,8),new Vector3(0,0,-2));
            game.Boss.Field.Cast(BossAttack.Fire,boss.transform.position,Vector3.forward);
            var fire=game.Boss.Field.Hazards[0];
            Check(fire.end.z<2,"Boss fire paths stop at solid cathedral architecture");
            yield return new WaitForSeconds(2.4f);
            Check(game.Vitals.Life==100,"Cathedral architecture blocks traveling hellfire damage");

            BossFixture(new Vector3(-5,0,-10)); game.Boss.Cast(BossAttack.Nova);
            yield return new WaitForSeconds(1.6f);
            Check(Mathf.Abs(game.Vitals.Life-(100-game.settings.bossNovaDamage))<.1f,"Hollow nova damages the warned annular region");
            BossFixture(new Vector3(-5,0,-6)); game.Boss.Cast(BossAttack.Nova);
            yield return new WaitForSeconds(1.6f);
            Check(game.Vitals.Life==100,"Hollow nova leaves a safe inner zone");
            BossFixture(new Vector3(-5,0,-19)); game.Boss.Cast(BossAttack.Nova);
            yield return new WaitForSeconds(1.6f);
            Check(game.Vitals.Life==100,"Escaping beyond the nova boundary avoids damage");

            BossFixture(new Vector3(-5,0,-7)); game.Boss.Cast(BossAttack.Slash);
            yield return new WaitForSeconds(1.2f); yield return Capture("28-reaping-scythe.png");
            Check(Mathf.Abs(game.Vitals.Life-(100-game.settings.bossSlashDamage))<.1f,"Close-range scythe sweep deals heavy single-hit damage");
            BossFixture(new Vector3(-5,0,-7)); game.Boss.Cast(BossAttack.Slash);
            game.Player.Agent.Warp(new Vector3(-5,0,1)); yield return new WaitForSeconds(1.25f);
            Check(game.Vitals.Life==100,"Dodging behind the locked scythe sector avoids the slash");
            Check(game.Boss.Field.ActiveCount<=game.Boss.Field.Capacity,"Boss hazards stay within their bounded reusable pool");
            BossFixture(new Vector3(-5,0,-20)); game.Boss.AutomaticAttacks=true;
            var casts=game.Boss.Casts; yield return new WaitForSeconds(4);
            Check(game.Boss.Casts>=casts+2,"Boss AI automatically chains different ranged attacks in the final phase");
            game.Boss.AutomaticAttacks=false;

            var score=game.Enemies.Score; game.Vitals.TakeDamage(10000); yield return new WaitForSeconds(.2f);
            Check(game.Waves.State==WaveDirector.EncounterState.BossApproaching && game.Waves.Wave==10,"Player defeat retries the boss without replaying ten waves");
            Check(game.Boss.Field.ActiveCount==0 && game.Boss.Summons.ActiveCount==0 && !game.Boss.IsAlive && Camera.main.GetComponent<SanctuaryCamera>().BossZoom==0 && game.Enemies.Score==score,"Boss retry clears all attacks, summons and camera effects while retaining score");
            game.Waves.Tick(Time.time+game.settings.bossRestSeconds+.1f); game.Boss.AutomaticAttacks=false;
            Check(game.Boss.Enemy==boss && boss.Health==boss.MaxHealth && !boss.IsBurning && !boss.IsStunned && boss.EncounterScale==1 && game.Boss.Phase==1,"Retry restores first-phase size, health and clean status effects");
            game.Boss.Cast(BossAttack.Meteor); score=game.Enemies.Score;
            yield return DefeatBossThroughPhases();
            boss.Hit(100000,DivineDamage.WordOfGod,game.Player.transform.position);
            yield return null;
            Check(game.Waves.State==WaveDirector.EncounterState.Victory && game.Enemies.Score==score+5000,"Defeating Vorath grants final victory and awards 5000 points exactly once");
            Check(game.Boss.Field.ActiveCount==0 && game.Boss.Summons.ActiveCount==0 && !game.Boss.Hud.Visible && Camera.main.GetComponent<SanctuaryCamera>().BossTarget==null,"Boss death clears hazards, summons, health bar and temporary camera target");
            game.Waves.BeginScenario();
            Check(game.Waves.Wave==0 && game.Enemies.Score==0 && !game.Boss.IsAlive,"Replay restores the normal ten-wave scenario after the boss");
            game.Waves.StopScenario();
            var guard=game.Enemies.SpawnAt(new Vector3(-5,0,-4),DemonKind.ObsidianGuard,true);
            Check(guard!=null && !guard.IsBoss && !guard.IsBossSummon && guard.EncounterScale==1 && guard.MaxHealth<100,"Reused ordinary demons have no boss identity, summon flags or growth");
            game.Enemies.DespawnAll(); game.Drops.Clear();
        }
    }
}
