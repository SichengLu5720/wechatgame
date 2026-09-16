using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    // Sorting reserves platform capacity immediately. Characters are visual followers,
    // not physical obstacles. Their fixed floor routes need no per-person path search.
    public static class FastMovement
    {
        public const float SpeedMultiplier=.85f;
        sealed class Corridors {public readonly Dictionary<int,Vector3[]> edges=new Dictionary<int,Vector3[]>();}
        static readonly ConditionalWeakTable<WalkSpace,Corridors> layouts=new ConditionalWeakTable<WalkSpace,Corridors>();
        static Corridors Compile(WalkSpace space)
        {
            var result=new Corridors();
            foreach(var stair in space.Stairs)if(stair.polygon==null){
                result.edges[stair.index]=new[]{space.Centers[stair.a]+Vector3.up*WalkSpace.FootGap,space.Lift(new Vector2(stair.start.x,stair.start.z)),space.Lift(new Vector2(stair.end.x,stair.end.z)),space.Centers[stair.b]+Vector3.up*WalkSpace.FootGap};
            }return result;
        }
        internal static void Forget(WalkSpace space){layouts.Remove(space);PlaybackFloor.Forget(space);}
        public static void Prepare(WalkSpace space){layouts.GetValue(space,Compile);PlaybackFloor.For(space);}
        public static MotionPlan Build(WalkSpace space,State before,Move move)
        {
            if(!move.ok)throw new ArgumentException("Invalid move");var corridors=layouts.GetValue(space,Compile);var after=Rules.Apply(before,move);
            after.actorSlots=after.actorSlots??new int[space.Level.groups.Length*4];
            for(int n=0;n<after.queues.Length;n++)for(int row=0;row<after.queues[n].Count;row++)for(int member=0;member<4;member++){
                int id=after.queues[n][row]*4+member;
                if(space.Level.nodes[n].transit||move.ids.Contains(after.queues[n][row])||before.actorSlots==null)after.actorSlots[id]=row*4+member;
            }
            var plan=new MotionPlan{move=move,initial=space.Positions(before),final=space.Positions(after),finalSlots=after.actorSlots};
            var spine=new List<Vector3>();
            for(int i=1;i<move.path.Length;i++){
                int a=move.path[i-1],b=move.path[i];int edge=Array.FindIndex(space.Level.edges,e=>e.a==a&&e.b==b||e.a==b&&e.b==a);
                Vector3[] route;if(edge<0||!corridors.edges.TryGetValue(edge,out route))throw new Exception("Missing floor connection");
                if(space.Level.edges[edge].a==a)spine.AddRange(route);else spine.AddRange(route.Reverse());
            }
            var travelers=new HashSet<int>(move.ids.SelectMany(g=>Enumerable.Range(g*4,4)));
            for(int actor=0;actor<plan.initial.Length;actor++){
                if(Vector3.Distance(plan.initial[actor],plan.final[actor])<.0001f)continue;
                var points=new List<Vector3>{plan.initial[actor]};if(travelers.Contains(actor)){
                    float lane=(actor%4-1.5f)*.28f;
                    for(int i=0;i<spine.Count;i++){var direction=Vector3.ProjectOnPlane(spine[Mathf.Min(i+1,spine.Count-1)]-spine[Mathf.Max(0,i-1)],Vector3.up).normalized;points.Add(spine[i]+Vector3.Cross(Vector3.up,direction)*lane);}
                }points.Add(plan.final[actor]);
                for(int i=points.Count-1;i>0;i--)if(Vector3.Distance(points[i],points[i-1])<.0001f)points.RemoveAt(i);
                var track=ActorTrack.Create(actor,points);
                if(travelers.Contains(actor))track.start=Array.IndexOf(move.ids,actor/4)*.08f+(actor%4)*.008f;
                else{float scale=.22f/track.duration;for(int i=0;i<track.times.Length;i++)track.times[i]*=scale;track.duration=.22f;}
                plan.tracks.Add(track);plan.duration=Mathf.Max(plan.duration,track.End);
            }
            if(plan.duration>1.4f){float scale=1.4f/plan.duration;foreach(var track in plan.tracks){track.start*=scale;track.duration*=scale;for(int i=0;i<track.times.Length;i++)track.times[i]*=scale;}plan.duration=1.4f;}
            foreach(var track in plan.tracks){track.start/=SpeedMultiplier;track.duration/=SpeedMultiplier;for(int i=0;i<track.times.Length;i++)track.times[i]/=SpeedMultiplier;}
            plan.duration/=SpeedMultiplier;plan.PreparePlayback(space);return plan;
        }
    }
}
