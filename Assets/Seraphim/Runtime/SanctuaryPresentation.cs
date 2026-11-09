using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    // Imported presentation never applies combat damage. The existing locked
    // warnings, projectiles, hit tests and cooldowns remain authoritative.
    public sealed class SanctuaryPresentation : MonoBehaviour
    {
        sealed class Instance
        {
            public GameObject root;
            public EffectBinding binding;
            public ParticleSystem[] particles;
            public Transform follow;
            public Vector3 offset;
            public Quaternion originalRotation;
            public float expires;
        }
        readonly List<Instance> pool=new List<Instance>();
        readonly float[] next=new float[Enum.GetValues(typeof(EffectCue)).Length];
        SanctuaryGame game;
        public PresentationBank Bank { get; private set; }
        public bool DownloadedEffectsAvailable => Bank!=null && Bank.effects!=null && Bank.effects.Length>0;
        public int ActiveCount { get { var count=0; foreach (var item in pool) if (item.root.activeSelf) count++; return count; } }
        public int PoolCount => pool.Count;
        public int PlayedEffects { get; private set; }
        public int ActiveParticleCount { get { var count=0; foreach (var item in pool) if (item.root.activeSelf) foreach (var ps in item.particles) count+=ps.particleCount; return count; } }
        public const int PoolLimit=96;
        public const int ActiveLimit=32;
        public void Initialize(SanctuaryGame owner)
        {
            game=owner;
            if (!Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-fallback-presentation")) Bank=Resources.Load<PresentationBank>("SeraphimPresentation");
        }
        public bool Play(EffectCue cue,Vector3 point,Transform follow=null,float scale=1,float seconds=0,Vector3? direction=null)
        {
            var binding=Bank!=null?Bank.Effect(cue):null;
            if (binding==null || binding.prefab==null || Time.time<next[(int)cue]) return false;
            if (game.Player!=null && (point-game.Player.transform.position).sqrMagnitude>48*48) return false;
            var active=0; Instance available=null;
            foreach (var item in pool) if (item.binding==binding)
            {
                if (item.root.activeSelf) active++; else if (available==null) available=item;
            }
            if (active>=binding.limit || ActiveCount>=ActiveLimit || ActiveParticleCount>16000) return false;
            if (available==null)
            {
                if (pool.Count>=PoolLimit) return false;
                var root=Instantiate(binding.prefab,transform); root.SetActive(false);
                available=new Instance {root=root,binding=binding,particles=root.GetComponentsInChildren<ParticleSystem>(true),originalRotation=root.transform.localRotation};
                pool.Add(available);
            }
            next[(int)cue]=Time.time+(cue==EffectCue.EnemyHit?.075f:cue==EffectCue.EnemySpawn?.12f:.025f);
            available.follow=follow; available.offset=follow!=null?point-follow.position:Vector3.zero;
            available.expires=Time.time+(seconds>0?seconds:binding.seconds);
            available.root.transform.SetPositionAndRotation(point,direction.HasValue && direction.Value.sqrMagnitude>.001f?Quaternion.LookRotation(direction.Value)*available.originalRotation:available.originalRotation);
            available.root.transform.localScale=Vector3.one*(binding.scale*scale);
            available.root.SetActive(true);
            foreach (var ps in available.particles) { ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear); ps.Clear(false); }
            foreach (var ps in available.particles) ps.Play(false);
            PlayedEffects++; return true;
        }
        void Release(Instance item)
        {
            foreach (var ps in item.particles) ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            item.root.SetActive(false); item.follow=null;
        }
        public void Clear(bool bossOnly=false)
        {
            foreach (var item in pool) if (item.root.activeSelf && (!bossOnly || item.binding.boss)) Release(item);
            if (!bossOnly) Array.Clear(next,0,next.Length);
        }
        public void ClearUltimate()
        {
            foreach (var item in pool) if (item.root.activeSelf && (item.binding.cue==EffectCue.DivineAura || item.binding.cue==EffectCue.Ascend)) Release(item);
        }
        void LateUpdate()
        {
            foreach (var item in pool)
            {
                if (!item.root.activeSelf) continue;
                if (Time.time>=item.expires || (item.follow!=null && !item.follow.gameObject.activeInHierarchy)) { Release(item); continue; }
                if (item.follow!=null) item.root.transform.position=item.follow.position+item.offset;
            }
        }
        void OnDisable() { Clear(); }
    }
}
