using UnityEngine;
namespace StairsCrowd.Runtime
{
    public static class GameSettings
    {
        public static bool Sound {get{return PlayerPrefs.GetInt("islands.nightv1.settings.sound",1)!=0;}set{Set("sound",value);}}
        public static bool Music {get{return PlayerPrefs.GetInt("islands.nightv1.settings.music",0)!=0;}set{Set("music",value);}}
        public static bool Vibration {get{return PlayerPrefs.GetInt("islands.nightv1.settings.vibration",1)!=0;}set{Set("vibration",value);}}
        static void Set(string key,bool value){PlayerPrefs.SetInt("islands.nightv1.settings."+key,value?1:0);PlayerPrefs.Save();}
    }
    public sealed class IslandAudio : MonoBehaviour
    {
        AudioSource music,effects;AudioClip loop,tap,chime;
        void Awake(){music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();music.playOnAwake=effects.playOnAwake=false;music.loop=true;music.volume=.16f;effects.volume=.2f;loop=Tone("Cloud ambience",8,220,true);tap=Tone("Selection",.14f,660,false);chime=Tone("Gathering",.55f,880,false);music.clip=loop;Apply();}
        AudioClip Tone(string label,float seconds,float frequency,bool ambient){int rate=22050;var samples=new float[(int)(rate*seconds)];for(int i=0;i<samples.Length;i++){float t=i/(float)rate,envelope=ambient?Mathf.Pow(Mathf.Sin(Mathf.PI*t/seconds),2):Mathf.Sin(Mathf.PI*t/seconds)*Mathf.Exp(-4*t/seconds);samples[i]=envelope*(Mathf.Sin(2*Mathf.PI*frequency*t)*.35f+Mathf.Sin(2*Mathf.PI*frequency*1.5f*t)*.12f);}var clip=AudioClip.Create(label,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;}
        public void Apply(){if(!music)return;if(GameSettings.Music){if(!music.isPlaying)music.Play();}else music.Stop();if(!GameSettings.Sound)effects.Stop();}
        public void Play(bool complete=false){if(GameSettings.Sound&&effects)effects.PlayOneShot(complete?chime:tap);}
        void OnApplicationPause(bool paused){if(paused){music.Pause();effects.Stop();}else if(GameSettings.Music)music.UnPause();}
        void OnDestroy(){if(loop)Destroy(loop);if(tap)Destroy(tap);if(chime)Destroy(chime);}
    }
}
