using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class PropsVerification
{
    static int assertions;
    static void Check(bool value,string reason){assertions++;if(!value)throw new Exception("PROPS: "+reason);}
    public static LevelSpec Fixture(){return new LevelSpec{name="Prop regression",tip="",nodes=new[]{new NodeSpec{x=0,y=1,z=0,capacity=4,queue=new[]{0,1,2,3}},new NodeSpec{x=8,y=1,z=0,capacity=4,transit=true,queue=new int[0]},new NodeSpec{x=16,y=1,z=0,capacity=4,transit=true,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1},new EdgeSpec{a=1,b=2}},groups=Enumerable.Range(0,4).Select(i=>new GroupSpec{id=i,color=i%2,hidden=i>0}).ToArray(),solution=new EdgeSpec[0]};}
    public static void Run()
    {
        try{
            for(int seed=0;seed<50;seed++){var l=Fixture();var b=new Board(l);var key=Rules.Key(l,b.Current);var others=b.Current.queues[1].ToArray();Check(PropActions.Shuffle(b,0,new System.Random(seed)),"shuffle executes");Check(key!=Rules.Key(l,b.Current),"shuffle changes colors");Check(others.SequenceEqual(b.Current.queues[1]),"other platforms unchanged");Check(b.Current.queues[0].OrderBy(x=>x).SequenceEqual(new[]{0,1,2,3}),"identity preserved");Check(b.Current.revealed[b.Current.queues[0][0]],"front revealed");Check(b.Moves==0,"props do not count as moves");Check(b.Undo()&&Rules.Key(l,b.Current)==key,"shuffle full undo");Check(!PropActions.Shuffle(b,1,new System.Random(seed))&&!b.CanUndo,"empty no mutation");}
            var source=Fixture();var board=new Board(source);LevelSpec candidate;WalkSpace geometry;
            Check(StairsGame.TryPlatformCandidate(board,0,new System.Random(42),out candidate,out geometry),"legal platform found");
            Check(candidate.nodes[candidate.nodes.Length-1].y>=1f&&candidate.nodes[candidate.nodes.Length-1].y<=3.6f&&Math.Abs(candidate.nodes[candidate.nodes.Length-1].y-source.nodes[0].y)>.001f,"random height follows generated level range");
            Check(candidate.nodes.Length==source.nodes.Length+1&&source.nodes.Length==3,"source topology not mutated");
            var state=board.Current.Clone();Array.Resize(ref state.queues,candidate.nodes.Length);state.queues[state.queues.Length-1]=new List<int>();board.ApplyProp(candidate,state);
            Check(board.Current.queues.Length==board.Level.nodes.Length,"state topology matches");
            Check(board.Undo()&&ReferenceEquals(board.Level,source)&&board.Current.queues.Length==3,"topology undone");
            board.ApplyProp(candidate,state);board.Reset();Check(ReferenceEquals(board.Level,source)&&!board.CanUndo&&board.Moves==0,"reset original topology");
            var mono=Fixture();foreach(var group in mono.groups)group.color=0;var same=new Board(mono);Check(!PropActions.Shuffle(same,0,new System.Random(1)),"completed unchanged");
            var blocked=Fixture();blocked.nodes[0].sticky=true;Check(!PropActions.Shuffle(new Board(blocked),0,new System.Random(1)),"sticky preserved");blocked.nodes[0].sticky=false;blocked.nodes[0].unlockAfter=1;Check(!PropActions.Shuffle(new Board(blocked),0,new System.Random(1)),"frozen preserved");
            Directory.CreateDirectory("artifacts/settings-props");File.WriteAllText("artifacts/settings-props/core.json","{\"passed\":true,\"assertions\":"+assertions+"}");Debug.Log("PROPS CORE PASSED "+assertions);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
