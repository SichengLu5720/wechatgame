using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
using Debug = UnityEngine.Debug;

// Same observations before and after TASK-004. Reports are evidence, never source data.
public static class NavigationDataVerification
{
    [Serializable] public sealed class Case
    {
        public string id, geometry, solvedState, error;
        public int hash, nodes, stairs, routes, moves, samples;
        public double geometryMs, routeMs, replayMs;
        public float duration;
    }
    [Serializable] public sealed class Report { public bool passed; public Case[] cases; public long resourceBytes; }
    public static IEnumerable<KeyValuePair<string,LevelSpec>> Levels(bool delivered=false)
    {
        var actual=CampaignRepository.LoadOriginal();var tutorial=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("tutorial-v1").text);
        for(int i=0;i<tutorial.levels.Length;i++)actual.levels[i]=tutorial.levels[i];
        if(delivered)actual=CampaignRepository.Load();
        var daily=delivered?new Catalog{levels=DailyChallengeRepository.Pool.levels.Select(l=>LevelPresentation.Apply(l)).ToArray()}:DailyChallengeRepository.Pool;
        var sets = new[] { actual, CampaignRepository.LoadOriginal(),
            JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text), daily };
        var names = new[] { "actual", "original", "legacy", "daily" };
        for (int s=0;s<sets.Length;s++) for(int i=0;i<sets[s].levels.Length;i++)
            yield return new KeyValuePair<string,LevelSpec>(names[s]+"-"+i.ToString("00"),sets[s].levels[i]);
    }
    public static string GeometryObservation(WalkSpace space)
    {
        using(var data=new MemoryStream()) using(var w=new BinaryWriter(data))
        {
            foreach(var p in space.Centers){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
            foreach(var s in space.Stairs){w.Write(s.a);w.Write(s.b);w.Write(s.index);w.Write(s.width);w.Write(s.steps);
                foreach(var p in new[]{s.start,s.end}){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
                w.Write(s.polygon==null?0:s.polygon.Length);
                if(s.polygon!=null)foreach(var p in s.polygon){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
                for(int i=0;i<=20;i++){var p=Vector3.Lerp(s.start,s.end,i/20f);float h;w.Write(s.Height(new Vector2(p.x,p.z),out h));w.Write(h);}
            }
            for(int n=0;n<space.Centers.Length;n++)for(int row=0;row<4;row++)for(int m=0;m<4;m++){
                var p=space.Seat(n,row,m);w.Write(p.x);w.Write(p.y);w.Write(p.z);
            }
            w.Flush();using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data.ToArray())).Replace("-","");
        }
    }
    public static Case Check(string id,LevelSpec level)
    {
        var result=new Case{id=id};var timer=Stopwatch.StartNew();
        try
        {
            level.Validate();var space=new WalkSpace(level,null,true);
            result.geometryMs=timer.Elapsed.TotalMilliseconds;result.hash=space.GeometryHash();result.geometry=GeometryObservation(space);
            result.nodes=space.Centers.Length;result.stairs=space.Stairs.Length;timer.Restart();
            foreach(var edge in level.edges)foreach(bool reverse in new[]{false,true}){
                int a=reverse?edge.b:edge.a,b=reverse?edge.a:edge.b;
                var path=Navigator.Find(space,new[]{space.Centers[a]+Vector3.up*WalkSpace.FootGap},0,space.Centers[b]+Vector3.up*WalkSpace.FootGap,space.RouteMask(new[]{a,b}));
                if(path.Count==0)throw new Exception("Empty edge path");result.routes++;
            }
            result.routeMs=timer.Elapsed.TotalMilliseconds;timer.Restart();var board=new Board(level);
            foreach(var action in level.solution){
                var move=Rules.Preview(level,board.Current,action.a,action.b);if(!move.ok)throw new Exception("Illegal witness: "+move.reason);
                var plan=FastMovement.Build(space,board.Current,move);
                foreach(var track in plan.tracks)for(float t=track.start;t<track.End;t+=.13f){
                    var expected=plan.SupportedPosition(space,track.actor,t);var actual=track.PlaybackPosition(t);
                    if(Mathf.Abs(actual.y-expected.y)>.001f)throw new Exception("Playback floor differs actor="+track.actor+" t="+t+" actual="+actual.y+" expected="+expected.y);
                    result.samples++;
                }
                var before=Rules.Key(level,board.Current);board.TryMove(action.a,action.b,plan.finalSlots);board.Undo();
                if(Rules.Key(level,board.Current)!=before)throw new Exception("Undo changed state");
                board.TryMove(action.a,action.b,plan.finalSlots);result.duration+=plan.duration;result.moves++;
            }
            if(!board.Solved)throw new Exception("Witness not solved");result.solvedState=Rules.Key(level,board.Current);board.Reset();
            if(board.Moves!=0||board.CanUndo)throw new Exception("Reset failed");result.replayMs=timer.Elapsed.TotalMilliseconds;
        }
        catch(Exception e){result.error=e.ToString();}
        return result;
    }
    public static void Matrix()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-navigationReport");
        string folder=at>=0&&at+1<args.Length?args[at+1]:"artifacts/task004/after";Directory.CreateDirectory(folder);
        var list=new List<Case>();
        foreach(var entry in Levels()){
            var c=Check(entry.Key,entry.Value);list.Add(c);Debug.Log("NAV MATRIX "+c.id+" routes="+c.routes+" moves="+c.moves+" "+(c.error??"PASS"));
            File.WriteAllText(Path.Combine(folder,"matrix.json"),JsonUtility.ToJson(new Report{passed=list.All(x=>x.error==null),cases=list.ToArray(),resourceBytes=Directory.GetFiles("Assets/Resources","*.bytes",SearchOption.AllDirectories).Where(p=>p.Contains("navigation")).Sum(p=>new FileInfo(p).Length)},true));
        }
        EditorApplication.Exit(list.All(x=>x.error==null)?0:1);
    }
}
