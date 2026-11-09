using UnityEngine;

namespace Seraphim
{
    public enum SoundCue { Radiance,Spear,Lightning,Fire,Ascend,Revelation,Pulse,Hit,Banish,Hurt,Life,Spirit,Step,Warning,Charge,Wave,Victory,Ui,BossSwing,Meteor,Hellfire,Nova,Roar }

    // Original procedural sound design; no external samples or runtime downloads.
    internal static class SanctuarySound
    {
        const int Rate=22050;
        public static AudioClip Create(SoundCue cue)
        {
            var duration=cue==SoundCue.Revelation?2.5f:cue==SoundCue.Victory?2:cue==SoundCue.Ascend?1.1f:cue==SoundCue.Fire?.9f:
                cue==SoundCue.Hit || cue==SoundCue.Step?.12f:cue==SoundCue.Ui?.18f:.48f;
            var data=new float[(int)(Rate*duration)]; uint seed=7757+(uint)cue*157;
            var low=0f;
            for (var i=0;i<data.Length;i++)
            {
                var t=i/(float)Rate; var p=t/duration;
                seed=seed*1664525+1013904223; var noise=(seed>>8)/(float)0xffffff*2-1; low=Mathf.Lerp(low,noise,.08f);
                var envelope=Mathf.Min(1,t*80)*Mathf.Pow(1-p,1.8f);
                var tone=0f;
                switch (cue)
                {
                    case SoundCue.Radiance: tone=Sin(620*t-230*t*t)*.5f+noise*.13f; break;
                    case SoundCue.Spear: tone=Sin(1500*t-900*t*t)*.35f+noise*.3f; break;
                    case SoundCue.Lightning: tone=noise*.7f*(.6f+.4f*Sin(39*t))+Sin(180*t)*.18f; break;
                    case SoundCue.Fire: tone=low*1.8f+Sin(65*t)*.25f+noise*.13f; break;
                    case SoundCue.Ascend: tone=(Sin(110*t+100*t*t)+Sin(165*t+150*t*t)+Sin(220*t+200*t*t))*.18f; envelope=Mathf.Sin(p*Mathf.PI); break;
                    case SoundCue.Revelation: tone=(Sin(110*t)+Sin(220*t)*.8f+Sin(330*t)*.6f+Sin(440*t)*.4f)*.27f; break;
                    case SoundCue.Pulse: tone=Sin(220*t)*.4f+Sin(440*t)*.15f; break;
                    case SoundCue.Hit: tone=noise*.5f+Sin(155*t-300*t*t)*.3f; break;
                    case SoundCue.Banish: tone=Sin(540*t-300*t*t)*.3f+Sin(810*t)*.2f+noise*.15f; break;
                    case SoundCue.Hurt: tone=Sin(95*t)*.45f+low*.8f; break;
                    case SoundCue.Life: tone=Sin(523.25f*t)*.45f+Sin(784*t)*.2f; break;
                    case SoundCue.Spirit: tone=Sin(659.25f*t)*.4f+Sin(988*t)*.2f; break;
                    case SoundCue.Step: tone=low*2+noise*.09f; break;
                    case SoundCue.Warning: tone=Sin(185*t)*.35f+Sin(277*t)*.2f; break;
                    case SoundCue.Charge: tone=noise*.23f+Sin(85*t+120*t*t)*.45f; break;
                    case SoundCue.Wave: tone=Sin(220*t)*.3f+Sin(330*t)*.25f+Sin(440*t)*.15f; break;
                    case SoundCue.Victory: tone=Sin(261.63f*t)*.25f+Sin(329.63f*t)*.23f+Sin(392*t)*.2f+Sin(523.25f*t)*.12f; break;
                    case SoundCue.BossSwing: tone=low*1.4f+noise*.3f+Sin(75*t)*.2f; break;
                    case SoundCue.Meteor: tone=low*2+Sin(45*t)*.4f; break;
                    case SoundCue.Hellfire: tone=low*1.8f+noise*.25f; break;
                    case SoundCue.Nova: tone=Sin(95*t-40*t*t)*.5f+noise*.2f; break;
                    case SoundCue.Roar: tone=Sin(45*t)*.4f+low*1.6f; break;
                    default: tone=Sin(880*t)*.45f+Sin(1320*t)*.12f; break;
                }
                data[i]=Mathf.Clamp(tone*envelope,-.8f,.8f);
            }
            return Clip("Seraphim · "+cue,data);
        }
        public static AudioClip FallbackMusic()
        {
            const int seconds=32;
            var notes=new[]{new[]{146.83f,174.61f,220f,293.66f},new[]{116.54f,146.83f,174.61f,233.08f},new[]{130.81f,174.61f,220f,261.63f},new[]{130.81f,164.81f,196f,261.63f}};
            var data=new float[Rate*seconds];
            for (var i=0;i<data.Length;i++)
            {
                var t=i/(float)Rate; var bar=Mathf.Min(3,(int)(t/8)); var blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((t%8)/1.4f));
                var value=0f;
                for (var voice=0;voice<4;voice++)
                {
                    var now=notes[bar][voice]; var previous=notes[(bar+3)%4][voice];
                    var a=(Sin(now*t)+Sin(now*2*t)*.3f+Sin(now*3.003f*t)*.13f)*.09f;
                    var b=(Sin(previous*t)+Sin(previous*2*t)*.3f+Sin(previous*3.003f*t)*.13f)*.09f;
                    value+=Mathf.Lerp(b,a,blend);
                }
                var fade=Mathf.Min(1,Mathf.Min(t*3,(seconds-t)*3));
                data[i]=value*fade*(.8f+.15f*Sin(t*.09f));
            }
            return Clip("Original sanctuary choir and organ",data);
        }
        static float Sin(float cycles) => Mathf.Sin(cycles*2*Mathf.PI);
        static AudioClip Clip(string name,float[] data)
        {
            var clip=AudioClip.Create(name,data.Length,1,Rate,false); clip.SetData(data,0); return clip;
        }
    }
}
