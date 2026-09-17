using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Opt-in destructive TEST setup uses verification-prefixed progress only.
    // Every observed gameplay selection/move below goes through down/up dispatch.
    public sealed class GameplayReadabilityCoverageProbe:MonoBehaviour
    {
        [Serializable] public sealed class LevelEvidence { public string id;public int width,nodes,sourceSelections,pointerMoves,dragCancels,rendererCorners,fullCrowdActors;public int[] hitSamples,stairRaySamples; }
        [Serializable] public sealed class ColorEvidence { public int color;public string state;public float reveal,propDim;public Color material;public bool mask,selected;public string image;public int restoredMaxChannelDifference,restoredChangedPixels; }
        [Serializable] public sealed class Report { public bool passed;public int checks;public LevelEvidence[] levels;public ColorEvidence[] colors;public string limitation="Desktop Player; WeChat device and subjective readability remain Human Gate."; }
        StairsGame game;string output;int checks;bool failed;readonly List<LevelEvidence> levels=new List<LevelEvidence>();readonly List<ColorEvidence> colors=new List<ColorEvidence>();
        string dailyKey;int savedDaily,savedCampaign;bool hadDaily,hadCampaign,saved;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-readabilitycoverage")>=0)new GameObject("Readability coverage").AddComponent<GameplayReadabilityCoverageProbe>();}
        void Check(bool ok,string reason){if(!ok)throw new Exception("READABILITY COVERAGE: "+reason);checks++;}
        void Restore(){if(!saved)return;if(hadDaily)PlayerPrefs.SetInt(dailyKey,savedDaily);else PlayerPrefs.DeleteKey(dailyKey);if(hadCampaign)PlayerPrefs.SetInt(CampaignProgress.StorageKey,savedCampaign);else PlayerPrefs.DeleteKey(CampaignProgress.StorageKey);PlayerPrefs.Save();saved=false;}
        void Log(string text,string trace,LogType type){if(failed||type!=LogType.Exception&&type!=LogType.Error)return;failed=true;Restore();File.WriteAllText(Path.Combine(output,"failure.txt"),text+"\n"+trace);Application.Quit(1);}
        IEnumerator Settle(){int n=0;while(game.Busy&&n++<1600){for(int i=0;i<4;i++)game.Advance(.25f);yield return null;}Check(!game.Busy,"settle");while(game.IntroVisible)game.ContinueIntro();yield return null;yield return null;Physics.SyncTransforms();}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));for(int i=0;i<4;i++)yield return null;}
        bool DirectHit(Vector2 p,int node){int person,platform;bool hp=game.TryPickPerson(p,out person),hg=game.TryPickNode(p,out platform);return GameplayLayout.Current.PlayPixels.Contains(p)&&(game.selected>=0&&hg&&(!hp||person!=platform)?platform==node:hp?person==node:hg&&platform==node);}
        Vector2 Point(int node)
        {
            Physics.SyncTransforms();var c=game.space.Centers[node];
            for(int radius=0;radius<=4;radius++)for(int x=-radius;x<=radius;x++)for(int z=-radius;z<=radius;z++){
                Vector2 p=game.view.WorldToScreenPoint(c+new Vector3(x*.5f,.05f,z*.5f));if(DirectHit(p,node))return p;
            }
            foreach(var person in game.scene.people)if(person.tag.node==node){Vector2 p=game.view.WorldToScreenPoint(person.root.position+Vector3.up*1.4f);if(DirectHit(p,node))return p;}
            throw new Exception("No direct pointer surface node "+node+" level "+game.board.Level.name);
        }
        void Tap(int node){var p=Point(node);game.BeginViewPointer(p);game.EndViewPointer(p);}
        void RendererBounds(LevelEvidence e)
        {
            var area=GameplayLayout.Current.PlayPixels;
            foreach(var person in game.scene.people)if(!game.scene.IsRetiring(person.tag.node))foreach(var r in person.visual.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy){var b=r.bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){var p=game.view.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));Check(area.Contains(p),e.id+" renderer envelope");e.rendererCorners++;}
            }
        }
        void FullCrowd(LevelEvidence e)
        {
            var original=game.scene;var originalCamera=game.view;original.root.SetActive(false);
            var camera=new GameObject("Full crowd evidence camera").AddComponent<Camera>();camera.CopyFrom(originalCamera);camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameplayPresentation.Background;camera.targetTexture=new RenderTexture(Screen.width,Screen.height,24);camera.aspect=Screen.width/(float)Screen.height;
            var level=LevelShare.Copy(game.board.Level);level.groups=new GroupSpec[level.nodes.Length*4];
            for(int n=0;n<level.nodes.Length;n++){level.nodes[n].queue=Enumerable.Range(n*4,4).ToArray();for(int row=0;row<4;row++)level.groups[n*4+row]=new GroupSpec{id=n*4+row,color=level.nodes[n].transit?n%6:(n+row)%6};}
            var board=new Board(level);var scene=new CrowdScene(NavigationFactory.Create(level),camera);scene.Populate(board.Current);
            GameplayFraming.Fit(camera,GameplayFraming.Envelope(scene.space),LevelPresentation.Direction(game.board.Level),GameplayLayout.Current.PlayPixels);
            game.scene=scene;game.view=camera;e.fullCrowdActors=scene.people.Length;RendererBounds(e);Render(camera,e.id+"-full-crowd-"+e.width);
            for(int n=0;n<level.nodes.Length;n++){scene.Highlight(board,n);RendererBounds(e);}scene.Highlight(board,-1);
            Physics.SyncTransforms();e.stairRaySamples=new int[scene.space.Stairs.Length];
            for(int i=0;i<scene.space.Stairs.Length;i++){
                var stair=scene.space.Stairs[i];var root=scene.LinkRoot(stair.index);if(!root)continue;
                foreach(var tread in stair.Treads){var point=camera.WorldToScreenPoint(tread.Center+Vector3.up*.02f);RaycastHit hit;
                    if(Physics.Raycast(camera.ScreenPointToRay(point),out hit,200,1<<8,QueryTriggerInteraction.Collide)&&hit.collider.transform.IsChildOf(root))e.stairRaySamples[i]++;
                }
                if(stair.Treads.Length>0)Check(e.stairRaySamples[i]>0,e.id+" physical stair screen visibility "+stair.index);
            }
            scene.Dispose();game.scene=original;game.view=originalCamera;original.root.SetActive(true);Destroy(camera.targetTexture);Destroy(camera.gameObject);Physics.SyncTransforms();
        }
        IEnumerator ProbeNodes(LevelEvidence e)
        {
            var l=game.board.Level;var state=game.board.Current;string key=Rules.Key(l,state);int moves=game.board.Moves;
            for(int n=0;n<l.nodes.Length;n++){
                if(game.scene.IsRetiring(n)||!game.scene.NodeRoot(n).gameObject.activeInHierarchy)continue;
                int count=0;for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++){Vector2 p=game.view.WorldToScreenPoint(game.space.Centers[n]+new Vector3(x*.5f,.05f,z*.5f));if(DirectHit(p,n))count++;}
                Check(count>0,e.id+" clickable node "+n);e.hitSamples[n]=Math.Max(e.hitSamples[n],count);
                bool selectable=!l.nodes[n].sticky&&state.queues[n].Count>0&&!Rules.Gated(l,state,n)&&!Rules.Complete(l,state,n);
                Tap(n);if(selectable){Check(game.selected==n,e.id+" source pointer "+n);e.sourceSelections++;RendererBounds(e);Tap(n);Check(game.selected<0,"deselect pointer");}
                Check(game.board.Moves==moves&&Rules.Key(l,game.board.Current)==key,"node inspection must not move");
                var p0=Point(n);var rotation=game.view.transform.rotation;game.BeginViewPointer(p0);game.MoveViewPointer(p0+Vector2.right*80);game.EndViewPointer(p0+Vector2.right*80);
                Check(game.selected<0&&game.board.Moves==moves&&game.view.transform.rotation==rotation,"drag cancels without committing or rotating");e.dragCancels++;
            }
            yield return null;
        }
        IEnumerator Witness(LevelEvidence e,bool probeAll)
        {
            var solution=game.board.Level.solution;var board=game.board;
            if(probeAll)yield return ProbeNodes(e);
            foreach(var action in solution){
                if(probeAll)yield return ProbeNodes(e);
                Tap(action.a);Check(game.selected==action.a,e.id+" witness source "+action.a);int count=game.board.Moves;Tap(action.b);
                yield return Settle();Check(board.Moves==count+1,e.id+" witness target "+action.b);e.pointerMoves++;
                if(board.Solved)break;
            }
            Check(board.Solved,e.id+" pointer witness solved");
        }
        IEnumerator Daily(Vector2Int size,DateTime date)
        {
            double now=0;game.DailyToday=()=>date;game.DailyNow=()=>now;
            game.SendMessage("OnApplicationFocus",true);game.SendMessage("OnApplicationPause",false);
            PlayerPrefs.SetInt(dailyKey,0);game.ReturnHome();yield return Settle();game.OpenDailyCalendar();Check(game.StartDailyChallenge(date),"enter actual daily");yield return Settle();
            Check(game.DailyActive&&!game.Home&&!game.DailyCalendarOpen,"daily gameplay HUD");yield return Capture("daily-hud-"+size.x);
            Tap(game.board.Level.solution[0].a);now=181;yield return null;yield return null;
            Check(game.FailureVisible,"daily timeout visible");yield return Capture("daily-failed-"+size.x);
            game.ResetLevel();yield return Settle();
            var e=new LevelEvidence{id="daily-"+date.ToString("yyyy-MM-dd"),width=size.x,nodes=game.space.Centers.Length,hitSamples=new int[game.space.Centers.Length]};levels.Add(e);
            yield return Witness(e,true);Check(game.DailyClock.Result==DailyResult.Won&&!game.DailyCanRetry,"daily completion no retry");yield return Capture("daily-completed-"+size.x);
        }
        Color MaterialColor(PersonView p)=>p.colored[0].sharedMaterial.GetColor("_ClothColor");
        Color32[] Render(Camera camera,string name)
        {
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;int w=camera.targetTexture.width,h=camera.targetTexture.height;var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();var pixels=texture.GetPixels32();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);RenderTexture.active=previous;return pixels;
        }
        IEnumerator Palette()
        {
            game.ReturnHome();yield return Settle();game.enabled=false;game.scene.root.SetActive(false);
            var camera=new GameObject("Palette evidence camera").AddComponent<Camera>();camera.CopyFrom(game.view);camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameplayPresentation.Background;camera.targetTexture=new RenderTexture(320,320,24);camera.aspect=1;
            for(int color=0;color<6;color++){
                var level=new LevelSpec{name="Palette runtime fixture",nodes=new[]{new NodeSpec{capacity=4,transit=true,queue=new[]{0,1,2,3}}},edges=Array.Empty<EdgeSpec>(),groups=Enumerable.Range(0,4).Select(i=>new GroupSpec{id=i,color=color}).ToArray(),solution=Array.Empty<EdgeSpec>()};
                var board=new Board(level);var scene=new CrowdScene(NavigationFactory.Create(level),camera);scene.Populate(board.Current);scene.SetPropFocus(null,board.Current);
                GameplayFraming.Fit(camera,GameplayFraming.Envelope(scene.space),GameplayPresentation.DefaultDirection,new Rect(8,8,304,304));
                Color32[] normal=null;var normalPositions=scene.people.Select(p=>p.visual.localPosition).ToArray();var normalScales=scene.people.Select(p=>p.visual.localScale).ToArray();
                foreach(string state in new[]{"normal","selected","hidden","reveal-mid","reveal-complete","prop-focus","prop-dim","restored"}){
                    if(state=="selected")scene.Highlight(board,0);
                    if(state=="hidden"){scene.Highlight(board,-1);for(int i=0;i<4;i++)board.Current.revealed[i]=false;scene.RefreshPeople(board.Current);}
                    if(state=="reveal-mid"){for(int i=0;i<4;i++)board.Current.revealed[i]=true;scene.RefreshPeople(board.Current);scene.TickReveals(.26f);}
                    if(state=="reveal-complete")scene.TickReveals(1);
                    if(state=="prop-focus")scene.SetPropFocus(new HashSet<int>{0},board.Current);
                    if(state=="prop-dim")scene.SetPropFocus(new HashSet<int>(),board.Current);
                    if(state=="restored"){scene.SetPropFocus(null,board.Current);scene.Highlight(board,-1);scene.RefreshPeople(board.Current);}
                    var p=scene.people[0];string name="color-"+color+"-"+state;var block=new MaterialPropertyBlock();p.colored[0].GetPropertyBlock(block);float dim=block.HasFloat("_PropDim")?block.GetFloat("_PropDim"):1;
                    Check(p.RevealUsesMask==(state=="hidden"),name+" mask");Check(p.IsSelected==(state=="selected"),name+" selection");
                    if(state=="reveal-mid")Check(p.RevealTint>.1f&&p.RevealTint<.9f,"reveal midpoint");
                    if(state=="restored"||state=="normal"){uint rgb=ColorCatalog.Rgb[color];var expected=new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);Check(MaterialColor(p)==expected,"color identity "+color);Check(p.RevealTint==1&&dim==1,"state restoration");}
                    if(state=="prop-focus")Check(dim==1,"focused person undimmed");if(state=="prop-dim")Check(Mathf.Abs(dim-.12f)<.0001f,"non-target person dimmed");
                    var pixels=Render(camera,name);int maxDifference=0,changed=0;if(state=="normal")normal=pixels;
                    if(state=="restored"){
                        for(int i=0;i<pixels.Length;i++){int difference=Math.Max(Math.Abs(pixels[i].r-normal[i].r),Math.Max(Math.Abs(pixels[i].g-normal[i].g),Math.Abs(pixels[i].b-normal[i].b)));maxDifference=Math.Max(maxDifference,difference);if(difference>0)changed++;}
                        for(int i=0;i<scene.people.Length;i++){var actor=scene.people[i];actor.colored[0].GetPropertyBlock(block);Check(actor.visual.localPosition==normalPositions[i]&&actor.visual.localScale==normalScales[i]&&!actor.IsSelected&&!actor.RevealUsesMask&&actor.RevealTint==1&&block.GetFloat("_PropDim")==1&&MaterialColor(actor)==MaterialColor(p),"all actors restore exact visual state");}
                        // Raster edge/order variance is reported, not hidden by PNG-byte
                        // equality. Limit it to <=0.1% of pixels and <=8/255 in any channel;
                        // exact semantic tint/material/transform checks above are separate.
                        Check(changed<=pixels.Length/1000&&maxDifference<=8,"restored raster tolerance color "+color);
                    }
                    colors.Add(new ColorEvidence{color=color,state=state,reveal=p.RevealTint,propDim=dim,material=MaterialColor(p),mask=p.RevealUsesMask,selected=p.IsSelected,image=name+".png",restoredMaxChannelDifference=maxDifference,restoredChangedPixels=changed});
                }
                scene.Dispose();yield return null;
            }
            Destroy(camera.targetTexture);Destroy(camera.gameObject);game.scene.root.SetActive(true);game.enabled=true;
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-readabilityOutput");output=args[at+1];Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Check(DailyChallengeProgress.Verification,"requires -dailyverify to isolate persistence");
            while(!(game=FindFirstObjectByType<StairsGame>())||game.board==null)yield return null;
            var date=new DateTime(2026,9,17);dailyKey=DailyChallengeProgress.StorageKey(date);hadDaily=PlayerPrefs.HasKey(dailyKey);savedDaily=PlayerPrefs.GetInt(dailyKey);hadCampaign=PlayerPrefs.HasKey(CampaignProgress.StorageKey);savedCampaign=PlayerPrefs.GetInt(CampaignProgress.StorageKey);saved=true;
            Screen.SetResolution(391,844,FullScreenMode.Windowed);yield return null;yield return null;
            if(Array.IndexOf(args,"-paletteonly")>=0){yield return Palette();Restore();File.WriteAllText(Path.Combine(output,"coverage.json"),JsonUtility.ToJson(new Report{passed=true,checks=checks,levels=levels.ToArray(),colors=colors.ToArray()},true));Debug.Log("READABILITY PALETTE PASSED "+checks);Application.Quit(0);yield break;}
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(700,1000)}){
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return null;yield return null;yield return Settle();
                foreach(int index in new[]{6,9,12,13,17}){
                    game.LoadLevel(index);yield return Settle();var e=new LevelEvidence{id="campaign-"+(index+1),width=size.x,nodes=game.space.Centers.Length,hitSamples=new int[game.space.Centers.Length]};levels.Add(e);RendererBounds(e);FullCrowd(e);
                    yield return Capture("pointer-level-"+(index+1)+"-"+size.x);yield return Witness(e,true);
                    Check(e.hitSamples.All(n=>n>0),"all platforms reached "+e.id);Debug.Log("COVERAGE POINTER "+e.id+" width="+e.width+" moves="+e.pointerMoves+" nodes="+e.nodes);
                }
                yield return Daily(size,date);
            }
            yield return Palette();Restore();File.WriteAllText(Path.Combine(output,"coverage.json"),JsonUtility.ToJson(new Report{passed=true,checks=checks,levels=levels.ToArray(),colors=colors.ToArray()},true));Debug.Log("READABILITY COVERAGE PASSED "+checks);Application.Quit(0);
        }
        void OnDestroy(){Restore();Application.logMessageReceived-=Log;}
    }
}
