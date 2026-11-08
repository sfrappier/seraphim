using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock.LowLevel;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        IEnumerator PadState(Gamepad pad,GamepadState state)
        {
            InputSystem.QueueStateEvent(pad,state);
            yield return null; // Let Unity's real dynamic input update consume the event.
            game.Gamepads.ReadPad(pad);
        }
        IEnumerator SteerFor(Vector2 keys,Vector2 stick,int frames=5)
        {
            for (var i=0;i<frames;i++) { game.Player.ApplyMovementInputs(keys,stick,.1f); yield return null; }
            game.Player.StopKeyboardMovement();
        }
        IEnumerator SonyState(Gamepad pad,byte buttons=8,byte leftY=128,byte rightX=128,byte options=0)
        {
            InputSystem.QueueStateEvent(pad,new DualSenseHIDInputReport {leftStickX=128,leftStickY=leftY,rightStickX=rightX,rightStickY=128,buttons0=buttons,buttons1=options});
            yield return null; game.Gamepads.ReadPad(pad);
        }
        IEnumerator ControllerChecks()
        {
            game.Waves.StopScenario(); game.Enemies.DespawnAll(); game.Drops.Clear();
            game.Player.InputEnabled=false; game.Skills.InputEnabled=false;
            game.Player.StopKeyboardMovement(); game.Player.Agent.ResetPath(); game.Player.Agent.Warp(new Vector3(-5,0,-8));
            var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            game.Gamepads.enabled=false; game.Gamepads.InputEnabled=true;
            var pad=InputSystem.AddDevice<Gamepad>();
            InputSystem.EnableDevice(pad);
            Gamepad sony=null;
            try
            {
                Check(game.Skills.Name(0)=="Radiance" && game.Skills.Name(1)=="Lance of Dawn" && game.Skills.Name(2)=="Chain of Heaven"
                    && game.Skills.Name(3)=="Seraphic Inferno" && game.Skills.Name(4)=="Word of God","Keyboard and HUD order place the ultimate in slot five");
                Check(game.Skills.CostLabel(4)=="ULTIMATE" && game.Skills.Duration(4)==30 && !game.Skills.IsAssigned(5),"Reordered ultimate retains its cooldown and the spare sixth slot");
                Check(SeraphimGamepadInput.FilterStick(new Vector2(.1f,.08f))==Vector2.zero,"Controller deadzone rejects resting-stick drift");
                Check(Mathf.Abs(SeraphimGamepadInput.FilterStick(Vector2.one).magnitude-1)<.001f,"Controller diagonals never exceed full movement speed");
                yield return PadState(pad,new GamepadState {leftStick=Vector2.up});
                var start=game.Player.transform.position;
                yield return SteerFor(Vector2.zero,game.Gamepads.Movement);
                var fullDistance=Vector3.Distance(start,game.Player.transform.position);
                Check(fullDistance>2.5f,"Injected left-stick input physically moves the player");
                var forward=game.Player.KeyboardDirection(Vector2.up);
                Check(Vector3.Dot((game.Player.transform.position-start).normalized,forward)>.95f,"Left-stick up moves toward the top of the overhead view");
                game.Player.Agent.Warp(new Vector3(-5,0,-8));
                yield return PadState(pad,new GamepadState {leftStick=new Vector2(0,.59f)});
                start=game.Player.transform.position;
                yield return SteerFor(Vector2.zero,game.Gamepads.Movement);
                var halfDistance=Vector3.Distance(start,game.Player.transform.position);
                Check(halfDistance>fullDistance*.4f && halfDistance<fullDistance*.6f,"Partial stick deflection gives proportional walking speed");
                Check(game.Player.MoveTo(new Vector3(-1,0,-4)),"Mouse-style navigation is available alongside controller input");
                yield return PadState(pad,new GamepadState {leftStick=Vector2.right});
                game.Player.ApplyMovementInputs(Vector2.zero,game.Gamepads.Movement,.1f); yield return null;
                Check(!game.Player.HasDestination && game.Player.IsKeyboardMoving,"Left-stick steering takes over an existing click path");
                yield return PadState(pad,new GamepadState());
                game.Player.ApplyMovementInputs(Vector2.zero,game.Gamepads.Movement,.1f);
                var released=game.Player.transform.position; yield return new WaitForSeconds(.2f);
                Check(Vector3.Distance(released,game.Player.transform.position)<.12f && !game.Player.IsKeyboardMoving,"Centering the stick stops movement without reviving an old click path");
                game.Player.Agent.Warp(new Vector3(-5,0,-8)); start=game.Player.transform.position;
                yield return SteerFor(Vector2.left,Vector2.right);
                Check(Vector3.Dot((game.Player.transform.position-start).normalized,game.Player.KeyboardDirection(Vector2.left))>.95f
                    && Vector3.Distance(start,game.Player.transform.position)<fullDistance*1.1f,"Simultaneous WASD and stick input keeps keyboard priority without a speed boost");
                game.Player.Agent.Warp(new Vector3(0,0,-1));
                var cameraForward=game.Player.KeyboardDirection(Vector2.up); var cameraRight=game.Player.KeyboardDirection(Vector2.right);
                var towardAltar=new Vector2(Vector3.Dot(Vector3.forward,cameraRight),Vector3.Dot(Vector3.forward,cameraForward));
                yield return SteerFor(Vector2.zero,towardAltar,10);
                Check(game.Player.transform.position.z<1.1f,"Controller steering respects the altar's navigation boundary");
                game.Player.Agent.Warp(new Vector3(-5,0,-8));
                var right=game.Player.KeyboardDirection(Vector2.right);
                var aimed=game.Enemies.SpawnAt(game.Player.transform.position+right*8,DemonKind.CinderImp,true);
                var near=game.Enemies.SpawnAt(game.Player.transform.position-right*3,DemonKind.CinderImp,true);
                yield return PadState(pad,new GamepadState {rightStick=Vector2.right});
                Check(aimed!=null && Vector3.Distance(game.Gamepads.AimPoint(),aimed.transform.position)<.5f,"Right stick aims at demons independently of the mouse cursor");
                yield return PadState(pad,new GamepadState());
                Check(near!=null && Vector3.Distance(game.Gamepads.AimPoint(),near.transform.position)<.5f,"Centered aim stick assists toward the nearest demon");
                game.Enemies.DespawnAll();

                game.Vitals.RestoreSpirit(100);
                while (game.Ability.CooldownRemaining>0) yield return null;
                var casts=game.Ability.CastCount;
                yield return PadState(pad,new GamepadState().WithButton(GamepadButton.South));
                game.Skills.ProcessControllerSkills();
                Check(game.Gamepads.SkillMask==1 && game.Ability.CastCount==casts+1,"Cross or Xbox A casts primary Radiance");
                Check(!game.Skills.TryUse(0),"Mouse, keyboard, and controller share the same Radiance cooldown");
                yield return new WaitForSeconds(.65f); game.Gamepads.ReadPad(pad); game.Skills.ProcessControllerSkills();
                Check(game.Ability.CastCount==casts+2,"Holding the primary controller button repeats Radiance after cooldown");
                yield return PadState(pad,new GamepadState());
                Check(game.Gamepads.SkillMask==0,"Releasing the primary button ends repeated attacks");
                var lance=game.Enemies.SpawnAt(game.Player.transform.position+Vector3.forward*6,DemonKind.CinderImp,true);
                casts=game.Powers.LanceCasts; game.Vitals.RestoreSpirit(100);
                yield return PadState(pad,new GamepadState().WithButton(GamepadButton.West)); game.Skills.ProcessControllerSkills();
                Check(game.Gamepads.SkillMask==(1<<1) && game.Powers.LanceCasts==casts+1,"Square or Xbox X fires the spear volley from slot two");
                yield return new WaitForSeconds(.5f);
                Check(lance!=null && !lance.IsAlive,"Controller-assisted spears hit the selected demon");
                game.Gamepads.ReadPad(pad);
                Check(game.Gamepads.SkillMask==0,"Holding a secondary power does not generate repeat presses");
                yield return PadState(pad,new GamepadState()); game.Enemies.DespawnAll();
                var chain=game.Enemies.SpawnAt(game.Player.transform.position+Vector3.forward*4,DemonKind.ObsidianGuard,true);
                casts=game.Powers.ChainCasts; game.Vitals.RestoreSpirit(100);
                yield return PadState(pad,new GamepadState().WithButton(GamepadButton.East)); game.Skills.ProcessControllerSkills();
                Check(game.Gamepads.SkillMask==(1<<2) && game.Powers.ChainCasts==casts+1 && chain!=null && chain.Health<chain.MaxHealth,"Circle or Xbox B casts lightning from slot three and damages a demon");
                yield return PadState(pad,new GamepadState());
                casts=game.Powers.InfernoCasts; game.Vitals.RestoreSpirit(100);
                yield return PadState(pad,new GamepadState().WithButton(GamepadButton.North)); game.Skills.ProcessControllerSkills();
                Check(game.Gamepads.SkillMask==(1<<3) && game.Powers.InfernoCasts==casts+1,"Triangle or Xbox Y casts fire from slot four");
                yield return PadState(pad,new GamepadState());
                while (game.Ultimate.CooldownRemaining>0) yield return null;
                casts=game.Ultimate.CastCount;
                yield return PadState(pad,new GamepadState().WithButton(GamepadButton.DpadUp)); game.Skills.ProcessControllerSkills();
                Check(game.Gamepads.SkillMask==(1<<4) && game.Ultimate.CastCount==casts+1 && game.Ultimate.IsActive,"D-pad Up activates Word of God in slot five");
                Check(!game.Skills.TryUse(4),"Ultimate cooldown also prevents recasting across input devices");
                yield return new WaitForSeconds(.25f); game.Gamepads.ReadPad(pad);
                Check(game.Gamepads.SkillMask==0,"Held D-pad Up cannot retrigger the ultimate");
                game.Ultimate.Cancel(); game.Enemies.DespawnAll();
                Check(game.Gamepads.BindingLabel(0)=="A" && game.Gamepads.BindingLabel(1)=="X" && game.Gamepads.BindingLabel(4)=="D-PAD UP","Xbox controller prompts follow physical button positions");
                sony=(Gamepad)InputSystem.AddDevice("DualSenseGamepadHID"); InputSystem.EnableDevice(sony); game.Gamepads.ReadPad(sony);
                Check(game.Gamepads.IsPlayStation && game.Gamepads.BindingLabel(0)=="CROSS" && game.Gamepads.BindingLabel(1)=="SQUARE"
                    && game.Gamepads.BindingLabel(2)=="CIRCLE" && game.Gamepads.BindingLabel(3)=="TRIANGLE","DualSense layout selects the requested PlayStation button prompts");
                yield return SonyState(sony);
                Check(game.Gamepads.Movement==Vector2.zero && game.Gamepads.SkillMask==0,"Centered native DualSense reports produce no drift or phantom attacks");
                yield return SonyState(sony,8,0,255);
                Check(game.Gamepads.Movement.y>.9f && Mathf.Abs(game.Gamepads.Movement.x)<.02f && game.Gamepads.AimStick.x>.9f,"Native DualSense axes normalize stick-up and independent right-stick aiming correctly");
                var reports=new byte[]{0x28,0x18,0x48,0x88,0x00};
                var names=new[]{"Cross","Square","Circle","Triangle","D-pad Up"};
                for (var i=0;i<reports.Length;i++)
                {
                    yield return SonyState(sony); yield return SonyState(sony,reports[i]);
                    Check(game.Gamepads.SkillMask==(1<<i),"Native DualSense "+names[i]+" report selects the correct skill");
                }
                yield return SonyState(sony); yield return SonyState(sony,8,128,128,0x20);
                Check(game.Gamepads.ReplayPressed,"Native DualSense Options report exposes the replay command");
                game.Gamepads.UseController(); yield return new WaitForSeconds(.15f);
                yield return Capture("17-controller-hud.png");
                InputSystem.RemoveDevice(sony); sony=null;
                InputSystem.RemoveDevice(pad); game.Gamepads.ReadPad(pad);
                Check(game.Gamepads.ActivePad==null && game.Gamepads.Movement==Vector2.zero && game.Gamepads.SkillMask==0,"Disconnecting the active pad clears movement and held attacks");
                pad=InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(pad);
                yield return PadState(pad,new GamepadState {leftStick=Vector2.up});
                Check(game.Gamepads.Movement.y>.9f,"A newly connected pad can resume control without restarting the game");
                game.Gamepads.UseKeyboardMouse();
                Check(game.Player.MoveTo(new Vector3(-2,0,-6)) && !game.Gamepads.PreferController,"Keyboard and mouse remain available after controller use and reconnection");
                game.Player.Agent.ResetPath(); game.Player.StopKeyboardMovement();
            }
            finally
            {
                if (sony!=null && sony.added) InputSystem.RemoveDevice(sony);
                if (pad!=null && pad.added) InputSystem.RemoveDevice(pad);
                game.Gamepads.ReadPad(null); game.Gamepads.InputEnabled=false; game.Gamepads.enabled=true; game.Gamepads.UseKeyboardMouse();
                InputSystem.settings.backgroundBehavior=background;
            }
        }
    }
}
