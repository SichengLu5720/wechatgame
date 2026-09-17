using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
public static class CampaignBuild
{
    public static void VerifyConcurrent()
    {
        try{var catalog=CampaignRepository.Load();foreach(var l in catalog.levels){var space=NavigationFactory.Create(l);var board=new Board(l);MotionPlan active=null;int step=0;foreach(var action in l.solution){try{var plan=MotionPlanner.Build(space,board.Current,Rules.Preview(l,board.Current,action.a,action.b));active=MotionComposer.Append(active,.05833333f,plan);board.TryMove(action.a,action.b);step++;}catch(Exception error){throw new Exception(l.provenance.id+" concurrent step="+step+" "+action.a+"->"+action.b,error);}}Debug.Log("CONCURRENT PASS "+l.provenance.id);}EditorApplication.Exit(0);}catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void ValidateAndBake()
    {
        try {
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText("artifacts/architecture-v6/candidate.json"));
            if(catalog.levels.Length!=20)throw new Exception("Incomplete pool");
            Directory.CreateDirectory("artifacts/architecture-v6/navigation");
            // Resolve a deterministic safe embedding for each topology before freezing the pool.
            foreach(var family in catalog.levels.GroupBy(l=>l.provenance.template)){
                var source=family.First();var original=source.nodes.Select(n=>new Vector3(n.x,n.y,n.z)).ToArray();bool fit=false;
                for(int attempt=0;attempt<30&&!fit;attempt++){
                    float angle=(attempt%6-2)*3f*Mathf.Deg2Rad,scale=1+(attempt/6)*.08f;
                    for(int n=0;n<source.nodes.Length;n++){
                        float x=original[n].x*1.4f,z=-original[n].z*1.96f;
                        source.nodes[n].x=(x*Mathf.Cos(angle)-z*Mathf.Sin(angle))*scale/1.4f;
                        source.nodes[n].z=-(x*Mathf.Sin(angle)+z*Mathf.Cos(angle))*scale/1.96f;
                    }
                    try{LayoutSafety.Validate(new WalkSpace(source));fit=true;Debug.Log("TEMPLATE FIT "+family.Key+" variant="+attempt);}catch(Exception){ }
                }
                if(!fit)throw new Exception("No safe geometry embedding for "+family.Key);
                foreach(var l in family)for(int n=0;n<l.nodes.Length;n++){
                    l.nodes[n].x=source.nodes[n].x;l.nodes[n].z=source.nodes[n].z;
                    if(n>=6){var toward=WalkSpace.WorldCenter(l.nodes[n-6])-WalkSpace.WorldCenter(l.nodes[n]);float angle=Mathf.Atan2(toward.z,toward.x)*Mathf.Rad2Deg;l.nodes[n].surfaceDirection=((Mathf.RoundToInt((-45-angle)/90)%4)+4)%4;}
                }
            }
            int moves=0,samples=0;float min=float.PositiveInfinity;
            foreach(var l in catalog.levels){
                CampaignValidator.Validate(l);var space=new WalkSpace(l);
                foreach(var edge in l.edges)try{Navigator.Find(space,new[]{space.Centers[edge.a]+Vector3.up*WalkSpace.FootGap},0,space.Centers[edge.b]+Vector3.up*WalkSpace.FootGap,space.RouteMask(new[]{edge.a,edge.b}));}catch(Exception error){foreach(var s in space.Stairs.Where(s=>s.a==edge.a&&s.b==edge.b))Debug.Log("STAIR "+s.start+" -> "+s.end+" polygon="+(s.polygon==null?"none":string.Join(";",s.polygon.Select(p=>p.ToString()))));throw new Exception(l.provenance.id+" edge "+edge.a+"-"+edge.b,error);}
                LayoutSafety.Validate(space);
                Debug.Log("CAMPAIGN GEOMETRY "+l.provenance.id);
                var blocked=new System.Collections.Generic.HashSet<string>();bool routed=false;
                for(int attempt=0;attempt<40&&!routed;attempt++){
                    var probe=new Board(l);routed=true;
                    foreach(var action in l.solution){
                        var move=Rules.Preview(l,probe.Current,action.a,action.b);
                        try{MotionPlanner.Build(space,probe.Current,move);}catch(Exception){blocked.Add(Rules.Key(l,probe.Current)+":"+action.a+":"+action.b);routed=false;break;}
                        probe.TryMove(action.a,action.b);
                    }
                    if(!routed){var solved=OmniscientSearch.Find(l,80000,30000,default(System.Threading.CancellationToken),(state,move)=>!blocked.Contains(Rules.Key(l,state)+":"+move.from+":"+move.to));if(solved.status!="SolvedFound")throw new Exception(l.provenance.id+" geometry-constrained search "+solved.status);l.solution=solved.moves;l.provenance.visited=solved.visited;l.provenance.solutionMoves=solved.moves.Length;Debug.Log("REPLAN "+l.provenance.id+" blocked="+blocked.Count);}
                }
                if(!routed)throw new Exception(l.provenance.id+" exhausted geometry replans");
                var board=new Board(l);
                foreach(var action in l.solution){
                    var move=Rules.Preview(l,board.Current,action.a,action.b);if(!move.ok)throw new Exception("Invalid witness");
                    var plan=MotionPlanner.Build(space,board.Current,move);float clearance=plan.MinimumClearance();min=Mathf.Min(min,clearance);
                    if(clearance<WalkSpace.Separation-.002f)throw new Exception("Crowd clearance failed");
                    for(float t=0;t<=plan.duration+.04f;t+=.08f)for(int actor=0;actor<plan.initial.Length;actor++){
                        var p=plan.SupportedPosition(space,actor,t);float floor;if(!space.FootHeight(new Vector2(p.x,p.z),out floor)||p.y<floor+.024f)throw new Exception("Foot penetrates surface");samples++;
                    }
                    string key=Rules.Key(l,board.Current);board.TryMove(action.a,action.b);board.Undo();if(Rules.Key(l,board.Current)!=key)throw new Exception("Undo mismatch");board.TryMove(action.a,action.b);moves++;
                }
                if(!board.Solved)throw new Exception("Incomplete replay");
                File.WriteAllBytes("artifacts/architecture-v6/navigation/"+l.provenance.id+".bytes",space.Bake());
                Debug.Log("CAMPAIGN REPLAY "+l.provenance.id+" moves="+l.solution.Length);
            }
            File.WriteAllText("artifacts/architecture-v6/embedded.json",JsonUtility.ToJson(catalog,true));
            File.WriteAllText("artifacts/architecture-v6/geometry.json",JsonUtility.ToJson(new Report{passed=true,levels=20,moves=moves,samples=samples,minimumClearance=min},true));
            Debug.Log("CAMPAIGN VALIDATION PASSED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    [Serializable] public class Report{public bool passed;public int levels,moves,samples;public float minimumClearance;}
}
