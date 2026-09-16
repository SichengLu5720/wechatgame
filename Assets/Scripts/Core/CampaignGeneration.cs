using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StairsCrowd.Core
{
    [Serializable] public sealed class LevelProvenance
    {
        public string id, generatorVersion, template, difficulty, solverStatus, reviewStatus;
        public int seed, visited, solutionMoves, mixedBoundaries;
    }
    public static class CampaignSchedule
    {
        public const int Count = 20;
        public static string Difficulty(int number)
        {
            if(number < 1 || number > Count) throw new ArgumentOutOfRangeException("number");
            return number == 9 || number == 19 ? "challenge" : number == 4 || number == 14 ? "hard" : "normal";
        }
    }
    // Offline authoring only: no Unity, rendering, storage or publishing dependencies.
    public static class CampaignGenerator
    {
        public const string Version = "six-color-templates-1";
        public static LevelSpec Candidate(int number, int seed)
        {
            var random = new Random(seed); int kind = (number-1)%4;
            var nodes = new List<NodeSpec>(); var edges = new List<EdgeSpec>();
            Action<float,float> add = (x,z) => nodes.Add(CloudLevels.N(x, 1.2f, z, true));
            Action<int,int> link = (a,b) => edges.Add(CloudLevels.E(a,b));
            // Grid cells are separated in world space. Collection leaves never carry through traffic.
            if(kind==0) { for(int i=0;i<6;i++)add(0,i*5); for(int i=1;i<6;i++)link(i-1,i); }
            if(kind==1) { add(0,0);add(0,5);add(-7,5);add(7,5);add(-7,10);add(7,10);link(0,1);link(1,2);link(1,3);link(2,4);link(3,5); }
            if(kind==2) { add(-7,0);add(0,0);add(7,0);add(7,5);add(0,5);add(-7,5);for(int i=0;i<6;i++)link(i,(i+1)%6); }
            if(kind==3) { add(-7,0);add(0,0);add(7,0);add(-7,5);add(0,5);add(7,5);for(int i=0;i<2;i++){link(i,i+1);link(i+3,i+4);}link(0,3);link(2,5);link(1,4); }
            for(int i=0;i<6;i++) {
                var p=nodes[i]; float x=p.x,z=p.z;
                if(kind==0)x+=(i%2==0?-7:7);
                else if(kind==1){if(i<2)z-=5;else x+=(x<0?-7:7);if(i==1){x=0;z=10;}}
                else z += i<3?-5:5;
                nodes.Add(CloudLevels.N(x,p.y+.4f,z,false));link(i,i+6);
            }
            // Align grid directions to the existing platform face normals (45 degrees).
            foreach(var n in nodes){float wx=n.x*1.4f,wz=-n.z*1.96f;n.x=(wx-wz)*.84852814f/1.4f;n.z=-(wx+wz)*.84852814f/1.96f;}
            var palette=Enumerable.Range(0,ColorCatalog.Count).ToArray();Shuffle(palette,random);
            string difficulty=CampaignSchedule.Difficulty(number); int block=difficulty=="normal"?2:difficulty=="hard"?3:6;
            var groups=new List<GroupSpec>();
            for(int start=0;start<6;start+=block){
                var colors=Enumerable.Range(start,block).SelectMany(c=>Enumerable.Repeat(palette[c],4)).ToArray();Shuffle(colors,random);
                for(int n=0;n<block;n++) {
                    nodes[6+start+n].queue=Enumerable.Range(groups.Count,4).ToArray();
                    for(int row=0;row<4;row++)groups.Add(new GroupSpec{id=groups.Count,color=colors[n*4+row]});
                }
            }
            var level=new LevelSpec{name="关卡 "+number,tip="",nodes=nodes.ToArray(),edges=edges.ToArray(),groups=groups.ToArray(),solution=new EdgeSpec[0],
                provenance=new LevelProvenance{id="campaign-v1-"+number.ToString("00"),generatorVersion=Version,template=new[]{"chain","branch","loop","dual"}[kind],difficulty=difficulty,seed=seed,reviewStatus="DRAFT"}};
            level.Validate();return level;
        }
        static void Shuffle(int[] a,Random r){for(int i=a.Length-1;i>0;i--){int j=r.Next(i+1);int t=a[i];a[i]=a[j];a[j]=t;}}
    }
    public sealed class SearchOutcome
    {
        public string status; public EdgeSpec[] moves=new EdgeSpec[0]; public int visited;
    }
    public static class OmniscientSearch
    {
        sealed class Entry { public State state;public Entry parent;public EdgeSpec move;public int depth,score,serial; }
        // Bounded best-first search. A found witness is explicitly not an optimality proof.
        public static SearchOutcome Find(LevelSpec level,int limit=30000,int milliseconds=15000,CancellationToken cancel=default(CancellationToken),Func<State,Move,bool> permit=null)
        {
            level.Validate();var watch=System.Diagnostics.Stopwatch.StartNew();int serial=0;
            var open=new SortedSet<Entry>(Comparer<Entry>.Create((a,b)=>a.score!=b.score?a.score.CompareTo(b.score):a.serial.CompareTo(b.serial)));
            var first=new Entry{state=Rules.Initial(level)};open.Add(first);var seen=new HashSet<string>{Rules.Key(level,first.state)};
            while(open.Count>0){
                cancel.ThrowIfCancellationRequested();if(watch.ElapsedMilliseconds>milliseconds)return new SearchOutcome{status="UnknownTime",visited=seen.Count};
                var e=open.Min;open.Remove(e);
                if(Rules.AllGathered(level,e.state)){var moves=new List<EdgeSpec>();for(var p=e;p.parent!=null;p=p.parent)moves.Add(p.move);moves.Reverse();return new SearchOutcome{status="SolvedFound",moves=moves.ToArray(),visited=seen.Count};}
                for(int a=0;a<level.nodes.Length;a++)for(int b=0;b<level.nodes.Length;b++){
                    var move=Rules.Preview(level,e.state,a,b);if(!move.ok||(permit!=null&&!permit(e.state,move)))continue;
                    var state=Rules.Apply(e.state,move);if(!seen.Add(Rules.Key(level,state)))continue;
                    if(seen.Count>limit)return new SearchOutcome{status="UnknownLimit",visited=seen.Count};
                    int cost=0;
                    for(int n=0;n<state.queues.Length;n++){
                        var q=state.queues[n];if(q.Count==0)continue;var colors=q.Select(id=>level.groups[id].color).ToArray();
                        int distinct=colors.Distinct().Count();cost+=distinct*35;
                        for(int i=1;i<colors.Length;i++)if(colors[i]!=colors[i-1])cost+=10;
                        if(level.nodes[n].transit)cost+=18;else if(distinct==1)cost-=q.Count*q.Count*5;
                    }
                    open.Add(new Entry{state=state,parent=e,move=new EdgeSpec{a=a,b=b},depth=e.depth+1,score=cost*4+e.depth+1,serial=++serial});
                }
            }
            return new SearchOutcome{status="Unsolvable",visited=seen.Count};
        }
    }
    public static class CampaignValidator
    {
        public static void Validate(LevelSpec l)
        {
            l.Validate();if(l.ColorCount!=6||l.groups.Length!=24||l.GoalCount!=6)throw new Exception("Campaign requires six colors, 24 rows and six collection platforms");
            if(l.groups.GroupBy(g=>g.color).Any(g=>g.Count()!=4))throw new Exception("Color conservation failed");
            if(l.nodes.Length!=12||l.nodes.Take(6).Any(n=>!n.transit)||l.nodes.Skip(6).Any(n=>n.transit))throw new Exception("Template node roles invalid");
            for(int i=6;i<12;i++)if(l.edges.Count(e=>e.a==i||e.b==i)!=1)throw new Exception("Collection platform must be a leaf");
            var core=l.edges.Where(e=>e.a<6&&e.b<6).ToArray();var reached=new HashSet<int>{0};
            for(int pass=0;pass<6;pass++)foreach(var e in core)if(reached.Contains(e.a)||reached.Contains(e.b)){reached.Add(e.a);reached.Add(e.b);}
            if(reached.Count!=6)throw new Exception("Disconnected core");
            var degree=Enumerable.Range(0,6).Select(n=>core.Count(e=>e.a==n||e.b==n)).ToArray();
            string template=l.provenance==null?"":l.provenance.template;
            if(template=="chain"&&(core.Length!=5||degree.Max()!=2)||template=="branch"&&(core.Length!=5||degree.Max()!=3)||template=="loop"&&(core.Length!=6||degree.Any(d=>d!=2))||template=="dual"&&(core.Length!=7||degree.Max()!=3)||!new[]{"chain","branch","loop","dual"}.Contains(template))throw new Exception("Topology does not match declared template");
            if(l.provenance==null||l.provenance.reviewStatus!="DRAFT")throw new Exception("Missing candidate provenance");
            if(Rules.Completed(l,Rules.Initial(l))!=0)throw new Exception("Initially completed platform");
            var board=new Board(l);foreach(var step in l.solution)if(!board.TryMove(step.a,step.b).ok)throw new Exception("Invalid witness");
            if(!board.Solved)throw new Exception("No complete no-prop witness");
        }
    }
}
