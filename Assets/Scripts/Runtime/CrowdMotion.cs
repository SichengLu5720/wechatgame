using SurfaceMask = StairsCrowd.Runtime.SurfaceSet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class ActorTrack
    {
        public int actor;internal int phase;public float start,duration;public Vector3[] points;public float[] times;
        public float End {get{return start+duration;}}
        PlaybackFloor.Profile[] floors;
        public void PrepareFloor(PlaybackFloor floor){floors=new PlaybackFloor.Profile[points.Length-1];for(int i=0;i<floors.Length;i++)floors[i]=floor.Segment(points[i],points[i+1]);}
        public Vector3 PlaybackPosition(float clock)
        {
            float t=clock-start;if(t<=0)return points[0];if(t>=duration)return points[points.Length-1];
            int lo=1,hi=times.Length-1;while(lo<hi){int mid=(lo+hi)/2;if(times[mid]<t)lo=mid+1;else hi=mid;}
            float u=(t-times[lo-1])/Mathf.Max(.00001f,times[lo]-times[lo-1]);var p=Vector3.Lerp(points[lo-1],points[lo],u);p.y=floors[lo-1].Height(u)+WalkSpace.FootGap;return p;
        }
        public Vector3 Position(float clock)
        {
            float t=clock-start;if(t<=0)return points[0];if(t>=duration)return points[points.Length-1];
            for(int i=1;i<times.Length;i++)if(t<=times[i])return Vector3.Lerp(points[i-1],points[i],(t-times[i-1])/Mathf.Max(.00001f,times[i]-times[i-1]));return points[points.Length-1];
        }
        public static ActorTrack Create(int actor,List<Vector3> path)
        {
            // Dense height samples lift the complete foot envelope ahead of risers.
            var times=new float[path.Count];for(int i=1;i<times.Length;i++)times[i]=times[i-1]+Vector3.Distance(path[i-1],path[i])/15.6f;
            return new ActorTrack{actor=actor,points=path.ToArray(),times=times,duration=Mathf.Max(.06f,times[times.Length-1])};
        }
    }
    public sealed class MotionPlan
    {
        public int[] finalSlots;public Vector3[] initial,final;public List<ActorTrack> tracks=new List<ActorTrack>();public float duration;public Move move;
        ActorTrack[] indexed;int indexedCount=-1;
        void Index(){if(indexedCount==tracks.Count)return;indexed=new ActorTrack[initial.Length];foreach(var track in tracks)indexed[track.actor]=track;indexedCount=tracks.Count;}
        public void PreparePlayback(WalkSpace space){Index();var floor=PlaybackFloor.For(space);foreach(var track in tracks)track.PrepareFloor(floor);}
        public Vector3 Position(int actor,float t){var track=Track(actor);return track==null?initial[actor]:track.Position(t);}
        public Vector3 SupportedPosition(WalkSpace space,int actor,float t){Vector3 p=Position(actor,t);float height;if(!space.FootHeight(new Vector2(p.x,p.z),out height))throw new Exception("Motion left walkable surface actor="+actor+" time="+t+" p="+p);p.y=height+WalkSpace.FootGap;return p;}
        public float MinimumClearance()
        {
            var lookup=new ActorTrack[initial.Length];foreach(var track in tracks)lookup[track.actor]=track;float min=float.PositiveInfinity;
            for(int a=0;a<initial.Length;a++)for(int b=a+1;b<initial.Length;b++)min=Mathf.Min(min,PairClearance(lookup[a],initial[a],lookup[b],initial[b],duration));return min;
        }
        public ActorTrack Track(int actor){Index();return indexed[actor];}
        public static float PairClearance(ActorTrack a,Vector3 pa,ActorTrack b,Vector3 pb,float end,float begin=0)
        {
            if(a==null&&b==null)return Vector2.Distance(new Vector2(pa.x,pa.z),new Vector2(pb.x,pb.z));
            if(a==null||b==null){var track=a??b;Vector3 fixedPoint=a==null?pa:pb;Vector2 point=new Vector2(fixedPoint.x,fixedPoint.z);float squared=float.PositiveInfinity;for(int i=1;i<track.points.Length;i++){var p=track.points[i-1];var q=track.points[i];squared=Mathf.Min(squared,WalkSpace.SegmentDistanceSquared(point,new Vector2(p.x,p.z),new Vector2(q.x,q.z)));}return Mathf.Sqrt(squared);}
            int ai=0,bi=0;float cursor=begin,min=float.PositiveInfinity;end=Mathf.Max(end,Mathf.Max(a.End,b.End));
            while(cursor<end-.000001f){
                while(ai<a.times.Length&&a.start+a.times[ai]<=cursor+.000001f)ai++;
                while(bi<b.times.Length&&b.start+b.times[bi]<=cursor+.000001f)bi++;
                float next=Mathf.Min(end,Mathf.Min(ai<a.times.Length?a.start+a.times[ai]:end,bi<b.times.Length?b.start+b.times[bi]:end));
                if(next<=cursor+.000001f)break;
                Vector3 a0=a.Position(cursor),a1=a.Position(next),b0=b.Position(cursor),b1=b.Position(next);
                Vector2 r0=new Vector2(a0.x-b0.x,a0.z-b0.z),r1=new Vector2(a1.x-b1.x,a1.z-b1.z),delta=r1-r0;
                float u=delta.sqrMagnitude<1e-10f?0:Mathf.Clamp01(-Vector2.Dot(r0,delta)/delta.sqrMagnitude);min=Mathf.Min(min,(r0+delta*u).magnitude);cursor=next;
            }return min;
        }
    }
    public static class MotionPlanner
    {
        static readonly int[][] MemberOrders=Permutations(new[]{1,2,0,3}).ToArray();
        static IEnumerable<int[]> Permutations(int[] values){if(values.Length==0){yield return new int[0];yield break;}foreach(int first in values)foreach(var rest in Permutations(values.Where(v=>v!=first).ToArray()))yield return new[]{first}.Concat(rest).ToArray();}
        public static MotionPlan Build(WalkSpace space,State before,Move move)
        {
            MotionPlan result=null;using(var work=BuildSteps(space,before,move,p=>result=p))while(work.MoveNext()){}return result;
        }
        public static IEnumerator<int> BuildSteps(WalkSpace space,State before,Move move,Action<MotionPlan> ready)
        {
            string key=string.Join("|",before.queues.Select(q=>string.Join(",",q)))+":"+move.from+":"+move.to+":"+move.count+":"+string.Join(",",move.path??new int[0]);
            key+=":"+string.Join(",",before.actorSlots??new int[0]);
            MotionPlan result;if(space.Plans.TryGetValue(key,out result)){ready(result);yield break;}
            using(var work=BuildUncachedSteps(space,before,move,p=>result=p))while(work.MoveNext())yield return 0;
            // Uniform time scaling preserves the validated paths and separation.
            // Longer serial maneuvers speed up together instead of feeling stalled.
            if(result.duration>1.8f){float scale=1.8f/result.duration;foreach(var track in result.tracks){track.start*=scale;track.duration*=scale;for(int i=0;i<track.times.Length;i++)track.times[i]*=scale;}result.duration*=scale;}
            if(space.Plans.Count>=768)space.Plans.Clear();space.Plans.TryAdd(key,result);ready(result);
        }
        static IEnumerator<int> Transfer(WalkSpace space,Vector3[] occupied,List<ActorTrack> order,int id,Vector3 goal,SurfaceMask mask)
        {
            if(Vector3.Distance(occupied[id],goal)<.005f)yield break;
            List<Vector3> path=null;using(var work=Navigator.FindSteps(space,occupied,id,goal,mask,p=>path=p))while(work.MoveNext())yield return 0;
            order.Add(ActorTrack.Create(id,path));occupied[id]=goal;yield return 0;
        }
        static IEnumerator<int> Reposition(WalkSpace space,Vector3[] occupied,List<ActorTrack> order,IEnumerable<int> actors,Vector3[] goals,int node)
        {
            var pending=actors.Where(id=>Vector3.Distance(occupied[id],goals[id])>.005f).ToList();
            var mask=space.NodeMask(node);var parked=new HashSet<string>();
            if(pending.Count>0)foreach(float spread in new[]{1f,1.3f,1.6f,2f,2.5f,3f}){
                // Validate synchronized settling, optionally opening the crowd a
                // little first so close neighbors can exchange positions safely.
                var waypoint=pending.ToDictionary(id=>id,id=>space.Centers[node]+(occupied[id]-space.Centers[node]-Vector3.up*WalkSpace.FootGap)*spread+Vector3.up*WalkSpace.FootGap);
                float duration=Mathf.Max(.12f,pending.Max(id=>Vector3.Distance(occupied[id],waypoint[id])+Vector3.Distance(waypoint[id],goals[id]))/15.6f);
                var together=pending.ToDictionary(id=>id,id=>new ActorTrack{actor=id,phase=order.Count+1,points=spread==1?new[]{occupied[id],goals[id]}:new[]{occupied[id],waypoint[id],goals[id]},times=spread==1?new[]{0f,duration}:new[]{0f,duration*.5f,duration},duration=duration});bool safe=true;
                foreach(int id in pending){
                    for(float t=0;t<=1.001f;t+=.025f){var point=together[id].Position(t*duration);float height;if(!space.FootHeight(new Vector2(point.x,point.z),out height)||Mathf.Abs(point.y-height-WalkSpace.FootGap)>.005f){safe=false;break;}}
                    for(int other=0;other<occupied.Length&&safe;other++)if(other!=id){ActorTrack track;together.TryGetValue(other,out track);if(MotionPlan.PairClearance(together[id],occupied[id],track,occupied[other],duration)<WalkSpace.Separation-.0001f)safe=false;}
                    if(!safe)break;yield return 0;
                }
                if(safe){foreach(int id in pending){order.Add(together[id]);occupied[id]=goals[id];}yield break;}
                yield return 0;
            }
            while(pending.Count>0){
                bool progressed=false;Exception last=null;
                foreach(int id in pending.ToArray()){
                    bool failed=false;
                    using(var work=Transfer(space,occupied,order,id,goals[id],mask)){
                        while(true){bool more=false;try{more=work.MoveNext();}catch(Exception e){failed=true;last=e;}if(failed||!more)break;yield return 0;}
                    }
                    if(failed)continue;pending.Remove(id);progressed=true;
                }
                if(!progressed){
                    // Break a slot dependency cycle through an unused landing slot.
                    // Collection residents never enter this branch: their seats are fixed.
                    foreach(int id in pending.ToArray()){
                        for(int slot=0;slot<16;slot++){
                            string key=id+":"+slot;if(parked.Contains(key))continue;
                            var spot=space.Seat(node,slot/4,slot%4);
                            if(pending.Any(other=>Vector3.Distance(spot,goals[other])<WalkSpace.Separation)||!space.SafePoint(new Vector2(spot.x,spot.z),mask,occupied,id))continue;
                            bool failed=false;
                            using(var work=Transfer(space,occupied,order,id,spot,mask)){while(true){bool more=false;try{more=work.MoveNext();}catch(Exception e){failed=true;last=e;}if(failed||!more)break;yield return 0;}}
                            parked.Add(key);if(!failed){progressed=true;break;}
                        }
                        if(progressed)break;
                    }
                    if(!progressed)throw new Exception("Cannot safely settle transit crowd",last);
                }
            }
        }
        static IEnumerator<int> BuildUncachedSteps(WalkSpace space,State before,Move move,Action<MotionPlan> ready)
        {
            if(!move.ok)throw new ArgumentException("Invalid move");State after=Rules.Apply(before,move);TransitSlots.Assign(space,before,after,move);var plan=new MotionPlan{move=move,initial=space.Positions(before),final=space.Positions(after),finalSlots=after.actorSlots};var occupied=(Vector3[])plan.initial.Clone();var order=new List<ActorTrack>();
            foreach(var point in plan.final){float height;if(!space.FootHeight(new Vector2(point.x,point.z),out height)||Mathf.Abs(point.y-height-WalkSpace.FootGap)>.005f)throw new Exception("Placement is covered by another surface");}
            using(var work=Reposition(space,occupied,order,before.queues[move.to].AsEnumerable().Reverse().SelectMany(g=>Enumerable.Range(g*4,4)),plan.final,move.to))while(work.MoveNext())yield return 0;
            foreach(int group in move.ids){
                int mark=order.Count;var saved=(Vector3[])occupied.Clone();Exception failure=null;bool found=false;
                foreach(var members in MemberOrders){
                    bool failed=false;
                    foreach(int member in members){
                        int id=group*Rules.MembersPerGroup+member;
                        using(var work=Transfer(space,occupied,order,id,plan.final[id],space.RouteMask(move.path))){
                            while(true){bool more=false;try{more=work.MoveNext();}catch(Exception e){failure=e;failed=true;}
                                if(failed||!more)break;yield return 0;
                            }
                        }
                        if(failed)break;
                    }
                    if(!failed){found=true;break;}
                    Array.Copy(saved,occupied,saved.Length);if(order.Count>mark)order.RemoveRange(mark,order.Count-mark);yield return 0;
                }
                if(!found){
                    // Departure and arrival can require different orders. Stage only
                    // this departing row, never displace settled collection residents.
                    var stages=new List<Vector3[]>();
                    foreach(int node in move.path)if(node!=move.from&&node!=move.to&&before.queues[node].Count==0)
                        for(int row=0;row<4;row++)stages.Add(Enumerable.Range(0,4).Select(m=>space.Seat(node,row,m)).ToArray());
                    foreach(var stair in space.Stairs)if(stair.polygon==null&&stair.Length>3&&move.path.Contains(stair.a)&&move.path.Contains(stair.b)){
                        var side=Vector3.Cross(Vector3.up,stair.Direction);var slots=new Vector3[4];
                        for(int i=0;i<4;i++){var p=Vector3.Lerp(stair.start,stair.end,.5f)+stair.Direction*((i/2==0?-1:1)*WalkSpace.SeatSpacing*.5f)+side*((i%2==0?-1:1)*WalkSpace.SeatSpacing*.5f);float y;slots[i]=space.FootHeight(new Vector2(p.x,p.z),out y)?new Vector3(p.x,y+WalkSpace.FootGap,p.z):new Vector3(float.NaN,0,0);}
                        if(slots.All(p=>!float.IsNaN(p.x)))stages.Add(slots);
                    }
                    foreach(var slots in stages){
                        foreach(var departure in MemberOrders){
                            Array.Copy(saved,occupied,saved.Length);if(order.Count>mark)order.RemoveRange(mark,order.Count-mark);bool failed=false;
                            foreach(int member in departure){int id=group*4+member;using(var work=Transfer(space,occupied,order,id,slots[member],space.RouteMask(move.path))){while(true){bool more=false;try{more=work.MoveNext();}catch(Exception e){failure=e;failed=true;}if(failed||!more)break;yield return 0;}}if(failed)break;}
                            if(failed)continue;var staged=(Vector3[])occupied.Clone();int stagedMark=order.Count;
                            foreach(var arrival in MemberOrders){
                                Array.Copy(staged,occupied,staged.Length);if(order.Count>stagedMark)order.RemoveRange(stagedMark,order.Count-stagedMark);failed=false;
                                foreach(int member in arrival){int id=group*4+member;using(var work=Transfer(space,occupied,order,id,plan.final[id],space.RouteMask(move.path))){while(true){bool more=false;try{more=work.MoveNext();}catch(Exception e){failure=e;failed=true;}if(failed||!more)break;yield return 0;}}if(failed)break;}
                                if(!failed){found=true;break;}
                            }
                            // All successful departure orders produce the same occupied
                            // slots. Retrying that arrangement cannot improve arrival.
                            break;
                        }
                        if(found)break;
                    }
                    if(!found)throw new Exception("No safe entry/exit order for group "+group,failure);
                }
            }
            using(var work=Reposition(space,occupied,order,after.queues[move.from].SelectMany(g=>Enumerable.Range(g*4,4)),plan.final,move.from))while(work.MoveNext())yield return 0;
            var scheduled=new ActorTrack[plan.initial.Length];
            if(order.Any(t=>t.phase>0)||order.GroupBy(t=>t.actor).Any(g=>g.Count()>1)){
                float serialClock=0;var paths=new Dictionary<int,List<Vector3>>();var times=new Dictionary<int,List<float>>();
                for(int eventIndex=0;eventIndex<order.Count;){int phase=order[eventIndex].phase;int end=eventIndex+1;if(phase>0)while(end<order.Count&&order[end].phase==phase)end++;float phaseDuration=0;
                for(int kEvent=eventIndex;kEvent<end;kEvent++){var eventTrack=order[kEvent];int id=eventTrack.actor;if(!paths.ContainsKey(id)){paths[id]=new List<Vector3>{plan.initial[id]};times[id]=new List<float>{0};}
                    var pv=paths[id];var tv=times[id];if(serialClock>tv.Last()){pv.Add(pv.Last());tv.Add(serialClock);}
                    for(int k=1;k<eventTrack.points.Length;k++){pv.Add(eventTrack.points[k]);tv.Add(serialClock+eventTrack.times[k]);}phaseDuration=Mathf.Max(phaseDuration,eventTrack.duration);
                }serialClock+=phaseDuration+.02f;eventIndex=end;}
                foreach(int id in paths.Keys)plan.tracks.Add(new ActorTrack{actor=id,start=0,duration=times[id].Last(),points=paths[id].ToArray(),times=times[id].ToArray()});
                plan.duration=serialClock+.06f;foreach(var track in plan.tracks)scheduled[track.actor]=track;
                for(int a=0;a<plan.initial.Length;a++){for(int b=a+1;b<plan.initial.Length;b++)if(MotionPlan.PairClearance(scheduled[a],plan.initial[a],scheduled[b],plan.initial[b],plan.duration)<WalkSpace.Separation-.0002f)throw new Exception("Staged row collision");yield return 0;}
                Debug.Log("STAGED ROW PLAN duration="+plan.duration);ready(plan);yield break;
            }
            foreach(var track in order){
                float latest=plan.tracks.Count==0?0:plan.tracks.Max(t=>t.End);bool placed=false;
                for(float start=0;start<=latest+.25f;start+=.08f){
                    track.start=start;bool safe=true;
                    for(int other=0;other<plan.initial.Length;other++){
                        if(other==track.actor)continue;var previous=scheduled[other];
                        if(MotionPlan.PairClearance(track,plan.initial[track.actor],previous,plan.initial[other],Mathf.Max(track.End,latest))<WalkSpace.Separation-.0001f){safe=false;break;}
                    }
                    yield return 0;if(safe){placed=true;break;}
                }
                if(!placed){track.start=latest+.1f;for(int other=0;other<plan.initial.Length;other++)if(other!=track.actor&&MotionPlan.PairClearance(track,plan.initial[track.actor],scheduled[other],plan.initial[other],track.End)<WalkSpace.Separation-.0001f)throw new Exception("Unable to schedule safe crowd motion");}
                plan.tracks.Add(track);scheduled[track.actor]=track;plan.duration=Mathf.Max(plan.duration,track.End);
            }
            plan.duration+=.06f;
            for(int a=0;a<plan.initial.Length;a++){
                for(int b=a+1;b<plan.initial.Length;b++)if(MotionPlan.PairClearance(scheduled[a],plan.initial[a],scheduled[b],plan.initial[b],plan.duration)<WalkSpace.Separation-.0002f)throw new Exception("Plan failed continuous collision validation");
                yield return 0;
            }
            for(int actor=0;actor<occupied.Length;actor++)if(Vector3.Distance(occupied[actor],plan.final[actor])>.001f)throw new Exception("Final seat mismatch");
            ready(plan);
        }
    }
}
