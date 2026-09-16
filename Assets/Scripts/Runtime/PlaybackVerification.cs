using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Unity.Profiling;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    // Actual game scenarios plus an old/new CPU microbenchmark. Not a phone FPS test.
    public sealed class PlaybackVerification : MonoBehaviour
    {
        [Serializable] sealed class Report {public bool passed,wechatDeviceTested;public int levels,moves,heightSamples,geometryCases;public bool drawCounterAvailable;public long drawsWithoutInstancing,drawsWithInstancing;public double referenceMs,playbackMs;public long referenceAllocBytes,playbackAllocBytes;public List<double> requestMs=new List<double>();public float maximumHeightError;}
        readonly Report report=new Report();string output;StairsGame game;float sink;
        IEnumerator Start()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../playback-check"));Directory.CreateDirectory(output);game=GetComponent<StairsGame>();game.enabled=false;
            var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception error){UnityEngine.Debug.LogException(error);Application.Quit(1);yield break;}if(!more)break;yield return run.Current;}
            report.passed=true;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));UnityEngine.Debug.Log("PLAYBACK VERIFICATION PASSED "+output);Application.Quit(0);
        }
        void Settle(){int guard=0;while(game.IsAssembling&&guard++<1000)game.Advance(.04f);if(game.IsAssembling)throw new Exception("Assembly timeout");while(game.IntroVisible)game.ContinueIntro();}
        IEnumerator Run()
        {
            foreach(float elevation in new[]{0f,2f,-2f}){
                var level=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0}},new NodeSpec{x=8,y=elevation,capacity=4,transit=true,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0}}};
                var space=new WalkSpace(level,null,true);var state=Rules.Initial(level);Check(FastMovement.Build(space,state,Rules.Preview(level,state,0,1)),space);report.geometryCases++;
            }
            foreach(int level in new[]{0,3,6,8}){
                game.LoadLevel(level);Settle();var actions=game.board.Level.solution;
                for(int step=0;step<actions.Length;step++){
                    var a=actions[step];var watch=Stopwatch.StartNew();if(!game.RequestMove(a.a,a.b))throw new Exception("Request rejected "+level+":"+step+" "+game.message);report.requestMs.Add(watch.Elapsed.TotalMilliseconds);
                    var plan=game.motion;if(plan==null)throw new Exception("Request did not start in same call");
                    Check(plan);Benchmark(plan);
                    int frames=0;while(game.motion!=null&&frames++<1000){game.Advance(.017f);CheckPeople();}if(game.motion!=null)throw new Exception("Motion timeout");report.moves++;
                    if(step==0){yield return new WaitForEndOfFrame();Capture("level-"+(level+1)+".png");}
                }
                if(!game.board.Solved)throw new Exception("Level not solved");report.levels++;
            }
            game.LoadLevel(6);Settle();yield return RenderBudget();
            // Keep all old input, composition, undo and exact final-seat assertions.
            game.catalog=CampaignRepository.LoadOriginal();yield return FastResponseVerification.Run(game,output);
        }
        void Check(MotionPlan plan,WalkSpace space=null)
        {
            space=space??game.space;
            foreach(var track in plan.tracks)for(int sample=1;sample<137;sample++){
                float t=track.start+track.duration*sample/137f;var expected=track.Position(t);float height;
                if(!space.FootHeight(new Vector2(expected.x,expected.z),out height))throw new Exception("Reference unsupported");var actual=track.PlaybackPosition(t);float error=Mathf.Abs(actual.y-height-WalkSpace.FootGap);report.maximumHeightError=Mathf.Max(report.maximumHeightError,error);report.heightSamples++;
                if(error>.005f)throw new Exception("Height mismatch "+error+" actor "+track.actor+" t "+t+" expected "+height+" actual "+actual);
            }
        }
        IEnumerator RenderBudget()
        {
            var materials=new HashSet<Material>();foreach(var person in game.scene.people)foreach(var renderer in person.colored)materials.Add(renderer.sharedMaterial);
            var old=new Dictionary<Material,bool>();foreach(var m in materials){old[m]=m.enableInstancing;m.enableInstancing=false;}
            using(var counter=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1)){
                report.drawCounterAvailable=counter.Valid;
                for(int i=0;i<4;i++)yield return null;report.drawsWithoutInstancing=counter.Valid?counter.LastValue:0;
                foreach(var entry in old)entry.Key.enableInstancing=entry.Value;
                for(int i=0;i<4;i++)yield return null;report.drawsWithInstancing=counter.Valid?counter.LastValue:0;
            }
        }
        void CheckPeople(){foreach(var p in game.scene.people){var pos=p.root.position;float height;if(!game.space.FootHeight(new Vector2(pos.x,pos.z),out height)||Mathf.Abs(pos.y-height-WalkSpace.FootGap)>.005f)throw new Exception("Runtime foot mismatch");}}
        void Benchmark(MotionPlan plan)
        {
            // Equal timestamps and active tracks; excludes rendering and asserts.
            var watch=new Stopwatch();long start=GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(int sample=1;sample<61;sample++)foreach(var track in plan.tracks){float t=track.start+track.duration*sample/61f;var p=track.Position(t);float h;game.space.FootHeight(new Vector2(p.x,p.z),out h);sink+=h;}
            watch.Stop();report.referenceMs+=watch.Elapsed.TotalMilliseconds;report.referenceAllocBytes+=GC.GetAllocatedBytesForCurrentThread()-start;
            start=GC.GetAllocatedBytesForCurrentThread();watch.Restart();for(int sample=1;sample<61;sample++)foreach(var track in plan.tracks)sink+=track.PlaybackPosition(track.start+track.duration*sample/61f).y;
            watch.Stop();report.playbackMs+=watch.Elapsed.TotalMilliseconds;report.playbackAllocBytes+=GC.GetAllocatedBytesForCurrentThread()-start;
        }
        void Capture(string name){var rt=RenderTexture.GetTemporary(540,960,24);var active=RenderTexture.active;var previous=game.view.targetTexture;try{game.view.targetTexture=rt;game.view.Render();RenderTexture.active=rt;var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());Destroy(image);}finally{game.view.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}
    }
}
