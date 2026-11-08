using UnityEngine;

namespace Seraphim
{
    public sealed class SeraphimSkillBar : MonoBehaviour
    {
        static readonly KeyCode[] Keys={KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6};
        SanctuaryGame game;
        public bool InputEnabled { get; set; }=true;
        public int SlotCount => Keys.Length;
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public bool IsAssigned(int slot) => slot==0 || slot==1;
        public string Name(int slot) => slot==0?"Radiance":slot==1?"Word of God":"Unassigned";
        public float Remaining(int slot) => slot==0?game.Ability.CooldownRemaining:slot==1?game.Ultimate.CooldownRemaining:0;
        public float Duration(int slot) => slot==0?game.settings.pulseCooldown:slot==1?game.settings.ultimateCooldown:1;
        public bool TryUse(int slot)
        {
            if (slot==0) return game.Ability.Cast();
            if (slot==1) return game.Ultimate.Cast();
            return false;
        }
        void Update()
        {
            if (!InputEnabled) return;
            for (var i=0;i<Keys.Length;i++) if (Input.GetKeyDown(Keys[i])) TryUse(i);
            if (Input.GetKeyDown(KeyCode.Keypad1)) TryUse(0);
            if (Input.GetKeyDown(KeyCode.Keypad2)) TryUse(1);
            if (Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(1) && !SanctuaryHud.BlocksScreenPoint(Input.mousePosition))) TryUse(0);
        }
    }
}
