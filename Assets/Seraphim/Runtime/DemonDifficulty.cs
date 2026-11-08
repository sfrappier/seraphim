using UnityEngine;

namespace Seraphim
{
    public readonly struct DemonDifficulty
    {
        public readonly float health,speed,damage,size,knockback,meleeInterval,specialInterval;
        public DemonDifficulty(SanctuarySettings settings,DemonKind kind,int wave)
        {
            var level=Mathf.Clamp(wave-1,0,settings.waveCount-1);
            health=(1+level*settings.enemyHealthGrowth)*(kind==DemonKind.ObsidianGuard?1+Mathf.Max(0,level-3)*.04f:1);
            speed=1+level*settings.enemySpeedGrowth;
            damage=1+level*settings.enemyDamageGrowth;
            size=settings.enemyVisualScale*(1+level*settings.enemySizeGrowth);
            knockback=Mathf.Max(.45f,1-level*.06f);
            meleeInterval=Mathf.Max(1,1.5f-level*.035f);
            specialInterval=Mathf.Max(4.2f,8-level*.38f);
        }
    }
}
