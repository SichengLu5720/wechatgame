using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed class FriendLeaderboardRuntimeVerification:MonoBehaviour
    {
        StairsGame game;string output;int checks;bool failed;
        void Check(bool ok,string message){checks++;if(!ok)throw new Exception("LEADERBOARD RUNTIME: "+message);}
        void Log(string message,string stack,LogType type){if(failed||(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert))return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),message);Application.Quit(1);}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        IEnumerator WaitReady(){yield return new WaitForSecondsRealtime(.8f);}
        IEnumerator Start()
        {
            output=Path.GetFullPath("artifacts/task008/final/runtime-"+Screen.width);Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            game=GetComponent<StairsGame>();Check(!WeChatLeaderboard.Available,"mock accessed platform");
            PlayerPrefs.DeleteKey(FriendLeaderboardProgress.StorageKey);PlayerPrefs.SetInt(CampaignProgress.StorageKey,19);PlayerPrefs.Save();
            var migrated=FriendLeaderboardProgress.Read(20);Check(migrated.completedCount==19&&migrated.achievedAtMs>0,"conservative migration");
            var repeated=FriendLeaderboardProgress.Read(20);Check(repeated.achievedAtMs==migrated.achievedAtMs,"migration retimed");
            PlayerPrefs.DeleteKey(FriendLeaderboardProgress.StorageKey);PlayerPrefs.SetInt(CampaignProgress.StorageKey,6);PlayerPrefs.Save();
            game.ReturnHome();for(int i=0;i<100&&game.Busy;i++){game.Advance(.1f);yield return null;}
            yield return Capture("home");int original=game.levelIndex;
            game.OpenFriendLeaderboard();Check(game.FriendLeaderboardOpen&&game.InterfaceBlocksInput,"modal missing");
            game.StartGame();game.OpenSettings();Check(game.Home&&!game.SettingsOpen&&game.levelIndex==original,"input guard");
            yield return Capture("loading");yield return WaitReady();Check(game.LeaderboardState==FriendLeaderboardState.Ready,"load ready");
            Check(game.LeaderboardRows.Count==15&&game.LeaderboardRows.Find(r=>r.Self).Rank==12,"own rank");yield return Capture("ready");
            var display=typeof(WeChatPlatform);var flags=BindingFlags.Static|BindingFlags.NonPublic;
            display.GetField("normalizedSafe",flags).SetValue(null,new Rect(0,.04f,1,.90f));display.GetField("hasSafe",flags).SetValue(null,true);
            display.GetField("normalizedCapsule",flags).SetValue(null,new Rect(.70f,.91f,.25f,.04f));display.GetField("hasCapsule",flags).SetValue(null,true);
            var safe=GameplayLayout.Current;var safeUi=FriendLeaderboardLayout.Current;
            Check(safe.Safe.Contains(safeUi.Panel.min)&&safe.Safe.Contains(safeUi.Panel.max),"runtime safe-area panel");yield return Capture("safe-area");
            display.GetField("hasSafe",flags).SetValue(null,false);display.GetField("hasCapsule",flags).SetValue(null,false);
            game.LeaderboardScroll=99999;yield return Capture("scrolled");Check(game.LeaderboardScroll<99999,"scroll clamp");
            game.SetLeaderboardMock(FriendLeaderboardState.ProfileUnavailable);yield return WaitReady();yield return Capture("profile");
            game.SetLeaderboardMock(FriendLeaderboardState.Empty);yield return WaitReady();Check(game.LeaderboardRows.Count==1,"empty fixture");yield return Capture("empty");
            game.RetryFriendLeaderboard();Check(game.LeaderboardState==FriendLeaderboardState.Loading,"empty profile retry unavailable");yield return WaitReady();Check(game.LeaderboardState==FriendLeaderboardState.Empty,"empty retry did not restore state");
            game.SetLeaderboardMock(FriendLeaderboardState.Failure);yield return WaitReady();yield return Capture("failure");
            game.SetLeaderboardMock(FriendLeaderboardState.Ready);yield return WaitReady();Check(game.LeaderboardState==FriendLeaderboardState.Ready,"retry recovery");
            game.CloseFriendLeaderboard();Check(game.Home&&game.levelIndex==original&&game.InterfaceBlocksInput,"close state and suppression");yield return null;yield return null;
            Check(!game.InterfaceBlocksInput,"input not restored");game.OpenSettings();Check(game.SettingsOpen,"settings blocked after close");yield return Capture("settings");game.CloseSettings();yield return null;yield return null;
            // Exercise the real victory entry, including final-level completion and excluded sources.
            PlayerPrefs.DeleteKey(FriendLeaderboardProgress.StorageKey);PlayerPrefs.SetInt(CampaignProgress.StorageKey,19);PlayerPrefs.Save();
            FriendLeaderboardProgress.Read(20);game.LoadLevel(19);
            foreach(var move in game.board.Level.solution){var result=game.board.TryMove(move.a,move.b);Check(result.ok,"last authored solution");}
            Check(game.board.Solved,"last board not solved");var method=typeof(StairsGame).GetMethod("RecordBuiltInVictory",BindingFlags.Instance|BindingFlags.NonPublic);method.Invoke(game,null);
            var won=FriendLeaderboardProgress.Read(20);Check(won.completedCount==20,"runtime final completion");method.Invoke(game,null);Check(FriendLeaderboardProgress.Read(20).achievedAtMs==won.achievedAtMs,"runtime replay timestamp");
            var custom=typeof(StairsGame).GetField("customPlaying",BindingFlags.Instance|BindingFlags.NonPublic);custom.SetValue(game,true);method.Invoke(game,null);Check(FriendLeaderboardProgress.Read(20).achievedAtMs==won.achievedAtMs,"custom completion changed score");custom.SetValue(game,false);
            PlayerPrefs.SetString(FriendLeaderboardProgress.StorageKey,"{broken");PlayerPrefs.Save();Check(FriendLeaderboardProgress.Read(20)==null,"corrupt record overwritten");Check(PlayerPrefs.GetString(FriendLeaderboardProgress.StorageKey)=="{broken","corrupt record mutated");
            PlayerPrefs.DeleteKey(FriendLeaderboardProgress.StorageKey);PlayerPrefs.DeleteKey(CampaignProgress.StorageKey);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output,"runtime.json"),"{\"passed\":true,\"checks\":"+checks+",\"width\":"+Screen.width+",\"height\":"+Screen.height+"}");Debug.Log("FRIEND LEADERBOARD RUNTIME PASSED checks="+checks);Application.Quit(0);
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
