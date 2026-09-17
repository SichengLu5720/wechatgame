using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
using Debug=UnityEngine.Debug;

public static class InMotionFormationVerification
{
    [Serializable] public sealed class Observation
    {
        public string id,error,key; public int moves,samples,residentTracks,travelerCount; public float duration,clearance=100,launchSpan;
        public double planningMs; public long playbackBytes; public ActorTrack[] tracks; public Vector3[] initial,final;
        public int heightMismatches,forwardDepartures; public float maxHeightDifference; public List<string> heightNotes=new List<string>();
    }
    [Serializable] public sealed class Report { public bool passed; public bool baseline; public int boundarySamples; public long boundaryBytes; public float maximumBoundaryHeightError; public List<Observation> cases=new List<Observation>(); }
    static bool baseline; static string folder;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Inspect(WalkSpace space,Board board,Move move,Observation result,bool details=false)
    {
        var before=board.Current.Clone();var timer=Stopwatch.StartNew();var plan=FastMovement.Build(space,before,move);result.planningMs+=timer.Elapsed.TotalMilliseconds;
        Require(plan.final.Length==space.Level.groups.Length*4,"actor count");
        var residents=before.queues[move.to].SelectMany(g=>Enumerable.Range(g*4,4)).ToArray();
        foreach(int id in residents)if(plan.Track(id)!=null||plan.initial[id]!=plan.final[id])result.residentTracks++;
        if(!baseline)Require(result.residentTracks==0,"destination resident moved");
        foreach(var track in plan.tracks)for(float t=0;t<track.End+.02f;t+=.045f){
            var actual=track.PlaybackPosition(t);var supported=plan.SupportedPosition(space,track.actor,t);
            float difference=Mathf.Abs(actual.y-supported.y);
            if(difference>.001f){result.heightMismatches++;result.maxHeightDifference=Mathf.Max(result.maxHeightDifference,difference);if(result.heightNotes.Count<10)result.heightNotes.Add("actor="+track.actor+" t="+t+" end="+track.End+" actual="+actual+" supported="+supported);}result.samples++;
        }
        result.clearance=Mathf.Min(result.clearance,plan.MinimumClearance());result.duration+=plan.duration;result.moves++;
        var arrivals=move.ids.SelectMany(g=>Enumerable.Range(g*4,4)).Select(plan.Track).ToArray();
        result.travelerCount=arrivals.Length;result.launchSpan=arrivals.Max(t=>t.start)-arrivals.Min(t=>t.start);
        foreach(var track in arrivals){Require(track!=null,"missing traveler");for(int i=1;i<track.times.Length;i++)Require(track.times[i]>track.times[i-1],"stationary wait waypoint");}
        var firstStair=space.Stairs.First(s=>s.polygon==null&&(s.a==move.path[0]&&s.b==move.path[1]||s.b==move.path[0]&&s.a==move.path[1]));
        var exit=(firstStair.a==move.from?firstStair.start:firstStair.end)-space.Centers[move.from];exit.y=0;
        foreach(var track in arrivals){float forward=Vector3.Dot(track.points[1]-track.points[0],exit.normalized);if(forward>0)result.forwardDepartures++;if(!baseline)Require(forward>0,"departure initially moves away from the exit");}
        if(!baseline){Require(result.launchSpan<=.18f,"crowd launches as separate waves");var repeated=FastMovement.Build(space,before,move);Require(repeated.finalSlots.SequenceEqual(plan.finalSlots),"nondeterministic vacancy assignment");}
        for(int k=0;k<3;k++)foreach(var track in plan.tracks)track.PlaybackPosition(plan.duration*.4f);
        long bytes=GC.GetAllocatedBytesForCurrentThread();for(int k=0;k<120;k++)foreach(var track in plan.tracks)track.PlaybackPosition(plan.duration*(k/120f));result.playbackBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        if(details){result.tracks=plan.tracks.ToArray();result.initial=plan.initial;result.final=plan.final;}
        var expected=Rules.Apply(before,move);board.TryMove(move.from,move.to,plan.finalSlots);
        Require(Rules.Key(space.Level,board.Current)==Rules.Key(space.Level,expected),"logical state differs");
        for(int n=0;n<expected.queues.Length;n++)Require(expected.queues[n].SequenceEqual(board.Current.queues[n]),"queue order differs");
        Require(expected.revealed.SequenceEqual(board.Current.revealed),"reveal differs");
        Require(space.Positions(board.Current).SequenceEqual(plan.final),"committed slots differ");
        board.Undo();Require(space.Positions(board.Current).SequenceEqual(plan.initial),"undo positions differ");
        board.TryMove(move.from,move.to,plan.finalSlots);result.key=Rules.Key(space.Level,board.Current);
    }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();baseline=Array.IndexOf(args,"-formationBaseline")>=0;int at=Array.IndexOf(args,"-formationOutput");folder=at>=0?args[at+1]:"artifacts/task007/after";Directory.CreateDirectory(folder);
        var report=new Report{baseline=baseline};
        foreach(var entry in NavigationDataVerification.Levels(true)){
            var r=new Observation{id=entry.Key};report.cases.Add(r);
            try{var space=new WalkSpace(entry.Value,null,true);var board=new Board(entry.Value);
                foreach(var action in entry.Value.solution){var move=Rules.Preview(entry.Value,board.Current,action.a,action.b);Require(move.ok,"witness invalid");Inspect(space,board,move,r);}
                Require(board.Solved,"witness unsolved");var start=space.Positions(Rules.Initial(entry.Value));board.Reset();Require(start.SequenceEqual(space.Positions(board.Current))&&!board.CanUndo&&board.Moves==0,"reset mismatch");
            }catch(Exception e){r.error=e.ToString();}Debug.Log("FORMATION "+r.id+" "+(r.error??"PASS"));
        }
        for(int groups=1;groups<=4;groups++)for(int residents=0;residents<4;residents++){
            var r=new Observation{id="crowd-"+groups*4+"-residents-"+residents*4};report.cases.Add(r);
            try{var level=InMotionFormationFixtures.Create(groups,residents);var space=new WalkSpace(level,null,true);var board=new Board(level);Inspect(space,board,Rules.Preview(level,board.Current,0,1),r,true);
                // Full return, repeated arrivals, and turn through the empty landing.
                Inspect(space,board,Rules.Preview(level,board.Current,1,2),r);Inspect(space,board,Rules.Preview(level,board.Current,2,0),r);
            }catch(Exception e){r.error=e.ToString();}Debug.Log("FORMATION "+r.id+" "+(r.error??"PASS"));
        }
        if(!baseline){
            foreach(var r in report.cases)if(r.heightMismatches!=0||r.playbackBytes!=0)r.error=(r.error??"")+" Height mismatch or playback allocation";
            try{Boundaries(report);}catch(Exception e){report.cases.Add(new Observation{id="exact-floor-boundaries",error=e.ToString()});}
        }
        report.passed=report.cases.All(c=>c.error==null);File.WriteAllText(Path.Combine(folder,"formation.json"),JsonUtility.ToJson(report,true));
        Debug.Log("FORMATION "+(report.passed?"PASSED":"FAILED"));if(report.passed&&Array.IndexOf(args,"-formationBuildOnce")>=0)Build();else EditorApplication.Exit(report.passed?0:1);
    }
    static void Boundaries(Report report)
    {
        var level=InMotionFormationFixtures.Create(4,0);var space=new WalkSpace(level,null,true);var before=Rules.Initial(level);var plan=FastMovement.Build(space,before,Rules.Preview(level,before,0,1));
        foreach(var track in plan.tracks)for(int segment=1;segment<track.points.Length;segment++){
            var profile=PlaybackFloor.For(space).Segment(track.points[segment-1],track.points[segment]);
            foreach(float boundary in profile.times)foreach(float offset in new[]{-.000001f,0,.000001f}){
                float u=boundary+offset;if(u<=0||u>=1)continue;
                float clock=track.start+Mathf.Lerp(track.times[segment-1],track.times[segment],u);
                var actual=track.PlaybackPosition(clock);var supported=plan.SupportedPosition(space,track.actor,clock);
                report.maximumBoundaryHeightError=Mathf.Max(report.maximumBoundaryHeightError,Mathf.Abs(actual.y-supported.y));
                long bytes=GC.GetAllocatedBytesForCurrentThread();for(int repeat=0;repeat<8;repeat++)track.PlaybackPosition(clock);report.boundaryBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;report.boundarySamples++;
            }
        }
        Require(report.boundarySamples>0&&report.maximumBoundaryHeightError<.001f&&report.boundaryBytes==0,"exact boundary height or allocation regression");
    }
    public static void Continuations()
    {
        try{
            var level=InMotionFormationFixtures.Create(2,2);level.nodes[2].queue=level.nodes[1].queue;level.nodes[1].queue=new int[0];
            var space=new WalkSpace(level,null,true);var board=new Board(level);var original=space.Positions(board.Current);
            var first=FastMovement.Build(space,board.Current,Rules.Preview(level,board.Current,0,1));board.TryMove(0,1,first.finalSlots);
            var next=FastMovement.Build(space,board.Current,Rules.Preview(level,board.Current,2,1));
            foreach(int id in first.move.ids.SelectMany(g=>Enumerable.Range(g*4,4)))Require(next.Track(id)==null,"new plan relocates in-flight resident");
            float clock=first.duration*.27f;var combined=MotionComposer.Append(first,clock,next);combined.PreparePlayback(space);
            int checks=0;
            foreach(int id in first.move.ids.SelectMany(g=>Enumerable.Range(g*4,4)))for(float t=0;t<first.duration-clock;t+=.037f){
                Require(Vector3.Distance(combined.Position(id,t),first.Position(id,t+clock))<.001f,"resident continuation changed");checks++;
            }
            board.TryMove(2,1,next.finalSlots);Require(board.Current.queues[1].Count==4,"append did not fill capacity");
            var outgoing=FastMovement.Build(space,board.Current,Rules.Preview(level,board.Current,1,0));
            var chained=MotionComposer.Append(combined,.08f,outgoing);chained.PreparePlayback(space);
            foreach(var track in chained.tracks)for(float t=.01f;t<track.End;t+=.07f){var p=track.PlaybackPosition(t);float height;Require(space.FootHeight(new Vector2(p.x,p.z),out height),"queued floor support");checks++;}
            board.TryMove(1,0,outgoing.finalSlots);Require(space.Positions(board.Current).SequenceEqual(chained.final),"chained final slots");
            while(board.Undo()){}Require(original.SequenceEqual(space.Positions(board.Current)),"chained undo initial positions");
            var valid=first.finalSlots;foreach(int invalid in new[]{-1,16}){var bad=(int[])valid.Clone();bad[first.move.ids[0]*4]=invalid;bool rejected=false;try{board.TryMove(0,1,bad);}catch(ArgumentException){rejected=true;}Require(rejected&&board.Moves==0,"invalid slot accepted or mutated board");}
            Directory.CreateDirectory("artifacts/task007/after");File.WriteAllText("artifacts/task007/after/continuations.json","{\"passed\":true,\"checks\":"+checks+"}");Debug.Log("FORMATION CONTINUATIONS PASSED "+checks);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Build()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationBuildPath");string output=at>=0?args[at+1]:"Builds/Task007/StairsCrowd.exe";
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
}
