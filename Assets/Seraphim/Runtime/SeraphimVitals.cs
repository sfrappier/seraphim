using UnityEngine;

namespace Seraphim
{
    public sealed class SeraphimVitals : MonoBehaviour
    {
        SanctuarySettings settings;
        float protectedUntil;
        SanctuaryAudio audio;
        SanctuaryPresentation presentation;
        public bool PresentationMode { get; set; }
        public float Life { get; private set; }
        public float Spirit { get; private set; }
        public float MaxLife => settings.maxLife;
        public float MaxSpirit => settings.maxSpirit;
        public float LifeFraction => Life/MaxLife;
        public float SpiritFraction => Spirit/MaxSpirit;

        public void Initialize(SanctuarySettings config,SanctuaryAudio sound=null,SanctuaryPresentation visual=null)
        {
            settings=config;
            audio=sound;
            presentation=visual;
            Life=MaxLife;
            Spirit=MaxSpirit;
        }

        public bool SpendSpirit(float amount)
        {
            if (amount<0 || Spirit<amount) return false;
            if (PresentationMode) return true;
            Spirit-=amount;
            return true;
        }
        public void RestoreSpirit(float amount) { Spirit=Mathf.Clamp(Spirit+Mathf.Max(0,amount),0,MaxSpirit); }
        public void RestoreLife(float amount) { Life=Mathf.Clamp(Life+Mathf.Max(0,amount),0,MaxLife); }
        public void TakeDamage(float amount)
        {
            if (PresentationMode || Time.time<protectedUntil) return;
            var before=Life; Life=Mathf.Clamp(Life-Mathf.Max(0,amount),0,MaxLife);
            if (Life<before) {audio?.Play(SoundCue.Hurt,transform.position);presentation?.Play(EffectCue.Hurt,transform.position,transform);}
        }
        public void Revive() { Life=MaxLife; Spirit=MaxSpirit; protectedUntil=Time.time+4; }
        void Update() { if (settings!=null) RestoreSpirit(settings.spiritRegeneration*Time.deltaTime); }
    }
}
