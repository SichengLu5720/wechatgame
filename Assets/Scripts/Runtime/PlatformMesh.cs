using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class PlatformMesh
    {
        public static Mesh Connector(Vector3[] polygon,Vector3 origin,float thickness)
        {
            var vertices=new List<Vector3>();var indices=new List<int>();Vector3 center=Vector3.zero;foreach(var p in polygon)center+=(p-origin)/polygon.Length;
            System.Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);};
            for(int i=0;i<polygon.Length;i++){var a=polygon[i]-origin;var b=polygon[(i+1)%polygon.Length]-origin;if(Vector3.Cross(b-a,center-a).y<0){var swap=a;a=b;b=swap;}var down=Vector3.down*thickness;tri(a,b,center);tri(a+down,center+down,b+down);tri(a,a+down,b);tri(b,a+down,b+down);}
            var mesh=new Mesh{name="Flush landing"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Prism(int sides)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            float radius=.5f/Mathf.Cos(Mathf.PI/sides);
            System.Action<Vector3,Vector3,Vector3> triangle=(a,b,c)=>{int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);};
            for(int i=0;i<sides;i++){
                float a=(i+.5f)*Mathf.PI*2/sides,b=(i+1.5f)*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Cos(a)*radius,.5f,Mathf.Sin(a)*radius);var q=new Vector3(Mathf.Cos(b)*radius,.5f,Mathf.Sin(b)*radius);
                var r=p-Vector3.up;var s=q-Vector3.up;
                triangle(Vector3.up*.5f,q,p);triangle(Vector3.down*.5f,r,s);triangle(p,q,s);triangle(p,s,r);
            }
            var mesh=new Mesh{name="Platform "+sides+" sides"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
    public sealed class AssemblySequence
    {
        struct Piece{public Transform transform;public Vector3 position,scale;public float delay,distance;public bool stair;}
        readonly List<Piece> pieces=new List<Piece>();readonly CrowdScene scene;float clock;
        public bool Complete {get;private set;}public float Duration{get;private set;}
        public AssemblySequence(CrowdScene scene)
        {
            this.scene=scene;int platform=0,stair=0;float lowest=float.PositiveInfinity;foreach(var c in scene.space.Centers)lowest=Mathf.Min(lowest,c.y);if(float.IsInfinity(lowest))lowest=2;
            foreach(Transform child in scene.root.transform){bool link=child.name.StartsWith("Stair Link");if(!link&&!child.name.StartsWith("Sorting Pavilion")&&!child.name.StartsWith("Transit Pier"))continue;
                float delay=link?.22f+Mathf.Min(.13f,stair++*.018f):Mathf.Min(.22f,platform++*.035f);
                pieces.Add(new Piece{transform=child,position=child.localPosition,scale=child.localScale,delay=delay,distance=Mathf.Max(30,child.localPosition.y-lowest+32),stair=link});Duration=Mathf.Max(Duration,delay+1.65f);
            }
            foreach(var c in scene.surfaces)c.enabled=false;Advance(0);
        }
        public void Advance(float delta)
        {
            if(Complete)return;clock+=Mathf.Max(0,delta);
            foreach(var piece in pieces){float t=Mathf.Clamp01((clock-piece.delay)/1.65f),ease=t*t*t*(10+t*(-15+6*t));piece.transform.gameObject.SetActive(t>0);
                piece.transform.localPosition=piece.position-Vector3.up*piece.distance*(1-ease);
                piece.transform.localScale=piece.scale;
            }
            if(clock>=Duration){Complete=true;foreach(var p in pieces){p.transform.gameObject.SetActive(true);p.transform.localPosition=p.position;p.transform.localScale=p.scale;}foreach(var c in scene.surfaces)c.enabled=true;Physics.SyncTransforms();}
        }
    }
}
