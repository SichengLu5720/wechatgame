using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed class SurfaceRegion
    {
        public Vector2[] normals;public float[] offsets;
        public static SurfaceRegion Platform(Vector3 c,float half,int sides,float rotation=0)
        {
            var r=new SurfaceRegion{normals=new Vector2[sides],offsets=new float[sides]};
            for(int i=0;i<sides;i++){float a=rotation+i*Mathf.PI*2/sides;r.normals[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a));r.offsets[i]=Vector2.Dot(r.normals[i],new Vector2(c.x,c.z))+half;}return r;
        }
        public static SurfaceRegion Stair(StairSurface s)
        {
            if(s.polygon!=null)return Polygon(s.polygon);
            var d=new Vector2(s.Direction.x,s.Direction.z);var side=new Vector2(-d.y,d.x);var start=new Vector2(s.start.x,s.start.z);var end=new Vector2(s.end.x,s.end.z);
            return new SurfaceRegion{normals=new[]{d,-d,side,-side},offsets=new[]{Vector2.Dot(d,end),Vector2.Dot(-d,start),Vector2.Dot(side,start)+s.width/2,Vector2.Dot(-side,start)+s.width/2}};
        }
        public static SurfaceRegion Polygon(GeometryArray<Vector3> vertices)
        {
            var result=new SurfaceRegion{normals=new Vector2[vertices.Length],offsets=new float[vertices.Length]};Vector2 center=Vector2.zero;foreach(var p in vertices)center+=new Vector2(p.x,p.z)/vertices.Length;
            for(int i=0;i<vertices.Length;i++){var a=new Vector2(vertices[i].x,vertices[i].z);var b=new Vector2(vertices[(i+1)%vertices.Length].x,vertices[(i+1)%vertices.Length].z);var d=b-a;var normal=new Vector2(-d.y,d.x).normalized;if(Vector2.Dot(normal,center-a)>0)normal=-normal;result.normals[i]=normal;result.offsets[i]=Vector2.Dot(normal,a);}return result;
        }
        public bool Contains(Vector2 p){for(int i=0;i<normals.Length;i++)if(Vector2.Dot(normals[i],p)>offsets[i]+.000001f)return false;return true;}
    }
    public static class SurfaceCoverage
    {
        public static readonly Vector2[] Footprint=CreateFootprint();
        static Vector2[] CreateFootprint(){var p=new Vector2[16];float r=1/Mathf.Cos(Mathf.PI/16);for(int i=0;i<p.Length;i++){float a=(i+.5f)*Mathf.PI/8;p[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}return p;}
        static List<Vector2> Clip(List<Vector2> input,Vector2 normal,float offset,bool inside)
        {
            var output=new List<Vector2>();if(input.Count==0)return output;
            var previous=input[input.Count-1];float pd=Vector2.Dot(previous,normal)-offset;bool pi=inside?pd<=0:pd>=0;
            foreach(var point in input){float d=Vector2.Dot(point,normal)-offset;bool included=inside?d<=0:d>=0;
                if(included!=pi){float t=pd/(pd-d);output.Add(Vector2.LerpUnclamped(previous,point,t));}if(included)output.Add(point);previous=point;pd=d;pi=included;}
            return output;
        }
        static float Area(List<Vector2> poly){if(poly.Count<3)return 0;double area=0;var origin=poly[0];for(int i=1;i<poly.Count-1;i++){var a=poly[i]-origin;var b=poly[i+1]-origin;area+=(double)a.x*b.y-(double)a.y*b.x;}return (float)(System.Math.Abs(area)*.5);}
        public static bool Covers(SurfaceRegion[] regions,Vector2 center,float radius)
        {
            // Allocation-free conservative interior test; clipping is only needed at joins.
            float bound=radius/Mathf.Cos(Mathf.PI/16);
            foreach(var r in regions){bool inside=true;for(int i=0;i<r.normals.Length;i++)if(Vector2.Dot(r.normals[i],center)+bound>r.offsets[i]){inside=false;break;}if(inside)return true;}
            var square=new List<Vector2>();foreach(var p in Footprint)square.Add(center+p*radius);
            foreach(var r in regions){bool all=true;foreach(var p in square)if(!r.Contains(p)){all=false;break;}if(all)return true;}
            var remaining=new List<List<Vector2>>{square};
            foreach(var r in regions){var next=new List<List<Vector2>>();foreach(var poly in remaining){var inside=poly;for(int i=0;i<r.normals.Length&&inside.Count>0;i++){
                        var outside=Clip(inside,r.normals[i],r.offsets[i]+.00001f,false);if(outside.Count>=3&&Area(outside)>.0000005f)next.Add(outside);
                        inside=Clip(inside,r.normals[i],r.offsets[i]+.00001f,true);
                    }}remaining=next;if(remaining.Count==0)return true;}
            return false;
        }
    }
}


