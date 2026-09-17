using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class DailyChallengeVerification
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static int Mixed(LevelSpec l){int count=0;foreach(var n in l.nodes)for(int i=1;i<n.queue.Length;i++)if(l.groups[n.queue[i-1]].color!=l.groups[n.queue[i]].color)count++;return count;}
    public static void Generate()
    {
        try {
            // New queues on the already authored geometry of level 11. Never uses levels 15+.
            var source=CampaignRepository.Load().levels[10];var accepted=new List<LevelSpec>();
            Directory.CreateDirectory("artifacts/navigation/daily");
            for(int seed=200200;seed<200400&&accepted.Count<7;seed++){
                var l=LevelShare.Copy(source);var random=new System.Random(seed);var colors=Enumerable.Range(0,6).SelectMany(c=>Enumerable.Repeat(c,4)).ToArray();
                for(int i=colors.Length-1;i>0;i--){int j=random.Next(i+1),c=colors[i];colors[i]=colors[j];colors[j]=c;}
                for(int i=0;i<l.groups.Length;i++)l.groups[i].color=colors[i];
                if(Mixed(l)<15)continue;
                var result=OmniscientSearch.Find(l,80000,15000);if(result.status!="SolvedFound"||result.moves.Length<24)continue;
                l.solution=result.moves;l.name="每日挑战";l.tip="";
                l.provenance=new LevelProvenance{id="daily-v1-"+accepted.Count.ToString("00"),generatorVersion="daily-offline-1",template=source.provenance.template,difficulty="daily",seed=seed,visited=result.visited,solutionMoves=result.moves.Length,mixedBoundaries=Mixed(l),solverStatus=result.status,reviewStatus="Human Check Pending"};
                try{Replay(l);}catch(Exception e){Debug.Log("DAILY REJECT "+seed+" "+e.Message);continue;}
                File.WriteAllBytes("artifacts/navigation/daily/"+l.provenance.id+".bytes",NavigationFactory.ForOfflineValidation(l).Bake());accepted.Add(l);Debug.Log("DAILY ACCEPT "+l.provenance.id+" seed="+seed+" moves="+result.moves.Length+" mixed="+Mixed(l));
            }
            Require(accepted.Count==7,"Not enough daily candidates");
            File.WriteAllText("Assets/Resources/daily-v1.json",JsonUtility.ToJson(new Catalog{levels=accepted.ToArray()},true));AssetDatabase.Refresh();
            Debug.Log("DAILY GENERATION PASSED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static int Replay(LevelSpec l)
    {
        l.Validate();var board=new Board(l);var space=NavigationFactory.Create(l);LayoutSafety.Validate(space);
        var seen=new HashSet<string>{Rules.Key(l,board.Current)};int count=0;
        foreach(var action in l.solution){
            Require(AttemptFailure.Classify(l,board.Current,seen)==AttemptOutcome.Continue,"Witness hits failure "+count);
            var move=Rules.Preview(l,board.Current,action.a,action.b);Require(move.ok,"Illegal witness");
            var plan=FastMovement.Build(space,board.Current,move);
            for(float t=0;t<plan.duration;t+=.13f)foreach(var track in plan.tracks)plan.SupportedPosition(space,track.actor,t);
            string before=Rules.Key(l,board.Current);board.TryMove(action.a,action.b,plan.finalSlots);board.Undo();Require(Rules.Key(l,board.Current)==before,"Undo changed state");board.TryMove(action.a,action.b,plan.finalSlots);seen.Add(Rules.Key(l,board.Current));count++;
        }
        Require(board.Solved,"Witness not solved");board.Reset();Require(board.Moves==0&&!board.CanUndo,"Reset failed");return count;
    }
    public static void Verify()
    {
        try {
            Require(DailyChallengeProgress.Verification,"Pass -dailyverify to isolate test storage");
            double time=0;var c=new DailyChallengeClock(()=>time);time=2000;c.Tick();Require(c.Remaining==180&&!c.Started,"Observation consumed time");c.Interact();time+=12;c.Tick();Require(c.Remaining==168,"Elapsed time");
            c.SetPause(DailyPause.Settings,true);time+=100;c.SetPause(DailyPause.Application,true);c.SetPause(DailyPause.Settings,false);time+=500;c.Tick();Require(c.Remaining==168,"Overlap pause");c.SetPause(DailyPause.Application,false);time+=1;c.Tick();Require(c.Remaining==167,"Resume charged background");time+=167;c.Tick();Require(c.Result==DailyResult.Failed&&!c.Interact(),"Deadline not terminal");c.Win();Require(c.Result==DailyResult.Failed,"Late win overturned failure");
            c=new DailyChallengeClock(()=>time);c.Interact();time+=179.9;c.Tick();c.Win();time+=999;c.Tick();Require(c.Result==DailyResult.Won&&c.Remaining>0,"Animation overturned win");
            foreach(var date in new[]{new DateTime(2024,2,29),new DateTime(2025,2,28),new DateTime(2026,8,31),new DateTime(2026,12,31),new DateTime(2027,1,1)}){
                string key=DailyChallengeProgress.StorageKey(date);PlayerPrefs.DeleteKey(key);Require(DailyChallengeProgress.CanStart(date,date),"Today rejected");Require(!DailyChallengeProgress.CanStart(date.AddDays(-1),date)&&!DailyChallengeProgress.CanStart(date.AddDays(1),date),"Past/future accepted");DailyChallengeProgress.Complete(date);DailyChallengeProgress.Complete(date);Require(DailyChallengeProgress.Completed(date)&&!DailyChallengeProgress.CanStart(date,date),"Replay accepted");PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();Require(DailyChallengeProgress.MonthRows(new DateTime(2026,8,1))==6,"Six row month failed");
            var pool=DailyChallengeRepository.Pool;int moves=0;foreach(var l in pool.levels)moves+=Replay(l);
            for(int i=0;i<370;i++){var day=new DateTime(2026,1,1).AddDays(i);Require(DailyChallengeRepository.Index(day,pool.levels.Length)==DailyChallengeRepository.Index(day.AddHours(23),pool.levels.Length),"Date mapping unstable");}
            var baseline=CampaignRepository.Load().levels.Take(14).Select((l,i)=>new Proxy{level=i+1,moves=l.solution.Length,mixed=Mixed(l),visited=l.provenance.visited}).ToArray();
            Directory.CreateDirectory("artifacts/task002");File.WriteAllText("artifacts/task002/offline-verification.json",JsonUtility.ToJson(new Report{passed=true,moves=moves,poolSize=pool.levels.Length,baseline=baseline,candidates=pool.levels.Select(l=>new Proxy{moves=l.solution.Length,mixed=Mixed(l),visited=l.provenance.visited}).ToArray()},true));
            Debug.Log("DAILY VERIFICATION PASSED pool="+pool.levels.Length+" moves="+moves);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    [Serializable] public class Proxy{public int level,moves,mixed,visited;}
    [Serializable] public class Report{public bool passed;public int moves,poolSize;public Proxy[] baseline,candidates;}
}
