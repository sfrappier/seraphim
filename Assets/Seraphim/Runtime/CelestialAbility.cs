using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    public sealed class CelestialAbility : MonoBehaviour
    {
        sealed class Pulse
        {
            public LineRenderer ring;
            public float age;
            public readonly HashSet<int> hit=new HashSet<int>();
        }
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly List<Waystone> stones = new List<Waystone>();
        SanctuarySettings settings;
        Material glow;
        ParticleSystem sparks;
        float readyAt;
        SeraphimVitals vitals;
        EnemyDirector enemies;
        public int CastCount { get; private set; }
        public float CooldownRemaining => Mathf.Max(0, readyAt - Time.time);

        public void Initialize(SanctuarySettings config, Material effect,Material particleMaterial,List<Waystone> waystones,SeraphimVitals resources,EnemyDirector director)
        {
            enemies=director;
            vitals=resources;
            settings = config;
            glow = effect;
            stones.AddRange(waystones);
            var particleObject = new GameObject("Light motes");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.up * .65f;
            sparks = particleObject.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = .9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3, 7);
            main.startSize = new ParticleSystem.MinMaxCurve(.04f, .12f);
            main.startColor = new Color(1,.85f,.4f,.85f);
            main.gravityModifier = -.15f;
            main.maxParticles = 180;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = sparks.emission;
            emission.enabled = false;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .4f;
            sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        }

        public bool Cast()
        {
            if (Time.time < readyAt) return false;
            if (vitals.Spirit<settings.radianceSpiritCost) return false;
            var pulse = pulses.Find(p => !p.ring.gameObject.activeSelf);
            if (pulse == null)
            {
                if (pulses.Count >= 4) return false;
                pulse = new Pulse { ring = SceneArt.Ring("Radiance", null, 1, .12f, glow) };
                pulses.Add(pulse);
            }
            vitals.SpendSpirit(settings.radianceSpiritCost);
            readyAt=Time.time+settings.pulseCooldown;
            CastCount++;
            enemies.Game.Audio?.Play(SoundCue.Radiance,transform.position);
            pulse.ring.gameObject.SetActive(true);
            pulse.ring.transform.position = transform.position + Vector3.up * .12f;
            pulse.age = 0;
            pulse.hit.Clear();
            sparks.Emit(120);
            foreach (var stone in stones)
                if (Vector3.Distance(transform.position, stone.transform.position) <= settings.pulseRadius) stone.Attune();
            return true;
        }

        void Update()
        {
            foreach (var pulse in pulses)
            {
                if (!pulse.ring.gameObject.activeSelf) continue;
                pulse.age += Time.deltaTime;
                var t = pulse.age / .85f;
                pulse.ring.transform.localScale = Vector3.one * Mathf.Lerp(.3f, settings.pulseRadius, t);
                enemies.Radial(pulse.ring.transform.position,Mathf.Lerp(.3f,settings.pulseRadius,Mathf.Clamp01(t)),settings.radianceDamage,DivineDamage.Radiance,pulse.hit,.7f,.12f);
                pulse.ring.startColor = pulse.ring.endColor = new Color(1,.85f,.4f, Mathf.Clamp01(1-t));
                if (t >= 1) pulse.ring.gameObject.SetActive(false);
            }
        }

        public void ResetForRun()
        {
            readyAt=0; CastCount=0; sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var pulse in pulses) { pulse.ring.gameObject.SetActive(false); pulse.hit.Clear(); }
        }
        void OnDestroy() { foreach (var pulse in pulses) if (pulse.ring != null) Destroy(pulse.ring.gameObject); }
    }
}
