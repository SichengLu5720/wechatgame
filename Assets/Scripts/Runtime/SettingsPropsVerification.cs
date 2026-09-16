using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using StairsCrowd.Core;
namespace StairsCrowd.Runtime
{
    public sealed class SettingsPropsVerification:MonoBehaviour
    {
        public static bool Active {get;private set;}StairsGame game;string output;bool sound,music,vibration,failed;int assertions;
        void Check(bool value,string reason){assertions++;if(!value)throw new Exception("SETTINGS PROPS: "+reason);}
        IEnumerator Settle(){int count=0;while(game.Busy&&count++<2000){game.Advance(.1f);if(count%10==0)yield return null;}Check(!game.Busy,"settles");while(game.IntroVisible)game.ContinueIntro();yield return null;}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());Destroy(texture);}
        void CheckPlayableFraming()
        {
            for(int n=0;n<game.board.Level.nodes.Length;n++){
                var filter=game.scene.platforms[n].GetComponent<MeshFilter>();
                foreach(var vertex in filter.sharedMesh.vertices){
                    var point=game.view.WorldToViewportPoint(filter.transform.TransformPoint(vertex));
                    Check(point.z>0&&point.x>=0&&point.x<=1&&point.y>=.19f&&point.y<=.87f,"platform surface stays inside playable area after visual zoom");
                }
            }
        }
        IEnumerator Start()
        {
            Active=true;game=GetComponent<StairsGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/settings-props"));Directory.CreateDirectory(output);sound=GameSettings.Sound;music=GameSettings.Music;vibration=GameSettings.Vibration;Application.logMessageReceived+=Log;
            yield return null;Screen.SetResolution(Screen.width==391?392:391,844,FullScreenMode.Windowed);yield return null;yield return null;Screen.SetResolution(390,844,FullScreenMode.Windowed);yield return null;yield return null;yield return Settle();yield return Capture("home-mobile.png");
            game.OpenSettings();Check(game.SettingsOpen,"settings opened");yield return Capture("settings-mobile.png");
            GameSettings.Sound=false;GameSettings.Music=true;GameSettings.Vibration=false;Check(!GameSettings.Sound&&GameSettings.Music&&!GameSettings.Vibration,"independent persisted settings");
            int pulses=0;var feedback=new InteractionFeedback(()=>pulses++);feedback.Select();feedback.Tick(.5f);Check(pulses==0,"vibration off");
            GameSettings.Vibration=true;feedback.Select();Check(pulses==1,"vibration on");GameSettings.Vibration=false;feedback.Tick(.5f);Check(!feedback.IsPlaying,"pending haptics stop");
            Screen.SetResolution(1000,700,FullScreenMode.Windowed);yield return null;yield return null;yield return Capture("settings-landscape.png");game.CloseSettings();yield return null;yield return null;
            Screen.SetResolution(390,844,FullScreenMode.Windowed);game.LoadLevel(0);yield return Settle();CheckPlayableFraming();yield return Capture("hud-mobile.png");Screen.SetResolution(1920,1040,FullScreenMode.Windowed);yield return null;yield return null;CheckPlayableFraming();yield return Capture("hud-wide.png");Screen.SetResolution(390,844,FullScreenMode.Windowed);yield return null;yield return null;
            string initial=Rules.Key(game.board.Level,game.board.Current);game.OpenSettings();game.ClickNode(0);Check(initial==Rules.Key(game.board.Level,game.board.Current),"modal blocks board");float clock=game.clock;game.Advance(.5f);Check(game.clock==clock,"modal pauses");game.CloseSettings();yield return null;yield return null;
            int target=Enumerable.Range(0,game.board.Level.nodes.Length).First(n=>!game.board.Level.nodes[n].sticky&&!Rules.Gated(game.board.Level,game.board.Current,n)&&game.board.Current.queues[n].Select(id=>game.board.Level.groups[id].color).Distinct().Count()>1);
            Check(game.BeginPropSelection(1),"enter shuffle selection");while(game.FindingPropTargets)yield return null;Check(game.PropTargets.Contains(target),"eligible shuffle target");var block=new MaterialPropertyBlock();foreach(int n in Enumerable.Range(0,game.board.Level.nodes.Length)){Check(Mathf.Abs(game.space.Half(n)-game.space.Half(0))<.0001f,"all platform footprints match");if(game.board.Level.nodes[n].transit&&!Rules.Gated(game.board.Level,game.board.Current,n)){game.scene.platforms[n].GetPropertyBlock(block);Check(block.GetFloat("_PropDim")==1f,"transit remains bright during prop selection");}}Check(game.ShuffleRemaining==1&&initial==Rules.Key(game.board.Level,game.board.Current),"selection is nonmutating");Check(!game.ChoosePropTarget(-1),"invalid target no effect");yield return Capture("shuffle-selection.png");game.CancelPropSelection();Check(game.ShuffleRemaining==1&&game.PropSelection==0,"cancel free");game.BeginPropSelection(1);while(game.FindingPropTargets)yield return null;game.ClickNode(target);Check(game.ShuffleRemaining==0&&game.board.Moves==0,"shuffle via platform click");Check(game.ShuffleRemaining==0&&game.UndoRemaining==1&&game.PlatformRemaining==0,"independent quotas");Check(!game.UseShuffleProp(),"shuffle cannot repeat");Check(game.UseUndoProp(),"undo prop");Check(initial==Rules.Key(game.board.Level,game.board.Current),"shuffle exact undo");Check(game.UndoRemaining==0&&game.ShuffleRemaining==0&&!game.UseUndoProp(),"no quota refunds");
            game.ResetLevel();yield return Settle();Check(game.UndoRemaining==1&&game.ShuffleRemaining==1&&game.PlatformRemaining==0,"restart quotas");
            int searches=game.PropCandidateSearches;Check(!game.BeginPropSelection(2),"platform prop selection is disabled");Check(!game.UsePlatformProp(),"platform prop action is disabled");Check(!game.FindingPropTargets&&game.PropSelection==0&&game.PropCandidateSearches==searches,"disabled platform prop never starts eligibility search");
            yield return Capture("hud-reset.png");for(int l=0;l<game.catalog.levels.Length;l++){game.LoadLevel(l);yield return Settle();CheckPlayableFraming();yield return Capture("zoom-level-"+(l+1)+".png");}game.ReturnHome();yield return Settle();Screen.SetResolution(700,1000,FullScreenMode.Windowed);yield return null;yield return null;yield return Capture("home-desktop.png");
            File.WriteAllText(Path.Combine(output,"runtime.json"),"{\"passed\":true,\"assertions\":"+assertions+"}");Debug.Log("SETTINGS PROPS RUNTIME PASSED "+assertions);Restore();Application.Quit(0);
        }
        void Restore(){GameSettings.Sound=sound;GameSettings.Music=music;GameSettings.Vibration=vibration;}
        void Log(string text,string trace,LogType type){if(failed||type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),text+"\n"+trace);Restore();Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;Active=false;}
    }
}
