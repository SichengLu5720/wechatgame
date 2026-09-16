using SurfaceMask = StairsCrowd.Runtime.SurfaceSet;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
public static class CloudNavDebug
{
 public static void Run(){try{
  int degree=4;var l=new LevelSpec{name="Nav debug",nodes=new NodeSpec[degree+1],edges=new EdgeSpec[degree],groups=Enumerable.Range(0,4).Select(i=>new GroupSpec{id=i,color=0}).ToArray()};l.nodes[0]=CloudLevels.N(0,1,0,true);
  for(int i=0;i<degree;i++){float a=i*Mathf.PI*2/degree;l.nodes[i+1]=CloudLevels.N(Mathf.Cos(a)*6,2,Mathf.Sin(a)*6,i!=0);l.nodes[i+1].queue=i==0?new[]{0,1}:i==1?new[]{2,3}:new int[0];l.edges[i]=CloudLevels.E(0,i+1);}
  var s=new WalkSpace(l);foreach(var st in s.Stairs.Where(st=>st.index==0))Debug.Log("SURFACE "+st.start+" -> "+st.end+" poly="+(st.polygon==null?"no":string.Join(";",st.polygon.Select(p=>p.ToString()))));
  var occupied=s.Positions(Rules.Initial(l));for(int actor=0;actor<4;actor++){var empty=occupied.Select((p,i)=>i==actor?p:new Vector3(1000+i,0,1000)).ToArray();try{var path=Navigator.Find(s,empty,actor,s.Seat(0,0,actor),s.RouteMask(new[]{1,0}));Debug.Log("EMPTY PASS "+actor+" points="+path.Count);}catch(Exception e){Debug.Log("EMPTY FAIL "+actor+" "+e.Message);}}
  var image=new Texture2D(s.Width,s.Depth);SurfaceMask mask=s.RouteMask(new[]{1,0});for(int z=0;z<s.Depth;z++)for(int x=0;x<s.Width;x++){int id=z*s.Width+x;image.SetPixel(x,z,(s.Owners[id]&mask)==0?Color.black:float.IsNaN(s.Heights[id])?Color.red:Color.green);}
  foreach(var p in occupied){int x=Mathf.RoundToInt((p.x-s.MinX)/WalkSpace.GridStep),z=Mathf.RoundToInt((p.z-s.MinZ)/WalkSpace.GridStep);for(int dx=-2;dx<=2;dx++)for(int dz=-2;dz<=2;dz++)if(x+dx>=0&&z+dz>=0&&x+dx<s.Width&&z+dz<s.Depth)image.SetPixel(x+dx,z+dz,Color.white);}image.Apply();File.WriteAllBytes("artifacts/cloud-iteration/nav-debug.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
