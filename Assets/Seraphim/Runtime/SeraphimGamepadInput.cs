using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Seraphim
{
    // Poll controllers once before gameplay. Keyboard and mouse remain independent.
    [DefaultExecutionOrder(-100)]
    public sealed class SeraphimGamepadInput : MonoBehaviour
    {
        SanctuaryGame game;
        Gamepad active;
        public bool InputEnabled { get; set; }=true;
        public Vector2 Movement { get; private set; }
        public Vector2 AimStick { get; private set; }
        public int SkillMask { get; private set; }
        public bool ReplayPressed { get; private set; }
        public bool PreferController { get; private set; }
        public Gamepad ActivePad => active;
        public bool IsPlayStation => active is DualShockGamepad;
        public int ConnectedCount => Gamepad.all.Count;
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public void UseKeyboardMouse() { PreferController=false; }
        public void UseController() { PreferController=true; }

        public static Vector2 FilterStick(Vector2 raw)
        {
            const float deadzone=.18f;
            var magnitude=raw.magnitude;
            return magnitude<=deadzone?Vector2.zero:raw/magnitude*Mathf.Clamp01((magnitude-deadzone)/(1-deadzone));
        }
        static bool Pressed(Gamepad pad) => pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
            || pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
        static bool Steering(Gamepad pad) => FilterStick(pad.leftStick.ReadUnprocessedValue()).sqrMagnitude>.001f
            || FilterStick(pad.rightStick.ReadUnprocessedValue()).sqrMagnitude>.001f;
        public void Poll()
        {
            Movement=AimStick=Vector2.zero; SkillMask=0; ReplayPressed=false;
            if (active!=null && (!active.added || !active.enabled)) active=null;
            foreach (var pad in Gamepad.all)
            {
                if (!pad.enabled) continue;
                if (active==null) active=pad;
                if (!InputEnabled) continue;
                if (Pressed(pad)) { active=pad; break; }
                if (Steering(pad) && !Steering(active)) active=pad;
            }
            ReadPad(active);
        }
        // The same device-reading path is used for live and injected device events.
        public void ReadPad(Gamepad pad)
        {
            Movement=AimStick=Vector2.zero; SkillMask=0; ReplayPressed=false;
            active=pad!=null && pad.added && pad.enabled?pad:null;
            if (!InputEnabled || active==null) return;
            Movement=FilterStick(active.leftStick.ReadUnprocessedValue());
            AimStick=FilterStick(active.rightStick.ReadUnprocessedValue());
            // Semantic positions map Cross/A, Square/X, Circle/B, Triangle/Y.
            // Primary fire repeats while held; expensive powers require a new press.
            if (active.buttonSouth.isPressed) SkillMask|=1<<0;
            if (active.buttonWest.wasPressedThisFrame) SkillMask|=1<<1;
            if (active.buttonEast.wasPressedThisFrame) SkillMask|=1<<2;
            if (active.buttonNorth.wasPressedThisFrame) SkillMask|=1<<3;
            if (active.dpad.up.wasPressedThisFrame) SkillMask|=1<<4;
            ReplayPressed=active.startButton.wasPressedThisFrame;
            if (Movement.sqrMagnitude>.001f || AimStick.sqrMagnitude>.001f || SkillMask!=0 || ReplayPressed) PreferController=true;
        }
        public Vector3 AimPoint()
        {
            var origin=game.Player.transform.position;
            if (AimStick.sqrMagnitude<.001f)
            {
                var nearest=game.Enemies.Nearest(origin,24);
                var facing=Movement.sqrMagnitude>.001f?game.Player.KeyboardDirection(Movement):game.Player.transform.forward;
                return nearest!=null?nearest.transform.position:origin+facing.normalized*24;
            }
            var direction=game.Player.KeyboardDirection(AimStick).normalized;
            DemonEnemy target=null; var best=26f;
            foreach (var enemy in game.Enemies.Enemies)
            {
                if (!enemy.IsAlive) continue;
                var offset=enemy.transform.position-origin; offset.y=0;
                var distance=offset.magnitude;
                if (distance<best && Vector3.Angle(direction,offset)<18) { target=enemy; best=distance; }
            }
            return target!=null?target.transform.position:origin+direction*24;
        }
        public string BindingLabel(int slot)
        {
            if (slot==4) return "D-PAD UP";
            if (IsPlayStation) return slot==0?"CROSS":slot==1?"SQUARE":slot==2?"CIRCLE":slot==3?"TRIANGLE":"";
            return slot==0?"A":slot==1?"X":slot==2?"B":slot==3?"Y":"";
        }
        public string ControlHint => IsPlayStation?"CROSS  RADIANCE    SQUARE  SPEARS    CIRCLE  LIGHTNING    TRIANGLE  FIRE    D-PAD UP  ULTIMATE"
            :"A  RADIANCE    X  SPEARS    B  LIGHTNING    Y  FIRE    D-PAD UP  ULTIMATE";
        void Update() { Poll(); }
        void OnDisable() { Movement=AimStick=Vector2.zero; SkillMask=0; ReplayPressed=false; }
    }
}
