using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class CloudVerification:MonoBehaviour
    {
        public static bool Active {get;private set;}StairsGame game;string output;int samples,moves;float clearance=100;double maxClick;bool failed;Color[] homeBand;
        void Require(bool value,string what){if(!value)throw new Exception("CLOUD TEST: "+what);}
        IEnumerator Start()
        {
            Active=true;game=GetComponent<StairsGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/cloud-iteration"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
#if STAIRS_COOPERATIVE_TEST
            output=Path.Combine(output,"cooperative");Directory.CreateDirectory(output);
#endif
            yield return null;Screen.SetResolution(Screen.width==700?701:700,1000,FullScreenMode.Windowed);yield return null;yield return null;Screen.SetResolution(700,1000,FullScreenMode.Windowed);yield return null;yield return null;game.Advance(1f);Require(game.IsAssembling,"generation remains smooth beyond one second");yield return new WaitForEndOfFrame();Capture("home-building.png");yield return Assemble();yield return new WaitForEndOfFrame();Capture("home.png");Require(game.Home&&game.scene.people.Length==0,"home no people");CheckHomeBand();
            for(int l=0;l<game.catalog.levels.Length;l++){
                game.LoadLevel(l);yield return Assemble();Require(game.IntroVisible,"intro required");int before=game.board.Moves;var first=game.board.Level.solution[0];Require(!game.TryMoveSync(first.a,first.b)&&before==game.board.Moves,"intro input leak");
                yield return new WaitForEndOfFrame();Capture("intro-"+(l+1)+".png");while(game.IntroVisible)game.ContinueIntro();yield return new WaitForEndOfFrame();Capture("level-"+(l+1)+".png");
                var begin=Rules.Key(game.board.Level,game.board.Current);Require(game.TryMoveSync(first.a,first.b),"first move");game.Advance(.12f);game.Undo();Require(begin==Rules.Key(game.board.Level,game.board.Current)&&game.motion==null,"moving undo");
                foreach(var step in game.board.Level.solution){Require(game.TryMoveSync(step.a,step.b),"authored move "+l+" "+step.a+"->"+step.b);maxClick=Math.Max(maxClick,game.LastMoveMilliseconds);if(game.motion!=null){float minimum=game.motion.MinimumClearance();clearance=Mathf.Min(clearance,minimum);Require(minimum>=WalkSpace.Separation-.0002f,"crowd intersection");}
                    // Rapid consecutive input tests reservations while earlier people walk.
                    game.Advance(.05f);CheckFeet();moves++;yield return null;
                }
                int frames=0;while(game.Busy&&frames++<10000){game.Advance(.05f);CheckFeet();if(frames%12==0)yield return null;}Require(!game.Busy&&game.board.Solved,"finish with retirement");
                for(int n=0;n<game.board.Level.nodes.Length;n++)Require(game.scene.NodeRoot(n).gameObject.activeSelf==game.board.Level.nodes[n].transit,"shared transit retained / sorting removed");
                yield return new WaitForEndOfFrame();Capture("finished-"+(l+1)+".png");game.Undo();Require(!game.board.Solved&&game.board.Moves==game.board.Level.solution.Length-1,"undo complete");
                game.ResetLevel();yield return Assemble();Require(game.IntroVisible&&game.board.Moves==0,"reset intro");
                Debug.Log("CLOUD LEVEL PASSED "+(l+1));
            }
            yield return CheckBatchInteraction();yield return CheckViewControls();yield return CheckFreeEditor();
            game.ReturnHome();yield return Assemble();game.OpenEditor();Require(game.EditorOpen,"editor entry");game.SetEditorDraft(game.catalog.levels[4]);
            game.RefreshEditorPreview();yield return new WaitForEndOfFrame();Capture("editor.png");var code=LevelShare.Encode(game.EditorDraft);var imported=LevelShare.Decode(code);Require(code==LevelShare.Encode(imported),"share stable");
            game.SetEditorDraft(imported);game.PlayDraft();yield return Assemble();Require(game.IntroVisible,"import intro");while(game.IntroVisible)game.ContinueIntro();Require(Rules.Key(game.board.Level,game.board.Current)==Rules.Key(imported,Rules.Initial(imported)),"import state");
            game.ReturnHome();yield return Assemble();game.OpenEditor();
            typeof(StairsGame).GetField("editorScroll",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game,new Vector2(0,300));
            yield return new WaitForEndOfFrame();game.ReturnHome();yield return Assemble();yield return new WaitForEndOfFrame();
            Require(game.scene.root.activeInHierarchy&&game.scene.people.Length==0,"editor return restores home model");CheckHomeBand();Capture("home-after-editor.png");
            File.WriteAllText(Path.Combine(output,"runtime.json"),"{\"passed\":true,\"moves\":"+moves+",\"surfaceSamples\":"+samples+",\"minimumCenterDistance\":"+clearance.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"maxClickMs\":"+maxClick.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"editorAndShare\":true,\"retirementAndUndo\":true}");
            Debug.Log("CLOUD RUNTIME PASSED");Application.Quit(0);
        }
        IEnumerator Assemble(){int count=0;while(game.IsAssembling&&count++<1500){game.Advance(.04f);if(count%6==0)yield return null;}Require(!game.IsAssembling,"assembly timeout");yield return null;}
        IEnumerator CheckViewControls()
        {
            game.ReturnHome();var assemblyField=typeof(StairsGame).GetField("assembly",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var rise=(AssemblySequence)assemblyField.GetValue(game);Require(rise.Duration<=2.001f&&rise.Duration>=1.8f,"two second assembly");
            game.LoadLevel(0);yield return Assemble();while(game.IntroVisible)game.ContinueIntro();
            var initial=Rules.Key(game.board.Level,game.board.Current);var positions=game.scene.people.Select(p=>p.root.position).ToArray();
            var centre=new Vector2(game.view.pixelRect.center.x,game.view.pixelRect.center.y);var rotation=game.view.transform.rotation;
            game.BeginViewPointer(centre);game.MoveViewPointer(centre+Vector2.right*Screen.width*.3f);Require(game.ViewPointerDragged,"swipe threshold");game.EndViewPointer(centre+Vector2.right*Screen.width*.3f);
            Require(Quaternion.Angle(rotation,game.view.transform.rotation)>40&&game.selected==-1&&game.board.Moves==0,"gameplay swipe rotates without a move");
            for(int turn=0;turn<4;turn++){
                game.scene.OrbitView(90);int candidate=-1;Vector2 tap=Vector2.zero;
                foreach(var person in game.scene.people){var screen=game.view.WorldToScreenPoint(person.root.position+Vector3.up*.6f);int picked;
                    if(game.view.pixelRect.Contains(screen)&&game.TryPickPerson(screen,out picked)&&picked==person.tag.node){candidate=picked;tap=screen;break;}}
                Require(candidate>=0,"person remains selectable from rotated view");game.BeginViewPointer(tap);game.EndViewPointer(tap);Require(game.selected==candidate,"tap selects after orbit");
                game.BeginViewPointer(tap);game.EndViewPointer(tap,true);Require(game.selected==candidate,"cancelled touch does not select");game.ClickPerson(candidate);
            }
            Require(initial==Rules.Key(game.board.Level,game.board.Current)&&game.board.Moves==0,"camera leaves board unchanged");
            for(int i=0;i<positions.Length;i++)Require(Vector3.Distance(positions[i],game.scene.people[i].root.position)<.0001f,"camera leaves people unchanged");
            yield return new WaitForEndOfFrame();Capture("rotated-view.png");
            game.scene.StartRetreat(0);game.scene.TickWorld(game.board.Current,1.9f);Require(game.scene.NodeRoot(0).gameObject.activeSelf,"retreat stays visible before two seconds");game.scene.TickWorld(game.board.Current,.1f);Require(game.scene.NodeRoot(0).gameObject.activeSelf,"collection remains after link retreat");
            game.OpenEditor();game.SetEditorDraft(game.catalog.levels[0]);var original=LevelShare.Copy(game.EditorDraft);var geometry=new WalkSpace(original,null,true);var facing=geometry.Facing(0);
            game.RotateGatheringSurface(0);var changed=new WalkSpace(game.EditorDraft,null,true);Require(Vector3.Angle(facing,changed.Facing(0))>89,"gathering surface quarter turn");
            Require(LevelShare.Decode(LevelShare.Encode(game.EditorDraft)).nodes[0].surfaceDirection==game.EditorDraft.nodes[0].surfaceDirection,"surface direction share");
            Require(geometry.Rotations.SequenceEqual(changed.Rotations)&&geometry.Stairs.Length==changed.Stairs.Length,"surface turn keeps architecture fixed");game.UndoEditor();Require(game.EditorDraft.nodes[0].surfaceDirection==original.nodes[0].surfaceDirection,"surface turn undo");
            game.RotateGatheringSurface(2);Require(game.EditorDraft.nodes[2].surfaceDirection==original.nodes[2].surfaceDirection,"transit cannot rotate");
            var moved=LevelShare.Copy(original);moved.edges=moved.edges.Reverse().ToArray();moved.nodes[0].x+=1;Require(new WalkSpace(moved,null,true).Rotations.All(r=>Mathf.Abs(r-Mathf.PI/4)<.0001f),"platform direction independent of connections and positions");
            Debug.Log("CLOUD VIEW CONTROLS PASSED");
        }
        IEnumerator CheckFreeEditor()
        {
            game.OpenEditor();game.SetEditorDraft(new LevelSpec{name="自由搭建",nodes=new NodeSpec[0],edges=new EdgeSpec[0],groups=new GroupSpec[0]});game.RefreshEditorPreview();
            Require(LevelShare.Decode(LevelShare.Encode(game.EditorDraft)).nodes.Length==0,"empty share");game.PlayDraft();yield return Assemble();while(game.IntroVisible)game.ContinueIntro();Require(!game.board.Solved,"empty free play");
            game.OpenEditor();game.PlaceEditorNode(false,new Vector3(0,2,0));game.PlaceEditorNode(true,new Vector3(14,7,0));game.PlaceEditorNode(false,new Vector3(0,2,-14));
            Require(game.EditorDraft.edges.Length==2&&game.EditorDraft.edges.All(e=>e.a==0),"nearest platform auto connection");
            game.ToggleEditorLink(1,2);Require(game.EditorDraft.edges.Length==3,"manual connection");game.UndoEditor();Require(game.EditorDraft.edges.Length==2,"editor undo connection");
            game.RefreshEditorPreview();var geometry=new WalkSpace(game.EditorDraft,null,true);Require(geometry.Stairs.Any(s=>s.steps>14)&&geometry.Stairs.Any(s=>s.steps==1&&s.polygon==null),"height adaptive stairs and flat bridge");
            game.EditorDraft.nodes[1].sticky=true;game.EditorDraft.nodes[1].unlockAfter=99;game.EditorDraft.nodes[1].colorRestricted=true;game.EditorDraft.nodes[1].curtain=true;
            var imported=LevelShare.Decode(LevelShare.Encode(game.EditorDraft));Require(imported.nodes[1].sticky&&imported.nodes[1].unlockAfter==99&&imported.nodes[1].transit,"mechanisms on empty transit share");
            game.SetEditorDraft(imported);game.PlayDraft();yield return Assemble();Require(!game.board.Solved,"unsolvable draft can launch");game.OpenEditor();
            var large=new LevelSpec{nodes=Enumerable.Range(0,70).Select(i=>CloudLevels.N(i*7,0,0,true)).ToArray(),edges=new EdgeSpec[0],groups=new GroupSpec[0]};large.Validate();var wide=new WalkSpace(large,null,true);Require(wide.NodeMask(0)!=wide.NodeMask(64),"independent masks beyond 64 surfaces");
            var overlap=LevelShare.Copy(imported);overlap.nodes[1].x=overlap.nodes[0].x;overlap.nodes[1].z=overlap.nodes[0].z;game.SetEditorDraft(overlap);game.RefreshEditorPreview();game.PlayDraft();yield return Assemble();Require(!game.board.Solved,"overlapping layout allowed");
            Debug.Log("CLOUD FREE EDITOR PASSED");
        }
        IEnumerator CheckBatchInteraction()
        {
            for(int rows=2;rows<=4;rows++){
                var level=LevelShare.Copy(game.catalog.levels[0]);foreach(var g in level.groups){g.hidden=false;g.color=g.id<4?0:1;}foreach(var n in level.nodes)n.queue=new int[0];
                level.nodes[3].queue=Enumerable.Range(0,rows).ToArray();level.nodes[0].queue=Enumerable.Range(rows,4-rows).ToArray();level.nodes[1].queue=new[]{4,5};level.nodes[4].queue=new[]{6,7};
                game.OpenEditor();game.SetEditorDraft(level);game.PlayDraft();yield return Assemble();while(game.IntroVisible)game.ContinueIntro();
                game.ClickPerson(3);Require(game.selected==3&&game.scene.people.Count(p=>p.IsSelected)==rows*4,"entire transit crowd highlighted");
                game.ClickPerson(4);Require(game.selected==4&&game.board.Moves==0,"different color switches selection");game.ClickPerson(3);game.ClickNode(5);
                Require(game.board.Moves==1&&game.board.Current.queues[5].Count==rows&&game.board.Current.queues[3].Count==0,"transit batch command");
                Require(game.motion.MinimumClearance()>=WalkSpace.Separation-.0002f,"batch continuous separation");game.Advance(.05f);CheckFeet();
                if(rows<4){game.ClickPerson(0);game.ClickPerson(5);Require(game.board.Moves==2&&game.board.Current.queues[5].Count==rows+1&&game.selected==-1,"same-color person merges during previous motion");}
                game.ClickPerson(5);game.ClickNode(3);Require(game.board.Current.queues[5].Count==0,"merged crowd departs together");
                float minimum=game.motion.MinimumClearance();clearance=Mathf.Min(clearance,minimum);Require(minimum>=WalkSpace.Separation-.0002f,"composed batch separation");
                int frames=0;while(game.Busy&&frames++<10000){game.Advance(.05f);CheckFeet();if(frames%12==0)yield return null;}Require(!game.Busy,"batch arrival");
                game.Undo();Require(game.board.Current.queues[5].Count==(rows<4?rows+1:rows),"batch undo");
            }
            Debug.Log("CLOUD BATCH INTERACTION PASSED");
        }
        void CheckHomeBand()
        {
            int y=Mathf.RoundToInt(Screen.height*.275f);var pixels=new Texture2D(Screen.width,1,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,y,Screen.width,1),0,0);pixels.Apply();var row=pixels.GetPixels();Destroy(pixels);
            Require(game.view.rect==new Rect(0,0,1,1),"home background fills screen");var backdrop=row[row.Length/2];Require(backdrop.r<.25f&&backdrop.b>backdrop.r+.015f&&backdrop.b>.04f,"night home band is blue-black and rendered");
            if(homeBand==null){homeBand=row;return;}for(int x=0;x<row.Length;x++)Require(Mathf.Abs(row[x].r-homeBand[x].r)+Mathf.Abs(row[x].g-homeBand[x].g)+Mathf.Abs(row[x].b-homeBand[x].b)<.025f,"home editor text or scrollbar residue");
        }
        int waterVertexChecks;
        void CheckFeet(){foreach(var p in game.scene.people){if(game.scene.IsRetiring(p.tag.node)||!p.root.gameObject.activeSelf)continue;float height;Require(game.space.FootHeight(new Vector2(p.root.position.x,p.root.position.z),out height),"off platform");Require(p.root.position.y>=height+.024f,"floor penetration");samples++;
            if(p.waterBird.Blend>.001f&&samples%7==0)foreach(var local in p.waterBird.FlowMesh.vertices){var world=p.root.TransformPoint(local);SurfaceSet mask;Require(game.space.Surface(new Vector2(world.x,world.z),out height,out mask)&&world.y>=height-.002f,"water stair penetration");Require(new Vector2(local.x,local.z).magnitude<=WalkSpace.ActorRadius+.0001f,"water exceeds actor footprint");waterVertexChecks++;}
        }if(waterVertexChecks>0)System.IO.File.WriteAllText(System.IO.Path.Combine(output,"water-surfaces.json"),"{\"checkedVertices\":"+waterVertexChecks+",\"passed\":true}");}
        void Capture(string name){var frame=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);frame.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(output,name),frame.EncodeToPNG());Destroy(frame);}
        void Log(string text,string trace,LogType type){if(failed||type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),text+"\n"+trace);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;Active=false;}
    }
}
