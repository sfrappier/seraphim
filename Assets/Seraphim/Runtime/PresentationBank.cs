using System;
using UnityEngine;

namespace Seraphim
{
    public enum EffectCue { Radiance,SpearCast,SpearHit,LightningCast,LightningHit,Inferno,Ascend,Revelation,DivineAura,Pulse,Life,Spirit,EnemySpawn,EnemyHit,Banish,Hurt,EnemyRift,BossMeteor,BossFire,BossNova,BossSlash,BossTransform,BossCast,Ambient }
    [Serializable] public sealed class SoundBinding
    {
        public SoundCue cue;
        public AudioClip[] variants;
        public float gain=1;
    }
    [Serializable] public sealed class EffectBinding
    {
        public EffectCue cue;
        public GameObject prefab;
        public float scale=1;
        public float seconds=1;
        public int limit=3;
        public bool boss;
    }
    // Kept under ignored AssetStore/Resources when licensed packs are available.
    // A public clean checkout retains the existing original audio/VFX fallback.
    public sealed class PresentationBank : ScriptableObject
    {
        public SoundBinding[] sounds;
        public EffectBinding[] effects;
        public SoundBinding Sound(SoundCue cue)
        {
            if (sounds!=null) foreach (var entry in sounds) if (entry.cue==cue) return entry;
            return null;
        }
        public EffectBinding Effect(EffectCue cue)
        {
            if (effects!=null) foreach (var entry in effects) if (entry.cue==cue) return entry;
            return null;
        }
    }
}
