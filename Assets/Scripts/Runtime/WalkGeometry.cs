using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    // No mutable global geometry knobs: a change creates a new space and therefore
    // new corridor/profile caches. Defaults reproduce the existing authored geometry.
    public sealed class WalkGeometryConfig
    {
        public static readonly WalkGeometryConfig Default = new WalkGeometryConfig();
        public const int AlgorithmVersion = 1;
        public readonly float LayerHeightScale, StairWidth, TargetRiserHeight;
        public const float OriginHeight=2, XScale=1.4f, ZScale=1.96f;
        public const float ApronDepth=.38f, ApronOverlap=.045f, TreadBaseThickness=.25f;
        public WalkGeometryConfig(float layerHeightScale=1.5f,float stairWidth=3.4f,float targetRiserHeight=.22f)
        {
            if(!Finite(layerHeightScale)||layerHeightScale<=0||!Finite(stairWidth)||stairWidth<=WalkSpace.ActorRadius*2+.05f||!Finite(targetRiserHeight)||targetRiserHeight<=0)
                throw new ArgumentOutOfRangeException("Geometry parameters must be finite and positive; stairs must contain a character footprint.");
            LayerHeightScale=layerHeightScale;StairWidth=stairWidth;TargetRiserHeight=targetRiserHeight;
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public float WorldHeight(float height)=>OriginHeight+height*LayerHeightScale;
        public float LevelHeight(float worldHeight)=>(worldHeight-OriginHeight)/LayerHeightScale;
        public Vector3 WorldCenter(NodeSpec n)=>new Vector3(n.x*XScale,WorldHeight(n.y),-n.z*ZScale);
        public Vector3 WorldToLevel(Vector3 world)=>new Vector3(world.x/XScale,LevelHeight(world.y),-world.z/ZScale);
    }

    public sealed class GeometryArray<T> : IReadOnlyList<T>
    {
        readonly T[] values;
        public GeometryArray(IEnumerable<T> source){values=source.ToArray();}
        public int Length=>values.Length;
        public int Count=>values.Length;
        public T this[int index]=>values[index];
        public IEnumerator<T> GetEnumerator()=>((IEnumerable<T>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()=>values.GetEnumerator();
    }

    public readonly struct StairTread
    {
        public readonly Vector3 Center;
        public readonly float Height, Length, Width, Thickness, AlongStart, AlongEnd;
        internal StairTread(StairSurface stair,int index)
        {
            AlongStart=stair.Length*index/stair.steps;AlongEnd=stair.Length*(index+1)/stair.steps;
            Length=stair.Length/stair.steps;Width=stair.width;
            Height=Mathf.Lerp(stair.start.y,stair.end.y,stair.steps==1?0:index/(float)(stair.steps-1));
            var center=Vector3.Lerp(stair.start,stair.end,(index+.5f)/stair.steps);center.y=Height;Center=center;
            Thickness=WalkGeometryConfig.TreadBaseThickness+Mathf.Abs(stair.end.y-stair.start.y)/Mathf.Max(1,stair.steps-1);
        }
    }

    public sealed class StairSurface
    {
        readonly SurfaceRegion footprint;
        public readonly GeometryArray<Vector3> polygon;
        public readonly int a,b,index,steps;
        public readonly Vector3 start,end,Direction;
        public readonly float width,Length;
        readonly Lazy<GeometryArray<StairTread>> treads;
        public GeometryArray<StairTread> Treads=>treads.Value;
        internal StairSurface(int a,int b,int index,Vector3 start,Vector3 end,WalkGeometryConfig config,Vector3[] polygon=null)
        {
            this.a=a;this.b=b;this.index=index;this.start=start;this.end=end;width=config.StairWidth;
            this.polygon=polygon==null?null:new GeometryArray<Vector3>(polygon);
            var delta=Vector3.ProjectOnPlane(end-start,Vector3.up);Length=delta.magnitude;Direction=Length>0?delta/Length:Vector3.zero;
            if(polygon==null&&(double)Mathf.Abs(end.y-start.y)/config.TargetRiserHeight>99999)throw new ArgumentOutOfRangeException("Geometry requires too many treads");
            steps=polygon!=null||Mathf.Abs(end.y-start.y)<.001f?1:Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(end.y-start.y)/config.TargetRiserHeight)+1);
            if(steps>100000)throw new ArgumentOutOfRangeException("Geometry requires too many treads");
            footprint=polygon==null?null:SurfaceRegion.Polygon(this.polygon);
            treads=new Lazy<GeometryArray<StairTread>>(()=>new GeometryArray<StairTread>(polygon==null?Enumerable.Range(0,steps).Select(i=>new StairTread(this,i)):Enumerable.Empty<StairTread>()));
        }
        public bool Height(Vector2 point,out float y)
        {
            if(polygon!=null){y=start.y;return footprint.Contains(point);}
            var delta=point-new Vector2(start.x,start.z);float t=Vector2.Dot(delta,new Vector2(Direction.x,Direction.z));
            float across=Vector2.Dot(delta,new Vector2(-Direction.z,Direction.x));
            if(t<-.001f||t>Length+.001f||Mathf.Abs(across)>width/2+.001f){y=0;return false;}
            y=Treads[Mathf.Clamp(Mathf.FloorToInt(t/Length*steps),0,steps-1)].Height;return true;
        }
    }

    // Immutable generated geometry, shared by rendering, support and playback.
    // LevelSpec remains the authored fact; no generated fields are serialized to it.
    public sealed class WalkGeometry
    {
        readonly LevelSpec source;
        public readonly WalkGeometryConfig Config;
        public readonly GeometryArray<Vector3> Centers, EntryFacing;
        public readonly GeometryArray<int> Sides;
        public readonly GeometryArray<float> Rotations;
        public readonly GeometryArray<StairSurface> Stairs;
        public readonly GeometryArray<int> MissingEdges;
        internal readonly SurfaceRegion[] Regions;
        public readonly float PlatformHalf,MinX,MinZ;
        public readonly int Width,Depth;
        public readonly string Signature;
        public WalkGeometry(LevelSpec level,WalkGeometryConfig config=null)
        {
            source=LevelShare.Copy(level);Config=config??WalkGeometryConfig.Default;
            Centers=new GeometryArray<Vector3>(source.nodes.Select(Config.WorldCenter));
            Sides=new GeometryArray<int>(source.nodes.Select((n,i)=>Math.Max(4,source.edges.Where(e=>e.a==i||e.b==i).Select(e=>e.a==i?e.b:e.a).Distinct().Count())));
            PlatformHalf=Sides.Length==0?2.6f:Mathf.Max(2.6f,Sides.Max(s=>s>4?1.85f/Mathf.Tan(Mathf.PI/s):2.6f));
            Rotations=new GeometryArray<float>(source.nodes.Select(n=>Mathf.PI/4));
            var ports=new Dictionary<int,Vector3>[Centers.Length];for(int n=0;n<Centers.Length;n++)ports[n]=AssignPorts(n);
            for(int pass=0;pass<4;pass++)foreach(int n in Enumerable.Range(0,Centers.Length).OrderByDescending(i=>ports[i].Count))ports[n]=AssignPorts(n,ports);
            EntryFacing=new GeometryArray<Vector3>(source.nodes.Select((n,i)=>ports[i].Count==1?ports[i].Values.First():Quaternion.AngleAxis(90*(((n.surfaceDirection%4)+4)%4+1),Vector3.up)*new Vector3(1,0,1).normalized));
            var stairs=new List<StairSurface>();var missing=new List<int>();
            for(int i=0;i<source.edges.Length;i++){var e=source.edges[i];try{stairs.AddRange(Connection(e.a,e.b,i,ports[e.a][e.b],ports[e.b][e.a]));}catch(InvalidOperationException){missing.Add(i);}}
            Stairs=new GeometryArray<StairSurface>(stairs);MissingEdges=new GeometryArray<int>(missing);
            Regions=Centers.Select((c,i)=>SurfaceRegion.Platform(c,PlatformHalf,Sides[i],Rotations[i])).Concat(Stairs.Select(SurfaceRegion.Stair)).ToArray();
            float extent=Centers.Length==0||Sides.All(s=>s==4)?3.2f:Enumerable.Range(0,Centers.Length).Max(i=>PlatformHalf/Mathf.Cos(Mathf.PI/Sides[i]))+1;
            MinX=Centers.Length==0?-extent:Centers.Min(p=>p.x)-extent;MinZ=Centers.Length==0?-extent:Centers.Min(p=>p.z)-extent;
            Width=Mathf.Max(1,Mathf.CeilToInt(((Centers.Length==0?0:Centers.Max(p=>p.x))+extent-MinX)/WalkSpace.GridStep)+1);
            Depth=Mathf.Max(1,Mathf.CeilToInt(((Centers.Length==0?0:Centers.Max(p=>p.z))+extent-MinZ)/WalkSpace.GridStep)+1);
            Signature=ComputeSignature();
        }
        public void RequireComplete(){if(MissingEdges.Length>0)throw new InvalidOperationException("Missing generated stair connections: "+string.Join(",",MissingEdges));}
        public Vector3 FaceNormal(int node,int side){float angle=Rotations[node]+side*Mathf.PI*2/Sides[node];return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));}
        StairSurface[] Connection(int a,int b,int index,Vector3 na,Vector3 nb)
        {
            float apronA=WalkGeometryConfig.ApronDepth,apronB=apronA;Vector3 start=Vector3.zero,end=Vector3.zero;
            for(int iteration=0;iteration<16;iteration++){
                start=Centers[a]+na*(PlatformHalf+apronA);end=Centers[b]+nb*(PlatformHalf+apronB);
                var forward=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;var side=Vector3.Cross(Vector3.up,forward);
                apronA=Mathf.Lerp(apronA,WalkGeometryConfig.ApronDepth+Config.StairWidth*.5f*Mathf.Abs(Vector3.Dot(side,na)),.65f);
                apronB=Mathf.Lerp(apronB,WalkGeometryConfig.ApronDepth+Config.StairWidth*.5f*Mathf.Abs(Vector3.Dot(side,nb)),.65f);
            }
            start=Centers[a]+na*(PlatformHalf+apronA);end=Centers[b]+nb*(PlatformHalf+apronB);
            var main=new StairSurface(a,b,index,start,end,Config);
            if(main.Length<.4f||Vector3.Dot(main.Direction,na)<.08f||Vector3.Dot(-main.Direction,nb)<.08f)throw new InvalidOperationException("Connection does not reach both outer faces");
            return new[]{main,Apron(a,b,index,Centers[a]+na*(PlatformHalf-WalkGeometryConfig.ApronOverlap),start,na,main.Direction),Apron(a,b,index,Centers[b]+nb*(PlatformHalf-WalkGeometryConfig.ApronOverlap),end,nb,-main.Direction)};
        }
        StairSurface Apron(int a,int b,int index,Vector3 port,Vector3 outer,Vector3 normal,Vector3 direction)
        {
            var side=Vector3.Cross(Vector3.up,normal)*(Config.StairWidth*.5f);var endSide=Vector3.Cross(Vector3.up,direction)*(Config.StairWidth*.5f);
            var points=new[]{port-side,port+side,outer+endSide,outer-endSide}.OrderBy(p=>p.x).ThenBy(p=>p.z).ToArray();var hull=new List<Vector3>();
            Func<Vector3,Vector3,Vector3,float> cross=(p,q,r)=>(q.x-p.x)*(r.z-p.z)-(q.z-p.z)*(r.x-p.x);
            foreach(var p in points){while(hull.Count>=2&&cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;for(int i=points.Length-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            hull.RemoveAt(hull.Count-1);if(hull.Count<3)throw new InvalidOperationException("Degenerate landing");
            return new StairSurface(a,b,index,port,outer,Config,hull.ToArray());
        }
        Dictionary<int,Vector3> AssignPorts(int node,Dictionary<int,Vector3>[] known=null)
        {
            var neighbors=source.edges.Where(e=>e.a==node||e.b==node).Select(e=>e.a==node?e.b:e.a).Distinct().ToArray();
            if(neighbors.Length>6){var available=Enumerable.Range(0,Sides[node]).ToList();var assigned=new Dictionary<int,Vector3>();foreach(int neighbor in neighbors){var direction=(Centers[neighbor]-Centers[node]).normalized;int face=available.OrderByDescending(side=>Vector3.Dot(FaceNormal(node,side),direction)).First();available.Remove(face);assigned.Add(neighbor,FaceNormal(node,face));}return assigned;}
            int[] best=null,current=new int[neighbors.Length];bool[] used=new bool[Sides[node]];float cost=float.PositiveInfinity;
            Action<int,float> search=null;search=(at,score)=>{if(score>=cost)return;if(at==neighbors.Length){cost=score;best=(int[])current.Clone();return;}var toward=Vector3.ProjectOnPlane(Centers[neighbors[at]]-Centers[node],Vector3.up).normalized;
                for(int side=0;side<Sides[node];side++)if(!used[side]){var normal=FaceNormal(node,side);float extra=1-Vector3.Dot(toward,normal);
                    if(known!=null){int neighbor=neighbors[at];var other=known[neighbor][node];var start=Centers[node]+normal*(PlatformHalf+.7f);var end=Centers[neighbor]+other*(PlatformHalf+.7f);var dir=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;extra=extra*.15f+2-Vector3.Dot(normal,dir)-Vector3.Dot(other,-dir);try{Connection(node,neighbor,0,normal,other);}catch(InvalidOperationException){extra+=1000;}}
                    used[side]=true;current[at]=side;search(at+1,score+extra);used[side]=false;}};
            search(0,0);var result=new Dictionary<int,Vector3>();for(int i=0;i<neighbors.Length;i++)result.Add(neighbors[i],FaceNormal(node,best[i]));return result;
        }
        string ComputeSignature()
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){
                w.Write(WalkGeometryConfig.AlgorithmVersion);w.Write(Config.LayerHeightScale);w.Write(Config.StairWidth);w.Write(Config.TargetRiserHeight);
                foreach(float f in new[]{WalkGeometryConfig.OriginHeight,WalkGeometryConfig.XScale,WalkGeometryConfig.ZScale,WalkGeometryConfig.ApronDepth,WalkGeometryConfig.ApronOverlap,WalkGeometryConfig.TreadBaseThickness,WalkSpace.ActorRadius,WalkSpace.Separation,WalkSpace.FootGap,WalkSpace.GridStep,WalkSpace.SeatSpacing,WalkSpace.GridClearance,PlatformHalf,MinX,MinZ})w.Write(f);
                w.Write(Width);w.Write(Depth);w.Write(Centers.Length);
                for(int i=0;i<Centers.Length;i++){Write(w,Centers[i]);Write(w,EntryFacing[i]);w.Write(Sides[i]);w.Write(Rotations[i]);w.Write(source.nodes[i].capacity);w.Write(source.nodes[i].transit);w.Write(source.nodes[i].surfaceDirection);}
                w.Write(source.edges.Length);foreach(var e in source.edges){w.Write(e.a);w.Write(e.b);}
                w.Write(Stairs.Length);foreach(var s in Stairs){w.Write(s.a);w.Write(s.b);w.Write(s.index);Write(w,s.start);Write(w,s.end);w.Write(s.width);w.Write(s.steps);w.Write(s.polygon?.Length??0);if(s.polygon!=null)foreach(var p in s.polygon)Write(w,p);foreach(var t in s.Treads){Write(w,t.Center);w.Write(t.Height);w.Write(t.Length);w.Write(t.Width);w.Write(t.Thickness);w.Write(t.AlongStart);w.Write(t.AlongEnd);}}
                w.Write(Regions.Length);foreach(var r in Regions){w.Write(r.normals.Length);for(int i=0;i<r.normals.Length;i++){w.Write(r.normals[i].x);w.Write(r.normals[i].y);w.Write(r.offsets[i]);}}
                w.Write(SurfaceCoverage.Footprint.Length);foreach(var f in SurfaceCoverage.Footprint){w.Write(f.x);w.Write(f.y);}
                w.Flush();using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","");
            }
        }
        static void Write(BinaryWriter w,Vector3 p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
    }
}
