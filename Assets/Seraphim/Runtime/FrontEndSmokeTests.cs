using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        IEnumerator FrontEndChecks()
        {
            var audio=game.Audio;
            audio.FlushPreferences();
            var hadMusic=PlayerPrefs.HasKey(SanctuaryAudio.MusicKey); var hadEffects=PlayerPrefs.HasKey(SanctuaryAudio.EffectsKey);
            var musicPref=PlayerPrefs.GetFloat(SanctuaryAudio.MusicKey); var effectsPref=PlayerPrefs.GetFloat(SanctuaryAudio.EffectsKey);
            var originalMusic=audio.MusicVolume; var originalEffects=audio.EffectsVolume;
            try
            {
                if (!game.FrontEnd.IsMainMenu) game.FrontEnd.ShowMainMenu();
                var began=game.Player.transform.position;
                yield return new WaitForSeconds(3);
                Check(game.Attract.IsRunning && game.Attract.AutomatedCasts>=5,"Main menu automatically moves and casts all five powers");
                Check(Vector3.Distance(began,game.Player.transform.position)>.5f,"Showcase seraphim navigates autonomously");
                Check(game.Enemies.ActiveCount>=64 && game.Enemies.ActiveCount<=game.settings.menuEnemyLimit,"Showcase maintains a large bounded demon population");
                Check(game.Enemies.PopulationLimit==96 && game.settings.enemyLimit==24,"Showcase population is separate from campaign balance");
                Check(game.Vitals.PresentationMode && !game.Hud.HudCanvas.enabled && !game.Player.InputEnabled && !game.Skills.InputEnabled,"Menu battle protects the hero and disables gameplay input and HUD");
                Check(EventSystem.current.currentSelectedGameObject==game.FrontEnd.StartButton.gameObject,"Start is selected for keyboard and controller navigation");
                var rect=game.FrontEnd.StartButton.GetComponent<RectTransform>();
                Check(rect.anchorMin==new Vector2(0,1) && rect.anchoredPosition.x<100 && rect.anchoredPosition.y>-250,"Start button occupies the upper left corner");
                Check(!game.Player.MoveFromScreen(Camera.main.WorldToScreenPoint(new Vector3(6,0,-1))),"Main menu blocks world clicking");
                Check(audio.ActiveVoices<=20,"Showcase sound effects use a bounded voice pool");
                yield return Capture("21-main-menu.png");
                var deadline=Time.realtimeSinceStartup+12;
                while (!audio.MusicReady && Time.realtimeSinceStartup<deadline) yield return null;
                Check(audio.MusicReady && audio.MusicSource.loop && audio.MusicSource.isPlaying,"Background music loads and loops in the exported player");
                if (audio.MusicReady)
                {
                    var fallback=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-fallback-music") || !System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"..","Music","seraphim-theme.ogg"));
                    Check(fallback?!audio.LicensedMusicLoaded:audio.LicensedMusicLoaded,"Requested local or fallback music source is selected");
                    Check(Mathf.Abs(audio.MusicSource.clip.length-(audio.LicensedMusicLoaded?1909:32))<1,"Music duration matches the selected cycle or original fallback");
                    // The first request initializes Unity's output history buffer.
                    audio.SetMusicVolume(.5f,false); audio.MusicOutputRms(); yield return new WaitForSecondsRealtime(.25f);
                    var rms=audio.MusicOutputRms(); Debug.Log($"SERAPHIM_MUSIC_RMS: {rms}; source time={audio.MusicSource.time}; DSP={AudioSettings.dspTime}; listener={AudioListener.volume}; virtual={audio.MusicSource.isVirtual}");
                    Check(rms>.000001f,"Music channel produces an audible nonzero audio signal");
                }
                game.FrontEnd.OpenSettings(); yield return null;
                Check(game.FrontEnd.IsSettings && Time.timeScale==1 && game.Attract.IsRunning,"Menu settings keep the background battle running");
                game.FrontEnd.MusicSlider.value=.2f; game.FrontEnd.EffectsSlider.value=0;
                Check(Mathf.Abs(audio.MusicSource.volume-.04f)<.0001f && audio.EffectsPeakGain==0,"Effects mute independently while music retains its own volume");
                game.FrontEnd.MusicSlider.value=0; game.FrontEnd.EffectsSlider.value=.65f;
                audio.Play(SoundCue.Revelation);
                Check(audio.MusicSource.volume==0 && audio.EffectsPeakGain>0 && audio.ActiveVoices>0,"Music mute leaves sound effects active");
                game.FrontEnd.MusicSlider.value=.35f;
                yield return Capture("22-audio-settings.png");
                audio.SetMusicVolume(.23f); audio.SetEffectsVolume(.56f); audio.FlushPreferences();
                audio.SetMusicVolume(0,false); audio.SetEffectsVolume(0,false); audio.ReloadPreferences();
                Check(Mathf.Abs(audio.MusicVolume-.23f)<.001f && Mathf.Abs(audio.EffectsVolume-.56f)<.001f,"Independent audio preferences persist and reload");
                game.FrontEnd.CloseSettings(); game.FrontEnd.StartButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.35f);
                Check(!game.Attract.IsRunning && !game.Vitals.PresentationMode && game.FrontEnd.State==SeraphimFrontEnd.ScreenState.Playing,"Start transitions from showcase into the playable campaign");
                Check(game.Enemies.Score==0 && game.Enemies.KillCount==0 && game.Enemies.ActiveCount==0 && game.Drops.ActiveCount==0 && game.AttunedCount==0,"Starting clears demonstration kills, enemies, drops and waystones");
                var ready=true; for (var slot=0;slot<5;slot++) ready&=game.Skills.Remaining(slot)<=0;
                Check(ready && game.Vitals.Life==game.Vitals.MaxLife && game.Vitals.Spirit==game.Vitals.MaxSpirit,"Fresh campaign restores resources and all five ability cooldowns");
                Check(game.Enemies.PopulationLimit==24 && Camera.main.GetComponent<SanctuaryCamera>().ShowcaseZoom==0 && game.Hud.HudCanvas.enabled,"Campaign restores normal camera, HUD and population cap");
                Check(game.Player.InputEnabled && game.Skills.InputEnabled && game.Gamepads.InputEnabled && !EventSystem.current.sendNavigationEvents,"Gameplay resumes concurrent keyboard, mouse and controller input");
                yield return Capture("23-started-game.png");
                game.FrontEnd.OpenSettings(); var paused=Time.time;
                yield return new WaitForSecondsRealtime(.3f);
                Check(Time.timeScale==0 && Mathf.Abs(Time.time-paused)<.001f && !game.Player.InputEnabled,"In-game settings pause simulation and movement");
                Check(audio.MusicSource.isPlaying,"Music continues while in-game settings pause combat");
                Check(!game.Player.MoveFromScreen(Camera.main.WorldToScreenPoint(new Vector3(6,0,-1))),"Settings clicks cannot move the character");
                game.FrontEnd.CloseSettings(); yield return new WaitForSecondsRealtime(.3f);
                Check(Time.timeScale==1 && game.Player.InputEnabled && !game.FrontEnd.IsSettings,"Resume restores gameplay after settings");
                game.FrontEnd.ShowMainMenu(); yield return null;
                Check(game.Attract.IsRunning && game.Enemies.ActiveCount>0 && !game.Player.InputEnabled,"Returning to the main menu restarts the autonomous showcase");
                game.FrontEnd.StartGame(); yield return null;
            }
            finally
            {
                Time.timeScale=1; audio.FlushPreferences();
                if (hadMusic) PlayerPrefs.SetFloat(SanctuaryAudio.MusicKey,musicPref); else PlayerPrefs.DeleteKey(SanctuaryAudio.MusicKey);
                if (hadEffects) PlayerPrefs.SetFloat(SanctuaryAudio.EffectsKey,effectsPref); else PlayerPrefs.DeleteKey(SanctuaryAudio.EffectsKey);
                PlayerPrefs.Save(); audio.SetMusicVolume(originalMusic,false); audio.SetEffectsVolume(originalEffects,false);
                game.Waves.StopScenario(); game.Player.InputEnabled=false; game.Skills.InputEnabled=false; game.Gamepads.InputEnabled=false;
            }
        }
    }
}
