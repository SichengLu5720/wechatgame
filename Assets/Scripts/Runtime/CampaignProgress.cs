using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class CampaignProgress
    {
        public const string Key="islands.stairs-sort.progress.v1.next-built-in-level";
        public static string StorageKey {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-failureprogresstest")>=0?Key+".verification":Key;}}
        public static int Normalize(int raw,int levelCount){return levelCount<=0?0:Mathf.Clamp(raw,0,levelCount-1);}
        public static int NextAfterCompletion(int current,int completed,int levelCount)
        {
            current=Normalize(current,levelCount);if(levelCount<=0||completed!=current)return current;return Math.Min(current+1,levelCount-1);
        }
        public static bool IsBuiltInCompletion(bool customPlaying,int completed,int levelCount)
        {return !customPlaying&&completed>=0&&completed<levelCount;}
        public static int CurrentIndex(int levelCount)
        {
            int raw=0;try{raw=PlayerPrefs.GetInt(StorageKey,0);}catch(Exception error){Debug.LogWarning("CAMPAIGN PROGRESS READ FAILED: "+error.Message);}
            int valid=Normalize(raw,levelCount);if(valid!=raw)Write(valid);return valid;
        }
        public static int RecordCompletion(int completed,int levelCount)
        {
            int current=CurrentIndex(levelCount),next=NextAfterCompletion(current,completed,levelCount);if(next!=current)Write(next);return next;
        }
        static void Write(int value)
        {
            try{PlayerPrefs.SetInt(StorageKey,value);PlayerPrefs.Save();}
            catch(Exception error){Debug.LogWarning("CAMPAIGN PROGRESS WRITE FAILED: "+error.Message);}
        }
    }
}
