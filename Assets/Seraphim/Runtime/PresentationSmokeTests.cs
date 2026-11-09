using System;
using System.Collections;
using UnityEngine;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        IEnumerator PresentationChecks()
        {
            var presentation=game.Presentation;
            if (presentation.Bank==null)
            {
                Check(game.Audio.DownloadedCueCount==0,"Clean-checkout presentation uses original sound and particle fallbacks");
                yield break;
            }
            Check(game.Audio.DownloadedCueCount==Enum.GetValues(typeof(SoundCue)).Length,"Every game sound cue maps to a downloaded sample");
            var nonzero=true;
            foreach (var binding in presentation.Bank.sounds) foreach (var clip in binding.variants)
            {
                var samples=new float[clip.samples*clip.channels];
                var read=clip.GetData(samples,0);var peak=0f;foreach (var value in samples) peak=Mathf.Max(peak,Mathf.Abs(value));
                nonzero &= read && peak>.0001f;
            }
            Check(nonzero,"Downloaded sound files decode and contain audible samples");
            var originalVolume=game.Audio.EffectsVolume;
            game.Audio.SetEffectsVolume(.7f,false);game.Audio.StopEffects();game.Audio.Play(SoundCue.Radiance);
            yield return new WaitForSeconds(.1f);
            Check(game.Audio.LastEffectClip==presentation.Bank.Sound(SoundCue.Radiance).variants[0] && game.Audio.ActiveVoices>0,"Radiance plays the imported holy-magic recording");
            game.Audio.SetEffectsVolume(0,false);
            Check(game.Audio.EffectsPeakGain==0,"Downloaded samples obey the existing independent effects mute");
            game.Audio.SetEffectsVolume(originalVolume,false);game.Audio.StopEffects();
            Check(presentation.Bank.effects.Length==Enum.GetValues(typeof(EffectCue)).Length,"All presentation cues have adapted downloaded effect prefabs");
            var safe=true;
            foreach (var binding in presentation.Bank.effects)
            {
                safe &= binding.prefab.GetComponentsInChildren<MonoBehaviour>(true).Length==0;
                foreach (var renderer in binding.prefab.GetComponentsInChildren<Renderer>(true)) foreach (var material in renderer.sharedMaterials)
                    safe &= material!=null && material.shader.isSupported && material.shader.name=="Seraphim/Imported Particle";
            }
            Check(safe,"Imported effects use supported URP materials without demo scripts");
            ResetPressureFixture(new Vector3(-5,0,-24));
            game.Ability.ResetForRun();game.Powers.ResetForRun();game.Ultimate.ResetForRun();
            for (var i=0;i<4;i++) game.Enemies.SpawnAt(new Vector3(-9+i*3,0,-18),DemonKind.ObsidianGuard,true,8);
            var played=presentation.PlayedEffects;
            Check(game.Skills.TryUseDirected(0,new Vector3(-5,0,-18)) && game.Skills.TryUseDirected(1,new Vector3(-5,0,-18))
                && game.Skills.TryUseDirected(2,new Vector3(-5,0,-18)),"Radiance, spears and lightning still cast with downloaded effects");
            yield return new WaitForSeconds(.15f);game.Skills.TryUseDirected(3,new Vector3(-5,0,-18));
            yield return new WaitForSeconds(.15f);yield return Capture("35-divine-effects.png");
            Check(presentation.PlayedEffects>played && presentation.ActiveCount>0,"Gameplay triggers the downloaded particle pool");
            game.Vitals.RestoreSpirit(100);
            Check(game.Skills.TryUseDirected(4,game.Player.transform.position),"Word of God casts with the downloaded presentation");
            yield return new WaitForSeconds(1.3f);yield return Capture("36-divine-revelation.png");
            Check(game.Ultimate.IsActive && presentation.ActiveCount>0,"The imported ascension aura follows the fifteen-second ultimate");
            game.Ultimate.Cancel();presentation.Clear();
            Check(presentation.ActiveCount==0 && presentation.ActiveParticleCount==0,"Cancel and encounter cleanup remove every imported particle");
            for (var i=0;i<200;i++) presentation.Play((EffectCue)(i%Enum.GetValues(typeof(EffectCue)).Length),game.Player.transform.position);
            yield return null;
            Check(presentation.ActiveCount<=SanctuaryPresentation.ActiveLimit && presentation.PoolCount<=SanctuaryPresentation.PoolLimit,"Crowded effects stay inside fixed active and retained pool limits");
            presentation.Clear();game.Enemies.DespawnAll();game.Drops.Clear();
            game.Ability.ResetForRun();game.Powers.ResetForRun();game.Ultimate.ResetForRun();game.Audio.StopEffects();
            game.Player.Agent.Warp(game.settings.spawnPoint);game.Vitals.RestoreLife(100);game.Vitals.RestoreSpirit(100);
        }
    }
}
