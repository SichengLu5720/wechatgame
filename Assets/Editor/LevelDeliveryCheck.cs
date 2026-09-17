using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class LevelDeliveryCheck
{
    [Serializable] public sealed class Report {public bool passed;public int levels,moves,samples;public string error;}
    public static void Run()
    {
        var report=new Report();string folder="artifacts/level-delivery";
        try {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-deliveryFolder");if(i>=0)folder=args[i+1];
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(folder,"candidate.json")));
            foreach(var level in catalog.levels){
                CampaignValidator.Validate(level);
                var space=NavigationFactory.Create(level);LayoutSafety.Validate(space);FastMovement.Prepare(space);
                var board=new Board(level);
                foreach(var step in level.solution){
                    string before=Rules.Key(level,board.Current);
                    var move=Rules.Preview(level,board.Current,step.a,step.b);var plan=FastMovement.Build(space,board.Current,move);
                    foreach(var track in plan.tracks)for(int t=0;t<=32;t++){
                        var p=track.PlaybackPosition(track.start+track.duration*t/32f);float floor;
                        if(!space.FootHeight(new Vector2(p.x,p.z),out floor)||Mathf.Abs(p.y-floor-WalkSpace.FootGap)>.005f)throw new Exception(level.provenance.id+" unsupported route actor "+track.actor);
                        report.samples++;
                    }
                    if(!board.TryMove(step.a,step.b,plan.finalSlots).ok)throw new Exception("Runtime commit failed");
                    var positions=space.Positions(board.Current);
                    for(int actor=0;actor<positions.Length;actor++)if(Vector3.Distance(positions[actor],plan.final[actor])>.001f)throw new Exception("Final seat mismatch");
                    board.Undo();if(Rules.Key(level,board.Current)!=before)throw new Exception("Undo mismatch");board.TryMove(step.a,step.b,plan.finalSlots);report.moves++;
                }
                if(!board.Solved)throw new Exception("Incomplete witness");report.levels++;
            }
            report.passed=true;
        }catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"geometry-check.json"),JsonUtility.ToJson(report,true));
        EditorApplication.Exit(report.passed?0:1);
    }
}
