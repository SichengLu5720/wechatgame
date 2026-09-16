using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // One opaque mesh, one material: the white glyph is geometry, not a font or texture.
    public static class HiddenCharacterMesh
    {
        sealed class Builder
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<int> triangles=new List<int>();readonly List<Color> colors=new List<Color>();
            public void Face(Vector3 a,Vector3 b,Vector3 c,bool white=false)
            {
                int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                var color=white?Color.white:new Color(.018f,.018f,.018f,1);
                colors.Add(color);colors.Add(color);colors.Add(color);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);
            }
            public void Ring(float y0,float y1,float r0,float r1,int sides=8)
            {
                for(int i=0;i<sides;i++){
                    float a=(i+.5f)*Mathf.PI*2/sides,b=(i+1.5f)*Mathf.PI*2/sides;
                    var p=new Vector3(Mathf.Sin(a)*r0,y0,Mathf.Cos(a)*r0);var q=new Vector3(Mathf.Sin(b)*r0,y0,Mathf.Cos(b)*r0);
                    var r=new Vector3(Mathf.Sin(a)*r1,y1,Mathf.Cos(a)*r1);var s=new Vector3(Mathf.Sin(b)*r1,y1,Mathf.Cos(b)*r1);
                    Face(p,q,r);Face(q,s,r);
                }
            }
            public void Box(Vector3 center,Vector3 size)
            {
                var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                foreach(var f in new[]{new[]{0,4,6,2},new[]{1,3,7,5},new[]{0,1,5,4},new[]{2,6,7,3},new[]{0,2,3,1},new[]{4,5,7,6}}){Face(v[f[0]],v[f[1]],v[f[2]]);Face(v[f[0]],v[f[2]],v[f[3]]);}
            }
            Vector3 OnHood(Vector2 p){float r=Mathf.Lerp(.31f,.27f,Mathf.Clamp01((p.y-1.32f)/.38f));return new Vector3(-p.x*.9f,p.y,r*Mathf.Cos(Mathf.PI/8)+.002f);}
            public void Mark()
            {
                var path=new[]{new Vector2(-.099f,1.59f),new Vector2(-.098f,1.636f),new Vector2(-.056f,1.67f),new Vector2(.032f,1.67f),new Vector2(.084f,1.64f),new Vector2(.096f,1.599f),new Vector2(.068f,1.557f),new Vector2(.014f,1.526f),new Vector2(0,1.50f),new Vector2(0,1.47f)};
                for(int i=0;i<path.Length-1;i++){
                    var direction=(path[i+1]-path[i]).normalized;var side=new Vector2(-direction.y,direction.x)*.021f;
                    var a=OnHood(path[i]-side);var b=OnHood(path[i]+side);var c=OnHood(path[i+1]-side);var d=OnHood(path[i+1]+side);Face(a,b,c,true);Face(b,d,c,true);
                }
                foreach(var center in path)Disc(center,.021f);
                Disc(new Vector2(0,1.398f),.026f);
            }
            void Disc(Vector2 center,float r){for(int i=0;i<10;i++){float a=i*Mathf.PI/5,b=(i+1)*Mathf.PI/5;Face(OnHood(center),OnHood(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r),OnHood(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r),true);}}
            public Mesh Build()
            {
                for(int i=0;i<vertices.Count;i++){var v=vertices[i];vertices[i]=new Vector3(v.x,v.y*.74f,v.z)*SkyCharacterMesh.DisplayScale;}
                var mesh=new Mesh{name="Hidden character black hood and white question mark"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
        public static Mesh Create()
        {
            var b=new Builder();b.Box(new Vector3(-.105f,.08f,0),new Vector3(.15f,.16f,.22f));b.Box(new Vector3(.105f,.08f,0),new Vector3(.15f,.16f,.22f));
            b.Ring(.18f,.25f,.28f,.31f);b.Ring(.25f,1.06f,.31f,.20f);b.Ring(1.06f,1.16f,.20f,.12f);
            b.Ring(1.12f,1.15f,.20f,.22f);b.Ring(1.15f,1.24f,.22f,.20f);
            b.Ring(1.24f,1.32f,.20f,.31f);b.Ring(1.32f,1.70f,.31f,.27f);b.Ring(1.70f,1.79f,.27f,.11f);b.Ring(1.79f,1.80f,.11f,0);
            b.Mark();return b.Build();
        }
    }
}
