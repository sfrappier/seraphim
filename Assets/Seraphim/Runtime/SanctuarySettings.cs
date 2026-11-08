using UnityEngine;

namespace Seraphim
{
    [CreateAssetMenu(menuName = "Seraphim/Sanctuary settings")]
    public sealed class SanctuarySettings : ScriptableObject
    {
        [Header("Movement")]
        [Min(1)] public float moveSpeed = 6.5f;
        [Min(1)] public float acceleration = 32;
        public Vector3 spawnPoint = new Vector3(0, 0, -8);
        [Header("Camera")]
        [Range(30, 70)] public float cameraPitch = 52;
        public float cameraYaw = 38;
        public float cameraDistance = 20;
        public float minCameraDistance = 13;
        public float maxCameraDistance = 29;
        [Header("Sanctuary")]
        public int worldSeed = 1407;
        [Range(0, 80)] public int decorativeRubble = 28;
        [Header("Light ability")]
        public float pulseCooldown = .6f;
        public float pulseRadius = 7;
        [Header("Life and Spirit")]
        public float maxLife = 100;
        public float maxSpirit = 100;
        public float spiritRegeneration = 12;
        public float radianceSpiritCost = 6;
        [Header("Word of God")]
        public float ultimateCooldown = 30;
        public float ultimateRadius = 18;
        [Range(100, 1600)] public int ultimateParticles = 1100;
        public float ultimateDuration = 15;
        public float ultimatePulseInterval = 1;
        public float ultimateDamage = 16;
        [Header("Combat")]
        public float radianceDamage = 24;
        public float lanceCooldown = 1.8f;
        public float lanceCost = 12;
        public float lanceDamage = 55;
        public float chainCooldown = 3;
        public float chainCost = 18;
        public float chainDamage = 36;
        public float infernoCooldown = 4.5f;
        public float infernoCost = 22;
        public float infernoDamage = 35;
        [Header("Demon encounter")]
        [Range(1,32)] public int enemyLimit = 24;
        public int initialEnemies = 5;
        public float enemySpawnInterval = 3.5f;
        [Header("Cathedral scenario")]
        public Vector2 cathedralSize = new Vector2(84,112);
        public int waveCount = 10;
        public float waveRestSeconds = 6;
        public float waveSpawnInterval = 1.25f;
        public float lifeDropRestore = 12;
        public float spiritDropRestore = 18;
        [Header("Wave pressure")]
        public int firstWaveEnemies = 10;
        public int enemiesAddedPerWave = 6;
        public int firstWaveConcurrent = 4;
        public int concurrentAddedPerWave = 2;
        public float minimumWaveSpawnInterval = .32f;
        public float waveSpawnAcceleration = .105f;
        public float reinforcementInterval = 12;
        [Range(1,8)] public int maxConcurrentSpecialAttacks = 4;
        [Header("Demon scaling")]
        public float enemyVisualScale = 1.2f;
        public float enemySizeGrowth = .025f;
        public float enemyHealthGrowth = .18f;
        public float enemySpeedGrowth = .065f;
        public float enemyDamageGrowth = .18f;
        [Header("Main menu showcase")]
        [Range(32,128)] public int menuEnemyLimit = 96;
        [Header("Final boss")]
        public float bossHealth = 4800;
        public float bossMoveSpeed = 2.7f;
        public float bossMeteorDamage = 32;
        public float bossFireDamage = 11;
        public float bossSlashDamage = 38;
        public float bossNovaDamage = 34;
        public float bossRestSeconds = 7;
    }
}
