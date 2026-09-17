using System;
using System.Collections;
using System.IO;
using System.Reflection;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Opt-in player evidence; never active in an ordinary session.
    public sealed class GameplayReadabilityProbe:MonoBehaviour
    {
        StairsGame game;string output;int checks;bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-readabilitytest")>=0)new GameObject("Readability player probe").AddComponent<GameplayReadabilityProbe>();}
        void Check(bool ok,string reason){if(!ok)throw new Exception("READABILITY PLAYER: "+reason);checks++;}
        void Log(string text,string trace,LogType type){if(failed||type!=LogType.Exception&&type!=LogType.Error)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),text+"\n"+trace);Application.Quit(1);}
        IEnumerator Settle()
        {
            int count=0;while(game.Busy&&count++<1000){game.Advance(.2f);yield return null;}
            Check(!game.Busy,"settle");while(game.IntroVisible)game.ContinueIntro();yield return null;yield return null;
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));for(int i=0;i<8;i++)yield return null;}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-readabilityOutput");output=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"task005");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            while(!(game=FindFirstObjectByType<StairsGame>())||game.board==null)yield return null;
            Screen.SetResolution(391,844,FullScreenMode.Windowed);yield return null;yield return null;
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(700,1000)}){
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return null;yield return null;yield return Settle();
                game.ReturnHome();yield return Settle();yield return Capture("home-"+size.x);
                game.OpenDailyCalendar();yield return Capture("calendar-"+size.x);game.ReturnHome();yield return Settle();
                foreach(int index in new[]{0,6,9,12,13,17}){
                    game.LoadLevel(index);yield return Settle();
                    var layout=GameplayLayout.Current;
                    foreach(var p in GameplayFraming.Envelope(game.space))Check(layout.PlayPixels.Contains(game.view.WorldToScreenPoint(p)),"level "+(index+1)+" envelope");
                    var orientation=game.view.transform.rotation;var projection=game.view.orthographicSize;
                    yield return Capture("level-"+(index+1)+"-"+size.x);
                    var action=game.board.Level.solution[0];var world=game.space.Centers[action.a]+Vector3.up*.1f;Vector2 point=game.view.WorldToScreenPoint(world);
                    game.BeginViewPointer(point);game.EndViewPointer(point);yield return null;
                    Check(game.selected==action.a,"press/release selects source "+index);
                    yield return Capture("selected-"+(index+1)+"-"+size.x);
                    game.BeginViewPointer(point);game.MoveViewPointer(point+Vector2.right*80);game.EndViewPointer(point+Vector2.right*80);
                    Check(game.view.transform.rotation==orientation&&!StairsGame.UserRotationEnabled,"drag does not rotate");
                    if(game.board.Level.solution.Length==1)continue; // The first tutorial exits immediately on its only move.
                    Vector2 target=game.view.WorldToScreenPoint(game.space.Centers[action.b]+Vector3.up*.1f);game.BeginViewPointer(target);game.EndViewPointer(target);yield return Settle();Check(game.board.Moves==1,"pointer target/internal move count");
                    Check(game.UseUndoProp(),"undo");yield return Settle();Check(game.board.Moves==0,"undo count");
                    Check(game.view.transform.rotation==orientation&&Mathf.Abs(game.view.orthographicSize-projection)<.0001f,"stable framing during move/undo");
                }
                game.LoadLevel(13);yield return Settle();Check(game.BeginPropSelection(1),"prop mode");while(game.FindingPropTargets)yield return null;yield return Capture("props-"+size.x);game.CancelPropSelection();
                game.OpenSettings();yield return Capture("settings-"+size.x);game.CloseSettings();yield return null;yield return null;
            }
            File.WriteAllText(Path.Combine(output,"runtime.json"),"{\"passed\":true,\"checks\":"+checks+",\"weChatDevice\":false}");Debug.Log("READABILITY PLAYER PASSED "+checks);Application.Quit(0);
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
