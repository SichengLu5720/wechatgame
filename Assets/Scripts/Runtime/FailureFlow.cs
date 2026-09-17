using System.Collections.Generic;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        readonly HashSet<string> attemptSeen=new HashSet<string>();
        public bool FailureLocked {get;private set;}
        public bool FailureChecking {get{return false;}}
        public bool FailureVisible {get{return FailureLocked&&!Busy&&!IntroVisible&&!Home&&!editing;}}
        public AttemptOutcome CurrentAttemptOutcome {get;private set;}
        public int AttemptSeenCount {get{return attemptSeen.Count;}}
        const string FailureTitle="本关失败",FailureRetry="重试本关",FailureHome="返回主页";
        string FailureLevelText {get{return DailyActive?(CurrentAttemptOutcome==AttemptOutcome.TimedOut?"时间到了 · 每日挑战":"每日挑战"):customPlaying?"自由关卡":"第 "+(levelIndex+1)+" 关";}}
        public string FailurePanelCopy {get{return FailureTitle+"|"+FailureLevelText+"|"+FailureRetry+"|"+FailureHome;}}

        void ClearFailureState(string reason,bool clearHistory=false)
        {
            FailureLocked=false;CurrentAttemptOutcome=AttemptOutcome.Continue;
            if(clearHistory)attemptSeen.Clear();
        }

        void BeginAttempt()
        {
            ClearFailureState("attempt started",true);
            if(board!=null)attemptSeen.Add(Rules.Key(board.Level,board.Current));
        }

        void EvaluateCurrentBoard()
        {
            if(DailyActive&&DailyClock.Result==DailyResult.Failed&&FailureLocked)return;
            FailureLocked=false;CurrentAttemptOutcome=AttemptOutcome.Continue;
            if(board==null||Home||editing)return;

            attemptSeen.Add(Rules.Key(board.Level,board.Current));
            if(board.Solved){CurrentAttemptOutcome=AttemptOutcome.Solved;RecordBuiltInVictory();return;}

            CurrentAttemptOutcome=AttemptFailure.Classify(board.Level,board.Current,attemptSeen);
            if(CurrentAttemptOutcome!=AttemptOutcome.NoLegalMoves&&CurrentAttemptOutcome!=AttemptOutcome.OnlyRepeatedMoves)return;
            FailureLocked=true;if(DailyActive)DailyClock.Fail();CancelPropSelection();CancelViewPointer();selected=-1;
            if(scene!=null)scene.Highlight(board,-1);message="";
        }

        void RecordBuiltInVictory()
        {
            if(DailyActive){if(board!=null&&board.Solved)WinDaily();return;}
            if(board!=null&&board.Solved&&CampaignProgress.IsBuiltInCompletion(customPlaying,levelIndex,builtInCount)){
                bool improved=FriendLeaderboardProgress.RecordCompletion(levelIndex,CampaignProgress.CurrentIndex(builtInCount),builtInCount);
                CampaignProgress.RecordCompletion(levelIndex,builtInCount);if(improved)SyncLeaderboard();
            }
        }

        void DrawFailurePanel(float w,float h)
        {
            if(!FailureVisible)return;Fill(new Rect(0,0,w,h),new Color(.06f,.10f,.14f,.66f));
            float panelW=Mathf.Min(w-48,430),left=(w-panelW)/2,top=h*.34f,panelH=216;Round(new Rect(left,top+6,panelW,panelH),22,new Color(.11f,.17f,.18f,.28f));Round(new Rect(left,top,panelW,panelH),22,UiPaper);
            GUI.Label(new Rect(left+30,top+24,panelW-60,42),FailureTitle,Style(28,FontStyle.Bold,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(left+30,top+76,panelW-60,28),FailureLevelText,Style(15,FontStyle.Bold,TextAnchor.MiddleCenter));
            float bw=(panelW-76)/2;if(RoundButton(new Rect(left+26,top+132,bw,52),DailyActive&&!DailyCanRetry?"返回日历":FailureRetry,new Color(.78f,.82f,.75f)))ResetLevel();
            if(RoundButton(new Rect(left+50+bw,top+132,bw,52),FailureHome,new Color(.88f,.68f,.62f)))ReturnHome();
        }
    }
}
