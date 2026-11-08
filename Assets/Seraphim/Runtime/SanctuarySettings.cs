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
        public float pulseCooldown = 2.5f;
        public float pulseRadius = 7;
        [Header("Life and Spirit")]
        public float maxLife = 100;
        public float maxSpirit = 100;
        public float spiritRegeneration = 8;
        public float radianceSpiritCost = 15;
        [Header("Word of God")]
        public float ultimateCooldown = 30;
        public float ultimateRadius = 18;
        [Range(100, 1600)] public int ultimateParticles = 1100;
    }
}
