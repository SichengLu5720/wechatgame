using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class FailureProgressRuntimeVerification:MonoBehaviour
    {
        public static bool Active {get;private set;}
        static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        StairsGame game;string output;

        void Require(bool condition,string message){if(condition)return;Debug.LogError("FAILURE PROGRESS RUNTIME VERIFICATION FAILED: "+message);Cleanup();Application.Quit(1);throw new Exception(message);}
        static HashSet<string> Seen(StairsGame target){return (HashSet<string>)typeof(StairsGame).GetField("attemptSeen",PrivateInstance).GetValue(target);}
        static void Evaluate(StairsGame target){typeof(StairsGame).GetMethod("EvaluateCurrentBoard",PrivateInstance).Invoke(target,null);}
        static void MarkEveryLegalResultSeen(StairsGame target)
        {
            var level=target.board.Level;var state=target.board.Current;var seen=Seen(target);
            for(int a=0;a<level.nodes.Length;a++)for(int b=0;b<level.nodes.Length;b++){
                var move=Rules.Preview(level,state,a,b);if(move.ok)seen.Add(Rules.Key(level,Rules.Apply(state,move)));
            }
        }
        IEnumerator Settle()
        {
            int frames=0;while(game.Busy&&frames++<2000){game.Advance(.05f);yield return null;}
            Require(!game.Busy,"Runtime flow did not settle");
        }
        IEnumerator Ready()
        {
            yield return Settle();while(game.IntroVisible)game.ContinueIntro();yield return null;
        }
        IEnumerator PlaySolutionUntilFinalMove(int index)
        {
            game.LoadLevel(index);yield return Ready();var solution=game.board.Level.solution;Require(solution!=null&&solution.Length>0,"Missing authored solution for level "+index);
            for(int i=0;i<solution.Length;i++){
                Require(game.TryMoveSync(solution[i].a,solution[i].b),"Authored move rejected at level "+index+" step "+i);
                if(i+1<solution.Length)yield return Settle();
            }
            Require(game.board.Solved,"Authored solution did not reach logical victory for level "+index);
        }
        IEnumerator Start()
        {
            Active=true;game=GetComponent<StairsGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts/failure-and-level-progress"));Directory.CreateDirectory(output);
            PlayerPrefs.DeleteKey(CampaignProgress.StorageKey);PlayerPrefs.Save();
            // Home initializes from the isolated saved index.
                game.ReturnHome();yield return Settle();Require(game.Home&&game.levelIndex==0,"Missing progress did not initialize home at level 1");
                PlayerPrefs.SetInt(CampaignProgress.StorageKey,3);PlayerPrefs.Save();game.ReturnHome();yield return Settle();
                Require(game.Home&&game.levelIndex==3&&game.AttemptSeenCount==1,"Home did not reload saved progress into a fresh attempt");

                // Drive the real move/animation/input chain into OnlyRepeatedMoves.
                game.LoadLevel(4);yield return Ready();var first=game.board.Level.solution[0];Require(game.board.Level.solution.Length>1&&game.TryMoveSync(first.a,first.b),"Failure setup move was rejected or completed in one step");
                Require(game.Busy,"Failure setup move did not start presentation");MarkEveryLegalResultSeen(game);Evaluate(game);
                Require(game.FailureLocked&&(game.CurrentAttemptOutcome==AttemptOutcome.OnlyRepeatedMoves||game.CurrentAttemptOutcome==AttemptOutcome.NoLegalMoves),"Runtime chain did not lock one-step failure (outcome="+game.CurrentAttemptOutcome+")");
                Require(!game.FailureVisible,"Failure panel became visible before presentation settled");
                string lockedKey=Rules.Key(game.board.Level,game.board.Current);int lockedMoves=game.board.Moves,lockedSeen=game.AttemptSeenCount,undo=game.UndoRemaining,shuffle=game.ShuffleRemaining;
                Move legal=null;for(int a=0;a<game.board.Level.nodes.Length&&legal==null;a++)for(int b=0;b<game.board.Level.nodes.Length;b++){var candidate=Rules.Preview(game.board.Level,game.board.Current,a,b);if(candidate.ok){legal=candidate;break;}}
                Require(legal!=null&&!game.TryMoveSync(legal.from,legal.to),"Failure lock allowed TryMove");game.Undo();Require(!game.UseUndoProp()&&!game.UseShuffleProp(),"Failure lock allowed a prop");game.OpenSettings();Require(!game.SettingsOpen,"Failure lock opened settings");game.BeginViewPointer(new Vector2(Screen.width*.5f,Screen.height*.5f));game.EndViewPointer(new Vector2(Screen.width*.5f,Screen.height*.5f));
                Require(Rules.Key(game.board.Level,game.board.Current)==lockedKey&&game.board.Moves==lockedMoves&&game.AttemptSeenCount==lockedSeen&&game.UndoRemaining==undo&&game.ShuffleRemaining==shuffle,"A locked input mutated runtime state");
                yield return Settle();Require(game.FailureVisible,"Failure panel did not become visible after presentation settled");
                string panelCopy=game.FailurePanelCopy,removedHint="当前局面"+"无法继续推进";Require(panelCopy.Contains("本关失败")&&panelCopy.Contains("第 5 关")&&panelCopy.Contains("重试本关")&&panelCopy.Contains("返回主页"),"Failure panel lost required copy: "+panelCopy);Require(!panelCopy.Contains(removedHint),"Removed failure hint is still present in runtime panel copy");

                // Retry, explicit load, and home/start each create a fresh seen set.
                string initial=Rules.Key(game.board.Level,Rules.Initial(game.board.Level));game.ResetLevel();Require(!game.FailureLocked&&game.board.Moves==0&&game.AttemptSeenCount==1&&Rules.Key(game.board.Level,game.board.Current)==initial,"Retry did not restore a fresh initial attempt");yield return Ready();
                game.LoadLevel(1);Require(game.AttemptSeenCount==1&&game.board.Moves==0,"Load did not isolate attempt history");yield return Ready();
                PlayerPrefs.SetInt(CampaignProgress.StorageKey,2);PlayerPrefs.Save();game.ReturnHome();yield return Settle();Require(game.Home&&game.levelIndex==2&&game.AttemptSeenCount==1,"Home did not isolate history or reload progress");game.StartGame();Require(!game.Home&&game.AttemptSeenCount==1,"Start did not create a fresh attempt");yield return Ready();

                // Tutorial and ordinary victories persist exactly once even if re-evaluated.
                PlayerPrefs.SetInt(CampaignProgress.StorageKey,0);PlayerPrefs.Save();yield return PlaySolutionUntilFinalMove(0);Require(CampaignProgress.CurrentIndex(game.catalog.levels.Length)==1,"Tutorial victory did not advance once");Evaluate(game);Require(CampaignProgress.CurrentIndex(game.catalog.levels.Length)==1,"Tutorial victory advanced more than once");
                PlayerPrefs.SetInt(CampaignProgress.StorageKey,4);PlayerPrefs.Save();yield return PlaySolutionUntilFinalMove(4);Require(CampaignProgress.CurrentIndex(game.catalog.levels.Length)==5,"Ordinary victory did not advance once");Evaluate(game);Require(CampaignProgress.CurrentIndex(game.catalog.levels.Length)==5,"Ordinary victory advanced more than once");

                PlayerPrefs.SetInt(CampaignProgress.StorageKey,6);PlayerPrefs.Save();game.ReturnHome();yield return Settle();Require(game.Home&&game.levelIndex==6,"Reinitialized home did not read saved progress");
                File.WriteAllText(Path.Combine(output,"runtime-verification.json"),"{\"passed\":true,\"failureLocksImmediately\":true,\"panelWaitsForBusy\":true,\"failureCopy\":\"required-only\",\"removedHintAbsent\":true,\"lockedInputsStable\":true,\"attemptIsolation\":true,\"tutorialProgressOnce\":true,\"normalProgressOnce\":true,\"homeReloadsProgress\":true,\"storageKey\":\"isolated-verification\"}");
            Debug.Log("FAILURE PROGRESS RUNTIME VERIFICATION PASSED");Cleanup();Application.Quit(0);
        }
        void Cleanup(){PlayerPrefs.DeleteKey(CampaignProgress.StorageKey);PlayerPrefs.Save();Active=false;}
    }
}
