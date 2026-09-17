using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StairsCrowd.Runtime;
using StairsCrowd.Core;

public static class CharacterWalkVerification
{
    static string Folder
    {
        get { var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-characterEvidenceFolder");return at>=0&&at+1<args.Length?args[at+1]:"artifacts/task003-integration"; }
    }
    [Serializable] sealed class Report
    {
        public bool passed, bindSurfaceMatches, resetMatches, sharing, timingPreservesPaths, paletteMatches, materialRegionsPreserved, runtimeMaterialStates;
        public string[] palette;
        public int vertices,triangles,bones;
        public float bindError, maximumAnimatedRadius, maximumAnimatedHeight, maximumStairVisualRisePerFrame;
        public long steadyAllocatedBytes;
        public string sourceHash;
    }
    public static void Run()
    {
        try { Check();Debug.Log("CHARACTER WALK VERIFICATION PASSED");EditorApplication.Exit(0); }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Check()
    {
        Directory.CreateDirectory(Folder);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var asset=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");Require(asset&&asset.Valid,"Missing walk asset");
        var art=Resources.Load<RefinedArtLibrary>("RefinedArtLibrary");
        Require(art&&art.characters!=null,"Missing refined material library");
        var report=new Report{vertices=asset.mesh.vertexCount,triangles=asset.mesh.triangles.Length/3,bones=asset.bones.Length,sourceHash=asset.sourceHash};
        Require(report.vertices==asset.sourceVertexCount&&report.triangles==asset.sourceTriangleCount,"Imported mesh counts differ from source");
        report.palette=new string[ColorCatalog.Count];
        for(int i=0;i<ColorCatalog.Count;i++)
        {
            uint rgb=ColorCatalog.Rgb[i];var color=new Color((rgb>>16&255)/255f,(rgb>>8&255)/255f,(rgb&255)/255f,1);
            Require(Vector4.Distance(art.characters[i].GetColor("_ClothColor"),color)<.00001f,"ColorCatalog material mismatch "+i);
            Require(art.characters[i].HasProperty("_RevealTint")&&art.characters[i].HasProperty("_PropDim"),"Missing state properties");
            report.palette[i]=rgb.ToString("X6");
        }
        report.paletteMatches=true;
        var masks=asset.mesh.uv2;var vertexColors=asset.mesh.colors;int ivoryCount=0,clothCount=0;
        for(int i=0;i<masks.Length;i++)
        {
            Require(masks[i].y==0||masks[i].y==1,"Invalid dye region");
            if(masks[i].y==0){ivoryCount++;Require(Vector4.Distance(vertexColors[i],new Color(.9490196f,.9019608f,.75686276f,1))<.0001f,"Ivory Gamma conversion");}
            else{clothCount++;Require(vertexColors[i]==Color.white,"Cloth tint contaminated");}
        }
        Require(ivoryCount>0&&clothCount>0&&ivoryCount+clothCount==report.vertices,"Missing material region");report.materialRegionsPreserved=true;
        var person=MakePerson(asset,art.characters[0]);var baked=new Mesh();person.walk.Renderer.BakeMesh(baked);
        var expected=asset.mesh.vertices;var actual=baked.vertices;
        Require(expected.Length==actual.Length,"Bake vertex count");for(int i=0;i<actual.Length;i++)report.bindError=Mathf.Max(report.bindError,Vector3.Distance(expected[i],actual[i]));
        Require(report.bindError<.00001f,"Bind surface changed");report.bindSurfaceMatches=true;
        for(int i=0;i<120;i++)
        {
            person.root.position+=Vector3.forward*(1.25f/120);person.TickWalk(1f/120);
            person.walk.Renderer.BakeMesh(baked);foreach(var v in baked.vertices){report.maximumAnimatedRadius=Mathf.Max(report.maximumAnimatedRadius,new Vector2(v.x,v.z).magnitude);report.maximumAnimatedHeight=Mathf.Max(report.maximumAnimatedHeight,v.y);}
        }
        for(int i=0;i<60;i++)person.TickWalk(1f/120);
        Require(person.PresentationSettled,"Arrival did not settle");
        person.waterBird.Reset();person.walk.Renderer.BakeMesh(baked);actual=baked.vertices;
        for(int i=0;i<actual.Length;i++)Require(Vector3.Distance(expected[i],actual[i])<.00001f,"Reset changed surface");report.resetMatches=true;
        for(int i=0;i<30;i++){person.root.position+=Vector3.forward*.01f;person.TickWalk(.01f);}
        long allocated=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<1000;i++){person.root.position+=Vector3.forward*.01f;person.TickWalk(.01f);}
        report.steadyAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;Require(report.steadyAllocatedBytes==0,"Steady walk allocates");
        Require(report.maximumAnimatedRadius*1.1f<=WalkSpace.ActorRadius+.001f&&report.maximumAnimatedHeight*1.1f<=CrowdScene.PersonHeight+.001f,"Animated capsule envelope");
        person.root.position=Vector3.zero;person.waterBird.Reset();
        var duplicate=MakePerson(asset,art.characters[1]);Require(duplicate.walk.Renderer.sharedMesh==person.walk.Renderer.sharedMesh,"Mesh not shared");report.sharing=true;UnityEngine.Object.DestroyImmediate(duplicate.root.gameObject);
        CheckTiming();report.timingPreservesPaths=true;
        report.maximumStairVisualRisePerFrame=CheckStairs(asset,art.characters[0]);
        RenderViews(person,art);
        CheckRuntimeMaterials(asset);report.runtimeMaterialStates=true;
        report.passed=true;File.WriteAllText(Folder+"/verification.json",JsonUtility.ToJson(report,true));
        UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(person.root.gameObject);
    }
    static PersonView MakePerson(CharacterWalkAsset asset,Material material)
    {
        var p=new PersonView{root=new GameObject("QA character").transform};p.visual=new GameObject("Character visual").transform;p.visual.SetParent(p.root,false);
        p.waterBird=new WaterBirdVisual(p,asset.mesh,material,null,null,asset,null);return p;
    }
    static void CheckRuntimeMaterials(CharacterWalkAsset asset)
    {
        var nodes=new NodeSpec[6];var groups=new GroupSpec[6];var edges=new EdgeSpec[5];
        for(int i=0;i<6;i++){nodes[i]=new NodeSpec{x=i*8,capacity=4,queue=new[]{i}};groups[i]=new GroupSpec{id=i,color=i};if(i>0)edges[i-1]=new EdgeSpec{a=i-1,b=i};}
        var level=new LevelSpec{nodes=nodes,groups=groups,edges=edges};var board=new Board(level);
        var camera=new GameObject("Runtime material QA camera",typeof(Camera)).GetComponent<Camera>();camera.transform.position=new Vector3(5,8,10);
        var scene=new CrowdScene(new WalkSpace(level,null,true),camera);scene.Populate(board.Current);
        try
        {
            Require(scene.UsesCharacterWalk,"Production character disabled");var block=new MaterialPropertyBlock();
            for(int i=0;i<6;i++)
            {
                var p=scene.people[i*4];Require(p.walk!=null&&p.walk.Renderer.sharedMesh==asset.mesh&&p.colored.Length==1,"Runtime mesh/renderer sharing");
                uint rgb=ColorCatalog.Rgb[i];var color=new Color((rgb>>16&255)/255f,(rgb>>8&255)/255f,(rgb&255)/255f,1);
                Require(Vector4.Distance(p.walk.Renderer.sharedMaterial.GetColor("_ClothColor"),color)<.00001f,"Runtime cloth color");
                scene.Highlight(board,i);Require(p.IsSelected&&Vector4.Distance(p.walk.Renderer.sharedMaterial.GetColor("_ClothColor"),Color.Lerp(color,Color.white,.2f))<.00001f,"Selection tint");
                scene.Highlight(board,-1);Require(!p.IsSelected&&Vector4.Distance(p.walk.Renderer.sharedMaterial.GetColor("_ClothColor"),color)<.00001f,"Selection restore");
            }
            scene.SetPropFocus(new HashSet<int>{0},board.Current);
            for(int i=0;i<6;i++){scene.people[i*4].walk.Renderer.GetPropertyBlock(block);Require(Mathf.Abs(block.GetFloat("_PropDim")-(i==0?1:.12f))<.00001f,"Prop focus");}
            var actor=scene.people[0];actor.SetHiddenVisual(true);Require(!actor.walk.Renderer.gameObject.activeSelf,"Hidden production body visible");
            actor.SetHiddenVisual(false);Require(actor.RevealTint==0&&!actor.walk.Renderer.gameObject.activeSelf&&actor.RevealUsesMask,"Reveal masked start");actor.TickReveal(.22f);
            actor.walk.Renderer.GetPropertyBlock(block);Require(actor.RevealTint>0&&actor.RevealTint<1&&Mathf.Abs(block.GetFloat("_RevealTint")-actor.RevealTint)<.00001f&&block.GetFloat("_PropDim")==1,"Reveal overwrote focus");
            scene.SetPropFocus(null,board.Current);actor.walk.Renderer.GetPropertyBlock(block);Require(block.GetFloat("_PropDim")==1&&Mathf.Abs(block.GetFloat("_RevealTint")-actor.RevealTint)<.00001f,"Focus overwrote reveal");
            actor.TickReveal(1);Require(!actor.RevealAnimating&&actor.RevealTint==1,"Reveal completion");
        }
        finally {scene.Dispose();UnityEngine.Object.DestroyImmediate(camera.gameObject);}
    }
    static void CheckTiming()
    {
        var track=new ActorTrack{actor=0,start=.08f,duration=.5f,points=new[]{Vector3.zero,new Vector3(2,0,0),new Vector3(2,0,3)},times=new[]{0f,.2f,.5f}};
        var plan=new MotionPlan{duration=.58f};plan.tracks.Add(track);var old=new Vector3[101];for(int i=0;i<=100;i++)old[i]=track.Position(.58f*i/100);
        CharacterWalkTiming.Apply(plan);float factor=plan.duration/.58f;
        for(int i=0;i<=100;i++)Require(Vector3.Distance(old[i],track.Position(.58f*i/100*factor))<.00001f,"Timing modified path");
        for(int i=1;i<track.points.Length;i++)Require(Vector3.Distance(track.points[i],track.points[i-1])/(track.times[i]-track.times[i-1])<=CharacterWalkTiming.MaximumSpeed+.0001f,"Speed cap");
    }
    static float CheckStairs(CharacterWalkAsset asset,Material material)
    {
        var level=new LevelSpec{nodes=new[]{new NodeSpec{capacity=4,queue=new[]{0}},new NodeSpec{x=8,y=2,capacity=4,transit=true,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0}}};
        var space=new WalkSpace(level,null,true);var state=Rules.Initial(level);var plan=FastMovement.Build(space,state,Rules.Preview(level,state,0,1));var track=plan.Track(0);
        var person=MakePerson(asset,material);person.walkGround=space;person.root.position=plan.initial[0];person.waterBird.Reset();
        float previous=person.walk.Renderer.transform.position.y,maximum=0;
        for(float t=1f/60;t<=track.End+1f/60;t+=1f/60)
        {
            var position=track.PlaybackPosition(t);var direction=Vector3.ProjectOnPlane(position-person.root.position,Vector3.up);
            person.Pose(position,direction,0,t<track.End);person.remainingDistance=track.RemainingDistance(t);person.TickWalk(1f/60);
            float y=person.walk.Renderer.transform.position.y;maximum=Mathf.Max(maximum,Mathf.Abs(y-previous));previous=y;
        }
        Require(maximum<.15f,"Stair navigation jump leaked into visual root");
        for(int i=0;i<60;i++)person.TickWalk(1f/60);Require(person.PresentationSettled,"Stair arrival not settled");
        person.walkGround=null;person.waterBird.Reset();Require(person.PresentationSettled,"Ground reset failed");UnityEngine.Object.DestroyImmediate(person.root.gameObject);return maximum;
    }
    static void RenderViews(PersonView person,RefinedArtLibrary art)
    {
        var camera=new GameObject("QA camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.04f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.60f,.61f);camera.nearClipPlane=.01f;
        var light=new GameObject("QA key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=new Color(.78f,.78f,.87f);
        string[] names={"front","side","back","three-quarter"};float[] angles={0,90,180,45};
        for(int i=0;i<4;i++)
        {
            camera.transform.position=new Vector3(Mathf.Sin(angles[i]*Mathf.Deg2Rad)*5,1.45f,Mathf.Cos(angles[i]*Mathf.Deg2Rad)*5);camera.transform.LookAt(new Vector3(0,.82f,0));
            Capture(camera,Folder+"/model-"+names[i]+".png",512,640);
        }
        camera.transform.position=new Vector3(4,3.4f,5);camera.transform.LookAt(new Vector3(0,.82f,0));
        for(int i=0;i<ColorCatalog.Count;i++)
        {
            var material=new Material(art.characters[i]);material.SetFloat("_GameplayLighting",1);material.SetFloat("_CharacterShadeFloor",.72f);
            person.walk.Renderer.sharedMaterial=material;Capture(camera,Folder+"/color-"+i+".png",512,640);person.walk.Renderer.sharedMaterial=art.characters[i];UnityEngine.Object.DestroyImmediate(material);
        }
        person.walk.Renderer.sharedMaterial=art.characters[0];
        File.WriteAllText(Folder+"/camera.json","{\"projection\":\"orthographic\",\"orthographicSize\":1.04,\"lookAtY\":0.82,\"radius\":5,\"cameraY\":1.45,\"yaw\":[0,90,180,45],\"sourceReference\":\"artifacts/pilgrim-model/pilgrim-reference.glb\",\"colorIds\":\"Current ColorCatalog order; exact RGB in verification.json\",\"motionEvidence\":\"Player character-probe/live-motion-evidence.json with matching sourceHash; synchronous Editor frames are not motion evidence\"}");
    }
    static void Capture(Camera camera,string path,int width,int height)
    {
        var rt=RenderTexture.GetTemporary(width,height,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);
    }
    static void Require(bool value,string reason){if(!value)throw new Exception("TASK-003: "+reason);}
}
