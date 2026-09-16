using System;
using System.IO;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class RevisionVerification
{
    public static void VerifyAndBuild(){try{Run();UnityBuild.Build();}catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}}
    public static void Run()
    {
        var levels=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text).levels;
        int joins=0,seats=0,moves=0;float clearance=100;
        foreach(var level in levels){
            var space=new WalkSpace(level);var board=new Board(level);
            foreach(var stair in space.Stairs.Where(s=>s.polygon!=null)){
                var middle=(stair.polygon[0]+stair.polygon[1])/2;int node=Vector3.Distance(middle,space.Centers[stair.a])<Vector3.Distance(middle,space.Centers[stair.b])?stair.a:stair.b;
                var delta=middle-space.Centers[node];if(Mathf.Abs(delta.magnitude-(space.Half(node)-.045f))>.001f)throw new Exception("Dock not at side center");
                var normal=delta.normalized;var edge=stair.polygon[1]-stair.polygon[0];
                if(Mathf.Abs(Vector3.Dot(normal,edge.normalized))>.001f)throw new Exception("Landing not flush with platform side");
                if(Vector3.Dot(stair.Direction,normal)<.999f)throw new Exception("Authored platform not aligned to connection");
                for(int i=0;i<2;i++)if(!space.Inside(node,new Vector2(stair.polygon[i].x,stair.polygon[i].z)))throw new Exception("Landing clips platform corner");
                joins++;
            }
            for(int n=0;n<level.nodes.Length;n++)for(int row=0;row<4;row++)for(int member=0;member<4;member++){
                var p=space.Seat(n,row,member);float floor;if(!space.FootHeight(new Vector2(p.x,p.z),out floor))throw new Exception("Sixteen-seat footprint unsupported");seats++;
            }
            MotionPlan timeline=null;float now=0;
            foreach(var action in level.solution){
                var move=Rules.Preview(level,board.Current,action.a,action.b);var next=MotionPlanner.Build(space,board.Current,move);
                var merged=MotionComposer.Append(timeline,now,next);
                if(timeline!=null)for(int actor=0;actor<merged.initial.Length;actor++)if(Vector3.Distance(timeline.Position(actor,now),merged.Position(actor,0))>.001f)throw new Exception("Concurrent click teleported a character");
                board.TryMove(action.a,action.b);timeline=merged;now=.04f;moves++;
            }
            clearance=Mathf.Min(clearance,timeline.MinimumClearance());
            for(float t=0;t<timeline.duration+.05f;t+=.027f)for(int actor=0;actor<timeline.initial.Length;actor++)timeline.SupportedPosition(space,actor,t);
            if(!board.Solved)throw new Exception("Queued solution failed");Debug.Log("CONCURRENT LEVEL PASSED "+level.name);
        }
        var transit=RevisionCases.Transit();var transitBoard=new Board(transit);var transitSpace=new WalkSpace(transit);
        var first=Rules.Preview(transit,transitBoard.Current,1,0);if(first.count!=2)throw new Exception("Transit did not accept eight people");transitBoard.TryMove(1,0);
        string key=Rules.Key(transit,transitBoard.Current);if(transitBoard.TryMove(1,0).ok||Rules.Key(transit,transitBoard.Current)!=key)throw new Exception("Transit mixed colors");
        transitBoard.TryMove(2,0);if(transitBoard.Current.queues[0].Count!=4||Rules.Complete(transit,transitBoard.Current,0)||Rules.Completed(transit,transitBoard.Current)!=0)throw new Exception("Full transit incorrectly locked/completed");
        if(transitBoard.TryMove(1,0).ok)throw new Exception("Transit exceeded sixteen people");
        var depart=Rules.Preview(transit,transitBoard.Current,0,3);if(!depart.ok||depart.count!=4)throw new Exception("Full transit cannot depart");
        var departPlan=MotionPlanner.Build(transitSpace,transitBoard.Current,depart);clearance=Mathf.Min(clearance,departPlan.MinimumClearance());
        File.WriteAllText("artifacts/revision-12.json","{\"passed\":true,\"sixteenPersonSeats\":"+seats+",\"flushSideJoins\":"+joins+",\"rapidQueuedMoves\":"+moves+",\"transitCapacity\":16,\"transitRemainsMovable\":true,\"mixedAndOverflowRejected\":true,\"minimumCenterDistance\":"+clearance.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
    }
}
