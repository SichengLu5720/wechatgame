using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;
namespace StairsCrowd.Runtime
{
    public sealed class SkyPreviewVerification:MonoBehaviour
    {
        string output;StairsGame game;
        void CheckCollectionMoves()
        {
            var rearLevel=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0,1,2}},new NodeSpec{x=8,y=2,capacity=4,queue=new[]{3}}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0},new GroupSpec{id=1,color=0},new GroupSpec{id=2,color=1},new GroupSpec{id=3,color=0}}};
            var rearBoard=new Board(rearLevel);var rearSpace=new WalkSpace(rearLevel,null,true);var rearBefore=rearSpace.Positions(rearBoard.Current);var rearMove=Rules.Preview(rearLevel,rearBoard.Current,0,1);
            var rearPlan=MotionPlanner.Build(rearSpace,rearBoard.Current,rearMove);
            for(int id=8;id<16;id++)if(rearPlan.initial[id]!=rearPlan.final[id]||rearPlan.Track(id)!=null)throw new Exception("Settled collection characters were repositioned");
            if(Vector3.Dot(rearBefore[12]-rearSpace.Centers[1],rearSpace.Facing(1))>=0)throw new Exception("First collection arrival is not at the back");
            if(rearPlan.MinimumClearance()<WalkSpace.Separation-.0002f)throw new Exception("Rear-filled collection collision");
            rearBoard.TryMove(0,1);rearBoard.Undo();var restored=rearSpace.Positions(rearBoard.Current);for(int id=0;id<restored.Length;id++)if(restored[id]!=rearBefore[id])throw new Exception("Rear-filled undo seat mismatch");
            var square=SurfaceRegion.Platform(Vector3.zero,2.6f,4,Mathf.PI/4);
            var corner=new Vector2(0,1.86f*Mathf.Sqrt(2));
            if(!SurfaceCoverage.Covers(new[]{square},corner,.552f))throw new Exception("Round footprint rejected near platform corner");
            if(SurfaceCoverage.Covers(new[]{square},corner,.8f))throw new Exception("Unsupported footprint accepted");
            var gap=new[]{SurfaceRegion.Platform(new Vector3(-1.1f,0,0),1,4),SurfaceRegion.Platform(new Vector3(1.1f,0,0),1,4)};
            if(SurfaceCoverage.Covers(gap,Vector2.zero,.36f))throw new Exception("Real gap accepted as supported");
            var l=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0,1,2,3}},new NodeSpec{x=8,y=2,capacity=4,transit=true,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0},new GroupSpec{id=1,color=0},new GroupSpec{id=2,color=0},new GroupSpec{id=3,color=1}}};
            var b=new Board(l);var before=Rules.Key(l,b.Current);var m=b.TryMove(0,1);
            if(!m.ok||m.count!=3||b.Current.queues[0].Count!=1||b.Current.queues[1].Count!=3||b.Moves!=1)throw new Exception("Collection same-color batch failed");
            if(!b.Undo()||Rules.Key(l,b.Current)!=before)throw new Exception("Collection batch undo failed");
            var space=new WalkSpace(l,null,true);var plan=MotionPlanner.Build(space,b.Current,Rules.Preview(l,b.Current,0,1));
            if(plan.MinimumClearance()<WalkSpace.Separation-.0002f)throw new Exception("Collection batch collision");
            for(float t=0;t<plan.duration;t+=.04f)for(int id=0;id<plan.initial.Length;id++)plan.SupportedPosition(space,id,t);
            for(int row=0;row<m.ids.Length;row++)for(int member=0;member<Rules.MembersPerGroup;member++){
                int id=m.ids[row]*Rules.MembersPerGroup+member;
                if(Vector3.Distance(plan.final[id],space.Seat(1,plan.finalSlots[id]/4,plan.finalSlots[id]%4,m.ids.Length))>.001f)throw new Exception("Collection batch landing mismatch");
            }
            b.Current.revealed[1]=false;if(Rules.MovableCount(l,b.Current,0)!=1)throw new Exception("Hidden row was exposed by batch");
            var limited=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0,1}},new NodeSpec{capacity=4,transit=true,queue=new[]{2,3,4}}},edges=l.edges,groups=new[]{new GroupSpec{id=0,color=0},new GroupSpec{id=1,color=0},new GroupSpec{id=2,color=0},new GroupSpec{id=3,color=0},new GroupSpec{id=4,color=0}}};
            var limitedBoard=new Board(limited);var limitedMove=limitedBoard.TryMove(0,1);
            if(!limitedMove.ok||limitedMove.count!=1||limitedBoard.Current.queues[1].Count!=4)throw new Exception("Destination capacity ignored");
            var full=new Board(l);l.groups[3].color=0;if(Rules.Preview(l,full.Current,0,1).ok)throw new Exception("Completed collection unlocked");
        }
        void CheckSolidCharacters()
        {
            foreach(var person in game.scene.people){
                if(person.waterBird.FlowMesh!=null||person.waterBird.Blend!=0)throw new Exception("Water resource or blend remains");
                var model=person.visual.Find("Sky character");
                bool hidden=!game.board.Current.revealed[Array.IndexOf(game.scene.people,person)/4]&&!Rules.Complete(game.board.Level,game.board.Current,person.tag.node);
                bool mask=hidden||person.RevealUsesMask;
                if(!model||model.gameObject.activeSelf==mask||(mask&&(!person.hiddenCharacter||!person.hiddenCharacter.activeSelf))||model.localScale!=Vector3.one||person.root.Find("One bird one water stream"))throw new Exception("Character melted during movement");
            }
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(output,name),t.EncodeToPNG());Destroy(t);}
        IEnumerator Start(){
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/sky-v7"));Directory.CreateDirectory(output);game=GetComponent<StairsGame>();game.catalog=CampaignRepository.LoadOriginal();
            CheckCollectionMoves();
            yield return CompletionAnimationVerification.Run(game);
            TransitFormationVerification.Run();
            yield return FastResponseVerification.Run(game,output);
            Screen.SetResolution(1280,900,FullScreenMode.Windowed);yield return null;yield return null;game.LoadLevel(0);
            while(game.Busy){game.Advance(.08f);yield return null;}while(game.IntroVisible)game.ContinueIntro();
            var angle=game.view.transform.rotation;var position=game.view.transform.position;int moves=game.board.Moves;
            var pointer=new Vector2(Screen.width*.4f,Screen.height*.5f);game.BeginViewPointer(pointer);game.MoveViewPointer(pointer+Vector2.right*150);game.EndViewPointer(pointer+Vector2.right*150);
            if(Quaternion.Angle(angle,game.view.transform.rotation)>.001f||Vector3.Distance(position,game.view.transform.position)>.001f||game.board.Moves!=moves||game.selected!=-1)throw new Exception("Disabled rotation drag changed camera or board");
            game.PrepareColdPlanningCheck();
            var action=game.board.Level.solution[0];int actor=game.board.Current.queues[action.a][0]*Rules.MembersPerGroup;var tap=(Vector2)game.view.WorldToScreenPoint(game.scene.people[actor].root.position+Vector3.up*.9f);
            game.BeginViewPointer(tap);game.EndViewPointer(tap);if(game.selected!=action.a)throw new Exception("Tap selection disabled with rotation");
            tap=game.view.WorldToScreenPoint(game.space.Centers[action.b]+Vector3.up*.02f);game.BeginViewPointer(tap);var inputTimer=System.Diagnostics.Stopwatch.StartNew();game.EndViewPointer(tap);double inputMs=inputTimer.Elapsed.TotalMilliseconds;while(game.PreparingMove)yield return null;if(game.board.Moves!=moves+1)throw new Exception("Tap movement disabled with rotation");
            File.WriteAllText(Path.Combine(output,"large-scale-check.json"),"{\"displayScale\":1.5,\"seatSpacing\":"+WalkSpace.SeatSpacing.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"actorRadius\":"+WalkSpace.ActorRadius.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"firstClickPlanningMs\":"+game.LastPlanningMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"firstClickCached\":"+(game.LastMoveCached?"true":"false")+",\"rearFillChecksPassed\":true,\"wechatDeviceTested\":false}");
            File.WriteAllText(Path.Combine(output,"input-planning-check.json"),"{\"inputMs\":"+inputMs.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"planningFrames\":"+game.LastPlanningFrames+",\"maxPlanningSliceMs\":"+game.LastPlanningSliceMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"planningElapsedMs\":"+game.LastPlanningMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"cached\":"+(game.LastMoveCached?"true":"false")+"}");
            int solidSamples=0;while(game.Busy){game.Advance(.04f);CheckSolidCharacters();solidSamples++;yield return null;}
            if(solidSamples==0)throw new Exception("No movement animation sampled");
            File.WriteAllText(Path.Combine(output,"collection-solid-check.json"),"{\"passed\":true,\"collectionBatch\":true,\"capacity\":true,\"hiddenBoundary\":true,\"undo\":true,\"completedLock\":true,\"solidMovementSamples\":"+solidSamples+"}");
            game.ResetLevel();while(game.Busy){game.Advance(.08f);yield return null;}while(game.IntroVisible)game.ContinueIntro();
            if(!game.RequestMove(action.a,action.b)||game.PreparingMove||game.motion==null)throw new Exception("Immediate move did not start");
            game.ResetLevel();if(game.PreparingMove)throw new Exception("Reset did not cancel planned move");while(game.Busy){game.Advance(.08f);yield return null;}for(int wait=0;wait<5;wait++)yield return null;if(game.board.Moves!=0)throw new Exception("Cancelled move committed after reset");while(game.IntroVisible)game.ContinueIntro();
            File.WriteAllText(Path.Combine(output,"cancel-check.json"),"{\"passed\":true,\"noLateCommit\":true}");
            File.WriteAllText(Path.Combine(output,"rotation-check.json"),"{\"passed\":true,\"dragCameraUnchanged\":true,\"dragDoesNotMove\":true,\"tapSelect\":true,\"tapMove\":true}");
            var towers=game.TowerBackground;int towerMesh=towers.MeshId;long towerBytes=towers.ResourceBytes;
            for(int repeat=0;repeat<3;repeat++){game.LoadLevel(0);while(game.Busy){game.Advance(.08f);yield return null;}while(game.IntroVisible)game.ContinueIntro();if(towers!=game.TowerBackground||towerMesh!=towers.MeshId||towerBytes!=towers.ResourceBytes)throw new Exception("Background recreated during load");}
            towers.RefreshAspect();long allocated=GC.GetAllocatedBytesForCurrentThread();var timer=System.Diagnostics.Stopwatch.StartNew();long timerAllocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)towers.RefreshAspect();long updateBytes=GC.GetAllocatedBytesForCurrentThread()-timerAllocation;timer.Stop();
            if(updateBytes!=0||towers.Triangles>1000||towerBytes>131072)throw new Exception("Tower resource budget exceeded");
            File.WriteAllText(Path.Combine(output,"tower-check.json"),"{\"passed\":true,\"reloads\":3,\"sameMesh\":true,\"towerCount\":3,\"triangles\":"+towers.Triangles+",\"resourceBytes\":"+towerBytes+",\"updateAllocatedBytes\":"+updateBytes+",\"updateCalls\":10000,\"updateTotalMs\":"+timer.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"wechatDeviceTested\":false}");
            yield return Capture("game-wide.png");
            Screen.SetResolution(540,960,FullScreenMode.Windowed);yield return null;yield return null;yield return Capture("game-mobile.png");
            var savedPosition=game.view.transform.position;float savedSize=game.view.orthographicSize;
            game.view.orthographicSize=7.2f;game.view.transform.position=game.space.Centers[6]+Vector3.up*.8f-game.view.transform.forward*30;yield return Capture("platform-large.png");
            game.view.transform.position=savedPosition;game.view.orthographicSize=savedSize;
            game.enabled=false;game.scene.root.SetActive(false);Screen.SetResolution(1440,800,FullScreenMode.Windowed);yield return null;yield return null;
            var formationNodes=Enumerable.Range(0,4).Select(i=>new NodeSpec{x=(i%2)*7,y=0,z=(i/2)*5,capacity=4,transit=true,queue=Enumerable.Range(i*(i+1)/2,i+1).ToArray()}).ToArray();
            var formationLevel=new LevelSpec{nodes=formationNodes,edges=new EdgeSpec[0],groups=Enumerable.Range(0,10).Select(i=>new GroupSpec{id=i,color=1}).ToArray()};
            var formationSpace=new WalkSpace(formationLevel,null,true);var formationScene=new CrowdScene(formationSpace,game.view);formationScene.Populate(Rules.Initial(formationLevel));
            game.view.orthographicSize*=.80f;yield return Capture("transit-crowds.png");formationScene.Dispose();
            var gallery=new GameObject("Character gallery");var coat=SkyCharacterMesh.Coat();var trim=SkyCharacterMesh.Trim();
            foreach(var mesh in new[]{coat,trim})foreach(var vertex in mesh.vertices)if(new Vector2(vertex.x,vertex.z).magnitude*1.1f>WalkSpace.ActorRadius||vertex.y*1.1f>CrowdScene.PersonHeight)throw new Exception("Sky mesh exceeds actor envelope");
            for(int i=0;i<6;i++)foreach(bool detail in new[]{false,true}){var obj=new GameObject("Character "+i,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(gallery.transform);obj.transform.position=new Vector3(i*1.15f,0,0);obj.transform.rotation=Quaternion.Euler(0,155,0);obj.GetComponent<MeshFilter>().sharedMesh=detail?trim:coat;uint c=ColorCatalog.Rgb[i];obj.GetComponent<MeshRenderer>().sharedMaterial=game.scene.Mat(detail?new Color(.94f,.91f,.79f):new Color((c>>16&255)/255f,(c>>8&255)/255f,(c&255)/255f));}
            foreach(var renderer in gallery.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial.SetFloat("_ContactTint",1);
            var floor=PrimitiveGeometry.Create(PrimitiveType.Cube);floor.transform.SetParent(gallery.transform);floor.transform.position=new Vector3(2.875f,-.1f,0);floor.transform.localScale=new Vector3(7.4f,.2f,1.7f);floor.GetComponent<Renderer>().sharedMaterial=game.scene.Mat(new Color(.94f,.91f,.79f));
            game.view.rect=new Rect(0,0,1,1);game.view.orthographicSize=2.3f;game.view.transform.position=new Vector3(2.875f,3,-8);game.view.transform.LookAt(new Vector3(2.875f,.85f,0));yield return Capture("characters.png");
            gallery.SetActive(false);
            var demo=new LevelSpec{name="Sky architecture inspection",nodes=new[]{new NodeSpec{x=0,y=2,z=0,capacity=4,queue=new int[0]},new NodeSpec{x=8,y=2,z=6,capacity=4,queue=new int[0]},new NodeSpec{x=-8,y=0,z=6,capacity=4,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1},new EdgeSpec{a=0,b=2}},groups=new GroupSpec[0]};
            var demoSpace=new WalkSpace(demo,null,true);var architecture=new CrowdScene(demoSpace,game.view);game.view.orthographicSize*=.83f;yield return Capture("architecture.png");
            foreach(var s in demoSpace.Stairs)if(s.polygon==null&&Mathf.Abs(s.width-3.4f)>.0001f)throw new Exception("Connection width mismatch");
            foreach(var p in architecture.platforms)if(Mathf.Abs(p.transform.localScale.x-5.2f)>.0001f||Mathf.Abs(p.transform.localScale.z-5.2f)>.0001f)throw new Exception("Platform size mismatch");
            if(CrowdScene.Camber(0)!=0||CrowdScene.Camber(1)!=0||Mathf.Abs(CrowdScene.Camber(.5f)-.22f)>.00001f)throw new Exception("Bridge camber endpoints");
            File.WriteAllText(Path.Combine(output,"preview-check.json"),"{\"passed\":true,\"characterEnvelope\":true,\"bridgeCamber\":true,\"artHumanAccepted\":false}");Application.Quit(0);
        }
    }
}



