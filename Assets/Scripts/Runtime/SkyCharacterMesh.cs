using System.Collections.Generic;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    // Shared low-poly visual meshes; navigation roots and colliders are unchanged.
    public static class SkyCharacterMesh
    {
        public const float DisplayScale=1.5f;
        sealed class Shape
        {
            readonly List<Vector3> v=new List<Vector3>();readonly List<int> t=new List<int>();readonly List<Color> colors=new List<Color>();
            void Face(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);colors.Add(Color.white);colors.Add(Color.white);colors.Add(Color.white);t.Add(n);t.Add(n+1);t.Add(n+2);}
            public void ContactShadow(){for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;var p=new Vector3(Mathf.Sin(a)*.32f,.009f,Mathf.Cos(a)*.32f);var q=new Vector3(Mathf.Sin(b)*.32f,.009f,Mathf.Cos(b)*.32f);Face(Vector3.up*.009f,p,q);int n=colors.Count;colors[n-3]=new Color(.66f,.66f,.66f,0);colors[n-2]=colors[n-1]=new Color(1,1,1,0);}}
            public void Ring(float y0,float y1,float r0,float r1,int sides=8)
            {
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;var p=new Vector3(Mathf.Sin(a)*r0,y0,Mathf.Cos(a)*r0);var q=new Vector3(Mathf.Sin(b)*r0,y0,Mathf.Cos(b)*r0);var r=new Vector3(Mathf.Sin(a)*r1,y1,Mathf.Cos(a)*r1);var s=new Vector3(Mathf.Sin(b)*r1,y1,Mathf.Cos(b)*r1);Face(p,q,r);Face(q,s,r);}
            }
            public void Box(Vector3 c,Vector3 size)
            {
                var p=new Vector3[8];for(int i=0;i<8;i++)p[i]=c+Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                foreach(var f in new[]{new[]{0,4,6,2},new[]{1,3,7,5},new[]{0,1,5,4},new[]{2,6,7,3},new[]{0,2,3,1},new[]{4,5,7,6}}){Face(p[f[0]],p[f[1]],p[f[2]]);Face(p[f[0]],p[f[2]],p[f[3]]);}
            }
            public void Diamond(float y,float z,float size){var a=new Vector3(0,y+size,z);var b=new Vector3(size*.6f,y,z);var c=new Vector3(0,y-size,z);var d=new Vector3(-size*.6f,y,z);Face(a,d,b);Face(b,d,c);}
            public void Cloak(){for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;float ra=i%2==0?.31f:.275f,rb=i%2==0?.275f:.31f;var p=new Vector3(Mathf.Sin(a)*ra,.25f,Mathf.Cos(a)*ra);var q=new Vector3(Mathf.Sin(b)*rb,.25f,Mathf.Cos(b)*rb);var r=new Vector3(Mathf.Sin(a)*.205f,1.06f,Mathf.Cos(a)*.205f);var s=new Vector3(Mathf.Sin(b)*.205f,1.06f,Mathf.Cos(b)*.205f);Face(p,q,r);Face(q,s,r);}}
            public void Crest(){var points=new[]{new Vector3(0,1.68f,-.16f),new Vector3(0,1.76f,-.08f),new Vector3(0,1.80f,.06f),new Vector3(0,1.74f,.16f),new Vector3(0,1.65f,.25f),new Vector3(0,1.58f,.315f)};for(int i=0;i<points.Length-1;i++){var p=points[i]-Vector3.right*.06f;var q=points[i+1]-Vector3.right*.06f;Face(p,q,p+Vector3.right*.12f);Face(q,q+Vector3.right*.12f,p+Vector3.right*.12f);}}
            public void Scarf(){Face(new Vector3(-.19f,1.12f,.20f),new Vector3(.06f,.89f,.244f),new Vector3(.19f,1.12f,.20f));}
            public Mesh Mesh(string name){for(int i=0;i<v.Count;i++)v[i]=new Vector3(v[i].x,v[i].y*.74f,v[i].z)*DisplayScale;var m=new Mesh{name=name};m.SetVertices(v);m.SetColors(colors);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
        public static Mesh Coat()
        {
            var s=new Shape();s.Ring(.18f,.25f,.285f,.31f);s.Cloak();s.Ring(1.06f,1.16f,.205f,.12f);s.Ring(1.16f,1.25f,.205f,.205f);
            s.Crest();s.Diamond(1.46f,.315f,.09f);s.ContactShadow();return s.Mesh("Sky pilgrim colored cloak crest and badge");
        }
        public static Mesh Trim()
        {
            var s=new Shape();s.Box(new Vector3(-.105f,.08f,0),new Vector3(.15f,.16f,.22f));s.Box(new Vector3(.105f,.08f,0),new Vector3(.15f,.16f,.22f));
            s.Ring(.19f,.25f,.29f,.312f);s.Ring(1.08f,1.13f,.21f,.185f);s.Ring(1.24f,1.32f,.17f,.265f);s.Ring(1.32f,1.45f,.265f,.31f);s.Ring(1.45f,1.60f,.31f,.28f);s.Ring(1.60f,1.73f,.28f,.14f);s.Ring(1.73f,1.77f,.14f,0);
            s.Scarf();s.Diamond(.47f,.303f,.06f);s.Diamond(.31f,.321f,.035f);return s.Mesh("Sky pilgrim ivory hood hem and feet");
        }
    }
}




