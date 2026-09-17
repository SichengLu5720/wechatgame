using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    [Serializable]
    public sealed class FriendLeaderboardRecord
    {
        public int version=1,completedCount;
        public long achievedAtMs;
        public string marker;
        public bool Valid => version==1&&completedCount>=0&&completedCount<=20&&achievedAtMs>0&&achievedAtMs<9007199254740991L&&marker!=null&&System.Text.RegularExpressions.Regex.IsMatch(marker,"^[a-f0-9]{32}$");
    }

    public static class FriendLeaderboardProgress
    {
        public const string Key="islands.stairs-sort.leaderboard.v1";
        public static bool Verification => Array.IndexOf(Environment.GetCommandLineArgs(),"-friendleaderboardtest")>=0;
        public static string StorageKey => Key+(Verification||CampaignProgress.StorageKey!=CampaignProgress.Key?".verification":"");
        public static FriendLeaderboardRecord Read(int levelCount)
        {
            try {
                string json=PlayerPrefs.GetString(StorageKey,"");
                if(!string.IsNullOrEmpty(json)) {
                    var existing=JsonUtility.FromJson<FriendLeaderboardRecord>(json);
                    // Do not replace a corrupt record with a lower score.
                    return existing!=null&&existing.Valid?existing:null;
                }
                var migrated=new FriendLeaderboardRecord{completedCount=Math.Min(19,CampaignProgress.CurrentIndex(levelCount)),achievedAtMs=Now(),marker=Guid.NewGuid().ToString("N")};
                Save(migrated);return migrated;
            }catch(Exception){return null;}
        }
        public static long Now()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public static FriendLeaderboardRecord Advance(FriendLeaderboardRecord current,int completedIndex,int nextIndex,int levelCount,long now)
        {
            if(current==null||!current.Valid||levelCount<=0||completedIndex<0||completedIndex>=levelCount||completedIndex!=nextIndex||completedIndex+1<=current.completedCount)return current;
            var next=new FriendLeaderboardRecord{completedCount=Math.Min(20,completedIndex+1),achievedAtMs=Math.Max(now,current.achievedAtMs+1),marker=current.marker};
            return next.Valid?next:current;
        }
        public static bool RecordCompletion(int completedIndex,int nextIndex,int levelCount)
        {
            var old=Read(levelCount);var updated=Advance(old,completedIndex,nextIndex,levelCount,Now());
            if(ReferenceEquals(old,updated))return false;
            return Save(updated);
        }
        static bool Save(FriendLeaderboardRecord record)
        {
            try{PlayerPrefs.SetString(StorageKey,JsonUtility.ToJson(record));PlayerPrefs.Save();return true;}catch(Exception){return false;}
        }
    }
}
