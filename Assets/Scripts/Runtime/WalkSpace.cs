using SurfaceMask = StairsCrowd.Runtime.SurfaceSet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class WalkSpace
    {
        internal readonly System.Collections.Concurrent.ConcurrentDictionary<string,MotionPlan> Plans=new System.Collections.Concurrent.ConcurrentDictionary<string,MotionPlan>();
        // The enlarged animated mesh stays inside the reserved footprint, including limbs.
        // The baked footprint includes half a grid cell in each axis. Every point
        // rounded to a valid cell therefore retains its entire physical footprint.
        public static float LayerHeightScale=>WalkGeometryConfig.Default.LayerHeightScale;
        public static float WorldHeight(float height)=>WalkGeometryConfig.Default.WorldHeight(height);
        public static float LevelHeight(float worldHeight)=>WalkGeometryConfig.Default.LevelHeight(worldHeight);
        public static Vector3 WorldCenter(NodeSpec node)=>WalkGeometryConfig.Default.WorldCenter(node);
        public const float ActorRadius=.54f, Separation=1.09f, FootGap=.025f, GridStep=.15f, SeatSpacing=1.24f, GridClearance=.63f;
        readonly SurfaceRegion[] regions;readonly GeometryArray<Vector3> entryFacing;readonly float platformHalf;
        public readonly WalkGeometry Geometry;
        public readonly GeometryArray<float> Rotations;public readonly GeometryArray<int> Sides;
        public readonly LevelSpec Level;public readonly GeometryArray<Vector3> Centers;public readonly GeometryArray<StairSurface> Stairs;
        public readonly float MinX,MinZ;public readonly int Width,Depth;public readonly float[] Heights;public readonly SurfaceMask[] Owners;
        public string GeometrySignature=>Geometry.Signature;
        public string CacheStatus {get;private set;}
        // Direct construction remains available for offline dense navigation checks.
        // Official play/editor loading uses NavigationFactory.Create (sparse/on demand).
        public WalkSpace(LevelSpec level,byte[] baked=null,bool geometryOnly=false,WalkGeometryConfig config=null)
        {
            Level=level;Geometry=new WalkGeometry(level,config);
            Centers=Geometry.Centers;Sides=Geometry.Sides;Rotations=Geometry.Rotations;Stairs=Geometry.Stairs;
            platformHalf=Geometry.PlatformHalf;entryFacing=Geometry.EntryFacing;regions=Geometry.Regions;
            MinX=Geometry.MinX;MinZ=Geometry.MinZ;Width=Geometry.Width;Depth=Geometry.Depth;
            long cells=(long)Width*Depth;
            Heights=new float[geometryOnly||cells>2000000?0:(int)cells];Owners=new SurfaceMask[Heights.Length];
            if(Heights.Length==0){CacheStatus=baked==null?"Sparse":"Sparse: cache bypassed";return;}
            string reason="Missing";
            if(baked!=null&&NavigationCache.TryRead(this,baked,out reason)){CacheStatus="Valid";return;}
            else reason=baked==null?"Missing":reason;
            CacheStatus=reason;
            for(int i=0;i<Heights.Length;i++){var cell=SampleCell(i);Heights[i]=cell.height;Owners[i]=cell.owners;}
        }
        struct Cell{public float height;public SurfaceMask owners;}
        readonly System.Collections.Concurrent.ConcurrentDictionary<long,Cell> sparse=new System.Collections.Concurrent.ConcurrentDictionary<long,Cell>();
        Cell SampleCell(long id){Vector2 p=GridPoint(id);SurfaceMask owner;float raw,h;Surface(p,out raw,out owner);return new Cell{owners=owner,height=FootHeight(p,out h,GridClearance)&&SurfaceCoverage.Covers(regions,p,GridClearance)?h:float.NaN};}
        public float HeightAt(long id){return Heights.Length>0?Heights[(int)id]:sparse.GetOrAdd(id,SampleCell).height;}
        public SurfaceMask OwnerAt(long id){return Owners.Length>0?Owners[(int)id]:sparse.GetOrAdd(id,SampleCell).owners;}

        public Vector3 FaceNormal(int node,int side)=>Geometry.FaceNormal(node,side);
        // Compatibility fingerprint; persisted caches use the full SHA-256 signature.
        public int GeometryHash()=>unchecked((int)Convert.ToUInt32(GeometrySignature.Substring(0,8),16));
        public byte[] Bake()=>NavigationCache.Write(this);
        public bool Inside(int n,Vector2 point){var region=regions[n];for(int i=0;i<region.normals.Length;i++)if(Vector2.Dot(region.normals[i],point)>region.offsets[i]+.001f)return false;return true;}
        public float Boundary(int n,Vector3 direction){float dot=0;for(int i=0;i<Sides[n];i++){float angle=Rotations[n]+i*Mathf.PI*2/Sides[n];dot=Mathf.Max(dot,direction.x*Mathf.Cos(angle)+direction.z*Mathf.Sin(angle));}return Half(n)/dot;}
        public bool GridSupport(Vector2 point,SurfaceMask mask,out float height){int x=Mathf.RoundToInt((point.x-MinX)/GridStep),z=Mathf.RoundToInt((point.y-MinZ)/GridStep);height=0;if(x<0||z<0||x>=Width||z>=Depth)return false;long id=(long)z*Width+x;height=HeightAt(id);return !float.IsNaN(height)&&(OwnerAt(id)&mask)!=0;}
        public float Half(int n){return platformHalf;}
        public Vector3 Facing(int node){return entryFacing[node];}
        // Permanent physical IDs. Initial subsets are centered and grow symmetrically.
        static readonly int[] TransitInitialOrder={5,6,9,10,1,7,14,8,2,11,13,4,0,3,12,15};
        public static int InitialTransitSlot(int index)=>TransitInitialOrder[index];
        public Vector3 Seat(int node,int row,int member,int occupiedRows=4)
        {
            if(Level.nodes[node].transit){
                int slot=occupiedRows==4?row*4+member:InitialTransitSlot(row*4+member);
                int index=slot%4,line=slot/4;
                bool outerX=index==0||index==3,outerZ=line==0||line==3;
                float x=Mathf.Sign(index-1.5f)*(outerX?(outerZ?1.68f:2.045f):.65f);
                float z=Mathf.Sign(1.5f-line)*(outerZ?(outerX?1.68f:2.045f):.65f);
                Vector3 facing=Facing(node),across=Vector3.Cross(Vector3.up,facing);
                return Centers[node]+facing*z+across*x+Vector3.up*FootGap;
            }
            row+=Level.nodes[node].capacity-occupiedRows;
            Vector3 forward=Facing(node),right=Vector3.Cross(Vector3.up,forward);return Centers[node]+forward*((1.5f-row)*SeatSpacing)+right*((member-1.5f)*SeatSpacing)+Vector3.up*FootGap;
        }
        public Vector3 SeatFor(State state,int node,int row,int member)
        {
            bool transit=Level.nodes[node].transit;
            int slot=state.actorSlots!=null?state.actorSlots[state.queues[node][row]*4+member]:(transit?InitialTransitSlot(row*4+member):row*4+member);
            if(!transit)slot=row*4+slot%4;
            return Seat(node,slot/4,slot%4,transit?4:state.queues[node].Count);
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



