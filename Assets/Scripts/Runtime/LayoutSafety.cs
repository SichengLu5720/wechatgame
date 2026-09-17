using System;
using System.Linq;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class LayoutSafety
    {
        static Vector2 Flat(Vector3 p){return new Vector2(p.x,p.z);}
        static bool Overlap(Vector2[] a,Vector2[] b)
        {
            foreach(var polygon in new[]{a,b})for(int i=0;i<polygon.Length;i++){
                var edge=polygon[(i+1)%polygon.Length]-polygon[i];var axis=new Vector2(-edge.y,edge.x).normalized;
                float amin=float.PositiveInfinity,amax=float.NegativeInfinity,bmin=amin,bmax=amax;
                foreach(var p in a){float d=Vector2.Dot(axis,p);amin=Mathf.Min(amin,d);amax=Mathf.Max(amax,d);}foreach(var p in b){float d=Vector2.Dot(axis,p);bmin=Mathf.Min(bmin,d);bmax=Mathf.Max(bmax,d);}
                if(amax<=bmin+.015f||bmax<=amin+.015f)return false;
            }return true;
        }
        static Vector2[] Footprint(StairSurface s)
        {
            if(s.polygon!=null)return s.polygon.Select(Flat).ToArray();var side=Vector3.Cross(Vector3.up,s.Direction)*s.width*.5f;
            return new[]{Flat(s.start-side),Flat(s.start+side),Flat(s.end+side),Flat(s.end-side)};
        }
        public static void Validate(WalkSpace space)
        {
            space.Geometry.RequireComplete();
            var platforms=new Vector2[space.Centers.Length][];
            for(int n=0;n<platforms.Length;n++){int sides=space.Sides[n];float radius=(space.Half(n)+.04f)/Mathf.Cos(Mathf.PI/sides);platforms[n]=new Vector2[sides];for(int i=0;i<sides;i++){float a=space.Rotations[n]+(i+.5f)*Mathf.PI*2/sides;platforms[n][i]=Flat(space.Centers[n])+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}}
            for(int a=0;a<platforms.Length;a++)for(int b=a+1;b<platforms.Length;b++)if(Overlap(platforms[a],platforms[b]))throw new Exception("平台 "+(a+1)+" 与 "+(b+1)+" 重叠，请拉开距离");
            var stairs=space.Stairs.Select(Footprint).ToArray();
            for(int i=0;i<stairs.Length;i++){
                var s=space.Stairs[i];for(int n=0;n<platforms.Length;n++)if(n!=s.a&&n!=s.b&&Overlap(stairs[i],platforms[n]))throw new Exception("楼梯穿过平台 "+(n+1)+"，请调整连接");
                for(int j=i+1;j<stairs.Length;j++){var t=space.Stairs[j];if(s.index==t.index||s.a==t.a||s.a==t.b||s.b==t.a||s.b==t.b)continue;if(Overlap(stairs[i],stairs[j]))throw new Exception("两条楼梯交叉，请调整布局");}
            }
            for(int n=0;n<platforms.Length;n++)for(int row=0;row<4;row++)for(int member=0;member<4;member++){var p=space.Seat(n,row,member);float height;if(!space.FootHeight(Flat(p),out height)||Mathf.Abs(height-space.Centers[n].y)>.01f)throw new Exception("平台 "+(n+1)+" 的站位被其他建筑遮挡");}
            foreach(var e in space.Level.edges){try{Navigator.Find(space,new[]{space.Centers[e.a]+Vector3.up*WalkSpace.FootGap},0,space.Centers[e.b]+Vector3.up*WalkSpace.FootGap,space.RouteMask(new[]{e.a,e.b}));}catch(Exception){throw new Exception("平台 "+(e.a+1)+" 与 "+(e.b+1)+" 之间无法安全通行，请调整位置或高度");}}
        }
    }
}
