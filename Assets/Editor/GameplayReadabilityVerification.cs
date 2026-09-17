using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
using UnityEngine;
using UnityEditor;

public static class GameplayReadabilityVerification
{
    [Serializable] public sealed class Sample {public string id;public int width,height;public float widthUse,heightUse,actorPixels,margin;}
    [Serializable] public sealed class Report {public bool passed;public int layouts,palette;public Sample[] samples;}
    static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
    public static void Verify()
    {
        Camera camera=null;
        try {
            var samples=new List<Sample>();int layouts=0;
            // A deliberately camera-aligned upper platform must reject a candidate,
            // proving the delivery fallback checks screen occlusion, not just XZ.
            var fixture=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,transit=true,queue=Array.Empty<int>()},new NodeSpec{x=20,capacity=4,transit=true,queue=Array.Empty<int>()}},edges=Array.Empty<EdgeSpec>(),groups=Array.Empty<GroupSpec>()};
            var clear=NavigationFactory.Create(fixture);var occluded=LevelShare.Copy(fixture);
            var aligned=clear.Geometry.Config.WorldToLevel(clear.Centers[0]+GameplayPresentation.DefaultDirection.normalized*12);
            occluded.nodes[1].x=aligned.x;occluded.nodes[1].y=aligned.y;occluded.nodes[1].z=aligned.z;
            Require(!GameplayScreenSafety.NoWorse(GameplayScreenSafety.Analyze(clear,GameplayPresentation.DefaultDirection),GameplayScreenSafety.Analyze(NavigationFactory.Create(occluded),GameplayPresentation.DefaultDirection)),"Screen-occluded candidate must fall back");
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(700,1000),new Vector2Int(1080,2341)})foreach(int mode in new[]{0,1,2,3}){
                var safe=mode==0?new Rect(0,0,size.x,size.y):new Rect(0,34,size.x,size.y-78);
                var capsule=mode==1?new Rect(size.x-105,size.y-92,90,32):mode==3?new Rect(float.NaN,0,2,2):default;
                var layout=new GameplayLayout(size.x,size.y,safe,capsule,mode!=0);
                Require(layout.Play.width>0&&layout.Play.height>0,"Positive play area");
                Require(!layout.Gear.Overlaps(layout.Title)&&!layout.Title.Overlaps(layout.Play)&&!layout.Play.Overlaps(layout.Props),"HUD disjoint");
                Require(layout.Play.xMin>=layout.Safe.xMin&&layout.Play.xMax<=layout.Safe.xMax&&layout.Props.yMax<=layout.Safe.yMax,"Safe bounds");
                var pixels=layout.PlayPixels;Require(GameplayLayout.Valid(pixels,size.x,size.y),"Pixel conversion");layouts++;
            }
            var css=WeChatPlatform.WindowRect(285,60,90,32,390,844);
            Require(Mathf.Abs(css.y-(1-92f/844))<.00001f,"CSS capsule origin");
            Require(!StairsGame.UserRotationEnabled,"No user rotation");
            uint[] expected={0xEE5147,0x187FE8,0x43BF58,0xE9AC16,0xB44CDB,0x202024};Require(ColorCatalog.Rgb.SequenceEqual(expected),"Stable six colors");
            var library=Resources.Load<RefinedArtLibrary>("RefinedArtLibrary");
            for(int i=0;i<6;i++){
                uint hex=expected[i];var c=library.characters[i].GetColor("_ClothColor");
                Require(Mathf.Abs(c.r-((hex>>16)&255)/255f)<.00001f&&Mathf.Abs(c.g-((hex>>8)&255)/255f)<.00001f&&Mathf.Abs(c.b-(hex&255)/255f)<.00001f,"Formal material "+i);
            }
            camera=new GameObject("Readability verification camera").AddComponent<Camera>();
            foreach(var item in NavigationDataVerification.Levels()){
                var level=LevelPresentation.Apply(item.Value);var metadata=LevelPresentation.Find(level);Require(metadata!=null,"Missing delivered camera "+item.Key);
                Require(LevelPresentation.Signature(LevelPresentation.Apply(level))==LevelPresentation.Signature(level),"Idempotent layout");
                var space=NavigationFactory.Create(level);var points=GameplayFraming.Envelope(space);
                Require(GameplayScreenSafety.Analyze(space,metadata.direction).Reachable,"Delivered platform/crowd/stair has no visible samples "+item.Key);
                if(metadata.operations.Length>0)Require(GameplayScreenSafety.NoWorse(GameplayScreenSafety.Analyze(NavigationFactory.Create(item.Value),metadata.direction),GameplayScreenSafety.Analyze(space,metadata.direction)),"Fold screen occlusion regression "+item.Key);
                foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(700,1000)}){
                    if(camera.targetTexture)UnityEngine.Object.DestroyImmediate(camera.targetTexture);
                    camera.targetTexture=new RenderTexture(size.x,size.y,16);camera.rect=new Rect(0,0,1,1);camera.aspect=size.x/(float)size.y;
                    var layout=new GameplayLayout(size.x,size.y,new Rect(0,34,size.x,size.y-78),new Rect(size.x-105,size.y-92,90,32),true);
                    GameplayFraming.Fit(camera,points,metadata.direction,layout.PlayPixels);
                    float xmin=float.PositiveInfinity,ymin=xmin,xmax=float.NegativeInfinity,ymax=xmax,margin=float.PositiveInfinity;
                    foreach(var p in points){var screen=camera.WorldToScreenPoint(p);var area=layout.PlayPixels;
                        Require(screen.z>0&&screen.x>=area.xMin-.01f&&screen.x<=area.xMax+.01f&&screen.y>=area.yMin-.01f&&screen.y<=area.yMax+.01f,"Clipped "+item.Key+" screen="+screen+" area="+area+" camera="+camera.pixelRect+" zoom="+camera.orthographicSize);
                        xmin=Mathf.Min(xmin,screen.x);xmax=Mathf.Max(xmax,screen.x);ymin=Mathf.Min(ymin,screen.y);ymax=Mathf.Max(ymax,screen.y);
                        margin=Mathf.Min(margin,screen.x-area.xMin,area.xMax-screen.x,screen.y-area.yMin,area.yMax-screen.y);
                    }
                    var pos=camera.transform.position;float zoom=camera.orthographicSize;GameplayFraming.Fit(camera,points,metadata.direction,layout.PlayPixels);
                    Require(Vector3.Distance(pos,camera.transform.position)<.0001f&&Mathf.Abs(zoom-camera.orthographicSize)<.0001f,"Framing stability");
                    var actor=camera.WorldToScreenPoint(Vector3.up*CrowdScene.PersonHeight)-camera.WorldToScreenPoint(Vector3.zero);
                    samples.Add(new Sample{id=item.Key,width=size.x,height=size.y,widthUse=(xmax-xmin)/layout.PlayPixels.width,heightUse=(ymax-ymin)/layout.PlayPixels.height,actorPixels=Mathf.Abs(actor.y),margin=margin});
                }
            }
            Directory.CreateDirectory("artifacts/task005");File.WriteAllText("artifacts/task005/readability.json",JsonUtility.ToJson(new Report{passed=true,layouts=layouts,palette=6,samples=samples.ToArray()},true));
            Debug.Log("READABILITY VERIFIED layouts="+layouts+" projections="+samples.Count);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{if(camera){if(camera.targetTexture)UnityEngine.Object.DestroyImmediate(camera.targetTexture);UnityEngine.Object.DestroyImmediate(camera.gameObject);}}
    }
    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName="Builds/Task005/StairsCrowd.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        Debug.Log("TASK005 PLAYER BUILD "+report.summary.result);EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}
