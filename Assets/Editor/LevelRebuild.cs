using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class LevelRebuild
{
    public static void VerifyHintsAndBuild()
    {
        try{
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Resources/levels.json"));int checks=0;
            foreach(var level in catalog.levels){var board=new Board(level);
                for(int step=0;step<level.solution.Length;step++){
                    var result=Solver.Solve(level,board.Current,150000);if(result.status!="solved"||result.moves.Length==0)throw new Exception("Missing hint continuation");
                    var replay=new Board(level);for(int i=0;i<step;i++)replay.TryMove(level.solution[i].a,level.solution[i].b);
                    foreach(var move in result.moves)if(!replay.TryMove(move.a,move.b).ok)throw new Exception("Hint rejected by new rules");if(!replay.Solved)throw new Exception("Hint does not complete level");
                    board.TryMove(level.solution[step].a,level.solution[step].b);checks++;
                }
            }
            File.WriteAllText("artifacts/hints-13.json","{\"passed\":true,\"continuations\":"+checks+"}");UnityBuild.Build();
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Release()
    {
        try {
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Resources/levels.json"));
            var report=new System.Collections.Generic.List<string>();int bases=0;
            foreach(var level in catalog.levels){
                level.Validate();var solved=Solver.Solve(level,Rules.Initial(level),1200000);
                if(solved.status!="solved")throw new Exception(level.name+" not solved: "+solved.status+" visited="+solved.visited);
                level.solution=solved.moves;Debug.Log("REBUILT "+level.name+" people="+level.groups.Length*4+" sorting="+level.nodes.Count(n=>!n.transit)+" steps="+solved.moves.Length+" searched="+solved.visited);
                var board=new Board(level);foreach(var m in level.solution)if(!board.TryMove(m.a,m.b).ok)throw new Exception("Invalid rebuilt solution");if(!board.Solved||board.Current.queues.Where((q,i)=>level.nodes[i].transit).Any(q=>q.Count!=0))throw new Exception("Not all people on sorting platforms");
                var camera=new GameObject("Foundation verification camera").AddComponent<Camera>();var scene=new CrowdScene(new WalkSpace(level),camera);
                for(int n=0;n<level.nodes.Length;n++){
                    var platform=scene.platforms[n].transform;var foot=platform.parent.Find("Foundation foot "+n);var cap=platform.parent.Find("Foundation cap "+n);
                    if(!foot||!cap)throw new Exception("Foundation missing");
                    if(Vector3.ProjectOnPlane(foot.position-platform.position,Vector3.up).magnitude>.0001f||Quaternion.Angle(foot.rotation,platform.rotation)>.001f)throw new Exception("Foundation/platform alignment mismatch");
                    if(Mathf.Abs(foot.localScale.x/platform.localScale.x-.86f)>.001f||Mathf.Abs(foot.localScale.z/platform.localScale.z-.86f)>.001f)throw new Exception("Foundation proportions mismatch");bases++;
                }
                scene.Dispose();UnityEngine.Object.DestroyImmediate(camera.gameObject);
                report.Add("{\"name\":\""+level.name+"\",\"people\":"+level.groups.Length*4+",\"sortingPlatforms\":"+level.nodes.Count(n=>!n.transit)+",\"transitPlatforms\":"+level.nodes.Count(n=>n.transit)+",\"solutionMoves\":"+solved.moves.Length+"}");
            }
            // Reject both too many and too few sorting destinations when authoring levels.
            var sample=JsonUtility.FromJson<LevelSpec>(JsonUtility.ToJson(catalog.levels[0]));sample.nodes.First(n=>n.transit).transit=false;bool rejected=false;try{sample.Validate();}catch(Exception){rejected=true;}if(!rejected)throw new Exception("Excess sorting platforms accepted");
            sample=JsonUtility.FromJson<LevelSpec>(JsonUtility.ToJson(catalog.levels[0]));sample.nodes.First(n=>!n.transit).transit=true;rejected=false;try{sample.Validate();}catch(Exception){rejected=true;}if(!rejected)throw new Exception("Missing sorting platforms accepted");
            File.WriteAllText("Assets/Resources/levels.json",JsonUtility.ToJson(catalog,true));AssetDatabase.Refresh();
            File.WriteAllText("artifacts/levels-13.json","{\"passed\":true,\"alignedFoundations\":"+bases+",\"strictCountValidation\":true,\"levels\":["+string.Join(",",report)+"]}");
            RevisionVerification.Run();UnityBuild.Release();
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
