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
        public string Name(int slot) => slot==0?"Radiance":slot==1?"Lance of Dawn":slot==2?"Chain of Heaven":slot==3?"Seraphic Inferno":slot==4?"Word of God":"Unassigned";
        public float Remaining(int slot) => slot==0?game.Ability.CooldownRemaining:slot==4?game.Ultimate.CooldownRemaining:slot>=1 && slot<=3?game.Powers.Remaining(slot-1):0;
        public float Duration(int slot) => slot==0?game.settings.pulseCooldown:slot==4?game.settings.ultimateCooldown:slot>=1 && slot<=3?game.Powers.Cooldown(slot-1):1;
        public string CostLabel(int slot) => slot==4?"ULTIMATE":IsAssigned(slot)?Mathf.CeilToInt(slot==0?game.settings.radianceSpiritCost:game.Powers.Cost(slot-1))+" SPIRIT":"—";
        public bool TryUse(int slot)
        {
            game.Gamepads?.UseKeyboardMouse();
            return Cast(slot,game.Powers.AimPoint());
        }
        public bool TryUseController(int slot)
        {
            game.Gamepads.UseController();
            return Cast(slot,game.Gamepads.AimPoint());
        }
        bool Cast(int slot,Vector3 aim)
        {
            if (slot==0) return game.Ability.Cast();
            if (slot==4) return game.Ultimate.Cast();
            if (slot>=1 && slot<=3) return game.Powers.Cast(slot-1,aim);
            return false;
        }
        void Update()
        {
            if (!InputEnabled) return;
            for (var i=0;i<Keys.Length;i++) if (Input.GetKeyDown(Keys[i])) TryUse(i);
            for (var i=0;i<PadKeys.Length;i++) if (Input.GetKeyDown(PadKeys[i])) TryUse(i);
            if (Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(1) && !SanctuaryHud.BlocksScreenPoint(Input.mousePosition))) TryUse(0);
            ProcessControllerSkills();
        }
        public void ProcessControllerSkills()
        {
            if (game.Gamepads==null) return;
            for (var slot=0;slot<5;slot++) if ((game.Gamepads.SkillMask & (1<<slot))!=0) TryUseController(slot);
        }
    }
}
