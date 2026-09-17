using System;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        public bool DailyActive {get;private set;}
        public bool DailyCalendarOpen {get;private set;}
        public DateTime DailyDate {get;private set;}
        public DailyChallengeClock DailyClock {get;private set;}
        internal Func<DateTime> DailyToday=()=>DateTime.Today;
        internal Func<double> DailyNow=()=>Time.realtimeSinceStartupAsDouble;
        LevelSpec dailyLevel;
        DailyPause dailyBackground;
        void DailyPlatformBackground(bool hidden){PauseDaily(DailyPause.Platform,hidden);}
        public bool DailyCanRetry => DailyActive&&DailyDate==DailyToday().Date&&!DailyChallengeProgress.Completed(DailyDate)&&DailyClock.Result!=DailyResult.Won;
        public string DailyTimeText {get{int seconds=(int)Math.Ceiling(DailyClock?.Remaining??180);return (seconds/60)+":"+(seconds%60).ToString("00");}}

        void LeaveDaily(){DailyActive=false;DailyCalendarOpen=false;DailyClock=null;dailyLevel=null;}
        public void OpenDailyCalendar(){if(!Home)ReturnHome();CloseSettings();DailyCalendarOpen=true;CancelViewPointer();}
        public bool StartDailyChallenge(DateTime requested)
        {
            if(!Home||!DailyCalendarOpen||!DailyChallengeProgress.CanStart(requested,DailyToday()))return false;
            dailyLevel=DailyChallengeRepository.ForDate(requested);DailyDate=requested.Date;DailyActive=true;DailyCalendarOpen=false;
            DailyClock=new DailyChallengeClock(DailyNow,dailyBackground);customPlaying=false;editing=false;Home=false;
            DisposeEditorPreview();DiscardPreparedScene();LoadBoard(-1,true);return true;
        }
        void TickDaily()
        {
            if(!DailyActive||DailyClock==null)return;DailyClock.Tick();
            if(DailyClock.Result==DailyResult.Failed&&!FailureLocked){FailureLocked=true;CurrentAttemptOutcome=AttemptOutcome.TimedOut;CancelPropSelection();CancelViewPointer();selected=-1;if(scene!=null)scene.Highlight(board,-1);message="";}
        }
        bool DailyInput(bool start=false)
        {
            if(!DailyActive)return true;TickDaily();
            return !FailureLocked&&DailyClock.Result==DailyResult.Pending&&DailyClock.Pauses==DailyPause.None&&(!start||DailyClock.Interact());
        }
        void PauseDaily(DailyPause reason,bool paused)
        {
            if(reason!=DailyPause.Settings)dailyBackground=paused?dailyBackground|reason:dailyBackground&~reason;
            if(DailyActive){DailyClock.SetPause(reason,paused);TickDaily();}
        }
        void WinDaily(){DailyClock.Win();DailyChallengeProgress.Complete(DailyDate);CancelPropSelection();CancelViewPointer();}
        bool ResetDailyAttempt()
        {
            if(!DailyActive)return true;
            if(!DailyCanRetry){ReturnHome();OpenDailyCalendar();return false;}
            DailyClock=new DailyChallengeClock(DailyNow,dailyBackground);return true;
        }
        void DrawDailyCalendar(float w,float h,bool completedPanel=false)
        {
            DateTime today=DailyToday().Date;
            Fill(new Rect(0,0,w,h),new Color(.06f,.10f,.14f,.76f));
            var layout=GameplayLayout.Current;float insetTop=layout.Gear.y,insetBottom=Mathf.Max(16,h-layout.Safe.yMax+12);
            float width=Mathf.Min(w-32,460),height=Mathf.Min(566,h-insetTop-insetBottom),left=(w-width)/2,top=insetTop+(h-insetTop-insetBottom-height)/2;
            Round(new Rect(left,top+5,width,height),22,new Color(.07f,.15f,.18f,.45f));Round(new Rect(left,top,width,height),22,UiPaper);
            GUI.Label(new Rect(left+48,top+18,width-96,38),"每日挑战",Style(27,FontStyle.Bold,TextAnchor.MiddleCenter));
            if(!completedPanel){var close=new Rect(left+width-48,top+17,32,32);if(RoundButton(close,"×",new Color(.85f,.85f,.78f))){DailyCalendarOpen=false;suppressInputThrough=Time.frameCount+1;}}
            GUI.Label(new Rect(left+24,top+62,width-48,35),today.Year+" 年 "+today.Month+" 月",Style(21,FontStyle.Bold,TextAnchor.MiddleCenter));
            float gridTop=top+134,cell=(width-40)/7,gap=5,rowHeight=Mathf.Min(cell+7,(height-260)/6);
            string[] weekdays={"日","一","二","三","四","五","六"};
            for(int i=0;i<7;i++)GUI.Label(new Rect(left+20+i*cell,top+104,cell,24),weekdays[i],Style(14,FontStyle.Bold,TextAnchor.MiddleCenter));
            int offset=DailyChallengeProgress.FirstWeekday(today),days=DateTime.DaysInMonth(today.Year,today.Month);
            for(int day=1;day<=days;day++){
                int slot=offset+day-1;var date=new DateTime(today.Year,today.Month,day);bool done=DailyChallengeProgress.Completed(date),current=day==today.Day;
                var rect=new Rect(left+20+slot%7*cell,gridTop+slot/7*rowHeight,cell-gap,rowHeight-gap);
                Color fill=done?new Color(.59f,.80f,.72f):current?new Color(.88f,.68f,.62f):day>today.Day?new Color(.94f,.92f,.86f):new Color(.90f,.89f,.83f);
                Round(rect,10,fill);
                if(done){var center=rect.center;float s=Mathf.Min(rect.width,rect.height)*.26f;Stroke(center+new Vector2(-s,0),center+new Vector2(-s*.2f,s*.7f),3,UiInk);Stroke(center+new Vector2(-s*.2f,s*.7f),center+new Vector2(s,-s*.65f),3,UiInk);}
                else GUI.Label(rect,day.ToString(),Style(18,current?FontStyle.Bold:FontStyle.Normal,TextAnchor.MiddleCenter));
            }
            bool doneToday=DailyChallengeProgress.Completed(today);
            string status=completedPanel?"挑战完成 · 已记录 "+DailyDate.ToString("M 月 d 日"):doneToday?"今日已完成":"完成今日挑战，点亮日历";
            GUI.Label(new Rect(left+18,top+height-117,width-36,28),status,Style(15,FontStyle.Normal,TextAnchor.MiddleCenter));
            if(completedPanel||doneToday){if(RoundButton(new Rect(left+30,top+height-70,width-60,48),"返回主页",new Color(.59f,.80f,.72f)))ReturnHome();}
            else if(RoundButton(new Rect(left+30,top+height-70,width-60,48),"挑战今日",new Color(.88f,.68f,.62f)))StartDailyChallenge(today);
        }
    }
}
