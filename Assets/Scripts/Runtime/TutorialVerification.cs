using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed class TutorialVerification : MonoBehaviour
    {
        public static bool Active {get;private set;}
        StairsGame game;string output;int samples;double maxRequest;
        static void Require(bool ok,string reason){if(!ok)throw new Exception("Tutorial: "+reason);}
        public static void CheckDefinitions(string path)
        {
            var catalog=CampaignRepository.Load();var original=CampaignRepository.LoadOriginal();int moves=0,feet=0;
            for(int i=10;i<20;i++)Require(JsonUtility.ToJson(catalog.levels[i])==JsonUtility.ToJson(original.levels[i]),"remaining campaign changed");
            foreach(var level in catalog.levels.Take(10)){
                level.Validate();Require(level.groups.GroupBy(g=>g.color).All(g=>g.Count()==4),"color total");
                for(int n=0;n<level.nodes.Length;n++)if(!level.nodes[n].transit)Require(level.edges.Count(e=>e.a==n||e.b==n)==1,"collection exit count");
                var space=new WalkSpace(level,null,true);LayoutSafety.Validate(space);
                Require(space.Stairs.Count(s=>s.polygon==null)==level.edges.Length,"missing stair");
                var board=new Board(level);
                foreach(var action in level.solution){
                    var move=Rules.Preview(level,board.Current,action.a,action.b);Require(move.ok,level.name+" witness: "+move.reason);
                    var plan=FastMovement.Build(space,board.Current,move);
                    for(float t=0;t<plan.duration+.025f;t+=.025f)for(int id=0;id<plan.initial.Length;id++){
                        var p=plan.SupportedPosition(space,id,t);float floor;
                        Require(space.FootHeight(new Vector2(p.x,p.z),out floor)&&p.y>=floor+.024f,"unsupported foot");feet++;
                    }
                    var before=Rules.Key(level,board.Current);board.TryMove(action.a,action.b,plan.finalSlots);board.Undo();
                    Require(Rules.Key(level,board.Current)==before,"undo");board.TryMove(action.a,action.b,plan.finalSlots);moves++;
                }
                Require(board.Solved,level.name+" unsolved");board.Reset();Require(board.Moves==0&&!board.CanUndo,"reset");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,"{\"passed\":true,\"tutorialLevels\":10,\"moves\":"+moves+",\"footSamples\":"+feet+",\"remainingCampaignUnchanged\":true}");
        }
        void Tick(float delta=.025f)
        {
            game.Advance(delta);
            if(game.motion!=null)foreach(var actor in game.scene.people){float floor;var p=actor.root.position;Require(game.space.FootHeight(new Vector2(p.x,p.z),out floor)&&p.y>=floor+.024f,"runtime unsupported foot");samples++;}
        }
        void Ready(){int frames=0;while(game.Busy&&frames++<500)Tick();Require(!game.Busy&&!game.IntroVisible,"tutorial waits for modal");}
        void FinishMove(EdgeSpec action,bool pointer=false)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            if(pointer){
                int actor=game.board.Current.queues[action.a][0]*4;
                Vector2 tap=game.view.WorldToScreenPoint(game.scene.people[actor].root.position+Vector3.up*.9f);
                game.BeginViewPointer(tap);game.EndViewPointer(tap);Require(game.selected==action.a,"tutorial source tap");
                tap=game.view.WorldToScreenPoint(game.space.Centers[action.b]+Vector3.up*.02f);
                game.BeginViewPointer(tap);game.EndViewPointer(tap);Require(game.board.Moves==1&&game.motion!=null,"tutorial destination tap");
            }else Require(game.RequestMove(action.a,action.b),"request rejected");
            maxRequest=Math.Max(maxRequest,timer.Elapsed.TotalMilliseconds);
            int frames=0;while(game.motion!=null&&frames++<500)Tick();Require(game.motion==null,"movement timeout");
        }
        void Capture(string name)
        {
            // Real scene cameras; no generated concept art, no IMGUI overlay in these images.
            var target=RenderTexture.GetTemporary(540,960,24);var active=RenderTexture.active;
            try{
                foreach(var camera in Camera.allCameras.OrderBy(c=>c.depth)){
                    var old=camera.targetTexture;try{camera.targetTexture=target;camera.Render();}finally{camera.targetTexture=old;}
                }
                RenderTexture.active=target;var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());Destroy(image);
            }finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
        }
        IEnumerator Start()
        {
            Active=true;game=GetComponent<StairsGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/sky-v7.12-tutorial"));Directory.CreateDirectory(output);
            Screen.SetResolution(540,960,FullScreenMode.Windowed);yield return null;yield return null;
            var tutorial=game.catalog;game.catalog=CampaignRepository.LoadOriginal();game.LoadLevel(0);
            Require(game.space.Level==game.board.Level,"stale campaign geometry");
            game.catalog=tutorial;
            game.LoadLevel(0);
            Require(game.space.Level==game.board.Level&&game.space.Centers.Length==3,"stale tutorial geometry");
            for(int level=0;level<10;level++){
                if(level>=4)game.LoadLevel(level);
                Require(game.levelIndex==level,"wrong automatic next level");Ready();Capture("level-"+(level+1).ToString("00")+".png");
                foreach(var action in game.board.Level.solution){FinishMove(action,level==0);yield return null;}
                Require(game.board.Solved,"runtime did not solve");
                if(level<3){
                    var scene=game.scene;var origin=scene.root.transform.position;var actor=scene.people[0].root;var relative=actor.position-origin;
                    Tick(.1f);Require(game.TutorialExiting&&game.Busy&&scene.root.transform.position.y<origin.y,"no collection exit");
                    Require(Vector3.Distance(actor.position-scene.root.transform.position,relative)<.001f,"people do not follow collection");
                    game.OpenSettings();var paused=scene.root.transform.position;Tick(1);Require(scene.root.transform.position==paused&&game.levelIndex==level,"pause advanced transition");game.CloseSettings();
                    yield return null;yield return null;yield return null;
                    if(level==0)Capture("collection-exit.png");
                    int frames=0;while(game.levelIndex==level&&frames++<100)Tick();
                    Require(game.levelIndex==level+1&&game.IsAssembling&&!game.IntroVisible,"next level did not rise immediately");
                }else{
                    Ready();for(int i=0;i<120;i++)Tick();Require(game.levelIndex==level&&game.board.Solved,"normal settlement skipped");
                }
                Debug.Log("TUTORIAL RUNTIME LEVEL "+(level+1)+" PASS");
            }
            // Cancellation must revoke the pending auto advance and restore the root.
            foreach(string cancel in new[]{"undo","reset","home"}){
                game.LoadLevel(0);Ready();FinishMove(game.board.Level.solution[0]);Tick(.15f);Require(game.TutorialExiting,"exit did not start");
                if(cancel=="undo")game.Undo();else if(cancel=="reset")game.ResetLevel();else game.ReturnHome();
                for(int i=0;i<180;i++)Tick();Require(!game.TutorialExiting&&game.levelIndex==0&&game.scene.root.transform.position==Vector3.zero,"cancelled exit leaked: "+cancel);
            }
            File.WriteAllText(Path.Combine(output,"runtime-check.json"),"{\"passed\":true,\"levels\":10,\"automaticTransitions\":3,\"pause\":true,\"undoResetHomeCancel\":true,\"footSamples\":"+samples+",\"maxRequestMs\":"+maxRequest.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"wechatDeviceTested\":false}");
            Active=false;Application.Quit(0);
        }
    }
}
