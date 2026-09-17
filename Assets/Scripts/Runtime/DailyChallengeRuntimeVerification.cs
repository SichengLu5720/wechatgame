using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class DailyChallengeRuntimeVerification:MonoBehaviour
    {
        StairsGame game;string output;double now;DateTime today=new DateTime(2026,9,17);int checks;bool failed;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        void Call(string method,params object[] args){typeof(StairsGame).GetMethod(method,Private).Invoke(game,args);}
        void Check(bool ok,string text){if(!ok)throw new Exception("DAILY RUNTIME: "+text+" [moves="+game.board?.Moves+", outcome="+game.CurrentAttemptOutcome+", busy="+game.Busy+", remaining="+game.DailyClock?.Remaining+", pauses="+game.DailyClock?.Pauses+", message="+game.message+"]");checks++;}
        void Logs(string text,string stack,LogType type){if(failed||(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert))return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),text+"\n"+stack);Application.Quit(1);}
        IEnumerator Settle()
        {
            int frames=0;while(game.Busy&&frames++<1000){for(int i=0;i<4;i++)game.Advance(.1f);yield return null;}
            Check(!game.Busy,"presentation did not settle");while(game.IntroVisible)game.ContinueIntro();yield return null;yield return null;
        }
        IEnumerator Capture(string name)
        {
            string path=Path.Combine(output,name);ScreenCapture.CaptureScreenshot(path);for(int i=0;i<8;i++)yield return null;
        }
        IEnumerator Begin()
        {
            game.ReturnHome();game.OpenDailyCalendar();Check(game.DailyCalendarOpen&&game.Home,"calendar entry");Check(!game.StartDailyChallenge(today.AddDays(-1))&&!game.StartDailyChallenge(today.AddDays(1)),"past/future blocked");Check(game.StartDailyChallenge(today),"today entry");yield return Settle();
            Check(game.DailyActive&&!game.IsTutorial&&!game.NextSceneReady&&game.DailyTimeText=="3:00","daily session isolation");
        }
        void AdvanceTime(double seconds){now+=seconds;Call("TickDaily");}
        IEnumerator Start()
        {
            game=GetComponent<StairsGame>();var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-dailyOutput");output=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"task002-verification");Directory.CreateDirectory(output);Application.logMessageReceived+=Logs;
            game.DailyToday=()=>today;game.DailyNow=()=>now;
            Call("OnApplicationFocus",true);Call("OnApplicationPause",false);
            if(Array.IndexOf(args,"-dailyread")>=0){
                Check(DailyChallengeProgress.Completed(today),"record did not persist across process");game.ReturnHome();game.OpenDailyCalendar();yield return Settle();Check(!game.StartDailyChallenge(today),"restart replay accepted");yield return Capture("calendar-persisted.png");Finish("persistence.json");yield break;
            }
            foreach(var date in new[]{today,new DateTime(2026,8,31),new DateTime(2026,12,31),new DateTime(2027,1,1),new DateTime(2024,2,29)})PlayerPrefs.DeleteKey(DailyChallengeProgress.StorageKey(date));
            PlayerPrefs.SetInt(CampaignProgress.StorageKey,6);PlayerPrefs.Save();game.ReturnHome();
            // Some Windows drivers do not create a readable backbuffer when the first resize equals the launch size.
            Screen.SetResolution(391,844,FullScreenMode.Windowed);yield return null;yield return null;Screen.SetResolution(390,844,FullScreenMode.Windowed);yield return Settle();yield return Capture("home-390x844.png");
            game.OpenDailyCalendar();yield return null;yield return Capture("calendar-390x844.png");yield return Begin();string initial=Rules.Key(game.board.Level,game.board.Current),identity=game.board.Level.provenance.id;
            AdvanceTime(300);Check(!game.DailyClock.Started&&game.DailyTimeText=="3:00","observation timer");
            game.HandlePointer(new Vector2(-1000,-1000));game.ClickNode(-1);Check(!game.DailyClock.Started,"blank/invalid start");
            game.OpenSettings();AdvanceTime(400);game.CloseSettings();yield return null;yield return null;Check(!game.DailyClock.Started,"settings starts timer");
            var point=new Vector2(Screen.width*.5f,Screen.height*.5f);var rotation=game.view.transform.rotation;game.BeginViewPointer(point);game.MoveViewPointer(point+Vector2.right*80);game.EndViewPointer(point+Vector2.right*80);Check(!game.DailyClock.Started&&game.view.transform.rotation==rotation,"drag starts timer/rotates view");
            Check(!game.BeginPropSelection(2)&&!game.UseUndoProp()&&!game.DailyClock.Started,"unavailable prop starts timer");
            yield return Capture("timer-390x844.png");game.ClickPerson(game.board.Level.solution[0].a);Check(game.DailyClock.Started,"person starts timer");AdvanceTime(10);Check(game.DailyClock.Remaining==170,"clock decrement");
            game.OpenSettings();AdvanceTime(50);Call("OnApplicationPause",true);game.CloseSettings();AdvanceTime(80);Check(game.DailyClock.Remaining==170,"settings/background overlap");Call("OnApplicationPause",false);Call("OnApplicationFocus",false);AdvanceTime(60);Call("OnApplicationFocus",true);AdvanceTime(2);Check(game.DailyClock.Remaining==168,"focus and resume");
            game.ResetLevel();yield return Settle();Check(game.DailyTimeText=="3:00"&&!game.DailyClock.Started&&Rules.Key(game.board.Level,game.board.Current)==initial&&game.AttemptSeenCount==1,"retry reset");
            game.ClickNode(game.board.Level.solution[0].a);Check(game.DailyClock.Started,"platform starts timer");game.ResetLevel();yield return Settle();Check(game.BeginPropSelection(1)&&game.DailyClock.Started,"available prop starts timer");game.CancelPropSelection();game.ResetLevel();yield return Settle();
            var action=game.board.Level.solution[0];Check(game.TryMoveSync(action.a,action.b)&&game.Busy,"move setup");AdvanceTime(181);Check(game.FailureLocked&&game.CurrentAttemptOutcome==AttemptOutcome.TimedOut,"timeout lock");string locked=Rules.Key(game.board.Level,game.board.Current);Check(!game.TryMoveSync(action.a,action.b)&&!game.BeginPropSelection(1)&&!game.UseUndoProp(),"timeout APIs");game.Undo();game.ClickNode(0);Call("EvaluateCurrentBoard");Check(game.FailureLocked&&Rules.Key(game.board.Level,game.board.Current)==locked,"timeout terminal overwritten");yield return Settle();Check(game.FailureVisible,"timeout presentation");yield return Capture("timeout-390x844.png");
            game.ResetLevel();yield return Settle();Check(game.board.Level.provenance.id==identity&&Rules.Key(game.board.Level,game.board.Current)==initial&&!game.FailureLocked,"same date retry identity");
            game.ClickNode(action.a);now+=181;Check(!game.TryMoveSync(action.a,action.b)&&game.FailureLocked&&game.board.Moves==0,"input failed to settle deadline before Update");
            // The first three reads are the pre-input guard and Interact; the fourth is Commit's
            // guard after FastMovement.Build and MotionComposer.Append. Cross the deadline there.
            bool crossDuringPlanning=false;int planningReads=0;double deadline=now+180;
            game.DailyNow=()=>{if(crossDuringPlanning&&++planningReads>=4)now=deadline+.01;return now;};
            game.ResetLevel();yield return Settle();crossDuringPlanning=true;
            Check(!game.TryMoveSync(action.a,action.b),"planning crossed deadline but committed");crossDuringPlanning=false;
            Check(planningReads>=4&&game.LastPlanningMilliseconds>0&&game.DailyClock.Started&&game.FailureLocked&&game.CurrentAttemptOutcome==AttemptOutcome.TimedOut&&game.board.Moves==0&&game.motion==null&&Rules.Key(game.board.Level,game.board.Current)==initial,"planning deadline guard did not preserve board/terminal state");
            game.DailyNow=()=>now;
            today=new DateTime(2026,12,31);yield return Begin();game.ClickNode(game.board.Level.solution[0].a);today=today.AddDays(1);AdvanceTime(181);yield return Settle();Check(!game.DailyCanRetry,"expired retry allowed");game.ResetLevel();Check(game.Home&&game.DailyCalendarOpen&&!game.DailyActive,"expired failed reset did not return calendar");
            today=new DateTime(2026,8,31);yield return Begin();today=today.AddDays(1);game.OpenSettings();game.ResetLevel();Check(game.Home&&game.DailyCalendarOpen,"expired settings reset");
            // Win at 179.9 seconds; animation finishes after midnight and after deadline.
            today=new DateTime(2026,8,31);yield return Begin();var solution=game.board.Level.solution;
            for(int i=0;i<solution.Length;i++){if(i==solution.Length-1)AdvanceTime(179.9);Check(game.TryMoveSync(solution[i].a,solution[i].b),"no-prop witness move "+i);if(i<solution.Length-1)yield return Settle();}
            Check(game.board.Solved&&game.DailyClock.Result==DailyResult.Won,"logical win");today=today.AddDays(1);AdvanceTime(999);yield return Settle();Check(game.DailyClock.Result==DailyResult.Won&&!game.FailureLocked&&DailyChallengeProgress.Completed(new DateTime(2026,8,31))&&!DailyChallengeProgress.Completed(today),"cross-midnight win attribution");
            Check(!game.UseUndoProp()&&!game.TryMoveSync(0,1),"won mutation");game.OpenSettings();Check(!game.SettingsOpen,"won settings restart escape");game.ResetLevel();Check(game.Home&&game.DailyCalendarOpen,"won reset escape");
            today=new DateTime(2026,9,17);yield return Begin();solution=game.board.Level.solution;foreach(var step in solution){Check(game.TryMoveSync(step.a,step.b),"today witness");yield return Settle();}Check(DailyChallengeProgress.Completed(today),"today completion");yield return Capture("complete-390x844.png");
            game.ReturnHome();game.OpenDailyCalendar();Check(!game.StartDailyChallenge(today),"complete replay entry");yield return Capture("calendar-complete-390x844.png");
            today=new DateTime(2026,8,31);yield return null;yield return Capture("calendar-six-rows.png");today=new DateTime(2024,2,29);yield return null;yield return Capture("calendar-leap-day.png");
            Check(CampaignProgress.CurrentIndex(20)==6,"daily changed campaign progress");game.ReturnHome();game.StartGame();yield return Settle();Check(!game.DailyActive&&game.levelIndex==6,"campaign switch");game.OpenEditor();Check(!game.DailyActive&&game.EditorOpen,"editor switch");game.PlayDraft();yield return Settle();Check(!game.DailyActive&&!game.IsTutorial,"custom switch");
            today=new DateTime(2027,1,1);yield return Begin();Check(CampaignProgress.CurrentIndex(20)==6,"cross mode progress");game.ReturnHome();Screen.SetResolution(700,1000,FullScreenMode.Windowed);yield return Settle();yield return Capture("home-700x1000.png");game.OpenDailyCalendar();yield return null;yield return Capture("calendar-700x1000.png");
            Finish("runtime.json");
        }
        void Finish(string name){File.WriteAllText(Path.Combine(output,name),"{\"passed\":true,\"assertions\":"+checks+",\"storage\":\"isolated-verification\"}");Debug.Log("DAILY RUNTIME VERIFICATION PASSED "+name+" checks="+checks);Application.logMessageReceived-=Logs;Application.Quit(0);}
    }
}
