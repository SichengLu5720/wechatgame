using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class RuntimeTuning
    {
        public const string Key="islands.dev.runtime-tuning.v1";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const bool Available=true;
#else
        public const bool Available=false;
#endif
        [Serializable] public struct Values
        {
            public int version; public float speed,platform,person; public bool linked;
            public static Values Default=>new Values{version=1,speed=1.25f,platform=1,person=1,linked=true};
            public bool Valid=>version==1&&Finite(speed,.5f,20)&&Finite(platform,.75f,1.25f)&&Finite(person,.75f,1.25f);
            static bool Finite(float v,float min,float max)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>=min&&v<=max;
        }
        static bool loaded; static Values working,saved;
        public static int Revision {get;private set;}
        public static int SaveCount {get;private set;}
        public static bool HasSaved {get;private set;}
        public static Values Working {get{EnsureLoaded();return working;}}
        public static Values Saved {get{EnsureLoaded();return saved;}}
        public static bool Dirty {get{EnsureLoaded();return working.speed!=saved.speed||working.platform!=saved.platform||working.person!=saved.person||working.linked!=saved.linked;}}
        public static float Speed=>Available?Working.speed:1;
        public static float Platform=>Available?Working.platform:1;
        public static float Person=>Available?Working.person:1;
        public static void EnsureLoaded()
        {
            if(loaded)return;loaded=true;HasSaved=false;working=saved=Values.Default;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(PlayerPrefs.HasKey(Key))try{var value=JsonUtility.FromJson<Values>(PlayerPrefs.GetString(Key));if(value.Valid){working=saved=value;HasSaved=true;}}catch(ArgumentException){}
#endif
        }
        public static void ChangeSpeed(float value){if(!Available)return;EnsureLoaded();working.speed=Mathf.Round(Mathf.Clamp(value,.5f,20)*20)/20;Revision++;}
        public static void SetLinked(bool linked){if(!Available)return;EnsureLoaded();working.linked=linked;Revision++;}
        public static void ChangeScale(bool person,float value)
        {
            if(!Available)return;EnsureLoaded();value=Mathf.Round(Mathf.Clamp(value,.75f,1.25f)*100)/100;
            if(working.linked){float old=person?working.person:working.platform;float ratio=value/old;
                ratio=Mathf.Clamp(ratio,Mathf.Max(.75f/working.platform,.75f/working.person),Mathf.Min(1.25f/working.platform,1.25f/working.person));
                // Float multiplication can overshoot an exact shared boundary by
                // one ULP; keep persisted values inside the validation interval.
                working.platform=Mathf.Clamp(working.platform*ratio,.75f,1.25f);working.person=Mathf.Clamp(working.person*ratio,.75f,1.25f);
            }else if(person)working.person=value;else working.platform=value;
            Revision++;
        }
        public static void Save()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EnsureLoaded();PlayerPrefs.SetString(Key,JsonUtility.ToJson(working));PlayerPrefs.Save();saved=working;HasSaved=true;SaveCount++;
#endif
        }
        public static void Reload(){loaded=false;EnsureLoaded();Revision++;}
    }
}
