using UnityEngine;

namespace Seraphim
{
    public sealed class SeraphimSkillBar : MonoBehaviour
    {
        static readonly KeyCode[] Keys={KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6};
        static readonly KeyCode[] PadKeys={KeyCode.Keypad1,KeyCode.Keypad2,KeyCode.Keypad3,KeyCode.Keypad4,KeyCode.Keypad5,KeyCode.Keypad6};
        SanctuaryGame game;
        public bool InputEnabled { get; set; }=true;
        public int SlotCount => Keys.Length;
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public bool IsAssigned(int slot) => slot>=0 && slot<5;
        public string Name(int slot) => slot==0?"Radiance":slot==1?"Word of God":slot==2?"Lance of Dawn":slot==3?"Chain of Heaven":slot==4?"Seraphic Inferno":"Unassigned";
        public float Remaining(int slot) => slot==0?game.Ability.CooldownRemaining:slot==1?game.Ultimate.CooldownRemaining:slot<5?game.Powers.Remaining(slot-2):0;
        public float Duration(int slot) => slot==0?game.settings.pulseCooldown:slot==1?game.settings.ultimateCooldown:slot<5?game.Powers.Cooldown(slot-2):1;
        public string CostLabel(int slot) => slot==1?"ULTIMATE":IsAssigned(slot)?Mathf.CeilToInt(slot==0?game.settings.radianceSpiritCost:game.Powers.Cost(slot-2))+" SPIRIT":"—";
        public bool TryUse(int slot)
        {
            if (slot==0) return game.Ability.Cast();
            if (slot==1) return game.Ultimate.Cast();
            if (slot>=2 && slot<5) return game.Powers.Cast(slot-2,game.Powers.AimPoint());
            return false;
        }
        void Update()
        {
            if (!InputEnabled) return;
            for (var i=0;i<Keys.Length;i++) if (Input.GetKeyDown(Keys[i])) TryUse(i);
            for (var i=0;i<PadKeys.Length;i++) if (Input.GetKeyDown(PadKeys[i])) TryUse(i);
            if (Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(1) && !SanctuaryHud.BlocksScreenPoint(Input.mousePosition))) TryUse(0);
        }
    }
}
