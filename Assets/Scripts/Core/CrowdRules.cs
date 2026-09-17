using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StairsCrowd.Core
{
    [Serializable] public class Catalog { public LevelSpec[] levels; }
    [Serializable] public class NodeSpec { public float x,y,z; public int capacity,unlockAfter; public bool transit,sticky,colorRestricted,curtain; public int targetColor,surfaceDirection; public int[] queue; }
    [Serializable] public class EdgeSpec { public int a,b; }
    [Serializable] public class GroupSpec { public int id,color; public bool hidden; }
    [Serializable] public class LevelSpec
    {
        public LevelProvenance provenance; public string name,tip; public NodeSpec[] nodes; public EdgeSpec[] edges; public GroupSpec[] groups; public EdgeSpec[] solution;
        public int ColorCount { get { return groups.Select(g=>g.color).Distinct().Count(); } }
        public int GoalCount { get { return nodes.Count(n=>!n.transit); } }
        public void Validate()
        {
            if(nodes==null||groups==null||edges==null||nodes.Any(n=>n==null)||groups.Any(g=>g==null)||edges.Any(e=>e==null))throw new Exception("关卡内容不完整");
            var ids=new HashSet<int>();
            for(int i=0;i<groups.Length;i++)if(groups[i].id!=i||!ColorCatalog.Contains(groups[i].color))throw new Exception("人物编号或颜色无效");
            foreach(var n in nodes){if(n.capacity!=4)throw new Exception("平台容量必须为 16 人");if(n.queue==null||n.queue.Length>n.capacity||n.unlockAfter<0)throw new Exception("初始人数或封印次数无效");foreach(int id in n.queue)if(id<0||id>=groups.Length||!ids.Add(id))throw new Exception("人物重复或编号无效");}
            if(ids.Count!=groups.Length)throw new Exception("存在未放置的人物");
            foreach(var n in nodes)if(n.transit&&n.queue.Select(id=>groups[id].color).Distinct().Count()>1)throw new Exception("中转平台只能放置同一种颜色的人物");
            foreach(var e in edges)if(e.a<0||e.b<0||e.a>=nodes.Length||e.b>=nodes.Length||e.a==e.b)throw new Exception("楼梯连接无效");
            var links=new HashSet<string>();foreach(var e in edges)if(!links.Add(Math.Min(e.a,e.b)+":"+Math.Max(e.a,e.b)))throw new Exception("存在重复连接");
            foreach(var n in nodes){
                if(float.IsNaN(n.x)||float.IsNaN(n.y)||float.IsNaN(n.z)||float.IsInfinity(n.x)||float.IsInfinity(n.y)||float.IsInfinity(n.z))throw new Exception("平台位置必须为有效数字");
                if(n.colorRestricted&&(!ColorCatalog.Contains(n.targetColor)))throw new Exception("目标颜色无效");
            }
        }
    }
    public sealed class State
    {
        public List<int>[] queues; public bool[] revealed;
        // Presentation slots are independent of sorting order, and travel with undo.
        public int[] actorSlots;
        public State Clone(){return new State{queues=queues.Select(q=>new List<int>(q)).ToArray(),revealed=(bool[])revealed.Clone(),actorSlots=actorSlots==null?null:(int[])actorSlots.Clone()};}
    }
    public sealed class Move
    {
        public bool ok; public string reason; public int from,to,count; public int[] path,ids;
        public static Move Invalid(string text){return new Move{reason=text};}
    }
    public static class Rules
    {
        public const int MembersPerGroup=4;
        public static State Initial(LevelSpec l){var state=new State{queues=l.nodes.Select(n=>new List<int>(n.queue)).ToArray(),revealed=l.groups.Select(g=>!g.hidden).ToArray()};foreach(var q in state.queues)if(q.Count>0)state.revealed[q[0]]=true;return state;}
        static bool[] Completion(LevelSpec l,State s)
        {
            var done=new bool[l.nodes.Length];int count=0;bool changed=true;
            while(changed){changed=false;for(int n=0;n<done.Length;n++){var q=s.queues[n];var node=l.nodes[n];if(done[n]||node.transit||q.Count!=4||node.unlockAfter>count)continue;
                if((!node.colorRestricted||l.groups[q[0]].color==node.targetColor)&&q.All(id=>l.groups[id].color==l.groups[q[0]].color)){done[n]=true;count++;changed=true;}}}
            return done;
        }
        public static bool Complete(LevelSpec l,State s,int n){return Completion(l,s)[n];}
        public static int Completed(LevelSpec l,State s){return Completion(l,s).Count(v=>v);}
        public static bool AllGathered(LevelSpec l,State s){if(l.groups.Length==0)return false;var done=Completion(l,s);for(int n=0;n<s.queues.Length;n++)if(s.queues[n].Count>0&&!done[n])return false;return true;}
        public static bool Gated(LevelSpec l,State s,int n){return l.nodes[n].unlockAfter>Completed(l,s);}
        public static int MovableCount(LevelSpec l,State s,int n)
        {
            var q=s.queues[n];if(q.Count==0)return 0;
            int count=1,color=l.groups[q[0]].color;
            while(count<q.Count&&s.revealed[q[count]]&&l.groups[q[count]].color==color)count++;
            return count;
        }
        public static Move Preview(LevelSpec l,State s,int a,int b)
        {
            if(a<0||b<0||a>=l.nodes.Length||b>=l.nodes.Length||a==b)return Move.Invalid("请选择另一个平台");
            var source=s.queues[a];var dest=s.queues[b];
            if(source.Count==0)return Move.Invalid("先选择有人的平台");
            if(Gated(l,s,a)||Gated(l,s,b))return Move.Invalid("先完成一种颜色，解锁平台");
            if(Complete(l,s,a)||Complete(l,s,b))return Move.Invalid("这个平台已经完成集合");
            if(l.nodes[a].sticky)return Move.Invalid("这里的人已固定，无法搬出");
            if(dest.Count>=l.nodes[b].capacity)return Move.Invalid("目标平台已经站满");
            if(l.nodes[b].transit&&dest.Any(id=>l.groups[id].color!=l.groups[source[0]].color))return Move.Invalid("中转平台只能容纳同一种颜色");
            if(dest.Count>0&&l.groups[dest[0]].color!=l.groups[source[0]].color)return Move.Invalid("目标前排颜色不同");
            var route=Route(l,s,a,b);if(route==null)return Move.Invalid("楼梯被人群挡住，请先腾空落脚点");
            int count=Math.Min(MovableCount(l,s,a),l.nodes[b].capacity-dest.Count);
            return new Move{ok=true,from=a,to=b,count=count,path=route,ids=source.Take(count).ToArray()};
        }
        public static int[] Route(LevelSpec l,State s,int a,int b)
        {
            var parent=Enumerable.Repeat(-1,l.nodes.Length).ToArray();parent[a]=a;var queue=new Queue<int>();queue.Enqueue(a);
            while(queue.Count>0){int n=queue.Dequeue();foreach(var e in l.edges){int k=e.a==n?e.b:e.b==n?e.a:-1;if(k<0||parent[k]>=0||Gated(l,s,k))continue;if(k!=b&&s.queues[k].Count!=0)continue;parent[k]=n;if(k==b){var result=new List<int>{b};for(int j=b;j!=a;){j=parent[j];result.Add(j);}result.Reverse();return result.ToArray();}queue.Enqueue(k);}}
            return null;
        }
        public static State Apply(State s,Move m)
        {
            if(!m.ok)throw new ArgumentException("Cannot apply invalid move");var next=s.Clone();next.queues[m.from].RemoveRange(0,m.count);
            // Same-color crowds are equivalent. First departures occupy the deepest
            // reserved row, so later arrivals never walk through settled people.
            next.queues[m.to].InsertRange(0,m.ids.Reverse());
            if(next.queues[m.from].Count>0)next.revealed[next.queues[m.from][0]]=true;return next;
        }
        public static string Key(LevelSpec l,State s){return string.Join("|",s.queues.Select(q=>string.Join("",q.Select(id=>l.groups[id].color.ToString()+(s.revealed[id]?"":"?")))));}
    }
    public sealed class Board
    {
        public LevelSpec Level {get;private set;} public State Current {get;private set;} public int Moves {get;private set;}
        readonly LevelSpec original;
        sealed class Snapshot {public LevelSpec level;public State state;public int moves;}
        readonly Stack<Snapshot> history=new Stack<Snapshot>();
        public bool Solved { get {return Rules.AllGathered(Level,Current);} }
        public bool CanUndo {get{return history.Count>0;}}
        public Board(LevelSpec level){level.Validate();original=level;Reset();}
        void Save(){history.Push(new Snapshot{level=Level,state=Current.Clone(),moves=Moves});}
        public void Reset(){Level=original;Current=Rules.Initial(Level);Moves=0;history.Clear();}
        public Move TryMove(int from,int to,int[] actorSlots=null){var m=Rules.Preview(Level,Current,from,to);if(!m.ok)return m;var next=Rules.Apply(Current,m);if(actorSlots==null)next.actorSlots=null;if(actorSlots!=null){if(actorSlots.Length!=Level.groups.Length*4)throw new ArgumentException("Invalid actor slots");for(int n=0;n<Level.nodes.Length;n++)if(Level.nodes[n].transit){var used=new HashSet<int>();foreach(int g in next.queues[n])for(int i=0;i<4;i++){int slot=actorSlots[g*4+i];if(slot<0||slot>=Level.nodes[n].capacity*4||!used.Add(slot))throw new ArgumentException("Invalid transit slot assignment");}}next.actorSlots=(int[])actorSlots.Clone();}Save();Current=next;Moves++;return m;}
        public void ApplyProp(LevelSpec level,State state){level.Validate();if(state.queues.Length!=level.nodes.Length||state.revealed.Length!=level.groups.Length)throw new ArgumentException("Invalid prop state");for(int n=0;n<level.nodes.Length;n++)if(level.nodes[n].transit&&state.queues[n].Select(id=>level.groups[id].color).Distinct().Count()>1)throw new ArgumentException("中转平台只能容纳同一种颜色");Save();Level=level;Current=state.Clone();}
        public bool Undo(){if(history.Count==0)return false;var previous=history.Pop();Level=previous.level;Current=previous.state;Moves=previous.moves;return true;}
    }
    public sealed class SolveResult {public string status;public EdgeSpec[] moves;public int visited;}
    public static class Solver
    {
        public static SolveResult Solve(LevelSpec l,State start,int limit=150000,CancellationToken cancel=default(CancellationToken))
        {
            cancel.ThrowIfCancellationRequested();
            // Reuse a fully validated authored continuation before exploring alternatives.
            // Matching by visible queue state also supports equivalent same-color group IDs.
            if(l.solution!=null&&l.solution.Length>0){
                var probe=Rules.Initial(l);string wanted=Rules.Key(l,start);int match=Rules.Key(l,probe)==wanted?0:-1;bool valid=true;
                for(int i=0;i<l.solution.Length;i++){
                    cancel.ThrowIfCancellationRequested();var edge=l.solution[i];var move=Rules.Preview(l,probe,edge.a,edge.b);if(!move.ok){valid=false;break;}
                    probe=Rules.Apply(probe,move);if(Rules.Key(l,probe)==wanted)match=i+1;
                }
                if(valid&&match>=0&&Rules.AllGathered(l,probe))return new SolveResult{status="solved",moves=l.solution.Skip(match).ToArray(),visited=l.solution.Length+1};
            }
            var states=new List<State>{start};var parents=new List<int>{-1};var actions=new List<EdgeSpec>{null};var seen=new HashSet<string>{Rules.Key(l,start)};
            for(int h=0;h<states.Count;h++){
                if((h&127)==0)cancel.ThrowIfCancellationRequested();var s=states[h];
                if(Rules.AllGathered(l,s)){var result=new List<EdgeSpec>();for(int j=h;parents[j]>=0;j=parents[j])result.Add(actions[j]);result.Reverse();return new SolveResult{status="solved",moves=result.ToArray(),visited=states.Count};}
                for(int a=0;a<l.nodes.Length;a++)for(int b=0;b<l.nodes.Length;b++){var m=Rules.Preview(l,s,a,b);if(!m.ok)continue;var next=Rules.Apply(s,m);if(!seen.Add(Rules.Key(l,next)))continue;states.Add(next);parents.Add(h);actions.Add(new EdgeSpec{a=a,b=b});if(states.Count>=limit)return new SolveResult{status="limit",moves=new EdgeSpec[0],visited=states.Count};}
            }
            return new SolveResult{status="unsolvable",moves=new EdgeSpec[0],visited=states.Count};
        }
    }
}

