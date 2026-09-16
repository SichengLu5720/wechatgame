using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed class DeliveryPlayVerification : MonoBehaviour
    {
        [Serializable] sealed class Report {public bool passed,pressFeedback,dragCancelled,cancelledTouch,emptyPrompt,undoDuringMotion,wechatDeviceTested;public bool hapticScheduler,arrivalEvents,slowerMovement;public int levels,moves;public List<double> clickMilliseconds=new List<double>();}
        readonly Report report=new Report();StairsGame game;string output;
        IEnumerator Start(){
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../delivery-check"));Directory.CreateDirectory(output);game=GetComponent<StairsGame>();game.enabled=false;
            var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}if(!more)break;yield return run.Current;}
            report.passed=true;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(0);
        }
        void Settle(){int frames=0;while(game.Busy&&frames++<3000)game.Advance(.025f);if(game.Busy)throw new Exception("Settle timeout");while(game.IntroVisible)game.ContinueIntro();}
        void CheckHaptics(){
            bool enabled=GameSettings.Vibration;var pulses=new List<string>();var f=new InteractionFeedback((string type)=>pulses.Add(type));
            try{
                GameSettings.Vibration=true;f.Select();if(pulses.Count!=1||pulses[0]!="medium")throw new Exception("Selection haptic");
                f.Tick(.2f);f.Walk(.2f,true);f.Walk(.2f,true);if(pulses.Count!=2||pulses[1]!="medium")throw new Exception("Footstep cadence");
                f.Complete();f.Arrive();f.Select();f.Walk(2,true);if(pulses.Count!=3||pulses[2]!="heavy")throw new Exception("Completion priority");
                f.Tick(.16f);if(pulses.Count!=4||pulses[3]!="heavy")throw new Exception("Completion echo");
                f.Tick(.3f);f.Arrive();if(pulses.Count!=5||pulses[4]!="heavy")throw new Exception("Arrival haptic");
                f.Suspend(true);f.Tick(1);f.Walk(1,true);f.Select();if(pulses.Count!=5)throw new Exception("Background haptic");
                f.Suspend(false);GameSettings.Vibration=false;f.Select();f.Complete();f.Walk(1,true);if(pulses.Count!=5)throw new Exception("Disabled haptic");
                GameSettings.Vibration=true;f.Cancel();f.Walk(5,true);if(pulses.Count!=6)throw new Exception("Catch-up burst");
                f.Cancel();f.Walk(1,false);if(f.IsPlaying)throw new Exception("Stopped haptic");f.Complete();int beforeCancel=pulses.Count;f.Cancel();f.Tick(.2f);if(pulses.Count!=beforeCancel)throw new Exception("Cancelled echo");report.hapticScheduler=true;
            }finally{GameSettings.Vibration=enabled;}
        }
        IEnumerator Run(){CheckHaptics();
            for(int index=10;index<14;index++){
                game.LoadLevel(index);Settle();yield return null;
                var first=game.board.Level.solution[0];
                if(index==10){
                    var group=game.board.Current.queues[first.a][0];var person=game.scene.people[group*4];
                    Vector2 point=game.view.WorldToScreenPoint(person.root.position+Vector3.up*.8f);
                    game.BeginViewPointer(point);if(!game.PressPreviewActive||game.selected!=-1||game.board.Moves!=0)throw new Exception("Press preview missing or committed early");report.pressFeedback=true;
                    game.MoveViewPointer(point+Vector2.right*40);game.EndViewPointer(point+Vector2.right*40);
                    if(game.PressPreviewActive||game.selected!=-1||game.board.Moves!=0)throw new Exception("Drag committed tap");report.dragCancelled=true;
                    game.BeginViewPointer(point);game.EndViewPointer(point,true);
                    if(game.PressPreviewActive||game.selected!=-1)throw new Exception("Cancelled touch left selection");report.cancelledTouch=true;
                    int empty=Array.FindIndex(game.board.Level.nodes,n=>n.queue.Length==0);game.ClickNode(empty);
                    if(game.message.Contains("添加")||game.selected!=-1)throw new Exception("Empty platform still advertises disabled prop");report.emptyPrompt=true;
                    game.BeginViewPointer(point);game.EndViewPointer(point);if(game.selected!=first.a)throw new Exception("Pointer did not select expected source");
                    game.ClickNode(first.b);if(game.board.Moves!=1||game.motion==null)throw new Exception("Pointer-selected move failed");
                    game.Undo();Settle();if(game.board.Moves!=0)throw new Exception("Undo during movement failed");report.undoDuringMotion=true;
                }
                Capture("level-"+(index+1)+".png");
                foreach(var action in game.board.Level.solution){
                    int priorArrivals=game.Feedback.ArrivalCount,priorCompletions=game.Feedback.CompletionCount;
                    int group=game.board.Current.queues[action.a][0];
                    Vector2 source=game.view.WorldToScreenPoint(game.scene.people[group*4].root.position+Vector3.up*.8f);
                    game.BeginViewPointer(source);game.EndViewPointer(source);if(game.selected!=action.a)throw new Exception("Screen source selection failed at "+(index+1)+" node "+action.a);
                    Vector2 target=game.view.WorldToScreenPoint(game.space.Centers[action.b]+Vector3.up*.1f);
                    var timer=System.Diagnostics.Stopwatch.StartNew();game.BeginViewPointer(target);game.EndViewPointer(target);timer.Stop();report.clickMilliseconds.Add(timer.Elapsed.TotalMilliseconds);
                    if(game.motion==null||game.selected!=-1)throw new Exception("Move failed to start in click");if(game.motion.duration>1.4f/.85f+.001f)throw new Exception("Slowdown duration bound");
                    if(Mathf.Abs(game.motion.duration-1.4f/.85f)<.001f)report.slowerMovement=true;
                    Settle();if(game.Feedback.ArrivalCount+game.Feedback.CompletionCount<=priorArrivals+priorCompletions)throw new Exception("Missing arrival feedback");report.arrivalEvents=true;report.moves++;
                }
                if(!game.board.Solved)throw new Exception("Sample not completed");report.levels++;
            }
        }
        void Capture(string name){var target=RenderTexture.GetTemporary(540,960,24);var active=RenderTexture.active;try{foreach(var camera in Camera.allCameras.OrderBy(c=>c.depth)){var previous=camera.targetTexture;try{camera.targetTexture=target;camera.Render();}finally{camera.targetTexture=previous;}}RenderTexture.active=target;var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());Destroy(image);}finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}}
    }
}
