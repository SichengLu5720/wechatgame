using System;
using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Logical moves commit immediately. Only characters sharing space or identity wait.
    public static class MotionComposer
    {
        public static MotionPlan Append(MotionPlan active,float clock,MotionPlan next)
        {
            if(active==null)return next;
            var result=new MotionPlan{move=next.move,finalSlots=next.finalSlots,initial=new Vector3[active.initial.Length],final=next.final};
            for(int actor=0;actor<result.initial.Length;actor++){
                if(Vector3.Distance(active.final[actor],next.initial[actor])>.001f)throw new Exception("Queued move snapshot mismatch");
                result.initial[actor]=active.Position(actor,clock);var old=active.Track(actor);ActorTrack remaining=null;
                if(old!=null&&old.End>clock){
                    var points=new List<Vector3>{result.initial[actor]};var times=new List<float>{0};
                    for(int i=0;i<old.times.Length;i++)if(old.start+old.times[i]>clock+.000001f){points.Add(old.points[i]);times.Add(old.start+old.times[i]-clock);}
                    if(points.Count>1)remaining=new ActorTrack{actor=actor,points=points.ToArray(),times=times.ToArray(),duration=times[times.Count-1]};
                }
                var step=next.Track(actor);var chosen=step==null?remaining:Extend(remaining,result.initial[actor],step,Mathf.Max(step.start,remaining==null?0:remaining.End));
                if(chosen!=null){result.tracks.Add(chosen);result.duration=Mathf.Max(result.duration,chosen.End);}
            }return result;
        }
        static ActorTrack Extend(ActorTrack previous,Vector3 initial,ActorTrack step,float start)
        {
            var points=new List<Vector3>();var times=new List<float>();
            if(previous==null){points.Add(initial);times.Add(0);}
            else for(int i=0;i<previous.points.Length;i++){points.Add(previous.points[i]);times.Add(previous.start+previous.times[i]);}
            if(Vector3.Distance(points[points.Count-1],step.points[0])>.001f)throw new Exception("Actor continuation mismatch");
            if(start>times[times.Count-1]+.000001f){points.Add(step.points[0]);times.Add(start);}
            for(int i=1;i<step.points.Length;i++){points.Add(step.points[i]);times.Add(start+step.times[i]);}
            return new ActorTrack{actor=step.actor,points=points.ToArray(),times=times.ToArray(),duration=times[times.Count-1]};
        }
    }
}
