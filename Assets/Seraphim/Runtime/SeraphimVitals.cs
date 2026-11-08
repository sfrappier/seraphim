using UnityEngine;

namespace Seraphim
{
    public sealed class SeraphimVitals : MonoBehaviour
    {
        SanctuarySettings settings;
        float protectedUntil;
        public float Life { get; private set; }
        public float Spirit { get; private set; }
        public float MaxLife => settings.maxLife;
        public float MaxSpirit => settings.maxSpirit;
        public float LifeFraction => Life/MaxLife;
        public float SpiritFraction => Spirit/MaxSpirit;

        public void Initialize(SanctuarySettings config)
        {
            settings=config;
            Life=MaxLife;
            Spirit=MaxSpirit;
        }

        public bool SpendSpirit(float amount)
        {
            if (amount<0 || Spirit<amount) return false;
            Spirit-=amount;
            return true;
        }
        public void RestoreSpirit(float amount) { Spirit=Mathf.Clamp(Spirit+Mathf.Max(0,amount),0,MaxSpirit); }
        public void RestoreLife(float amount) { Life=Mathf.Clamp(Life+Mathf.Max(0,amount),0,MaxLife); }
        public void TakeDamage(float amount) { if (Time.time>=protectedUntil) Life=Mathf.Clamp(Life-Mathf.Max(0,amount),0,MaxLife); }
        public void Revive() { Life=MaxLife; Spirit=MaxSpirit; protectedUntil=Time.time+4; }
        void Update() { if (settings!=null) RestoreSpirit(settings.spiritRegeneration*Time.deltaTime); }
    }
}
