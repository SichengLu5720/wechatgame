using System;
using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.ArtProduction
{
    // Offline construction only. Exported prefabs contain no generators or behaviours.
    public sealed class RefinedMesh
    {
        readonly List<Vector3> vertices=new List<Vector3>(), normals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<Vector2> uv=new List<Vector2>(), surface=new List<Vector2>();
        readonly List<int> triangles=new List<int>();
        public Color Tint=Color.white; public float Roughness=.8f, Cloth=0, Occlusion=1;
        public Matrix4x4 Transform=Matrix4x4.identity;
        public int Count=>vertices.Count;
        public void Smooth(int start)
        {
            var points=new Dictionary<Vector3,List<int>>();
            for(int i=start;i<vertices.Count;i++){var p=vertices[i];p=new Vector3(Mathf.Round(p.x*10000)/10000,Mathf.Round(p.y*10000)/10000,Mathf.Round(p.z*10000)/10000);if(!points.TryGetValue(p,out var list)){list=new List<int>();points[p]=list;}list.Add(i);}
            var original=normals.ToArray();foreach(var list in points.Values)foreach(var index in list){var n=Vector3.zero;foreach(var other in list)if(Vector3.Dot(original[index],original[other])>.62f)n+=original[other];normals[index]=n.normalized;}
        }
        public void Material(Color color,float roughness=.8f,float cloth=0,float ao=1){Tint=color;Roughness=roughness;Cloth=cloth;Occlusion=ao;}
        public void Tri(Vector3 a,Vector3 b,Vector3 c)
        {
            var normal=Vector3.Cross(b-a,c-a).normalized;
            if(normal.sqrMagnitude<.5f)return;
            int n=vertices.Count;foreach(var p in new[]{a,b,c}){
                var point=Transform.MultiplyPoint3x4(p);var direction=Transform.MultiplyVector(normal).normalized;
                vertices.Add(point);normals.Add(direction);colors.Add(new Color(Tint.r,Tint.g,Tint.b,Occlusion));surface.Add(new Vector2(Roughness,Cloth));
                var axis=new Vector3(Mathf.Abs(direction.x),Mathf.Abs(direction.y),Mathf.Abs(direction.z));
                uv.Add(axis.y>=axis.x&&axis.y>=axis.z?new Vector2(point.x,point.z):axis.x>=axis.z?new Vector2(point.z,point.y):new Vector2(point.x,point.y));
            }
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
        public void Box(Vector3 center,Vector3 size,float bevel=.035f)
        {
            bevel=Mathf.Min(bevel,Mathf.Min(size.y,Mathf.Min(size.x,size.z))*.45f);
            Vector3[][] ring=new Vector3[4][];
            for(int r=0;r<4;r++){
                float shrink=r==0||r==3?bevel:0, x=size.x/2-shrink,z=size.z/2-shrink;
                float edge=Mathf.Min(bevel,Mathf.Min(x,z)*.45f);float y=r==0?-size.y/2:r==1?-size.y/2+bevel:r==2?size.y/2-bevel:size.y/2;
                ring[r]=new[]{new Vector3(-x+edge,y,-z),new Vector3(x-edge,y,-z),new Vector3(x,y,-z+edge),new Vector3(x,y,z-edge),new Vector3(x-edge,y,z),new Vector3(-x+edge,y,z),new Vector3(-x,y,z-edge),new Vector3(-x,y,-z+edge)};
                for(int j=0;j<8;j++)ring[r][j]+=center;
            }
            for(int r=0;r<3;r++)for(int j=0;j<8;j++){int k=(j+1)%8;Quad(ring[r][j],ring[r+1][j],ring[r+1][k],ring[r][k]);}
            for(int j=0;j<8;j++){int k=(j+1)%8;Tri(center+Vector3.up*size.y/2,ring[3][k],ring[3][j]);Tri(center-Vector3.up*size.y/2,ring[0][j],ring[0][k]);}
        }
        public void Ribbon(Vector3 a,Vector3 b,float width,float thickness=.006f)
        {
            var old=Transform;var middle=(a+b)/2;Transform=old*Matrix4x4.TRS(middle,Quaternion.LookRotation(b-a),Vector3.one);
            Box(Vector3.zero,new Vector3(width,thickness,(b-a).magnitude),thickness*.25f);Transform=old;
        }
        public void Gem(Vector3 center,float width,float height,float depth)
        {
            var a=center+Vector3.up*height/2;var b=center+Vector3.right*width/2;var c=center-Vector3.up*height/2;var d=center-Vector3.right*width/2;var p=center+Vector3.forward*depth;
            Tri(a,d,p);Tri(d,c,p);Tri(c,b,p);Tri(b,a,p);
        }
        public Mesh Finish(string name)
        {
            var vv=new List<Vector3>();var nn=new List<Vector3>();var cc=new List<Color>();var uu=new List<Vector2>();var ss=new List<Vector2>();var tt=new List<int>();
            var unique=new Dictionary<(Vector3,Vector3,Color,Vector2,Vector2),int>();
            foreach(int i in triangles){var key=(vertices[i],normals[i],colors[i],uv[i],surface[i]);if(!unique.TryGetValue(key,out int index)){index=vv.Count;unique.Add(key,index);vv.Add(vertices[i]);nn.Add(normals[i]);cc.Add(colors[i]);uu.Add(uv[i]);ss.Add(surface[i]);}tt.Add(index);}
            var mesh=new Mesh{name=name};if(vv.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vv);mesh.SetNormals(nn);mesh.SetColors(cc);mesh.SetUVs(0,uu);mesh.SetUVs(1,ss);mesh.SetTriangles(tt,0);mesh.RecalculateBounds();return mesh;
        }
    }

    public static partial class RefinedModels
    {
        public static readonly Color Stone=Hex("E9DBB7"),LightStone=Hex("F4E7C9"),StoneSide=Hex("C6B891"),Jade=Hex("3B8582"),Gold=Hex("BF9147"),Linen=Hex("EFE1BF"),Dark=Hex("171F23");
        public static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        static Vector3 Radial(float a,float y,float r){return new Vector3(Mathf.Sin(a)*r,y,Mathf.Cos(a)*r);}
        static void Ring(RefinedMesh m,float lower,float upper,float r0,float r1,int segments=40)
        {
            for(int i=0;i<segments;i++){float a=i*2*Mathf.PI/segments,b=(i+1)*2*Mathf.PI/segments;m.Quad(Radial(a,lower,r0),Radial(b,lower,r0),Radial(b,upper,r1),Radial(a,upper,r1));}
        }
        static float Fold(float a,float height){return Mathf.Lerp(.028f,.004f,Mathf.InverseLerp(.23f,1.08f,height))*Mathf.Cos(a*8);}
        static Vector3 CoatPoint(float a,float y,float radius){return Radial(a,y+(1-Mathf.Cos(a*2))*.006f,radius+Fold(a,y));}
        public static Mesh Character()
        {
            var m=new RefinedMesh();m.Transform=Matrix4x4.Scale(new Vector3(1.28f,1.11f,1.28f));
            // One mesh shared by all six colorways; cloth tint is a material property.
            m.Material(Hex("776147"),.92f,0,.88f);
            for(int side=-1;side<=1;side+=2){m.Box(new Vector3(side*.104f,.078f,.018f),new Vector3(.155f,.15f,.254f),.035f);}
            m.Material(Hex("AF9261"),.9f);for(int side=-1;side<=1;side+=2)m.Box(new Vector3(side*.104f,.038f,.022f),new Vector3(.165f,.045f,.259f),.012f);
            float[] heights={.19f,.24f,.40f,.68f,.90f,1.06f,1.12f};float[] radii={.29f,.304f,.285f,.245f,.215f,.198f,.13f};
            int clothStart=m.Count;
            for(int r=0;r<heights.Length-1;r++)for(int i=0;i<60;i++){
                float a=i*2*Mathf.PI/60,b=(i+1)*2*Mathf.PI/60;
                float ao=.86f+.14f*(.5f+.5f*Mathf.Cos((a+b)*4));m.Material(Color.white,.95f,1,ao);
                m.Quad(CoatPoint(a,heights[r],radii[r]),CoatPoint(b,heights[r],radii[r]),CoatPoint(b,heights[r+1],radii[r+1]),CoatPoint(a,heights[r+1],radii[r+1]));
            }
            m.Smooth(clothStart);
            // Wide tailored hem, small running stitches, central diamond clasp.
            m.Material(Linen,.9f);
            for(int i=0;i<60;i++){float a=i*2*Mathf.PI/60,b=(i+1)*2*Mathf.PI/60;m.Quad(CoatPoint(a,.205f,.303f),CoatPoint(b,.205f,.303f),CoatPoint(b,.225f,.307f),CoatPoint(a,.225f,.307f));}
            for(int i=0;i<36;i++){float a=i*2*Mathf.PI/36,b=a+.045f;m.Ribbon(CoatPoint(a,.273f,.301f),CoatPoint(b,.273f,.301f),.01f,.009f);}
            m.Material(Linen,.85f);m.Gem(new Vector3(0,.46f,.305f),.065f,.112f,.005f);
            m.Material(Gold,.48f);m.Gem(new Vector3(0,.46f,.312f),.026f,.055f,.007f);
            m.Material(Linen,.9f);m.Gem(new Vector3(0,.80f,.246f),.018f,.038f,.004f);
            // Shoulder cape and the overlapping scarf with a visible folded tail.
            m.Material(Linen,.93f,0,.84f);Ring(m,1.04f,1.11f,.215f,.23f);
            m.Material(Linen,.9f);Ring(m,1.11f,1.17f,.23f,.198f);Ring(m,1.17f,1.21f,.198f,.16f);
            m.Material(Hex("D3C298"),.92f);var sa=new Vector3(-.19f,1.122f,.226f);var sb=new Vector3(.067f,.970f,.250f);var sc=new Vector3(.19f,1.14f,.225f);var inset=Vector3.back*.016f;
            m.Tri(sa+inset,sc+inset,sb+inset);m.Quad(sa,sb,sb+inset,sa+inset);m.Quad(sb,sc,sc+inset,sb+inset);
            m.Material(Linen,.9f);m.Tri(sa,sb,sc);
            // Open hood: front lip, recessed dark face, sculpted shell and raised crown badge.
            const int n=48;Vector3[] outer=new Vector3[n],inner=new Vector3[n],middle=new Vector3[n],rear=new Vector3[n];
            for(int i=0;i<n;i++){
                float a=i*2*Mathf.PI/n;float x=-Mathf.Sin(a), y=Mathf.Cos(a);
                float taper=y>0?1-.38f*y:1;outer[i]=new Vector3(x*.288f*taper,1.456f+y*.306f,.248f-.108f*Mathf.Max(0,y));
                inner[i]=new Vector3(x*.201f*taper,1.424f+y*.194f,.259f-.06f*Mathf.Max(0,y));
                middle[i]=new Vector3(x*.31f*taper,1.48f+y*.31f,-.035f);
                rear[i]=new Vector3(x*.195f*taper,1.473f+y*.22f,-.24f);
            }
            int hoodStart=m.Count;
            for(int i=0;i<n;i++){
                int k=(i+1)%n;m.Material(Linen,.92f);
                m.Quad(outer[i],middle[i],middle[k],outer[k]);m.Quad(middle[i],rear[i],rear[k],middle[k]);m.Tri(rear[i],new Vector3(0,1.47f,-.292f),rear[k]);
            }
            m.Smooth(hoodStart);
            for(int i=0;i<n;i++){
                int k=(i+1)%n;
                m.Material(Hex("E4D2AB"),.9f);m.Quad(outer[i],outer[k],inner[k],inner[i]);
                m.Material(Hex("77664D"),.95f,0,.82f);m.Quad(inner[i],inner[k],inner[k]-Vector3.forward*.045f,inner[i]-Vector3.forward*.045f);
                m.Material(Dark,1,0,.64f);m.Tri(inner[i]-Vector3.forward*.045f,inner[k]-Vector3.forward*.045f,new Vector3(0,1.422f,.141f));
            }
            var characterSpace=m.Transform;m.Transform=characterSpace*Matrix4x4.TRS(new Vector3(0,1.706f,.169f),Quaternion.Euler(-20,0,0),Vector3.one);
            m.Material(Color.white,.7f,1);m.Gem(Vector3.zero,.075f,.105f,.016f);m.Transform=characterSpace;
            // Two subtle hood seam lines, sewn into the outer surface.
            m.Material(Hex("C7B78F"),.97f);m.Ribbon(new Vector3(-.147f,1.661f,.213f),new Vector3(-.235f,1.51f,.255f),.006f,.004f);
            m.Ribbon(new Vector3(.147f,1.661f,.213f),new Vector3(.235f,1.51f,.255f),.006f,.004f);
            return m.Finish("Pilgrim tailored cloak open hood - shared six colors");
        }
        static void Corner(RefinedMesh m,float x,float y,float z)
        {
            m.Material(StoneSide);m.Box(new Vector3(x,y-.12f,z),new Vector3(.58f,.18f,.58f),.035f);
            m.Material(Gold,.35f);m.Box(new Vector3(x,y-.021f,z),new Vector3(.55f,.04f,.55f),.012f);
            m.Material(Jade,.29f);m.Box(new Vector3(x,y+.10f,z),new Vector3(.55f,.21f,.55f),.043f);
            m.Material(Gold,.32f);m.Box(new Vector3(x,y+.215f,z),new Vector3(.21f,.035f,.21f),.013f);
            m.Material(Hex("478F87"),.38f);m.Box(new Vector3(x,y+.236f,z),new Vector3(.14f,.01f,.14f),.006f);
        }
        static void Arch(RefinedMesh m,float half,float top,float baseY)
        {
            float radius=half-.48f, spring=top-2.38f, rise=1.68f, depth=.42f;
            for(int side=0;side<4;side++){
                var old=m.Transform;m.Transform=old*Matrix4x4.Rotate(Quaternion.Euler(0,side*90,0));
                m.Material(StoneSide,.85f);m.Box(new Vector3(-half+.235f,(top+baseY)/2,half-.23f),new Vector3(.47f,top-baseY,.47f),.035f);
                for(int j=0;j<28;j++){
                    float a=j*Mathf.PI/28,b=(j+1)*Mathf.PI/28;
                    float x=-Mathf.Cos(a)*radius,xx=-Mathf.Cos(b)*radius,y=spring+Mathf.Sin(a)*rise,yy=spring+Mathf.Sin(b)*rise;
                    float z=half-.08f;
                    m.Material(Stone,.87f);m.Quad(new Vector3(x,top,z),new Vector3(x,y,z),new Vector3(xx,yy,z),new Vector3(xx,top,z));
                    m.Material(StoneSide,.94f,0,.69f);m.Quad(new Vector3(x,y,z),new Vector3(x,y,z-depth),new Vector3(xx,yy,z-depth),new Vector3(xx,yy,z));
                    m.Material(StoneSide,.9f);m.Quad(new Vector3(x,top,z-depth),new Vector3(xx,top,z-depth),new Vector3(xx,yy,z-depth),new Vector3(x,y,z-depth));
                    // Bevel ribbon around the arch opening, separate from dark intrados.
                    m.Material(LightStone,.8f);m.Quad(new Vector3(x,y,z+.012f),new Vector3(xx,yy,z+.012f),new Vector3(xx,yy+.085f,z+.018f),new Vector3(x,y+.085f,z+.018f));
                }
                m.Material(Gold,.38f);m.Gem(new Vector3(0,top-.28f,half-.055f),.18f,.29f,.017f);
                m.Material(Jade,.35f);m.Gem(new Vector3(0,top-.28f,half-.035f),.11f,.21f,.016f);
                m.Transform=old;
            }
        }
        public static Mesh Platform(bool tower=true,bool clean=false)
        {
            var m=new RefinedMesh();const float size=5.2f,half=size/2;
            m.Material(StoneSide);m.Box(new Vector3(0,-.34f,0),new Vector3(5.12f,.28f,5.12f),.055f);
            m.Material(Stone);m.Box(new Vector3(0,-.165f,0),new Vector3(size,.18f,size),.045f);
            if(clean){m.Material(LightStone);m.Box(new Vector3(0,-.051f,0),new Vector3(4.98f,.10f,4.98f),.018f);}
            const int tiles=4;float pitch=4.98f/tiles;
            for(int x=0;x<(clean?0:tiles);x++)for(int z=0;z<tiles;z++){
                float shade=.965f+.035f*((x*7+z*3)%5)/4;m.Material(new Color(LightStone.r*shade,LightStone.g*shade,LightStone.b*shade));
                m.Box(new Vector3((x-1.5f)*pitch,-.051f,(z-1.5f)*pitch),new Vector3(pitch-.011f,.10f,pitch-.011f),.018f);
            }
            for(int axis=0;axis<2;axis++)for(int sign=-1;sign<=1;sign+=2){m.Material(Stone);var center=axis==0?new Vector3(sign*2.55f,-.046f,0):new Vector3(0,-.046f,sign*2.55f);var scale=axis==0?new Vector3(.10f,.09f,5.02f):new Vector3(5.02f,.09f,.10f);m.Box(center,scale,.018f);}
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Corner(m,x*(half-.2f),-.08f,z*(half-.2f));
            if(tower)Arch(m,half-.045f,-.46f,-7.8f);
            return m.Finish(tower?"Platform 5.2m arched tower":"Platform 5.2m deck");
        }
        public static Mesh Stairs()
        {
            var m=new RefinedMesh();const int steps=12;const float length=5.2f,rise=2.4f,width=4.1f;
            // Z=0 is the low endpoint. Treads remain identical across this module.
            for(int i=0;i<steps;i++){
                float y=(i+1)*rise/steps,z=(i+.5f)*length/steps;
                m.Material(StoneSide);m.Box(new Vector3(0,y-.155f,z),new Vector3(width,.31f,length/steps+.006f),.026f);
                m.Material(LightStone);m.Box(new Vector3(0,y-.023f,z-.02f),new Vector3(width+.06f,.046f,length/steps+.036f),.016f);
            }
            for(int sign=-1;sign<=1;sign+=2){m.Material(Stone);m.Ribbon(new Vector3(sign*(width/2+.055f),.13f,.02f),new Vector3(sign*(width/2+.055f),rise+.11f,length-.02f),.13f,.16f);}
            return m.Finish("Stair 12 equal treads W4.1 L5.2 H2.4");
        }
        public static Mesh Bridge(bool clean=false)
        {
            var m=new RefinedMesh();const float length=5.2f,width=4.1f;const int segments=24;
            Func<float,float> arch=t=>.22f*4*t*(1-t);
            for(int i=0;i<segments;i++){
                float t=i/(float)segments,u=(i+1)/(float)segments,y=arch(t),yy=arch(u),z=t*length,zz=u*length;
                m.Material(Stone);m.Quad(new Vector3(-width/2,y,z),new Vector3(-width/2,yy,zz),new Vector3(width/2,yy,zz),new Vector3(width/2,y,z));
                m.Material(StoneSide);m.Quad(new Vector3(-width/2,y-.27f,z),new Vector3(width/2,y-.27f,z),new Vector3(width/2,yy-.27f,zz),new Vector3(-width/2,yy-.27f,zz));
                for(int s=-1;s<=1;s+=2){float x=s*width/2;var a=new Vector3(x,y,z);var b=new Vector3(x,yy,zz);m.Material(Stone);if(s<0)m.Quad(a,a+Vector3.down*.27f,b+Vector3.down*.27f,b);else m.Quad(a,b,b+Vector3.down*.27f,a+Vector3.down*.27f);m.Material(LightStone);m.Ribbon(a+Vector3.up*.031f,b+Vector3.up*.031f,.084f,.062f);}
            }
            m.Material(StoneSide);m.Quad(new Vector3(-width/2,0,0),new Vector3(width/2,0,0),new Vector3(width/2,-.27f,0),new Vector3(-width/2,-.27f,0));m.Quad(new Vector3(-width/2,0,length),new Vector3(-width/2,-.27f,length),new Vector3(width/2,-.27f,length),new Vector3(width/2,0,length));
            if(!clean)for(int i=1;i<8;i++){float t=i/8f;m.Material(Hex("B9AE8D"));m.Ribbon(new Vector3(-width/2+.07f,arch(t)+.003f,t*length),new Vector3(width/2-.07f,arch(t)+.003f,t*length),.012f,.004f);}
            return m.Finish("Bridge 02 shallow camber 0.22m W4.1 L5.2");
        }
        public static Mesh StepUnit(){var m=new RefinedMesh();m.Material(LightStone);m.Box(Vector3.zero,Vector3.one,.025f);return m.Finish("Clean beveled step unit");}
        public static Mesh Tower()
        {
            var m=new RefinedMesh();m.Material(Stone);m.Box(new Vector3(0,-5.5f,0),new Vector3(1.6f,11,1.6f),.04f);m.Material(StoneSide);m.Box(new Vector3(0,-.14f,0),new Vector3(1.82f,.28f,1.82f),.05f);
            m.Material(LightStone);m.Box(new Vector3(0,.36f,0),new Vector3(1.15f,.8f,1.15f),.05f);m.Material(Jade);m.Box(new Vector3(0,.82f,0),new Vector3(1.2f,.16f,1.2f),.025f);
            m.Material(Stone);m.Box(new Vector3(0,1.12f,0),new Vector3(.66f,.5f,.66f),.06f);
            for(int side=0;side<4;side++){
                m.Transform=Matrix4x4.Rotate(Quaternion.Euler(0,side*90,0));m.Material(Hex("72ACA6"),1);const float w=.19f;float z=.803f,lower=-7.5f,spring=-2;
                m.Quad(new Vector3(-w,lower,z),new Vector3(w,lower,z),new Vector3(w,spring,z),new Vector3(-w,spring,z));
                for(int i=0;i<12;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;m.Tri(new Vector3(0,spring,z),new Vector3(Mathf.Cos(a)*w,spring+Mathf.Sin(a)*.42f,z),new Vector3(Mathf.Cos(b)*w,spring+Mathf.Sin(b)*.42f,z));}
            }
            return m.Finish("Distant tower no base ornament");
        }
    }
}
