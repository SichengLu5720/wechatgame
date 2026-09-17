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
            var initial=space.Positions(before);
            var spine=new List<Vector3>();
            for(int i=1;i<move.path.Length;i++){
                int a=move.path[i-1],b=move.path[i];int edge=Array.FindIndex(space.Level.edges,e=>e.a==a&&e.b==b||e.a==b&&e.b==a);
                Vector3[] route;if(edge<0||!corridors.edges.TryGetValue(edge,out route))throw new Exception("Missing floor connection");
                if(space.Level.edges[edge].a==a)spine.AddRange(route);else spine.AddRange(route.Reverse());
            }
            for(int i=spine.Count-1;i>0;i--)if(Vector3.Distance(spine[i],spine[i-1])<.0001f)spine.RemoveAt(i);
            var exitDirection=Vector3.ProjectOnPlane(spine[1]-spine[0],Vector3.up).normalized;
            var exitSide=Vector3.Cross(Vector3.up,exitDirection);
            var entryDirection=Vector3.ProjectOnPlane(spine[spine.Count-1]-spine[spine.Count-2],Vector3.up).normalized;
            var entrySide=Vector3.Cross(Vector3.up,entryDirection);
            var lanes=new float[initial.Length];var entries=new Vector3[initial.Length];
            for(int actor=0;actor<initial.Length;actor++){
                // Preserve lateral order while narrowing towards the stair. Natural
                // differences in distance retain the crowd's front-to-back spacing.
                lanes[actor]=Mathf.Clamp(Vector3.Dot(initial[actor]-space.Centers[move.from],exitSide)*(.42f/(1.5f*WalkSpace.SeatSpacing)),-.42f,.42f);
                entries[actor]=space.Centers[move.to]-entryDirection*(space.Half(move.to)-WalkSpace.ActorRadius)+entrySide*lanes[actor]+Vector3.up*WalkSpace.FootGap;
            }
            TransitSlots.Assign(space,before,after,move,entries);
            var plan=new MotionPlan{move=move,initial=initial,final=space.Positions(after),finalSlots=after.actorSlots};
            var travelers=new HashSet<int>(move.ids.SelectMany(g=>Enumerable.Range(g*4,4)));
            for(int actor=0;actor<plan.initial.Length;actor++){
                if(Vector3.Distance(plan.initial[actor],plan.final[actor])<.0001f)continue;
                if(!travelers.Contains(actor))throw new InvalidOperationException("Resident presentation slot changed during a move");
                var points=new List<Vector3>{plan.initial[actor]};
                // Stay inside the convex platform until the narrow apron is reached.
                // Cutting directly to the stair's outer endpoint clips apron corners.
                points.Add(space.Centers[move.from]+exitDirection*(space.Half(move.from)-WalkSpace.ActorRadius)+exitSide*lanes[actor]+Vector3.up*WalkSpace.FootGap);
                // Omit both terminal platform centers. Departure simultaneously
                // advances and narrows; arrival goes directly from the stair to a
                // reserved vacancy, without collecting everyone in the center.
                for(int i=1;i<spine.Count-1;i++){var direction=Vector3.ProjectOnPlane(spine[i+1]-spine[i-1],Vector3.up).normalized;points.Add(spine[i]+Vector3.Cross(Vector3.up,direction)*lanes[actor]);}
                points.Add(entries[actor]);
                points.Add(plan.final[actor]);
                for(int i=points.Count-1;i>0;i--)if(Vector3.Distance(points[i],points[i-1])<.0001f)points.RemoveAt(i);
                var track=ActorTrack.Create(actor,points);
                track.start=Array.IndexOf(move.ids,actor/4)*.004f+(actor%4)*.001f;
                plan.tracks.Add(track);plan.duration=Mathf.Max(plan.duration,track.End);
            }
            bool naturalWalk=CharacterWalkTiming.Enabled;
            if(!naturalWalk&&plan.duration>1.4f){float scale=1.4f/plan.duration;foreach(var track in plan.tracks){track.start*=scale;track.duration*=scale;for(int i=0;i<track.times.Length;i++)track.times[i]*=scale;}plan.duration=1.4f;}
            foreach(var track in plan.tracks){track.start/=SpeedMultiplier;track.duration/=SpeedMultiplier;for(int i=0;i<track.times.Length;i++)track.times[i]/=SpeedMultiplier;}
            plan.duration/=SpeedMultiplier;if(naturalWalk)CharacterWalkTiming.Apply(plan);plan.PreparePlayback(space);return plan;
        }
    }
}
