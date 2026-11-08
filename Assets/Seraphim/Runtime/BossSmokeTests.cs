using System.Collections;
using UnityEngine;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        void BossFixture(Vector3 player,Vector3? boss=null)
        {
            game.Boss.AutomaticAttacks=false; game.Boss.Field.Clear();
            game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(player);
            game.Boss.Enemy.Agent.ResetPath(); game.Boss.Enemy.Agent.Warp(boss??new Vector3(-5,0,-3));
            game.Vitals.RestoreLife(100); game.Vitals.RestoreSpirit(100);
            game.Ability.ResetForRun(); game.Ultimate.ResetForRun(); game.Powers.ResetForRun();
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
            boss.Hit(boss.Health-boss.MaxHealth*.6f,DivineDamage.Lance,position); yield return null;
            Check(game.Boss.Phase==2 && game.Boss.Hud.DisplayedFraction<.65f,"Boss damage advances phase two and updates the health bar");
            boss.Hit(boss.Health-boss.MaxHealth*.25f,DivineDamage.Lance,position); yield return null;
            Check(game.Boss.Phase==3 && game.Drops.ActiveCount>=4 && game.Drops.ActiveCount<=48,"Final phase increases pressure and releases bounded restoration rewards");

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
            Check(game.Boss.Field.ActiveCount==0 && !game.Boss.IsAlive && Camera.main.GetComponent<SanctuaryCamera>().BossZoom==0 && game.Enemies.Score==score,"Boss retry clears all attacks and camera effects while retaining score");
            game.Waves.Tick(Time.time+game.settings.bossRestSeconds+.1f); game.Boss.AutomaticAttacks=false;
            Check(game.Boss.Enemy==boss && boss.Health==boss.MaxHealth && !boss.IsBurning && !boss.IsStunned,"Retry reuses the boss with full health and no stale status effects");
            game.Boss.Cast(BossAttack.Meteor); score=game.Enemies.Score;
            boss.Hit(100000,DivineDamage.WordOfGod,game.Player.transform.position); boss.Hit(100000,DivineDamage.WordOfGod,game.Player.transform.position);
            yield return null;
            Check(game.Waves.State==WaveDirector.EncounterState.Victory && game.Enemies.Score==score+5000,"Defeating Vorath grants final victory and awards 5000 points exactly once");
            Check(game.Boss.Field.ActiveCount==0 && !game.Boss.Hud.Visible && Camera.main.GetComponent<SanctuaryCamera>().BossTarget==null,"Boss death clears every pending hazard, health bar and temporary camera target");
            game.Waves.BeginScenario();
            Check(game.Waves.Wave==0 && game.Enemies.Score==0 && !game.Boss.IsAlive,"Replay restores the normal ten-wave scenario after the boss");
            game.Waves.StopScenario();
            var guard=game.Enemies.SpawnAt(new Vector3(-5,0,-4),DemonKind.ObsidianGuard,true);
            Check(guard!=null && !guard.IsBoss && guard.MaxHealth<100,"Normal spawning never reuses the dedicated boss as an ordinary guard");
            game.Enemies.DespawnAll(); game.Drops.Clear();
        }
    }
}
