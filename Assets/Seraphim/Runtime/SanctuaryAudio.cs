using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Seraphim
{
    public sealed class SanctuaryAudio : MonoBehaviour
    {
        public const string MusicKey="Seraphim.Audio.Music";
        public const string EffectsKey="Seraphim.Audio.Effects";
        const int VoiceCount=20;
        SanctuaryGame game;
        AudioSource music;
        readonly AudioSource[] voices=new AudioSource[VoiceCount];
        readonly float[] gains=new float[VoiceCount];
        AudioClip[] clips;
        bool[] owned;
        SoundBinding[] bindings;
        int[] variations;
        float[] lastPlayed;
        float nextStep,saveAt;
        bool dirty;
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        public AudioSource MusicSource => music;
        public bool MusicReady => music!=null && music.clip!=null;
        public bool LicensedMusicLoaded { get; private set; }
        public string MusicCredit => LicensedMusicLoaded?"Track Realm · Repeat Cycle 3":"Original sanctuary choir and organ";
        public int PlayedEffects { get; private set; }
        public int DownloadedCueCount {get;private set;}
        public AudioClip LastEffectClip {get;private set;}
        public bool HasDownloadedCue(SoundCue cue) => bindings!=null && bindings[(int)cue]!=null;
        public int ActiveVoices { get { var count=0; foreach (var source in voices) if (source!=null && source.isPlaying) count++; return count; } }
        public void Initialize(SanctuaryGame owner)
        {
            game=owner;
            MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey,.4f)); EffectsVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey,.7f));
            music=new GameObject("Music channel").AddComponent<AudioSource>(); music.transform.SetParent(transform,false);
            music.playOnAwake=false; music.loop=true; music.spatialBlend=0; music.priority=0; music.volume=Gain(MusicVolume);
            clips=new AudioClip[Enum.GetValues(typeof(SoundCue)).Length]; lastPlayed=new float[clips.Length];
            owned=new bool[clips.Length];bindings=new SoundBinding[clips.Length];variations=new int[clips.Length];
            for (var i=0;i<clips.Length;i++)
            {
                var binding=game.Presentation.Bank!=null?game.Presentation.Bank.Sound((SoundCue)i):null;
                if (binding!=null && binding.variants!=null && binding.variants.Length>0 && binding.variants[0]!=null)
                {bindings[i]=binding;clips[i]=binding.variants[0];DownloadedCueCount++;}
                else {clips[i]=SanctuarySound.Create((SoundCue)i);owned[i]=true;}
                lastPlayed[i]=-100;
            }
            for (var i=0;i<voices.Length;i++)
            {
                var source=new GameObject("Effects voice "+i).AddComponent<AudioSource>(); source.transform.SetParent(transform,false);
                source.playOnAwake=false; source.spatialBlend=0; source.priority=80; voices[i]=source;
            }
            StartCoroutine(LoadMusic());
        }
        IEnumerator LoadMusic()
        {
            var file=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Music","seraphim-theme.ogg"));
            if (Application.isEditor) file=Path.GetFullPath(Path.Combine(Application.dataPath,"..","LocalAudio","seraphim-theme.ogg"));
            var skip=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-seraphim-fallback-music");
            if (File.Exists(file) && !skip)
            {
                using (var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri,AudioType.OGGVORBIS))
                {
                    ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio=true;
                    yield return request.SendWebRequest();
                    if (request.result==UnityWebRequest.Result.Success)
                    {
                        music.clip=DownloadHandlerAudioClip.GetContent(request); music.clip.name="Track Realm · Repeat Cycle 3";
                        LicensedMusicLoaded=true;
                    }
                    else Debug.LogWarning("Local music could not be loaded; using the original fallback. "+request.error);
                }
            }
            if (music.clip==null) music.clip=SanctuarySound.FallbackMusic();
            music.Play();
        }
        public void SetMusicVolume(float value,bool persist=true)
        {
            MusicVolume=Mathf.Clamp01(value); music.volume=Gain(MusicVolume);
            if (persist) { PlayerPrefs.SetFloat(MusicKey,MusicVolume); MarkDirty(); }
        }
        public void SetEffectsVolume(float value,bool persist=true)
        {
            EffectsVolume=Mathf.Clamp01(value);
            for (var i=0;i<voices.Length;i++) voices[i].volume=gains[i]*Gain(EffectsVolume);
            if (persist) { PlayerPrefs.SetFloat(EffectsKey,EffectsVolume); MarkDirty(); }
        }
        public void Play(SoundCue cue,Vector3? world=null)
        {
            var index=(int)cue;
            var interval=cue==SoundCue.Hit?.075f:cue==SoundCue.Banish?.11f:cue==SoundCue.Warning?.18f:.035f;
            if (Time.unscaledTime-lastPlayed[index]<interval) return;
            if (world.HasValue && game.Player!=null && Vector3.Distance(world.Value,game.Player.transform.position)>34) return;
            lastPlayed[index]=Time.unscaledTime;
            var voice=-1;
            for (var i=0;i<voices.Length;i++) if (!voices[i].isPlaying) { voice=i; break; }
            var priority=cue==SoundCue.Revelation || cue==SoundCue.Ascend || cue==SoundCue.Victory || index>=(int)SoundCue.BossSwing?30:cue==SoundCue.Hit || cue==SoundCue.Banish?100:60;
            if (voice<0 && priority<=60)
            {
                for (var i=0;i<voices.Length;i++) if (voices[i].priority>priority && (voice<0 || voices[i].volume<voices[voice].volume)) voice=i;
            }
            if (voice<0) return;
            var level=cue==SoundCue.Hit?.16f:cue==SoundCue.Banish?.22f:cue==SoundCue.Step?.2f:cue==SoundCue.Warning?.28f:
                cue==SoundCue.Pulse?.25f:cue==SoundCue.Revelation?.75f:.55f;
            if (game.Attract!=null && game.Attract.IsRunning) level*=.7f;
            var binding=bindings[index];
            var clip=binding!=null?binding.variants[variations[index]++%binding.variants.Length]:clips[index];
            if (binding!=null) level*=binding.gain;
            gains[voice]=level; var source=voices[voice];source.Stop();source.priority=priority;source.clip=clip;source.volume=level*Gain(EffectsVolume);
            source.pitch=index>=(int)SoundCue.BossSwing?.74f:cue==SoundCue.Step?.95f+(variations[index]%4)*.03f:1;
            source.panStereo=world.HasValue && Camera.main!=null?Mathf.Clamp((Camera.main.WorldToViewportPoint(world.Value).x-.5f)*.8f,-.6f,.6f):0;
            source.Play(); PlayedEffects++;LastEffectClip=clip;
        }
        public void StopEffects()
        {
            foreach (var source in voices) if (source!=null) source.Stop();
            if (lastPlayed!=null) for (var i=0;i<lastPlayed.Length;i++) lastPlayed[i]=-100;
        }
        public void ReloadPreferences()
        {
            SetMusicVolume(PlayerPrefs.GetFloat(MusicKey,.4f),false); SetEffectsVolume(PlayerPrefs.GetFloat(EffectsKey,.7f),false);
        }
        public float EffectsPeakGain { get { var value=0f; foreach (var source in voices) if (source!=null) value=Mathf.Max(value,source.volume); return value; } }
        public float MusicOutputRms()
        {
            var samples=new float[512]; music.GetOutputData(samples,0); var sum=0f; foreach (var value in samples) sum+=value*value; return Mathf.Sqrt(sum/samples.Length);
        }
        static float Gain(float value) => value*value;
        void MarkDirty() { dirty=true; saveAt=Time.unscaledTime+.35f; }
        public void FlushPreferences() { if (dirty) { PlayerPrefs.Save(); dirty=false; } }
        void Update()
        {
            if (dirty && Time.unscaledTime>=saveAt) FlushPreferences();
            if (game.Player!=null && game.Player.Speed>.8f && !game.Ultimate.IsActive && Time.time>=nextStep)
            {
                nextStep=Time.time+.32f; Play(SoundCue.Step,game.Player.transform.position);
            }
        }
        void OnApplicationQuit() { FlushPreferences(); }
        void OnDestroy()
        {
            if (music!=null && music.clip!=null) Destroy(music.clip);
            if (clips!=null) for (var i=0;i<clips.Length;i++) if (owned[i]) Destroy(clips[i]);
        }
    }
}
