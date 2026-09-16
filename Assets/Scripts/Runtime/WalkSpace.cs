using SurfaceMask = StairsCrowd.Runtime.SurfaceSet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class StairSurface
    {
        SurfaceRegion footprint;public Vector3[] polygon; public int a,b,index; public Vector3 start,end; public float width=3.4f; public int steps=14;
        Vector3 direction;float length;bool frameReady;
        void PrepareFrame(){if(frameReady)return;var delta=Vector3.ProjectOnPlane(end-start,Vector3.up);length=delta.magnitude;direction=delta/length;frameReady=true;}
        public Vector3 Direction {get{PrepareFrame();return direction;}}
        public float Length {get{PrepareFrame();return length;}}
        public bool Height(Vector2 point,out float y)
        {
            if(polygon!=null){y=start.y;if(footprint==null)footprint=SurfaceRegion.Polygon(polygon);return footprint.Contains(point);}
            Vector3 d=Direction;Vector2 delta=point-new Vector2(start.x,start.z);float t=Vector2.Dot(delta,new Vector2(d.x,d.z));float across=Vector2.Dot(delta,new Vector2(-d.z,d.x));
            if(t<-.001f||t>Length+.001f||Mathf.Abs(across)>width/2+.001f){y=0;return false;}
            int step=Mathf.Clamp(Mathf.FloorToInt(t/Length*steps),0,steps-1);y=Mathf.Lerp(start.y,end.y,steps==1?0:step/(float)(steps-1));return true;
        }
    }
    public sealed class WalkSpace
    {
        internal readonly System.Collections.Concurrent.ConcurrentDictionary<string,MotionPlan> Plans=new System.Collections.Concurrent.ConcurrentDictionary<string,MotionPlan>();
        // The enlarged animated mesh stays inside the reserved footprint, including limbs.
        // The baked footprint includes half a grid cell in each axis. Every point
        // rounded to a valid cell therefore retains its entire physical footprint.
        public const float LayerHeightScale=1.5f;
        public static float WorldHeight(float height){return 2+height*LayerHeightScale;}
        public static float LevelHeight(float worldHeight){return (worldHeight-2)/LayerHeightScale;}
        public static Vector3 WorldCenter(NodeSpec node){return new Vector3(node.x*1.4f,WorldHeight(node.y),-node.z*1.96f);}
        public const float ActorRadius=.54f, Separation=1.09f, FootGap=.025f, GridStep=.15f, SeatSpacing=1.24f, GridClearance=.63f;
        readonly SurfaceRegion[] regions;readonly Vector3[] entryFacing;readonly float platformHalf;public readonly float[] Rotations;public readonly int[] Sides;public readonly LevelSpec Level; public readonly Vector3[] Centers;public readonly StairSurface[] Stairs;
        public readonly float MinX,MinZ;public readonly int Width,Depth;public readonly float[] Heights;public readonly SurfaceMask[] Owners;
        public WalkSpace(LevelSpec level,byte[] baked=null,bool geometryOnly=false)
        {
            Level=level;Centers=level.nodes.Select(WorldCenter).ToArray();
            Sides=level.nodes.Select((n,i)=>{int degree=level.edges.Where(e=>e.a==i||e.b==i).Select(e=>e.a==i?e.b:e.a).Distinct().Count();return degree>4?degree:4;}).ToArray();
            platformHalf=Sides.Length==0?2.6f:Mathf.Max(2.6f,Sides.Max(s=>s>4?1.85f/Mathf.Tan(Mathf.PI/s):2.6f));
            Rotations=level.nodes.Select(n=>Mathf.PI/4).ToArray();var stairs=new List<StairSurface>();
            var ports=new Dictionary<int,Vector3>[Centers.Length];for(int n=0;n<Centers.Length;n++)ports[n]=AssignPorts(n);
            // Resolve tied face choices together: independently choosing the nearest
            // face can put the two ends of a cardinal stair on opposite corners.
            for(int pass=0;pass<4;pass++)foreach(int n in Enumerable.Range(0,Centers.Length).OrderByDescending(i=>ports[i].Count))ports[n]=AssignPorts(n,ports);
            entryFacing=new Vector3[Centers.Length];for(int n=0;n<Centers.Length;n++){
                int direction=((level.nodes[n].surfaceDirection%4)+4)%4;
                entryFacing[n]=Quaternion.AngleAxis(90*(direction+1),Vector3.up)*new Vector3(1,0,1).normalized;
                // Any terminal platform faces its sole entrance, including transit leaves.
                if(ports[n].Count==1)entryFacing[n]=ports[n].Values.First();
            }
            for(int i=0;i<level.edges.Length;i++){var e=level.edges[i];try{stairs.AddRange(Connection(e.a,e.b,i,ports[e.a][e.b],ports[e.b][e.a]));}catch(Exception){}}
            Stairs=stairs.ToArray();regions=Centers.Select((c,i)=>SurfaceRegion.Platform(c,Half(i),Sides[i],Rotations[i])).Concat(Stairs.Select(SurfaceRegion.Stair)).ToArray();
            float extent=Centers.Length==0||Sides.All(s=>s==4)?3.2f:Enumerable.Range(0,Centers.Length).Max(i=>Half(i)/Mathf.Cos(Mathf.PI/Sides[i]))+1;
            MinX=Centers.Length==0?-extent:Centers.Min(p=>p.x)-extent;MinZ=Centers.Length==0?-extent:Centers.Min(p=>p.z)-extent;
            Width=Mathf.Max(1,Mathf.CeilToInt(((Centers.Length==0?0:Centers.Max(p=>p.x))+extent-MinX)/GridStep)+1);
            Depth=Mathf.Max(1,Mathf.CeilToInt(((Centers.Length==0?0:Centers.Max(p=>p.z))+extent-MinZ)/GridStep)+1);
            long cells=(long)Width*Depth;
            Heights=new float[geometryOnly||cells>2000000?0:(int)cells];Owners=new SurfaceMask[Heights.Length];
            if(baked!=null&&Heights.Length>0){using(var reader=new System.IO.BinaryReader(new System.IO.MemoryStream(baked))){
                if(reader.ReadInt32()!=311||reader.ReadInt32()!=GeometryHash()||reader.ReadInt32()!=Heights.Length)throw new Exception("Navigation bake does not match level geometry");
                for(int i=0;i<Heights.Length;i++){Heights[i]=reader.ReadSingle();Owners[i]=new SurfaceMask(reader.ReadBytes(reader.ReadInt32()));}
            }return;}
            for(int i=0;i<Heights.Length;i++){var cell=SampleCell(i);Heights[i]=cell.height;Owners[i]=cell.owners;}
        }
        struct Cell{public float height;public SurfaceMask owners;}
        readonly System.Collections.Concurrent.ConcurrentDictionary<long,Cell> sparse=new System.Collections.Concurrent.ConcurrentDictionary<long,Cell>();
        Cell SampleCell(long id){Vector2 p=GridPoint(id);SurfaceMask owner;float raw,h;Surface(p,out raw,out owner);return new Cell{owners=owner,height=FootHeight(p,out h,GridClearance)&&SurfaceCoverage.Covers(regions,p,GridClearance)?h:float.NaN};}
        public float HeightAt(long id){return Heights.Length>0?Heights[(int)id]:sparse.GetOrAdd(id,SampleCell).height;}
        public SurfaceMask OwnerAt(long id){return Owners.Length>0?Owners[(int)id]:sparse.GetOrAdd(id,SampleCell).owners;}

        StairSurface[] Connection(int a,int b,int index,Vector3 na,Vector3 nb)
        {
            float apronA=.38f,apronB=.38f;Vector3 start=Vector3.zero,end=Vector3.zero;
            for(int iteration=0;iteration<16;iteration++){
                start=Centers[a]+na*(Half(a)+apronA);end=Centers[b]+nb*(Half(b)+apronB);
                var forward=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;var side=Vector3.Cross(Vector3.up,forward);
                apronA=Mathf.Lerp(apronA,.38f+1.7f*Mathf.Abs(Vector3.Dot(side,na)),.65f);apronB=Mathf.Lerp(apronB,.38f+1.7f*Mathf.Abs(Vector3.Dot(side,nb)),.65f);
            }
            start=Centers[a]+na*(Half(a)+apronA);end=Centers[b]+nb*(Half(b)+apronB);
            var main=new StairSurface{a=a,b=b,index=index,start=start,end=end,steps=Mathf.Abs(end.y-start.y)<.001f?1:Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(end.y-start.y)/.22f)+1)};
            if(main.Length<.4f||Vector3.Dot(main.Direction,na)<.08f||Vector3.Dot(-main.Direction,nb)<.08f)throw new Exception("Connection does not reach both outer faces");
            return new[]{main,Apron(a,b,index,Centers[a]+na*(Half(a)-.045f),start,na,main.Direction),Apron(a,b,index,Centers[b]+nb*(Half(b)-.045f),end,nb,-main.Direction)};
        }
        StairSurface Apron(int a,int b,int index,Vector3 port,Vector3 outer,Vector3 normal,Vector3 direction)
        {
            var side=Vector3.Cross(Vector3.up,normal)*1.7f;var endSide=Vector3.Cross(Vector3.up,direction)*1.7f;
            // An oblique approach can put a corner inside the landing. Use its
            // convex outline so the deck has no folded triangles or unsupported gaps.
            var points=new[]{port-side,port+side,outer+endSide,outer-endSide}.OrderBy(p=>p.x).ThenBy(p=>p.z).ToArray();
            var hull=new List<Vector3>();
            Func<Vector3,Vector3,Vector3,float> cross=(p,q,r)=>(q.x-p.x)*(r.z-p.z)-(q.z-p.z)*(r.x-p.x);
            foreach(var p in points){while(hull.Count>=2&&cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;for(int i=points.Length-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            hull.RemoveAt(hull.Count-1);var polygon=hull.ToArray();if(polygon.Length<3)throw new Exception("Degenerate landing");
            return new StairSurface{a=a,b=b,index=index,start=port,end=outer,steps=1,polygon=polygon};
        }
        public Vector3 FaceNormal(int node,int side){float angle=Rotations[node]+side*Mathf.PI*2/Sides[node];return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));}
        Dictionary<int,Vector3> AssignPorts(int node,Dictionary<int,Vector3>[] known=null)
        {
            var neighbors=Level.edges.Where(e=>e.a==node||e.b==node).Select(e=>e.a==node?e.b:e.a).Distinct().ToArray();
            if(neighbors.Length>6){var available=Enumerable.Range(0,Sides[node]).ToList();var assigned=new Dictionary<int,Vector3>();foreach(int neighbor in neighbors){var direction=(Centers[neighbor]-Centers[node]).normalized;int face=available.OrderByDescending(side=>Vector3.Dot(FaceNormal(node,side),direction)).First();available.Remove(face);assigned.Add(neighbor,FaceNormal(node,face));}return assigned;}
            int[] best=null,current=new int[neighbors.Length];bool[] used=new bool[Sides[node]];float cost=float.PositiveInfinity;
            Action<int,float> search=null;search=(at,score)=>{if(score>=cost)return;if(at==neighbors.Length){cost=score;best=(int[])current.Clone();return;}var toward=Vector3.ProjectOnPlane(Centers[neighbors[at]]-Centers[node],Vector3.up).normalized;
                for(int side=0;side<Sides[node];side++)if(!used[side]){var normal=FaceNormal(node,side);float extra=1-Vector3.Dot(toward,normal);
                    if(known!=null){int neighbor=neighbors[at];var other=known[neighbor][node];var start=Centers[node]+normal*(Half(node)+.7f);var end=Centers[neighbor]+other*(Half(neighbor)+.7f);var dir=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;extra=extra*.15f+2-Vector3.Dot(normal,dir)-Vector3.Dot(other,-dir);try{Connection(node,neighbor,0,normal,other);}catch(Exception){extra+=1000;}}
                    used[side]=true;current[at]=side;search(at+1,score+extra);used[side]=false;}};
            search(0,0);var result=new Dictionary<int,Vector3>();for(int i=0;i<neighbors.Length;i++)result.Add(neighbors[i],FaceNormal(node,best[i]));return result;
        }
        public int GeometryHash(){unchecked{int hash=17;hash=hash*31+ActorRadius.GetHashCode();hash=hash*31+Separation.GetHashCode();hash=hash*31+SeatSpacing.GetHashCode();hash=hash*31+GridClearance.GetHashCode();for(int i=0;i<Centers.Length;i++){hash=hash*31+Centers[i].GetHashCode();hash=hash*31+Level.nodes[i].capacity;hash=hash*31+Sides[i];hash=hash*31+Level.nodes[i].surfaceDirection;hash=hash*31+Half(i).GetHashCode();}foreach(var e in Level.edges){hash=hash*31+e.a;hash=hash*31+e.b;}return hash;}}
        public byte[] Bake(){using(var stream=new System.IO.MemoryStream()){using(var writer=new System.IO.BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write(311);writer.Write(GeometryHash());writer.Write(Heights.Length);for(int i=0;i<Heights.Length;i++){writer.Write(Heights[i]);var bytes=Owners[i].ToByteArray();writer.Write(bytes.Length);writer.Write(bytes);}}return stream.ToArray();}}
        public bool Inside(int n,Vector2 point){var region=regions[n];for(int i=0;i<region.normals.Length;i++)if(Vector2.Dot(region.normals[i],point)>region.offsets[i]+.001f)return false;return true;}
        public float Boundary(int n,Vector3 direction){float dot=0;for(int i=0;i<Sides[n];i++){float angle=Rotations[n]+i*Mathf.PI*2/Sides[n];dot=Mathf.Max(dot,direction.x*Mathf.Cos(angle)+direction.z*Mathf.Sin(angle));}return Half(n)/dot;}
        public bool GridSupport(Vector2 point,SurfaceMask mask,out float height){int x=Mathf.RoundToInt((point.x-MinX)/GridStep),z=Mathf.RoundToInt((point.y-MinZ)/GridStep);height=0;if(x<0||z<0||x>=Width||z>=Depth)return false;long id=(long)z*Width+x;height=HeightAt(id);return !float.IsNaN(height)&&(OwnerAt(id)&mask)!=0;}
        public float Half(int n){return platformHalf;}
        public Vector3 Facing(int node){return entryFacing[node];}
        static readonly int[][] TransitWidths={new[]{2,2},new[]{3,2,3},new[]{4,4,4},new[]{4,4,4,4}};
        public Vector3 Seat(int node,int row,int member,int occupiedRows=4)
        {
            if(Level.nodes[node].transit){
                // Rounded, staggered crowd silhouettes; logical groups are not
                // drawn as front/back ranks on transit platforms.
                int index=row*4+member;
                int[] widths=TransitWidths[occupiedRows-1];
                int line=0;while(index>=widths[line]){index-=widths[line];line++;}
                float x=(index-(widths[line]-1)*.5f)*SeatSpacing;
                float z=((widths.Length-1)*.5f-line)*SeatSpacing;
                if(occupiedRows==3){
                    bool edge=index==0||index==3;
                    x=Mathf.Sign(x)*(edge?(line==1?2.045f:1.68f):.62f);
                    z=line==1?0:Mathf.Sign(z)*(edge?1.15f:1.5f);
                }
                if(occupiedRows==4){
                    bool outerX=index==0||index==3,outerZ=line==0||line==3;
                    x=Mathf.Sign(x)*(outerX?(outerZ?1.68f:2.045f):.65f);
                    z=Mathf.Sign(z)*(outerZ?(outerX?1.68f:2.045f):.65f);
                }
                Vector3 facing=Facing(node),across=Vector3.Cross(Vector3.up,facing);
                return Centers[node]+facing*z+across*x+Vector3.up*FootGap;
            }
            row+=Level.nodes[node].capacity-occupiedRows;
            Vector3 forward=Facing(node),right=Vector3.Cross(Vector3.up,forward);return Centers[node]+forward*((1.5f-row)*SeatSpacing)+right*((member-1.5f)*SeatSpacing)+Vector3.up*FootGap;
        }
        public Vector3 SeatFor(State state,int node,int row,int member)
        {
            int slot=state.actorSlots!=null?state.actorSlots[state.queues[node][row]*4+member]:row*4+member;
            if(!Level.nodes[node].transit)slot=row*4+slot%4;
            return Seat(node,slot/4,slot%4,state.queues[node].Count);
        }
        public Vector3[] Positions(State state)
        {
            var result=new Vector3[Level.groups.Length*Rules.MembersPerGroup];for(int n=0;n<state.queues.Length;n++)for(int row=0;row<state.queues[n].Count;row++)for(int m=0;m<Rules.MembersPerGroup;m++)result[state.queues[n][row]*Rules.MembersPerGroup+m]=SeatFor(state,n,row,m);return result;
        }
        public bool Surface(Vector2 p,out float height,out SurfaceMask owners)
        {
            height=float.NegativeInfinity;owners=0;
            for(int n=0;n<Centers.Length;n++){var c=Centers[n];if(Inside(n,p)){height=Mathf.Max(height,c.y);owners|=SurfaceMask.One<<n;}}
            foreach(var stair in Stairs){float y;if(stair.Height(p,out y)){height=Mathf.Max(height,y);owners|=SurfaceMask.One<<(Centers.Length+stair.index);}}
            return owners!=0;
        }
        public bool FootHeight(Vector2 p,out float height,float r=ActorRadius+.012f)
        {
            SurfaceMask mask;float h;height=float.NegativeInfinity;
            // Circumscribed polygon encloses the capsule without square corner inflation.
            
            if(!Surface(p,out height,out mask))return false;foreach(var offset in SurfaceCoverage.Footprint){if(!Surface(p+offset*r,out h,out mask))return false;height=Mathf.Max(height,h);}return SurfaceCoverage.Covers(regions,p,r);
        }
        public Vector2 GridPoint(long id){return new Vector2(MinX+(id%Width)*GridStep,MinZ+(id/Width)*GridStep);}
        public SurfaceMask NodeMask(int n){return SurfaceMask.One<<n;}
        public SurfaceMask RouteMask(int[] route)
        {
            SurfaceMask mask=0;foreach(int n in route)mask|=NodeMask(n);for(int i=1;i<route.Length;i++)foreach(var e in Stairs)if((e.a==route[i-1]&&e.b==route[i])||(e.b==route[i-1]&&e.a==route[i]))mask|=SurfaceMask.One<<(Centers.Length+e.index);return mask;
        }
        public bool SafePoint(Vector2 p,SurfaceMask mask,Vector3[] occupied,int actor)
        {
            float y;if(!PointSupport(p,mask,out y))return false;
            for(int i=0;i<occupied.Length;i++)if(i!=actor&&(p-new Vector2(occupied[i].x,occupied[i].z)).sqrMagnitude<Separation*Separation-.00001f)return false;return true;
        }
        public static float SegmentDistanceSquared(Vector2 p,Vector2 a,Vector2 b){Vector2 ab=b-a;float t=ab.sqrMagnitude<1e-9f?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);return (p-a-ab*t).sqrMagnitude;}
        public bool SupportedHeightChange(Vector2 a,Vector2 b,float from,float to)
        {
            float change=Mathf.Abs(from-to);if(change<=.4201f)return true;
            foreach(var stair in Stairs){
                if(stair.polygon!=null||stair.steps<2)continue;
                float low=Mathf.Min(stair.start.y,stair.end.y),high=Mathf.Max(stair.start.y,stair.end.y);
                if(Mathf.Min(from,to)<low-.0001f||Mathf.Max(from,to)>high+.0001f)continue;
                var direction=new Vector2(stair.Direction.x,stair.Direction.z);var side=new Vector2(-direction.y,direction.x);
                var origin=new Vector2(stair.start.x,stair.start.z);var pa=a-origin;var pb=b-origin;
                // Include the current stride so a footprint can leave the final
                // tread and land wholly on the adjoining platform.
                float reach=GridClearance*(Mathf.Abs(direction.x)+Mathf.Abs(direction.y))+Vector2.Distance(a,b);
                float ta=Vector2.Dot(pa,direction),tb=Vector2.Dot(pb,direction);
                if(Mathf.Min(ta,tb)<-reach-.001f||Mathf.Max(ta,tb)>stair.Length+reach+.001f||Mathf.Max(Mathf.Abs(Vector2.Dot(pa,side)),Mathf.Abs(Vector2.Dot(pb,side)))>stair.width/2+reach)continue;
                // One navigation cell can span several real treads on a steep stair.
                // Only permit the rise explained by that same stair's tread spacing.
                float riser=(high-low)/(stair.steps-1),tread=stair.Length/stair.steps;
                float allowed=riser*(Mathf.Ceil(Mathf.Abs(tb-ta)/tread)+1)+.0001f;
                if(change<=allowed)return true;
            }
            return false;
        }
        Vector2 SnapPoint(Vector2 p){return new Vector2(MinX+Mathf.Round((p.x-MinX)/GridStep)*GridStep,MinZ+Mathf.Round((p.y-MinZ)/GridStep)*GridStep);}
        bool PointSupport(Vector2 p,SurfaceMask mask,out float height){SurfaceMask owner;float raw;return Surface(p,out raw,out owner)&&((owner&mask)!=0)?FootHeight(p,out height):NoHeight(out height);}
        static bool NoHeight(out float height){height=0;return false;}
        public bool SafeGridEdge(long a,long b,SurfaceMask mask,Vector3[] occupied,int actor){if(float.IsNaN(HeightAt(a))||float.IsNaN(HeightAt(b))||(OwnerAt(a)&mask)==0||(OwnerAt(b)&mask)==0||!SupportedHeightChange(GridPoint(a),GridPoint(b),HeightAt(a),HeightAt(b)))return false;Vector2 p=GridPoint(a),q=GridPoint(b);for(int i=0;i<occupied.Length;i++)if(i!=actor&&SegmentDistanceSquared(new Vector2(occupied[i].x,occupied[i].z),p,q)<Separation*Separation-.00001f)return false;return true;}
        public bool SafeSegment(Vector2 a,Vector2 b,SurfaceMask mask,Vector3[] occupied,int actor)
        {
            bool safe=false;using(var work=SafeSegmentSteps(a,b,mask,occupied,actor,value=>safe=value))while(work.MoveNext()){}return safe;
        }
        public IEnumerator<int> SafeSegmentSteps(Vector2 a,Vector2 b,SurfaceMask mask,Vector3[] occupied,int actor,Action<bool> ready)
        {
            for(int i=0;i<occupied.Length;i++)if(i!=actor&&SegmentDistanceSquared(new Vector2(occupied[i].x,occupied[i].z),a,b)<Separation*Separation-.00001f){ready(false);yield break;}
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.015f));float previous=float.NaN;Vector2 previousPoint=a;
            for(int j=0;j<=steps;j++){
                if((j&31)==0)yield return 0;
                Vector2 p=Vector2.Lerp(a,b,j/(float)steps);float h;
                if(!PointSupport(p,mask,out h)||(!float.IsNaN(previous)&&!SupportedHeightChange(previousPoint,p,previous,h))){ready(false);yield break;}
                previous=h;previousPoint=p;
            }ready(true);
        }
        bool ExactSupport(Vector2 p)
        {
            const float radius=ActorRadius+.012f;
            foreach(var region in regions){bool inside=true;for(int i=0;i<region.normals.Length;i++){var normal=region.normals[i];if(Vector2.Dot(normal,p)+radius*(Mathf.Abs(normal.x)+Mathf.Abs(normal.y))>region.offsets[i]+.000001f){inside=false;break;}}if(inside)return true;}
            float height;return FootHeight(p,out height)&&SurfaceCoverage.Covers(regions,p,radius);
        }
        public Vector3 Lift(Vector2 p){float y;if(!FootHeight(p,out y))throw new Exception("No support under character footprint");return new Vector3(p.x,y+FootGap,p.y);}
    }
    struct SearchEntry {public long id;public float g,f;public SearchEntry(long i,float cost,float estimate){id=i;g=cost;f=estimate;}}
    sealed class MinHeap
    {
        readonly List<SearchEntry> data=new List<SearchEntry>();public int Count {get{return data.Count;}}
        public void Push(SearchEntry e){data.Add(e);int i=data.Count-1;while(i>0){int p=(i-1)/2;if(data[p].f<=e.f)break;data[i]=data[p];i=p;}data[i]=e;}
        public SearchEntry Pop(){var first=data[0];var last=data[data.Count-1];data.RemoveAt(data.Count-1);if(data.Count==0)return first;int i=0;while(i*2+1<data.Count){int c=i*2+1;if(c+1<data.Count&&data[c+1].f<data[c].f)c++;if(data[c].f>=last.f)break;data[i]=data[c];i=c;}data[i]=last;return first;}
    }
    public static class Navigator
    {
        public static List<Vector3> Find(WalkSpace space,Vector3[] occupied,int actor,Vector3 goal,SurfaceMask mask)
        {
            List<Vector3> result=null;using(var work=FindSteps(space,occupied,actor,goal,mask,p=>result=p))while(work.MoveNext()){}return result;
        }
        public static IEnumerator<int> FindSteps(WalkSpace space,Vector3[] occupied,int actor,Vector3 goal,SurfaceMask mask,Action<List<Vector3>> ready)
        {
            Vector2 start=new Vector2(occupied[actor].x,occupied[actor].z),end=new Vector2(goal.x,goal.z);
            if(Vector2.Distance(start,end)<.005f){ready(new List<Vector3>{occupied[actor],goal});yield break;}
            if(!space.SafePoint(end,mask,occupied,actor))throw new Exception("Destination blocked for actor "+actor);
            bool segmentSafe=false;using(var work=space.SafeSegmentSteps(start,end,mask,occupied,actor,v=>segmentSafe=v))while(work.MoveNext())yield return 0;
            if(segmentSafe){ready(new List<Vector3>{space.Lift(start),space.Lift(end)});yield break;}
            var costs=new SearchValues<float>(space.Heights.Length,float.PositiveInfinity);var parent=new SearchValues<long>(space.Heights.Length,-1);
            var heap=new MinHeap();int cx=Mathf.RoundToInt((start.x-space.MinX)/WalkSpace.GridStep),cz=Mathf.RoundToInt((start.y-space.MinZ)/WalkSpace.GridStep);
            for(int dx=-3;dx<=3;dx++)for(int dz=-3;dz<=3;dz++){int x=cx+dx,z=cz+dz;if(x<0||z<0||x>=space.Width||z>=space.Depth)continue;long id=(long)z*space.Width+x;Vector2 p=space.GridPoint(id);if(float.IsNaN(space.HeightAt(id)))continue;using(var work=space.SafeSegmentSteps(start,p,mask,occupied,actor,v=>segmentSafe=v))while(work.MoveNext())yield return 0;if(!segmentSafe)continue;float g=Vector2.Distance(start,p);costs[id]=g;parent[id]=-1;heap.Push(new SearchEntry(id,g,g+Vector2.Distance(p,end)));}
            long found=-1;int iterations=0;
            while(heap.Count>0){if((iterations++&15)==0)yield return 0;var entry=heap.Pop();long id=entry.id;if(entry.g>costs[id]+.00001f)continue;Vector2 p=space.GridPoint(id);
                if(Vector2.Distance(p,end)<.4f){using(var work=space.SafeSegmentSteps(p,end,mask,occupied,actor,v=>segmentSafe=v))while(work.MoveNext())yield return 0;if(segmentSafe){found=id;break;}}
                int x=(int)(id%space.Width),z=(int)(id/space.Width);
                for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++){if(dx==0&&dz==0)continue;int nx=x+dx,nz=z+dz;if(nx<0||nz<0||nx>=space.Width||nz>=space.Depth)continue;long next=(long)nz*space.Width+nx;if(float.IsNaN(space.HeightAt(next))||(space.OwnerAt(next)&mask)==0)continue;
                    float g=costs[id]+(dx!=0&&dz!=0?1.41421356f:1)*WalkSpace.GridStep;if(g>=costs[next])continue;Vector2 q=space.GridPoint(next);if(!space.SafeGridEdge(id,next,mask,occupied,actor))continue;costs[next]=g;parent[next]=id;heap.Push(new SearchEntry(next,g,g+Vector2.Distance(q,end)));}
            }
            if(found<0)throw new Exception("No collision-free path for actor "+actor+" start="+start+" end="+end+" mask="+mask+" occupants="+string.Join(";",occupied.Select((p,i)=>i+":"+p)));
            var raw=new List<Vector2>{end};for(long n=found;n>=0;n=parent[n])raw.Add(space.GridPoint(n));raw.Add(start);raw.Reverse();
            var points=new List<Vector3>{space.Lift(start)};int cursor=0;
            while(cursor<raw.Count-1){yield return 0;int far=raw.Count-1;while(far>cursor+1){using(var work=space.SafeSegmentSteps(raw[cursor],raw[far],mask,occupied,actor,v=>segmentSafe=v))while(work.MoveNext())yield return 0;if(segmentSafe)break;far--;}points.Add(space.Lift(raw[far]));cursor=far;}
            ready(points);
        }
    }
}



