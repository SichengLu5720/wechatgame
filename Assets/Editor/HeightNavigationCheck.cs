using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class HeightNavigationCheck
{
    public static void Run()
    {
        try{
            int routes=0;
            foreach(float multiplier in new[]{1f,2f,4f}){
                var level=CloudLevels.Create().levels[0];foreach(var node in level.nodes)node.y*=multiplier;
                var space=new WalkSpace(level);
                foreach(var edge in level.edges){
                    foreach(bool reverse in new[]{false,true}){
                        int from=reverse?edge.b:edge.a,to=reverse?edge.a:edge.b;
                        System.Collections.Generic.List<Vector3> path;
                        try{path=Navigator.Find(space,new[]{space.Centers[from]+Vector3.up*WalkSpace.FootGap},0,space.Centers[to]+Vector3.up*WalkSpace.FootGap,space.RouteMask(new[]{from,to}));}
                        catch(Exception){
                            foreach(var stair in space.Stairs)if(stair.polygon==null)Debug.Log("HEIGHT STAIR "+stair.a+"/"+stair.b+" "+stair.start.ToString("F5")+" to "+stair.end.ToString("F5")+" length="+stair.Length+" steps="+stair.steps);
                            var last=Vector2.zero;float lastHeight=float.NaN;
                            for(int sample=0;sample<=200;sample++){
                                var world=Vector3.Lerp(space.Centers[from],space.Centers[to],sample/200f);
                                int x=Mathf.RoundToInt((world.x-space.MinX)/WalkSpace.GridStep),z=Mathf.RoundToInt((world.z-space.MinZ)/WalkSpace.GridStep);long id=(long)z*space.Width+x;
                                var point=space.GridPoint(id);float height=space.HeightAt(id);
                                if(!float.IsNaN(lastHeight)&&!space.SupportedHeightChange(last,point,lastHeight,height))Debug.Log("HEIGHT BREAK "+last+" y="+lastHeight+" to "+point+" y="+height);
                                last=point;lastHeight=height;
                            }
                            throw;
                        }
                        if(path.Count==0)throw new Exception("Missing stair path");routes++;
                    }
                }
                if(space.SupportedHeightChange(new Vector2(200,200),new Vector2(200.15f,200),0,3))throw new Exception("Unconnected cliff accepted");
            }
            File.WriteAllText("artifacts/cloud-iteration/height-navigation.json","{\"passed\":true,\"routeChecks\":"+routes+",\"heightMultipliers\":[1,2,4],\"disconnectedCliffRejected\":true}");
            Debug.Log("HEIGHT NAVIGATION PASSED");EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
