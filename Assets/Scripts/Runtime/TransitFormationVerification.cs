using System;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class TransitFormationVerification
    {
        public static void Run()
        {
            for(int count=1;count<=4;count++){
                var level=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,transit=true,queue=Enumerable.Range(0,count).ToArray()}},edges=new EdgeSpec[0],groups=Enumerable.Range(0,count).Select(i=>new GroupSpec{id=i,color=0}).ToArray()};
                var board=new Board(level);var space=new WalkSpace(level,null,true);var positions=space.Positions(board.Current);
                for(int a=0;a<positions.Length;a++){
                    float height;if(!space.FootHeight(new Vector2(positions[a].x,positions[a].z),out height))throw new Exception("Transit circle outside platform");
                    for(int b=0;b<a;b++)if(Vector3.Distance(positions[a],positions[b])<WalkSpace.Separation)throw new Exception("Transit circle crowd overlap");
                }
                var centroid=positions.Aggregate(Vector3.zero,(sum,p)=>sum+p)/positions.Length;
                if(Vector3.Distance(centroid,space.Centers[0]+Vector3.up*WalkSpace.FootGap)>.001f)throw new Exception("Transit circle not centered");
                if(count>1){level.groups[1].color=1;bool rejected=false;try{level.Validate();}catch(Exception){rejected=true;}if(!rejected)throw new Exception("Mixed transit generation accepted");}
            }
            var fixture=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0,1}},new NodeSpec{x=8,y=2,capacity=4,transit=true,queue=new[]{2}}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0},new GroupSpec{id=1,color=1},new GroupSpec{id=2,color=0}}};
            var active=new Board(fixture);var geometry=new WalkSpace(fixture,null,true);var before=geometry.Positions(active.Current);
            var move=Rules.Preview(fixture,active.Current,0,1);var plan=MotionPlanner.Build(geometry,active.Current,move);
            if(plan.MinimumClearance()<WalkSpace.Separation-.0002f)throw new Exception("Transit merge collision");
            active.TryMove(0,1,plan.finalSlots);if(Rules.Preview(fixture,active.Current,0,1).ok)throw new Exception("Different color entered occupied transit");
            if(Rules.MovableCount(fixture,active.Current,1)!=2)throw new Exception("Transit same-color crowd not selectable together");
            active.Undo();if(!before.SequenceEqual(geometry.Positions(active.Current)))throw new Exception("Transit undo formation mismatch");
            var partial=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,transit=true,queue=new[]{0,1,2}},new NodeSpec{x=8,y=2,capacity=4,queue=new[]{3,4,5}}},edges=fixture.edges,groups=Enumerable.Range(0,6).Select(i=>new GroupSpec{id=i,color=0}).ToArray()};
            var partialBoard=new Board(partial);var partialSpace=new WalkSpace(partial,null,true);var partialMove=Rules.Preview(partial,partialBoard.Current,0,1);
            var partialPlan=MotionPlanner.Build(partialSpace,partialBoard.Current,partialMove);if(partialMove.count!=1||partialPlan.MinimumClearance()<WalkSpace.Separation-.0002f)throw new Exception("Transit partial departure failed");
            partialBoard.TryMove(0,1,partialPlan.finalSlots);if(!partialPlan.final.SequenceEqual(partialSpace.Positions(partialBoard.Current)))throw new Exception("Transit assigned slots were not committed");
            for(float t=0;t<partialPlan.duration;t+=.04f)for(int id=0;id<partialPlan.initial.Length;id++)partialPlan.SupportedPosition(partialSpace,id,t);
            var imported=LevelShare.Decode(LevelShare.Encode(partial));imported.Validate();
            partial.groups[1].color=1;bool exportRejected=false;try{LevelShare.Encode(partial);}catch(Exception){exportRejected=true;}if(!exportRejected)throw new Exception("Mixed transit share accepted");
            Debug.Log("TRANSIT FORMATION PASS: 4/8/12/16, footprint, separation, monochrome, merge, undo");
        }
    }
}
