using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    // Explicit verification switch only; ordinary game startup does not create this object.
    public sealed class InMotionFormationProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-formationProbe")>=0)new GameObject("Formation verification").AddComponent<InMotionFormationProbe>();}
        [Serializable] sealed class Evidence {public bool passed;public int screenshots,checks; public long playbackBytes;public double playbackMs;public bool device=false;}
        StairsGame game; string output; Evidence evidence=new Evidence();
        void Check(bool valid,string message){if(!valid)throw new Exception(message);evidence.checks++;}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationOutput");output=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"task007");Directory.CreateDirectory(output);
            while(!(game=FindFirstObjectByType<StairsGame>())||game.board==null)yield return null;
            game.enabled=false;var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)break;yield return run.Current;}
            evidence.passed=true;File.WriteAllText(Path.Combine(output,"player.json"),JsonUtility.ToJson(evidence,true));Application.Quit(0);
        }
        IEnumerator Run()
        {
            for(int i=0;game.IsAssembling&&i<1000;i++)game.Advance(.05f);
            game.scene.root.SetActive(false);Screen.SetResolution(700,1000,FullScreenMode.Windowed);
            foreach(int groups in new[]{1,2,3,4})foreach(int residents in new[]{0,4-groups}.Distinct()){
                var level=InMotionFormationFixtures.Create(groups,residents);var space=new WalkSpace(level,null,true);var board=new Board(level);
                var exhibit=new CrowdScene(space,game.view);exhibit.HomeFraming=false;exhibit.Populate(board.Current);exhibit.FitCamera();
                var move=Rules.Preview(level,board.Current,0,1);var plan=FastMovement.Build(space,board.Current,move);var residentsIds=board.Current.queues[1].SelectMany(g=>Enumerable.Range(g*4,4)).ToArray();
                var timer=new System.Diagnostics.Stopwatch();
                for(int frame=0;frame<=120;frame++){
                    float clock=plan.duration*frame/120f;timer.Restart();long bytes=GC.GetAllocatedBytesForCurrentThread();
                    foreach(var track in plan.tracks){var person=exhibit.people[track.actor];var p=track.PlaybackPosition(clock);var direction=p-person.root.position;direction.y=0;person.remainingDistance=track.RemainingDistance(clock);person.Pose(p,direction,clock,clock<track.End);person.TickWalk(plan.duration/120f);}
                    evidence.playbackBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;timer.Stop();evidence.playbackMs+=timer.Elapsed.TotalMilliseconds;
                    yield return new WaitForEndOfFrame();
                    if(frame%12==0)Capture("crowd-"+groups*4+"-residents-"+residents*4+"-"+frame.ToString("000"));
                }
                foreach(var track in plan.tracks)Check(Vector3.Distance(exhibit.people[track.actor].root.position,plan.final[track.actor])<.001f,"rendered final position");
                exhibit.Dispose();
            }
            game.scene.root.SetActive(true);
            foreach(int index in new[]{0,6,13,17}){
                game.LoadLevel(index);for(int i=0;game.IsAssembling&&i<1000;i++)game.Advance(.05f);while(game.IntroVisible)game.ContinueIntro();
                var initial=game.space.Positions(game.board.Current);var actions=game.board.Level.solution;
                if(actions.Length==0)continue;var first=actions[0];Check(game.TryMoveSync(first.a,first.b),"actual gameplay move");
                if(actions.Length>1){var next=actions[1];Check(game.TryMoveSync(next.a,next.b),"actual queued move");}
                for(int i=0;game.Busy&&i<5000;i++)game.Advance(.04f);
                Check(!game.Busy,"settled gameplay");if(index==0)continue;game.ResetLevel();for(int i=0;game.IsAssembling&&i<1000;i++)game.Advance(.05f);
                Check(initial.SequenceEqual(game.space.Positions(game.board.Current)),"runtime reset");
            }
        }
        void Capture(string name)
        {
            var target=RenderTexture.GetTemporary(700,1000,24);var previous=game.view.targetTexture;var active=RenderTexture.active;var image=new Texture2D(700,1000,TextureFormat.RGB24,false);
            game.view.targetTexture=target;game.view.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,700,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            game.view.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Destroy(image);evidence.screenshots++;
        }
    }
}
