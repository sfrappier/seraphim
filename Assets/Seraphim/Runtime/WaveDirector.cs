using UnityEngine;

namespace Seraphim
{
    public sealed class WaveDirector : MonoBehaviour
    {
        public enum EncounterState { Dormant,Resting,Fighting,BossApproaching,BossBattle,Victory }
        SanctuaryGame game;
        float nextSpawn,restUntil,waveStarted;
        public EncounterState State { get; private set; }
        public int Wave { get; private set; }
        public int Spawned { get; private set; }
        public int Slain { get; private set; }
        public int Total => TargetFor(Wave);
        public int ConcurrentLimit => LimitFor(Wave);
        public float RestRemaining => Mathf.Max(0,restUntil-Time.time);
        public int TargetFor(int wave) => game.settings.firstWaveEnemies+Mathf.Max(0,wave-1)*game.settings.enemiesAddedPerWave;
        public int LimitFor(int wave) => Mathf.Min(game.settings.enemyLimit,game.settings.firstWaveConcurrent+Mathf.Max(0,wave-1)*game.settings.concurrentAddedPerWave+(wave>=7?2:0));
        public float IntervalFor(int wave) => Mathf.Max(game.settings.minimumWaveSpawnInterval,game.settings.waveSpawnInterval-Mathf.Max(0,wave-1)*game.settings.waveSpawnAcceleration);
        public int BurstFor(int wave) => wave>=9?3:wave>=6?2:1;
        public int GroupsFor(int wave) => wave>=9?3:wave>=6?2:1;
        public int SpawnBudgetFor(int wave,float elapsed)
        {
            var groups=GroupsFor(wave);
            var available=Mathf.Clamp(1+Mathf.FloorToInt(Mathf.Max(0,elapsed)/game.settings.reinforcementInterval),1,groups);
            return Mathf.CeilToInt(TargetFor(wave)*available/(float)groups);
        }
        public float ReinforcementRemaining
        {
            get
            {
                if (State!=EncounterState.Fighting || SpawnBudgetFor(Wave,Time.time-waveStarted)>Spawned || Spawned>=Total) return 0;
                var stage=Mathf.FloorToInt(Mathf.Max(0,Time.time-waveStarted)/game.settings.reinforcementInterval)+1;
                return Mathf.Max(0,waveStarted+stage*game.settings.reinforcementInterval-Time.time);
            }
        }
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public void BeginScenario()
        {
            game.Boss?.Clear();
            game.Enemies.SpawnAutomatic=false; game.Enemies.DespawnAll(); game.Enemies.ResetScore();
            game.Ultimate.Cancel(); game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(game.settings.spawnPoint);
            game.Vitals.RestoreLife(game.Vitals.MaxLife); game.Vitals.RestoreSpirit(game.Vitals.MaxSpirit);
            game.Drops.Clear(); Wave=0; Spawned=Slain=0;
            State=EncounterState.Resting; restUntil=Time.time+4;
        }
        public void StartNextWave(float? now=null)
        {
            if (State!=EncounterState.Resting || game.Enemies.ActiveCount>0) return;
            Wave++; Spawned=Slain=0; State=EncounterState.Fighting; nextSpawn=waveStarted=now??Time.time;
            game.Audio?.Play(SoundCue.Wave);
        }
        public void BeginBossTrial()
        {
            BeginScenario(); Wave=game.settings.waveCount; State=EncounterState.BossApproaching;
            restUntil=Time.time+game.settings.bossRestSeconds;
        }
        public void RecordSlay(DemonEnemy enemy)
        {
            if (State==EncounterState.Fighting && enemy.WaveNumber==Wave) Slain++;
        }
        public void Recover()
        {
            if (State==EncounterState.BossBattle || State==EncounterState.BossApproaching)
            {
                game.Boss.Clear(); State=EncounterState.BossApproaching; restUntil=Time.time+game.settings.bossRestSeconds; return;
            }
            if (State!=EncounterState.Fighting && State!=EncounterState.Resting) return;
            // Retry the current wave with a fresh quota; score already earned is retained.
            if (State==EncounterState.Fighting) Wave--;
            Spawned=Slain=0; State=EncounterState.Resting; restUntil=Time.time+game.settings.waveRestSeconds;
        }
        public void StopScenario() { State=EncounterState.Dormant; game.Enemies.SpawnAutomatic=false; game.Boss?.Clear(); }
        public void BossDefeated()
        {
            if (State!=EncounterState.BossBattle) return;
            State=EncounterState.Victory; game.Audio.Play(SoundCue.Victory);
        }
        void Update()
        {
            if (game==null) return;
            if (State==EncounterState.Victory && (Input.GetKeyDown(KeyCode.R) || game.Gamepads.ReplayPressed)) BeginScenario();
            Tick(Time.time);
        }
        // Explicit clock keeps the encounter scheduler repeatable in player checks.
        public void Tick(float now)
        {
            if (State==EncounterState.BossApproaching && now>=restUntil)
            {
                if (game.Boss.Begin()) State=EncounterState.BossBattle;
                else restUntil=now+1;
            }
            if (State==EncounterState.Resting && now>=restUntil) StartNextWave(now);
            if (State!=EncounterState.Fighting) return;
            if (Slain>=Total && Spawned>=Total && game.Enemies.ActiveCount==0)
            {
                if (Wave>=game.settings.waveCount)
                {
                    State=EncounterState.BossApproaching; restUntil=now+game.settings.bossRestSeconds;
                    game.Drops.Clear(); game.Vitals.RestoreLife(35); game.Vitals.RestoreSpirit(100);
                    game.Audio.Play(SoundCue.Warning);
                }
                else
                {
                    State=EncounterState.Resting; restUntil=now+game.settings.waveRestSeconds;
                    game.Vitals.RestoreLife(15); game.Vitals.RestoreSpirit(25);
                }
                return;
            }
            var budget=SpawnBudgetFor(Wave,now-waveStarted);
            if (Spawned>=budget || game.Enemies.ActiveCount>=ConcurrentLimit || now<nextSpawn) return;
            nextSpawn=now+IntervalFor(Wave);
            for (var i=0;i<BurstFor(Wave) && Spawned<budget && game.Enemies.ActiveCount<ConcurrentLimit;i++)
            {
                var roll=Random.value;
                var guardChance=.06f+Wave*.021f;
                var scourgerChance=Wave>=4?.04f+(Wave-4)*.009f:0;
                var kind=roll<scourgerChance?DemonKind.RiftScourger:roll<scourgerChance+guardChance?DemonKind.ObsidianGuard:roll<scourgerChance+guardChance+.28f?DemonKind.AshStalker:DemonKind.CinderImp;
                if (game.Enemies.SpawnRandom(kind,Wave)!=null) Spawned++;
            }
        }
    }
}
