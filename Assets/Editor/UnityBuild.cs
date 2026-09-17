using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class UnityBuild
{
    static bool buildAfterVerify;public static void VerifyAndBuild(){buildAfterVerify=true;Verify();}
    public static void Verify()
    {
        try {
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text);
            int moves=0,samples=0;float min=float.PositiveInfinity;
            foreach(var level in catalog.levels){
                level.Validate();var board=new Board(level);var space=new WalkSpace(level);
                Debug.Log("VERIFY LEVEL "+level.name);
                foreach(var action in level.solution){
                    var move=Rules.Preview(level,board.Current,action.a,action.b);if(!move.ok)throw new Exception("Invalid solution: "+move.reason);
                    var plan=FastMovement.Build(space,board.Current,move);min=Mathf.Min(min,plan.MinimumClearance());
                    for(float t=0;t<=plan.duration+.04f;t+=.04f)for(int actor=0;actor<plan.initial.Length;actor++){
                        var p=plan.SupportedPosition(space,actor,t);float floor;if(!space.FootHeight(new Vector2(p.x,p.z),out floor)||p.y<floor+.024f)throw new Exception("Foot penetrates surface");samples++;
                    }
                    var seatsBefore=space.Positions(board.Current);var key=Rules.Key(level,board.Current);board.TryMove(action.a,action.b,plan.finalSlots);board.Undo();if(key!=Rules.Key(level,board.Current)||!seatsBefore.SequenceEqual(space.Positions(board.Current)))throw new Exception("Undo mismatch");board.TryMove(action.a,action.b,plan.finalSlots);moves++;
                    Debug.Log("VERIFY MOVE "+moves+" duration="+plan.duration+" clearance="+plan.MinimumClearance());
                }
                if(!board.Solved)throw new Exception("Unsolved level");board.Reset();if(board.Moves!=0||board.CanUndo)throw new Exception("Reset failed");
            }
            TutorialVerification.CheckDefinitions("artifacts/tutorial-definitions.json");
            FailureProgressVerification.Run();
            Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/verification.json","{\"passed\":true,\"levels\":"+catalog.levels.Length+",\"moves\":"+moves+",\"surfaceSamples\":"+samples+",\"actorOverlapAllowed\":true,\"minimumCenterDistance\":"+min.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            Debug.Log("VERIFICATION PASSED");if(buildAfterVerify)Build();else EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Build()
    {
        try {
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Stairs Sort",typeof(StairsGame)); ExportArt();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/StairsCrowd.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/StairsCrowd.unity",true)};
            PlayerSettings.companyName="Crowd Puzzle Lab";PlayerSettings.productName="群岛 · Stairs Sort";
            PlayerSettings.defaultScreenWidth=700;PlayerSettings.defaultScreenHeight=1000;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var player=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=player.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;player.ApplyModifiedPropertiesWithoutUndo();}
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            bool present=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==Shader.Find("Stairs/Pastel"))present=true;
            if(!present){int at=shaders.arraySize;shaders.InsertArrayElementAtIndex(at);shaders.GetArrayElementAtIndex(at).objectReferenceValue=Shader.Find("Stairs/Pastel");graphics.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName="Builds/Windows/StairsCrowd.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("WINDOWS BUILD PASSED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }    public static void VerifyAlternatives(){VerifyAlternativesInternal(false);}
    public static void VerifyAlternativesAndBuild(){VerifyAlternativesInternal(true);}
    static void VerifyAlternativesInternal(bool finishBuild)
    {
        try {
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text);int moves=0,states=0;float min=100;object gate=new object();var timings=new System.Collections.Generic.List<double>();
            var cases=new System.Collections.Generic.List<Tuple<WalkSpace,State,Move,string>>();
            foreach(var level in catalog.levels){
                var board=new Board(level);var space=new WalkSpace(level);
                for(int step=0;step<=level.solution.Length;step++){
                    states++;
                    for(int a=0;a<level.nodes.Length;a++)for(int b=0;b<level.nodes.Length;b++){
                        var move=Rules.Preview(level,board.Current,a,b);if(move.ok)cases.Add(Tuple.Create(space,board.Current.Clone(),move,level.name+" step="+step+" "+a+"->"+b));
                        else{var key=Rules.Key(level,board.Current);int beforeMoves=board.Moves;if(board.TryMove(a,b).ok||board.Moves!=beforeMoves||Rules.Key(level,board.Current)!=key)throw new Exception("Rejected move mutated state");}
                    }
                    if(step<level.solution.Length)board.TryMove(level.solution[step].a,level.solution[step].b);
                }
            }
            Debug.Log("VERIFYING "+cases.Count+" LEGAL MOVES");
            System.Threading.Tasks.Parallel.ForEach(cases,new System.Threading.Tasks.ParallelOptions{MaxDegreeOfParallelism=1},item=>{
                try {
                    var space=item.Item1;var timer=System.Diagnostics.Stopwatch.StartNew();var plan=MotionPlanner.Build(space,item.Item2,item.Item3);timer.Stop();timings.Add(timer.Elapsed.TotalMilliseconds);float clearance=plan.MinimumClearance();
                    for(int id=0;id<plan.initial.Length;id++){plan.SupportedPosition(space,id,0);plan.SupportedPosition(space,id,plan.duration);}
                    foreach(var track in plan.tracks)for(float t=track.start;t<=track.End;t+=.037f)plan.SupportedPosition(space,track.actor,t);
                    lock(gate){min=Mathf.Min(min,clearance);moves++;if(moves%50==0)Debug.Log("ALTERNATIVES "+moves+" / "+cases.Count);}
                }catch(Exception e){throw new Exception("Alternative failed: "+item.Item4,e);}
            });
            File.WriteAllText("artifacts/alternatives.json","{\"passed\":true,\"states\":"+states+",\"legalMoves\":"+moves+",\"minimumCenterDistance\":"+min.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            timings.Sort();File.WriteAllText("artifacts/click-performance.json", "{\"moves\":"+timings.Count+",\"meanMs\":"+timings.Average().ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"p95Ms\":"+timings[(int)(timings.Count*.95)].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"maxMs\":"+timings.Last().ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");Debug.Log("ALTERNATIVES PASSED");if(finishBuild){buildAfterVerify=true;Verify();}else EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }    static void ExportArt()
    {
        Directory.CreateDirectory("Assets/Art/Materials");Directory.CreateDirectory("Assets/Art/Prefabs");
        var camera=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.backgroundColor=new Color(.9f,.85f,.8f);camera.clearFlags=CameraClearFlags.SolidColor;
        var level=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text).levels[0];var preview=new CrowdScene(new WalkSpace(level),camera);preview.Populate(Rules.Initial(level));preview.root.name="Editor Preview";preview.root.tag="EditorOnly";
        for(int i=0;i<preview.materials.Count;i++){
            string path="Assets/Art/Materials/Pastel-"+i.ToString("00")+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(path))AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(preview.materials[i],path);
        }
        Action<GameObject,string> save=(go,name)=>{var old=go.transform.position;go.transform.position=Vector3.zero;PrefabUtility.SaveAsPrefabAsset(go,"Assets/Art/Prefabs/"+name+".prefab");go.transform.position=old;};
        foreach(Transform child in preview.root.transform){if(child.name=="Sorting Pavilion 0")save(child.gameObject,"SortingPavilion");if(child.name=="Transit Pier 2")save(child.gameObject,"TransitPier");if(child.name=="Stair Link 0")save(child.gameObject,"ConnectedStairway");}
        save(preview.people[0].root.gameObject,"CloakedWalker");
    }    public static void ExportAssets()
    {
        try{AssetDatabase.ExportPackage(new[]{"Assets/Art","Assets/Shaders","Assets/Scripts/Runtime/PlatformTag.cs"},"artifacts/StairsCrowdArt.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);Debug.Log("ART PACKAGE EXPORTED");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }    public static void OpenForEditing()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/StairsCrowd.unity");
        EditorApplication.delayCall+=()=>{var preview=GameObject.Find("Editor Preview");var bounds=new Bounds(Vector3.zero,Vector3.one);bool first=true;foreach(var renderer in preview.GetComponentsInChildren<Renderer>()){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}var view=SceneView.lastActiveSceneView;if(!view)view=EditorWindow.GetWindow<SceneView>();view.LookAt(bounds.center,Quaternion.Euler(39,-26.6f,0),bounds.extents.magnitude,true,true);Selection.activeGameObject=GameObject.Find("Stairs Sort");Debug.Log("EDITOR READY");};
    }    public static void Release()
    {
        try {
            Directory.CreateDirectory("artifacts/navigation/legacy");
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text);
            for(int i=0;i<catalog.levels.Length;i++){var space=NavigationFactory.ForOfflineValidation(catalog.levels[i]);var bytes=space.Bake();File.WriteAllBytes("artifacts/navigation/legacy/level-"+i+".bytes",bytes);var copy=NavigationFactory.ForOfflineValidation(catalog.levels[i],bytes);if(copy.CacheStatus!="Valid"||copy.GeometrySignature!=space.GeometrySignature)throw new Exception("Navigation bake roundtrip failed");}
            AssetDatabase.Refresh();VerifyShapes();VerifyAlternativesInternal(true);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void VerifyShapes()
    {
        int checkedMoves=0;
        foreach(int degree in new[]{4,5,6}){
            var level=new LevelSpec{name="Junction "+degree,tip="",nodes=new NodeSpec[degree+1],edges=new EdgeSpec[degree],groups=Enumerable.Range(0,4).Select(i=>new GroupSpec{id=i,color=0}).ToArray(),solution=new EdgeSpec[0]};
            level.nodes[0]=new NodeSpec{x=0,y=1,z=0,capacity=4,transit=true,queue=new int[0]};
            for(int i=0;i<degree;i++){float a=i*Mathf.PI*2/degree;level.nodes[i+1]=new NodeSpec{x=Mathf.Cos(a)*9,y=2,z=Mathf.Sin(a)*9,capacity=4,transit=i!=0,queue=i==0?new[]{0,1}:i==1?new[]{2,3}:new int[0]};level.edges[i]=new EdgeSpec{a=0,b=i+1};}
            level.nodes[1].surfaceDirection=1;
            var space=new WalkSpace(level);Debug.Log("FIXED SHAPE "+degree+" stairs "+string.Join(";",space.Stairs.Where(s=>s.polygon==null).Select(s=>s.index+":"+s.start+" to "+s.end)));if(space.Sides[0]!=degree)throw new Exception("Platform side count mismatch");
            var camera=new GameObject("Shape test camera").AddComponent<Camera>();var art=new CrowdScene(space,camera);art.Populate(Rules.Initial(level));Physics.SyncTransforms();
            if(degree>4&&!(art.platforms[0].GetComponent<Collider>() is MeshCollider))throw new Exception("Polygon missing collider");
            foreach(int destination in Enumerable.Range(0,degree+1).Where(n=>n!=1)){var move=Rules.Preview(level,Rules.Initial(level),1,destination);if(!move.ok)continue;var plan=MotionPlanner.Build(space,Rules.Initial(level),move);for(float t=0;t<plan.duration;t+=.031f)foreach(var track in plan.tracks)plan.SupportedPosition(space,track.actor,t);checkedMoves++;}
            art.Dispose();UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
        File.WriteAllText("artifacts/platform-shapes.json","{\"passed\":true,\"connections\":[4,5,6],\"shapeAndRouteChecks\":"+checkedMoves+"}");
    }}


