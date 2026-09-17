using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class NavigationGeometryVerification
{
    [Serializable] sealed class Report {public bool passed;public int cacheCases,parameterCases,physicalTreads,footSamples,edgeRoutes;public string[] invalidationReasons;public string error;}
    static Report report;
    static void Require(bool value,string message){if(!value)throw new Exception("TASK-004: "+message);}
    static LevelSpec Fixture(float height=2,float z=0)
    {
        return new LevelSpec{name="navigation fixture",nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0}},new NodeSpec{x=8,y=height,z=z,capacity=4,transit=true,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0}},solution=new EdgeSpec[0]};
    }
    static void EqualCells(WalkSpace a,WalkSpace b)
    {
        Require(a.Width==b.Width&&a.Depth==b.Depth,"grid changed");
        for(int i=0;i<a.Heights.Length;i++)Require(a.Heights[i].Equals(b.Heights[i])&&a.Owners[i]==b.Owners[i],"rebuilt cell differs "+i);
    }
    static byte[] Change(byte[] source,int offset,byte value){var copy=(byte[])source.Clone();copy[offset]=value;return copy;}
    static void RecheckDigest(byte[] data){using(var sha=SHA256.Create()){var digest=sha.ComputeHash(data,88,data.Length-88);Buffer.BlockCopy(digest,0,data,56,32);}}
    static void Caches()
    {
        var l=Fixture();var reference=NavigationFactory.ForOfflineValidation(l);var baked=reference.Bake();var reasons=new List<string>();
        var badLength=(byte[])baked.Clone();Buffer.BlockCopy(BitConverter.GetBytes(int.MaxValue),0,badLength,92,4);RecheckDigest(badLength);
        var infinite=(byte[])baked.Clone();Buffer.BlockCopy(BitConverter.GetBytes(float.PositiveInfinity),0,infinite,88,4);RecheckDigest(infinite);
        var owner=(byte[])baked.Clone();owner[96]=128;RecheckDigest(owner);
        var cases=new[]{(byte[])null,Array.Empty<byte>(),baked.Take(20).ToArray(),baked.Take(baked.Length-1).ToArray(),Change(baked,4,99),Change(baked,8,(byte)(baked[8]^1)),Change(baked,40,(byte)(baked[40]^1)),Change(baked,baked.Length-1,(byte)(baked[baked.Length-1]^1)),badLength,infinite,owner};
        foreach(var bytes in cases){var rebuilt=NavigationFactory.ForOfflineValidation(l,bytes);Require(rebuilt.CacheStatus!="Valid","bad cache accepted");EqualCells(reference,rebuilt);reasons.Add(rebuilt.CacheStatus);report.cacheCases++;}
        var good=NavigationFactory.ForOfflineValidation(l,baked);Require(good.CacheStatus=="Valid","valid cache rejected");EqualCells(reference,good);report.cacheCases++;
        foreach(var old in Directory.GetFiles("artifacts/task004/legacy-resource-caches","*.bytes",SearchOption.AllDirectories).Take(1)){
            var rebuilt=NavigationFactory.ForOfflineValidation(l,File.ReadAllBytes(old));Require(rebuilt.CacheStatus=="Unsupported version","old version accepted");EqualCells(reference,rebuilt);report.cacheCases++;reasons.Add(rebuilt.CacheStatus);
        }
        var changed=new WalkGeometryConfig(layerHeightScale:1.1f);var altered=NavigationFactory.ForOfflineValidation(l,baked,changed);
        Require(altered.CacheStatus=="Geometry signature mismatch","changed config failed invalidation");EqualCells(altered,NavigationFactory.ForOfflineValidation(l,null,changed));report.cacheCases++;
        var crowded=new LevelSpec{nodes=Enumerable.Range(0,70).Select(i=>new NodeSpec{capacity=4,queue=new int[0],transit=true}).ToArray(),edges=new EdgeSpec[0],groups=new GroupSpec[0]};
        var many=NavigationFactory.ForOfflineValidation(crowded);var round=NavigationFactory.ForOfflineValidation(crowded,many.Bake());Require(round.CacheStatus=="Valid"&&many.NodeMask(0)!=many.NodeMask(64),"extended surface mask");EqualCells(many,round);report.cacheCases++;
        report.invalidationReasons=reasons.ToArray();
    }
    static void Geometry()
    {
        var signatures=new HashSet<string>();
        foreach(var config in new[]{WalkGeometryConfig.Default,new WalkGeometryConfig(1.1f),new WalkGeometryConfig(2),new WalkGeometryConfig(targetRiserHeight:.16f),new WalkGeometryConfig(targetRiserHeight:.3f),new WalkGeometryConfig(stairWidth:3f),new WalkGeometryConfig(stairWidth:3.8f)}){
            var signature=NavigationFactory.Create(Fixture(),config).GeometrySignature;Require(signatures.Add(signature),"config signature collision");
            foreach(float height in new[]{0f,2f,-2f})foreach(float z in new[]{0f,3f}){
                var l=Fixture(height,z);var space=NavigationFactory.Create(l,config);Require(space.Heights.Length==0,"runtime dense allocation");
                foreach(var edge in l.edges)foreach(bool reverse in new[]{false,true}){int a=reverse?edge.b:edge.a,b=reverse?edge.a:edge.b;var path=Navigator.Find(space,new[]{space.Centers[a]+Vector3.up*WalkSpace.FootGap},0,space.Centers[b]+Vector3.up*WalkSpace.FootGap,space.RouteMask(new[]{a,b}));Require(path.Count>1,"parameter edge path");report.edgeRoutes++;}
                Require(!space.SupportedHeightChange(new Vector2(200,200),new Vector2(200.15f,200),0,3),"unsupported cliff accepted");
                var state=Rules.Initial(l);var plan=FastMovement.Build(space,state,Rules.Preview(l,state,0,1));
                foreach(var track in plan.tracks)for(int i=0;i<=100;i++){
                    float t=track.start+track.duration*i/100f;var p=track.PlaybackPosition(t);float h;Require(space.FootHeight(new Vector2(p.x,p.z),out h)&&Mathf.Abs(p.y-h-WalkSpace.FootGap)<.001f,"parameter foot height");report.footSamples++;
                }
                if(z==0&&height==2)PhysicalTreads(space);
                report.parameterCases++;
            }
        }
        // Generator input is snapshotted: changing the draft cannot corrupt already
        // compiled treads, signatures or PlaybackFloor/corridor identities.
        var mutable=Fixture();var old=NavigationFactory.Create(mutable);var oldHash=old.GeometrySignature;var center=old.Centers[1];mutable.nodes[1].y+=2;
        var next=NavigationFactory.Create(mutable);Require(old.GeometrySignature==oldHash&&old.Centers[1]==center&&next.GeometrySignature!=oldHash,"immutable snapshot");
        Require(!ReferenceEquals(PlaybackFloor.For(old),PlaybackFloor.For(next)),"height profiles reused across geometry");
        var diagonal=Fixture(0,3);var point=WalkGeometryConfig.Default.WorldCenter(diagonal.nodes[1]);var local=WalkGeometryConfig.Default.WorldToLevel(point);
        Require(Mathf.Abs(local.x-diagonal.nodes[1].x)<.00001f&&Mathf.Abs(local.z-diagonal.nodes[1].z)<.00001f,"coordinate roundtrip");
        var invalid=Fixture();invalid.nodes[1].x=0;Require(NavigationFactory.ForDraft(invalid).Geometry.MissingEdges.Length==1,"incomplete draft preview");
        bool rejected=false;try{NavigationFactory.Create(invalid);}catch(InvalidOperationException){rejected=true;}Require(rejected,"incomplete generated delivery accepted");
        var hub=new LevelSpec{nodes=new NodeSpec[8],edges=new EdgeSpec[7],groups=new GroupSpec[0]};hub.nodes[0]=new NodeSpec{capacity=4,queue=new int[0],transit=true};
        for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;var c=WalkGeometryConfig.Default.WorldToLevel(new Vector3(Mathf.Cos(a)*22,4,Mathf.Sin(a)*22));hub.nodes[i+1]=new NodeSpec{x=c.x,y=c.y,z=c.z,capacity=4,queue=new int[0],transit=true};hub.edges[i]=new EdgeSpec{a=0,b=i+1};}
        var junction=NavigationFactory.Create(hub);Require(junction.Sides[0]==7,"high-degree platform");
        foreach(var e in hub.edges)foreach(bool reverse in new[]{false,true}){int a=reverse?e.b:e.a,b=reverse?e.a:e.b;Navigator.Find(junction,new[]{junction.Centers[a]+Vector3.up*WalkSpace.FootGap},0,junction.Centers[b]+Vector3.up*WalkSpace.FootGap,junction.RouteMask(new[]{a,b}));report.edgeRoutes++;}
        var sparse=new WalkSpace(new LevelSpec{nodes=new[]{new NodeSpec{x=-10000,capacity=4,queue=new int[0]},new NodeSpec{x=10000,capacity=4,queue=new int[0]}},edges=new EdgeSpec[0],groups=new GroupSpec[0]});
        Require(sparse.Heights.Length==0,"oversized dense fallback");var id=(long)Mathf.RoundToInt((sparse.Centers[0].z-sparse.MinZ)/WalkSpace.GridStep)*sparse.Width+Mathf.RoundToInt((sparse.Centers[0].x-sparse.MinX)/WalkSpace.GridStep);Require(!float.IsNaN(sparse.HeightAt(id)),"large sparse sampling");
    }
    static void PhysicalTreads(WalkSpace space)
    {
        var camera=new GameObject("Navigation geometry check camera").AddComponent<Camera>();CrowdScene scene=null;
        try{
            scene=new CrowdScene(space,camera);Physics.SyncTransforms();
            foreach(var s in space.Stairs)if(s.polygon==null&&s.steps>1)foreach(var t in s.Treads){
                var colliders=scene.root.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Stair "+s.index+" step "+Array.IndexOf(s.Treads.ToArray(),t)).ToArray();
                Require(colliders.Length==1,"actual tread collider missing");RaycastHit hit;
                Require(colliders[0].Raycast(new Ray(t.Center+Vector3.up*10,Vector3.down),out hit,20),"actual tread raycast");
                Require(Mathf.Abs(hit.point.y-t.Height)<.0001f,"rendered/collision top differs from support");report.physicalTreads++;
            }
        }finally{if(scene!=null)scene.Dispose();UnityEngine.Object.DestroyImmediate(camera.gameObject);}
    }
    public static void Run()
    {
        report=new Report();try{Caches();Geometry();report.passed=true;}catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
        Directory.CreateDirectory("artifacts/task004/after");File.WriteAllText("artifacts/task004/after/cache-geometry.json",JsonUtility.ToJson(report,true));
        Debug.Log("NAVIGATION CACHE GEOMETRY "+(report.passed?"PASSED":"FAILED"));EditorApplication.Exit(report.passed?0:1);
    }
}
