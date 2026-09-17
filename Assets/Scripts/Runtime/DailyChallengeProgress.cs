using System;
using System.Globalization;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class DailyChallengeProgress
    {
        public static bool Verification => Array.IndexOf(Environment.GetCommandLineArgs(),"-dailytest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-dailyverify")>=0;
        public static string StorageKey(DateTime date) => "islands.stairs-sort.daily.v1."+(Verification?"verification.":"")+date.ToString("yyyy-MM",CultureInfo.InvariantCulture);
        public static bool Completed(DateTime date) => (PlayerPrefs.GetInt(StorageKey(date),0)&(1<<(date.Day-1)))!=0;
        public static void Complete(DateTime date){string key=StorageKey(date);PlayerPrefs.SetInt(key,PlayerPrefs.GetInt(key,0)|(1<<(date.Day-1)));PlayerPrefs.Save();}
        public static bool CanStart(DateTime requested,DateTime today) => requested.Date==today.Date&&!Completed(today);
        public static int FirstWeekday(DateTime date) => (int)new DateTime(date.Year,date.Month,1).DayOfWeek;
        public static int MonthRows(DateTime date) => (FirstWeekday(date)+DateTime.DaysInMonth(date.Year,date.Month)+6)/7;
    }
    public static class DailyChallengeRepository
    {
        static Catalog pool;
        public static Catalog Pool => pool??(pool=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("daily-v1").text));
        // Version 1 mapping must not depend on GetHashCode, timezone offsets or mutable campaign order.
        public static int Index(DateTime day,int count) => (int)((day.Date.Ticks/TimeSpan.TicksPerDay)%count);
        public static LevelSpec ForDate(DateTime date) => LevelPresentation.Apply(LevelShare.Copy(Pool.levels[Index(date,Pool.levels.Length)]));
        public static string NavigationPath(LevelSpec level) => "daily-navigation/"+level.provenance.id;
    }
}
