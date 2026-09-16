using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class FastResponseVerification
    {
        [Serializable] sealed class Sample {public int from,to;public double requestMs;public float firstMovementDelayMs;public bool startedInRequest;}
        [Serializable] sealed class Report {public bool passed,allStartedInRequest,desktopWithinOneFrame,wechatDeviceTested,queuedMoves,undoDuringMotion;public Sample[] samples;}
        public static IEnumerator Run(StairsGame game,string output)
        {
            game.LoadLevel(0);while(game.Busy){game.Advance(.05f);yield return null;}while(game.IntroVisible)game.ContinueIntro();
            var samples=new List<Sample>();
            foreach(var action in game.board.Level.solution.Take(6)){
                game.PrepareColdPlanningCheck();
                var move=Rules.Preview(game.board.Level,game.board.Current,action.a,action.b);
                var ids=move.ids.SelectMany(g=>Enumerable.Range(g*4,4)).ToArray();var before=ids.Select(id=>game.scene.people[id].root.position).ToArray();
                var watch=System.Diagnostics.Stopwatch.StartNew();bool accepted=game.RequestMove(action.a,action.b);watch.Stop();
                if(!accepted||game.PreparingMove||game.motion==null)throw new Exception("Fast response request did not commit immediately");
                Func<bool> moved=()=>ids.Where((id,i)=>Vector3.Distance(game.scene.people[id].root.position,before[i])>.0001f).Any();
                var sample=new Sample{from=action.a,to=action.b,requestMs=watch.Elapsed.TotalMilliseconds,startedInRequest=moved()};
                int frames=0;while(!moved()&&game.motion!=null&&frames++<240){game.Advance(1f/60);sample.firstMovementDelayMs+=1000f/60;yield return null;}
                if(!moved())throw new Exception("Selected crowd never moved");
                samples.Add(sample);
                if(samples.Count==1){
                    game.Advance(.2f);var target=RenderTexture.GetTemporary(720,1080,24);var previous=game.view.targetTexture;var active=RenderTexture.active;
                    try{game.view.targetTexture=target;game.view.Render();RenderTexture.active=target;var image=new Texture2D(720,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"fast-moving.png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
                    finally{game.view.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
                }

                while(game.Busy){game.Advance(.05f);yield return null;}
            }
            game.ResetLevel();while(game.Busy){game.Advance(.05f);yield return null;}while(game.IntroVisible)game.ContinueIntro();
            foreach(var action in game.board.Level.solution.Take(3))if(!game.RequestMove(action.a,action.b))throw new Exception("Rapid chained request rejected");
            if(game.board.Moves!=3||game.motion==null)throw new Exception("Rapid chain not reserved");
            game.Undo();if(game.board.Moves!=2)throw new Exception("Undo during movement failed");
            while(game.Busy){game.Advance(.05f);yield return null;}
            var retry=game.board.Level.solution[2];if(!game.RequestMove(retry.a,retry.b))throw new Exception("Retry after undo failed");
            while(game.Busy){game.Advance(.05f);yield return null;}
            var expected=game.space.Positions(game.board.Current);for(int id=0;id<expected.Length;id++)if(Vector3.Distance(expected[id],game.scene.people[id].root.position)>.001f)throw new Exception("Chained final seat mismatch");
            var report=new Report{passed=true,queuedMoves=true,undoDuringMotion=true,allStartedInRequest=samples.All(s=>s.startedInRequest),desktopWithinOneFrame=samples.All(s=>s.requestMs<1000.0/60&&s.startedInRequest),wechatDeviceTested=false,samples=samples.ToArray()};
            File.WriteAllText(Path.Combine(output,"fast-response-check.json"),JsonUtility.ToJson(report,true));
        }
    }
}
