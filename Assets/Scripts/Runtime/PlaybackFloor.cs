using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Compile piecewise-constant tread heights once per route segment. Playback
    // performs a binary lookup, never footprint clipping or a scene-wide search.
    public sealed class PlaybackFloor
    {
        sealed class Band { public SurfaceRegion region; public float height; }
        struct Event { public float t,height; public int delta; }
        public sealed class Profile
        {
            public float[] times,heights;
            public float Height(float t) { int lo=0,hi=times.Length;while(lo<hi){int m=(lo+hi)/2;if(times[m]<=t)lo=m+1;else hi=m;}return heights[Math.Max(0,lo-1)]; }
        }
        static readonly ConditionalWeakTable<WalkSpace,PlaybackFloor> floors=new ConditionalWeakTable<WalkSpace,PlaybackFloor>();
        readonly List<Band> bands=new List<Band>();
        readonly Dictionary<(Vector2,Vector2),Profile> profiles=new Dictionary<(Vector2,Vector2),Profile>();
        public static PlaybackFloor For(WalkSpace space)=>floors.GetValue(space,s=>new PlaybackFloor(s));
        public static void Forget(WalkSpace space)=>floors.Remove(space);
        PlaybackFloor(WalkSpace space)
        {
            for(int n=0;n<space.Centers.Length;n++)Add(SurfaceRegion.Platform(space.Centers[n],space.Half(n),space.Sides[n],space.Rotations[n]),space.Centers[n].y,.001f);
            foreach(var s in space.Stairs){
                if(s.polygon!=null){Add(SurfaceRegion.Polygon(s.polygon),s.start.y,.000001f);continue;}
                var d=new Vector2(s.Direction.x,s.Direction.z);var side=new Vector2(-d.y,d.x);var start=new Vector2(s.start.x,s.start.z);float origin=Vector2.Dot(d,start),across=Vector2.Dot(side,start);
                for(int i=0;i<s.steps;i++){
                    float a=origin+s.Length*i/s.steps-(i==0?.001f:0),b=origin+s.Length*(i+1)/s.steps+(i==s.steps-1?.001f:0);
                    Add(new SurfaceRegion{normals=new[]{d,-d,side,-side},offsets=new[]{b,-a,across+s.width/2+.001f,-across+s.width/2+.001f}},Mathf.Lerp(s.start.y,s.end.y,s.steps==1?0:i/(float)(s.steps-1)),0);
                }
            }
        }
        void Add(SurfaceRegion region,float height,float epsilon){for(int i=0;i<region.offsets.Length;i++)region.offsets[i]+=epsilon;bands.Add(new Band{region=region,height=height});}
        static bool Intersect(SurfaceRegion region,Vector2 a,Vector2 delta,out float enter,out float exit,float margin=0)
        {
            enter=0;exit=1;
            for(int i=0;i<region.normals.Length;i++){
                float direction=Vector2.Dot(region.normals[i],delta),distance=region.offsets[i]+margin-Vector2.Dot(region.normals[i],a);
                if(Mathf.Abs(direction)<1e-8f){if(distance<0)return false;continue;}
                float t=distance/direction;if(direction>0)exit=Mathf.Min(exit,t);else enter=Mathf.Max(enter,t);if(enter>exit)return false;
            }return exit>=enter;
        }
        public Profile Segment(Vector3 from,Vector3 to)
        {
            var a=new Vector2(from.x,from.z);var b=new Vector2(to.x,to.z);var key=(a,b);Profile profile;if(profiles.TryGetValue(key,out profile))return profile;
            var events=new List<Event>();var delta=b-a;
            foreach(var band in bands){
                // Reject unrelated treads once, before testing the 17 footprint points.
                float broadEnter,broadExit;if(!Intersect(band.region,a,delta,out broadEnter,out broadExit,(WalkSpace.ActorRadius+.012f)/Mathf.Cos(Mathf.PI/16)))continue;
                for(int sample=-1;sample<SurfaceCoverage.Footprint.Length;sample++){
                Vector2 offset=sample<0?Vector2.zero:SurfaceCoverage.Footprint[sample]*(WalkSpace.ActorRadius+.012f);float enter,exit;
                if(!Intersect(band.region,a+offset,delta,out enter,out exit)||exit-enter<1e-7f)continue;
                events.Add(new Event{t=enter,height=band.height,delta=1});events.Add(new Event{t=exit,height=band.height,delta=-1});
            }}
            events.Sort((x,y)=>x.t.CompareTo(y.t));var counts=new SortedDictionary<float,int>();var times=new List<float>();var heights=new List<float>();
            for(int i=0;i<events.Count;){float t=events[i].t;do{var e=events[i++];int count;counts.TryGetValue(e.height,out count);count+=e.delta;if(count==0)counts.Remove(e.height);else counts[e.height]=count;}while(i<events.Count&&events[i].t==t);
                if(t>=1)break;float highest=float.NegativeInfinity;foreach(var entry in counts)if(entry.Value>0)highest=entry.Key;
                if(float.IsNegativeInfinity(highest))throw new InvalidOperationException("Playback route leaves floor");
                if(heights.Count==0||heights[heights.Count-1]!=highest){times.Add(t);heights.Add(highest);}
            }
            if(times.Count==0||times[0]>0)throw new InvalidOperationException("Playback route starts outside floor");
            profile=new Profile{times=times.ToArray(),heights=heights.ToArray()};if(profiles.Count>=4096)profiles.Clear();profiles[key]=profile;return profile;
        }
    }
}
