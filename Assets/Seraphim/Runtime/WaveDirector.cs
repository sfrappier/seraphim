using UnityEngine;

namespace Seraphim
{
    public sealed class WaveDirector : MonoBehaviour
    {
        public enum EncounterState { Dormant,Resting,Fighting,Victory }
        SanctuaryGame game;
        float nextSpawn,restUntil;
        public EncounterState State { get; private set; }
        public int Wave { get; private set; }
        public int Spawned { get; private set; }
        public int Slain { get; private set; }
        public int Total => TargetFor(Wave);
        public int ConcurrentLimit => LimitFor(Wave);
        public float RestRemaining => Mathf.Max(0,restUntil-Time.time);
        public int TargetFor(int wave) => 6+Mathf.Max(0,wave-1)*2;
        public int LimitFor(int wave) => Mathf.Min(game.settings.enemyLimit,3+(Mathf.Max(1,wave)-1)/2+((wave>=8)?1:0));
        public float IntervalFor(int wave) => Mathf.Max(.65f,game.settings.waveSpawnInterval-(wave-1)*.13f);
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public void BeginScenario()
        {
            game.Enemies.SpawnAutomatic=false; game.Enemies.DespawnAll(); game.Enemies.ResetScore();
            game.Ultimate.Cancel(); game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(game.settings.spawnPoint);
            game.Vitals.RestoreLife(game.Vitals.MaxLife); game.Vitals.RestoreSpirit(game.Vitals.MaxSpirit);
            game.Drops.Clear(); Wave=0; Spawned=Slain=0;
            State=EncounterState.Resting; restUntil=Time.time+4;
        }
        public void StartNextWave()
        {
            if (State!=EncounterState.Resting || game.Enemies.ActiveCount>0) return;
            Wave++; Spawned=Slain=0; State=EncounterState.Fighting; nextSpawn=Time.time;
        }
        public void RecordSlay(DemonEnemy enemy)
        {
            if (State==EncounterState.Fighting && enemy.WaveNumber==Wave) Slain++;
        }
        public void Recover()
        {
            if (State!=EncounterState.Fighting && State!=EncounterState.Resting) return;
            // Retry the current wave with a fresh quota; score already earned is retained.
            if (State==EncounterState.Fighting) Wave--;
            Spawned=Slain=0; State=EncounterState.Resting; restUntil=Time.time+game.settings.waveRestSeconds;
        }
        public void StopScenario() { State=EncounterState.Dormant; game.Enemies.SpawnAutomatic=false; }
        void Update()
        {
            if (game==null) return;
            if (State==EncounterState.Victory && Input.GetKeyDown(KeyCode.R)) BeginScenario();
            Tick(Time.time);
        }
        // Explicit clock keeps the encounter scheduler repeatable in player checks.
        public void Tick(float now)
        {
            if (State==EncounterState.Resting && now>=restUntil) StartNextWave();
            if (State!=EncounterState.Fighting) return;
            if (Slain>=Total && Spawned>=Total && game.Enemies.ActiveCount==0)
            {
                if (Wave>=game.settings.waveCount) State=EncounterState.Victory;
                else
                {
                    State=EncounterState.Resting; restUntil=now+game.settings.waveRestSeconds;
                    game.Vitals.RestoreLife(15); game.Vitals.RestoreSpirit(25);
                }
                return;
            }
            if (Spawned>=Total || game.Enemies.ActiveCount>=ConcurrentLimit || now<nextSpawn) return;
            nextSpawn=now+IntervalFor(Wave);
            var roll=Random.value;
            var guardChance=.04f+Wave*.015f;
            var kind=roll<guardChance?DemonKind.ObsidianGuard:roll<.35f?DemonKind.AshStalker:DemonKind.CinderImp;
            if (game.Enemies.SpawnRandom(kind,Wave)!=null) Spawned++;
        }
    }
}
