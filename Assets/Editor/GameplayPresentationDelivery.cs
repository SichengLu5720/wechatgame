using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
using UnityEditor;
using UnityEngine;

// All search happens here, never on the player's device.
public static class GameplayPresentationDelivery
{
    [Serializable] public sealed class Result
    {
        public string id,reason;public string[] operations;
        public float beforeWidth,afterWidth,beforeSize,afterSize,beforeSeconds,afterSeconds;
        public int routes,moves;public bool fallback;
        public GameplayScreenSafety.Result beforeVisibility,afterVisibility;
    }
    [Serializable] public sealed class Report {public Result[] cases;}
    sealed class Candidate {public LevelSpec level;public WalkSpace space;public float score;public string operation;}
    static readonly Rect PhoneArea=new GameplayLayout(390,844,new Rect(0,34,390,766),new Rect(285,752,90,32),true).PlayPixels;
    public static void Generate()
    {
        try {
            var entries=new List<LevelPresentationEntry>();var reports=new List<Result>();var keys=new HashSet<string>();
            // Read source catalogs, never a previously folded runtime catalog.
            foreach(string resource in new[]{"tutorial-v1","campaign-v1","levels","daily-v1"}){
                var source=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>(resource).text);
                for(int i=0;i<source.levels.Length;i++){
                    var level=source.levels[i];string key=LevelPresentation.Signature(level);if(!keys.Add(key))continue;
                    Result result;entries.Add(Deliver(resource+"-"+(i+1),level,out result));reports.Add(result);
                    Debug.Log("PRESENTATION "+result.id+" folds="+result.operations.Length+" width="+result.beforeWidth+" -> "+result.afterWidth+" reason="+result.reason);
                }
            }
            File.WriteAllText("Assets/Resources/gameplay-presentation-v1.json",JsonUtility.ToJson(new LevelPresentationCatalog{entries=entries.ToArray()},true));
            Directory.CreateDirectory("artifacts/task005");File.WriteAllText("artifacts/task005/delivery.json",JsonUtility.ToJson(new Report{cases=reports.ToArray()},true));
            SyncMaterials();AssetDatabase.Refresh();Debug.Log("PRESENTATION DELIVERY COMPLETE "+entries.Count);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static LevelPresentationEntry Deliver(string id,LevelSpec source,out Result result)
    {
        var original=NavigationFactory.Create(source);var envelope=GameplayFraming.Envelope(original);
        var direction=GameplayPresentation.DefaultDirection;float best=float.PositiveInfinity;
        // Discrete fixed oblique orientations, chosen once as part of the delivery.
        for(int q=0;q<4;q++){
            var d=Quaternion.AngleAxis(q*90,Vector3.up)*GameplayPresentation.DefaultDirection;
            float score=Score(envelope,d);if(score<best-.001f){best=score;direction=d;}
        }
        var rotation=Quaternion.LookRotation(-direction.normalized,Vector3.up);
        Rect before=GameplayFraming.Project(envelope,rotation);
        var current=source;var currentSpace=original;var operations=new List<string>();
        var baseline=NavigationDataVerification.Check(id+"-before",source);
        result=new Result{id=id,beforeWidth=before.width,beforeSize=best,beforeSeconds=baseline.duration,beforeVisibility=GameplayScreenSafety.Analyze(original,direction)};
        if(baseline.error==null)for(int iteration=0;iteration<2;iteration++){
            var currentBounds=GameplayFraming.Project(GameplayFraming.Envelope(currentSpace),rotation);
            if(currentBounds.width/PhoneArea.width<=currentBounds.height/PhoneArea.height)break;
            var candidates=new List<Candidate>();
            for(int e=0;e<current.edges.Length;e++)foreach(bool reverse in new[]{false,true}){
                var edge=current.edges[e];int pivot=reverse?edge.b:edge.a,root=reverse?edge.a:edge.b;
                var branch=Branch(current,e,root,pivot);if(branch==null||branch.Count>current.nodes.Length/2)continue;
                foreach(int degrees in new[]{90,-90,180}){
                    var candidate=LevelShare.Copy(current);var center=currentSpace.Centers[pivot];
                    foreach(int node in branch){var p=currentSpace.Centers[node];float dx=p.x-center.x,dz=p.z-center.z;
                        var moved=degrees==90?new Vector3(center.x+dz,p.y,center.z-dx):degrees==-90?new Vector3(center.x-dz,p.y,center.z+dx):new Vector3(center.x-dx,p.y,center.z-dz);
                        var local=currentSpace.Geometry.Config.WorldToLevel(moved);candidate.nodes[node].x=local.x;candidate.nodes[node].z=local.z;
                    }
                    try {
                        var geometry=NavigationFactory.Create(candidate);var points=GameplayFraming.Envelope(geometry);
                        float score=Score(points,direction);
                        if(score>=best*.985f||GameplayFraming.Project(points,rotation).width>=currentBounds.width*.985f||!Clear(geometry,rotation))continue;
                        candidates.Add(new Candidate{level=candidate,space=geometry,score=score,operation="edge "+e+" pivot "+pivot+" root "+root+" rotate "+degrees});
                    }catch(Exception){ /* Invalid generated staircase: deterministic fallback candidate. */ }
                }
            }
            // Stable ordering makes ties independent of machine speed.
            Candidate accepted=null;
            foreach(var c in candidates.OrderBy(c=>c.score).ThenBy(c=>c.operation,StringComparer.Ordinal).Take(8)){
                try{LayoutSafety.Validate(c.space);}catch(Exception){continue;}
                if(!GameplayScreenSafety.NoWorse(result.beforeVisibility,GameplayScreenSafety.Analyze(c.space,direction)))continue;
                var check=NavigationDataVerification.Check(id+"-candidate",c.level);
                if(check.error!=null||check.solvedState!=baseline.solvedState)continue;
                accepted=c;result.afterSeconds=check.duration;result.routes=check.routes;result.moves=check.moves;break;
            }
            if(accepted==null)break;
            current=accepted.level;currentSpace=accepted.space;best=accepted.score;operations.Add(accepted.operation);
        }
        result.operations=operations.ToArray();result.afterWidth=GameplayFraming.Project(GameplayFraming.Envelope(currentSpace),rotation).width;result.afterSize=best;
        result.afterVisibility=GameplayScreenSafety.Analyze(currentSpace,direction);
        result.fallback=operations.Count==0;result.reason=baseline.error==null?(result.fallback?"No legal improving fold; original geometry retained":"Validated folded geometry"):"Pre-existing navigation/witness issue; original geometry retained: "+baseline.error;
        if(result.fallback){result.afterSeconds=baseline.duration;result.routes=baseline.routes;result.moves=baseline.moves;}
        return new LevelPresentationEntry{sourceSignature=LevelPresentation.Signature(source),geometrySignature=LevelPresentation.Signature(current),direction=direction,positions=current.nodes.Select(n=>new Vector3(n.x,n.y,n.z)).ToArray(),operations=operations.ToArray()};
    }
    static float Score(Vector3[] envelope,Vector3 direction)=>GameplayFraming.RequiredSize(GameplayFraming.Project(envelope,Quaternion.LookRotation(-direction.normalized,Vector3.up)),PhoneArea,844);
    static List<int> Branch(LevelSpec level,int cut,int root,int pivot)
    {
        var nodes=new List<int>{root};var seen=new HashSet<int>{root};
        for(int at=0;at<nodes.Count;at++)for(int i=0;i<level.edges.Length;i++){
            if(i==cut)continue;var e=level.edges[i];int next=e.a==nodes[at]?e.b:e.b==nodes[at]?e.a:-1;
            if(next==pivot)return null;if(next>=0&&seen.Add(next))nodes.Add(next);
        }
        return nodes;
    }
    static Vector3[] Platform(WalkSpace s,int n)
    {
        float radius=s.Half(n)/Mathf.Cos(Mathf.PI/s.Sides[n]);var result=new Vector3[s.Sides[n]];
        for(int i=0;i<result.Length;i++){float a=s.Rotations[n]+(i+.5f)*Mathf.PI*2/result.Length;result[i]=s.Centers[n]+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}return result;
    }
    static Vector2[] Project(IEnumerable<Vector3> points,Vector3 right,Vector3 up)=>points.Select(p=>new Vector2(Vector3.Dot(p,right),Vector3.Dot(p,up))).ToArray();
    static bool Overlap(Vector2[] a,Vector2[] b)
    {
        foreach(var polygon in new[]{a,b})for(int i=0;i<polygon.Length;i++){
            var d=polygon[(i+1)%polygon.Length]-polygon[i];var axis=new Vector2(-d.y,d.x).normalized;
            float amin=a.Min(v=>Vector2.Dot(v,axis)),amax=a.Max(v=>Vector2.Dot(v,axis));
            float bmin=b.Min(v=>Vector2.Dot(v,axis)),bmax=b.Max(v=>Vector2.Dot(v,axis));
            if(amax<=bmin+.025f||bmax<=amin+.025f)return false;
        }return true;
    }
    static bool Clear(WalkSpace s,Quaternion rotation)
    {
        var polygons=Enumerable.Range(0,s.Centers.Length).Select(n=>Platform(s,n)).ToArray();
        for(int a=0;a<polygons.Length;a++)for(int b=a+1;b<polygons.Length;b++){
            if(Overlap(Project(polygons[a],Vector3.right,Vector3.forward),Project(polygons[b],Vector3.right,Vector3.forward)))return false;
            if(Overlap(Project(polygons[a],rotation*Vector3.right,rotation*Vector3.up),Project(polygons[b],rotation*Vector3.right,rotation*Vector3.up)))return false;
        }
        var stairs=new List<(StairSurface stair,Vector2[] shape)>();
        foreach(var stair in s.Stairs){
            var side=Vector3.Cross(Vector3.up,stair.Direction)*stair.width/2;
            var vertices=stair.polygon!=null?stair.polygon.ToArray():new[]{stair.start-side,stair.end-side,stair.end+side,stair.start+side};
            var shape=Project(vertices,Vector3.right,Vector3.forward);
            for(int n=0;n<polygons.Length;n++)if(n!=stair.a&&n!=stair.b&&Overlap(shape,Project(polygons[n],Vector3.right,Vector3.forward)))return false;
            foreach(var prior in stairs)if(prior.stair.a!=stair.a&&prior.stair.a!=stair.b&&prior.stair.b!=stair.a&&prior.stair.b!=stair.b&&Overlap(shape,prior.shape))return false;
            stairs.Add((stair,shape));
        }return true;
    }
    static void SyncMaterials()
    {
        var library=Resources.Load<RefinedArtLibrary>("RefinedArtLibrary");
        for(int i=0;i<ColorCatalog.Count;i++){
            uint rgb=ColorCatalog.Rgb[i];var color=new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);
            library.characters[i].SetColor("_ClothColor",color);EditorUtility.SetDirty(library.characters[i]);
        }
        AssetDatabase.SaveAssets();
    }
}
