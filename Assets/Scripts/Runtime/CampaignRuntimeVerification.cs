using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using StairsCrowd.Core;
namespace StairsCrowd.Runtime
{
    public sealed class CampaignRuntimeVerification:MonoBehaviour
    {
        StairsGame game;string output;int moves,samples;bool failed;
        void Require(bool ok,string why){if(!ok)throw new Exception("CAMPAIGN RUNTIME: "+why);}
        IEnumerator Settle(){int i=0;while(game.Busy&&i++<15000){game.Advance(.08f);CheckFeet();if(i%12==0)yield return null;}Require(!game.Busy,"settle timeout");while(game.IntroVisible)game.ContinueIntro();yield return null;}
        void CheckFeet(){if(game.scene==null)return;foreach(var person in game.scene.people){if(!person.root.gameObject.activeSelf||game.scene.IsRetiring(person.tag.node))continue;float floor;Require(game.space.FootHeight(new Vector2(person.root.position.x,person.root.position.z),out floor)&&person.root.position.y>=floor+.024f,"actor floor");samples++;}}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(output,name),t.EncodeToPNG());Destroy(t);}
        IEnumerator Start()
        {
            game=GetComponent<StairsGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/architecture-v6/runtime"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            // Force a real swapchain resize when a prior automated run saved 390x844.
            Screen.SetResolution(Screen.width==390?391:390,844,FullScreenMode.Windowed);yield return null;yield return null;
            Screen.SetResolution(390,844,FullScreenMode.Windowed);yield return null;yield return null;
            Require(game.catalog.levels.Length==20,"pool size");
            for(int i=0;i<20;i++){
                game.LoadLevel(i);yield return Settle();Require(game.scene.people.Length==96,"six-color population");
                if(i==0){
                    int node=Enumerable.Range(0,game.board.Level.nodes.Length).First(n=>game.board.Current.queues[n].Count>0);int group=game.board.Current.queues[node][0];var actor=game.scene.people[group*Rules.MembersPerGroup];var baseMaterials=game.scene.platforms.Select(p=>p.sharedMaterial).ToArray();
                    game.HandlePointer(game.view.WorldToScreenPoint(actor.root.position+Vector3.up*(CrowdScene.PersonHeight*.65f)));Require(game.selected==node,"screen-coordinate click selection");var selected=game.scene.people.Where(p=>p.IsSelected).ToArray();Require(selected.Length==Rules.MovableCount(game.board.Level,game.board.Current,node)*Rules.MembersPerGroup&&selected.All(p=>Mathf.Abs(p.visual.localScale.x-1.1f)<.001f&&p.selectionRing.activeSelf),"selected row visual feedback");Require(Enumerable.Range(0,baseMaterials.Length).All(n=>ReferenceEquals(baseMaterials[n],game.scene.platforms[n].sharedMaterial)),"platform bases stay unhighlighted");game.ClickPerson(node);Require(game.selected==-1,"selection cancel");
                    var action=game.board.Level.solution[0];int sourceGroup=game.board.Current.queues[action.a][0];var sourceActor=game.scene.people[sourceGroup*Rules.MembersPerGroup];game.HandlePointer(game.view.WorldToScreenPoint(sourceActor.root.position+Vector3.up*(CrowdScene.PersonHeight*.65f)));Require(game.selected==action.a,"solution source selected by screen click");int before=game.board.Moves;var destinationScreen=game.view.WorldToScreenPoint(game.space.Centers[action.b]+Vector3.up*.02f);int personPick,platformPick;bool hasPerson=game.TryPickPerson(destinationScreen,out personPick),hasPlatform=game.TryPickNode(destinationScreen,out platformPick);game.HandlePointer(destinationScreen);Require(game.board.Moves==before+1&&game.motion!=null,"destination platform click starts movement; person="+(hasPerson?personPick.ToString():"none")+", platform="+(hasPlatform?platformPick.ToString():"none")+", expected="+action.b+", selected="+game.selected+", message="+game.message);yield return Settle();game.ResetLevel();yield return Settle();
                }
                foreach(var platform in game.scene.platforms)foreach(var vertex in platform.GetComponent<MeshFilter>().sharedMesh.vertices){var p=game.view.WorldToViewportPoint(platform.transform.TransformPoint(vertex));Require(p.z>0&&p.x>=0&&p.x<=1&&p.y>=.19f&&p.y<=.87f,"mobile playable framing");}
                yield return Capture("level-"+(i+1).ToString("00")+".png");
                foreach(var action in game.board.Level.solution){Require(game.TryMoveSync(action.a,action.b),"move "+(i+1)+" "+action.a+"->"+action.b);game.Advance(.05f);CheckFeet();moves++;yield return null;}
                yield return Settle();Require(game.board.Solved,"victory");
                for(int n=0;n<game.board.Level.nodes.Length;n++)Require(game.scene.NodeRoot(n).gameObject.activeSelf==game.board.Level.nodes[n].transit,"retirement preserves core");
                game.Undo();Require(!game.board.Solved,"undo victory");game.ResetLevel();yield return Settle();Require(game.board.Moves==0,"reset");Debug.Log("CAMPAIGN PLAYER LEVEL "+(i+1));
            }
            game.OpenEditor();game.SetEditorDraft(game.catalog.levels[0]);var imported=LevelShare.Decode(LevelShare.Encode(game.EditorDraft));Require(imported.ColorCount==6&&imported.provenance==null,"share strips verification and preserves colors");game.SetEditorDraft(imported);game.PlayDraft();yield return Settle();Require(game.board.Level.ColorCount==6,"custom six-color navigation");
            File.WriteAllText(Path.Combine(output,"runtime.json"),"{\"passed\":true,\"levels\":20,\"moves\":"+moves+",\"surfaceSamples\":"+samples+",\"mobileFraming\":true,\"editorShare\":true}");Application.Quit(0);
        }
        void Log(string text,string trace,LogType type){if(failed||type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),text+"\n"+trace);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
