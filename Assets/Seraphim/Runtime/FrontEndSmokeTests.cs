using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        [Serializable] sealed class ShowcaseReport
        {
            public float seconds,distance,averageSpeed,minimumFiveSecondSpeed,longestSlowStretch,width,depth;
            public int completedRuns,navigationRecoveries,casts,maximumEnemies;
        }
        ShowcaseReport showcaseReport;

        IEnumerator ShowcaseMovementChecks(float seconds)
        {
            var metrics=new ShowcaseReport { minimumFiveSecondSpeed=float.MaxValue };
            var began=Time.time; var previous=game.Player.transform.position;
            var minimum=previous; var maximum=previous;
            var windowAt=began; var windowDistance=0f; var slowStretch=0f;
            var onNavigation=true; var bounded=true; var firstCapture=false;
            while (Time.time-began<seconds)
            {
                yield return null;
                var position=game.Player.transform.position;
                var distance=Vector3.Distance(previous,position); previous=position;
                metrics.distance+=distance; windowDistance+=distance;
                minimum=Vector3.Min(minimum,position); maximum=Vector3.Max(maximum,position);
                slowStretch=distance<2*Time.deltaTime?slowStretch+Time.deltaTime:0;
                metrics.longestSlowStretch=Mathf.Max(metrics.longestSlowStretch,slowStretch);
                onNavigation&=UnityEngine.AI.NavMesh.SamplePosition(position,out _,.2f,UnityEngine.AI.NavMesh.AllAreas);
                metrics.maximumEnemies=Mathf.Max(metrics.maximumEnemies,game.Enemies.ActiveCount);
                bounded&=game.Enemies.ActiveCount<=game.settings.menuEnemyLimit && game.Audio.ActiveVoices<=20;
                if (Time.time-windowAt>=5)
                {
                    metrics.minimumFiveSecondSpeed=Mathf.Min(metrics.minimumFiveSecondSpeed,windowDistance/(Time.time-windowAt));
                    windowAt=Time.time; windowDistance=0;
                }
                if (!firstCapture && Time.time-began>=seconds*.5f)
                {
                    firstCapture=true; yield return Capture("24-menu-running.png");
                }
            }
            metrics.seconds=Time.time-began; metrics.averageSpeed=metrics.distance/metrics.seconds;
            metrics.width=maximum.x-minimum.x; metrics.depth=maximum.z-minimum.z;
            metrics.completedRuns=game.Attract.CompletedRuns; metrics.navigationRecoveries=game.Attract.NavigationRecoveries;
            metrics.casts=game.Attract.AutomatedCasts; showcaseReport=metrics;
            Check(metrics.averageSpeed>=game.settings.menuMoveSpeed*.8f && metrics.minimumFiveSecondSpeed>=game.settings.menuMoveSpeed*.7f,
                "Menu sustains actual running speed across successive five-second windows");
            Check(metrics.longestSlowStretch<1,"Menu hero never settles into a stationary or creeping attack loop");
            Check(metrics.completedRuns>=3 && metrics.width>15 && metrics.depth>25,"Menu hero completes distant runs across the cathedral");
            Check(onNavigation,"Extended menu movement stays on cathedral navigation");
            Check(bounded,"Extended showcase preserves enemy and audio pool limits");
            var allPowers=true; for (var slot=0;slot<5;slot++) allPowers&=game.Attract.CastsFor(slot)>0;
            Check(allPowers,"All five powers cast while the menu hero runs");
            if (seconds>=60) Check(game.Attract.CastsFor(4)>=3,"Menu running continues through three Word of God cycles");
            Debug.Log("SERAPHIM_SHOWCASE: "+JsonUtility.ToJson(metrics));
            yield return Capture("25-menu-running-later.png");
        }

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
                var extended=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-front-end-only");
                yield return ShowcaseMovementChecks(extended?90:12);
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
                var settingsPosition=game.Player.transform.position;
                yield return new WaitForSeconds(.6f);
                Check(Vector3.Distance(settingsPosition,game.Player.transform.position)>3,"Menu hero keeps running behind audio settings");
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
                Check(Mathf.Approximately(game.Player.Agent.speed,game.settings.moveSpeed)
                    && game.Player.Agent.avoidancePriority==50
                    && game.Player.Agent.obstacleAvoidanceType==UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance,
                    "Starting restores campaign movement speed and crowd avoidance");
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
