using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class FailureProgressVerification
{
    static NodeSpec Node(bool sticky=false,params int[] queue){return new NodeSpec{capacity=4,sticky=sticky,queue=queue};}
    static GroupSpec[] Groups(params int[] colors){return colors.Select((color,id)=>new GroupSpec{id=id,color=color}).ToArray();}
    static LevelSpec MovableLevel()
    {
        return new LevelSpec{name="one-step",tip="",nodes=new[]{Node(false,0,1,2),Node(false,3)},edges=new[]{new EdgeSpec{a=0,b=1}},groups=Groups(0,0,0,0),solution=new[]{new EdgeSpec{a=1,b=0}}};
    }
    static LevelSpec NoMoveLevel()
    {
        return new LevelSpec{name="no-move",tip="",nodes=new[]{Node(true,0,1,2,3)},edges=new EdgeSpec[0],groups=Groups(0,0,1,1),solution=new EdgeSpec[0]};
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static HashSet<string> LegalResultKeys(LevelSpec level,State state)
    {
        var result=new HashSet<string>();for(int a=0;a<level.nodes.Length;a++)for(int b=0;b<level.nodes.Length;b++){
            var move=Rules.Preview(level,state,a,b);if(move.ok)result.Add(Rules.Key(level,Rules.Apply(state,move)));
        }return result;
    }
    static void VerifyFailureRules()
    {
        var noMove=NoMoveLevel();noMove.Validate();var noMoveState=Rules.Initial(noMove);var noMoveSeen=new HashSet<string>{Rules.Key(noMove,noMoveState)};
        string beforeKey=Rules.Key(noMove,noMoveState);int beforeSeen=noMoveSeen.Count;
        Require(AttemptFailure.Classify(noMove,noMoveState,noMoveSeen)==AttemptOutcome.NoLegalMoves,"No-legal-move state was not classified as failure");
        Require(Rules.Key(noMove,noMoveState)==beforeKey&&noMoveSeen.Count==beforeSeen,"Failure preview mutated state or history");

        var movable=MovableLevel();movable.Validate();var state=Rules.Initial(movable);var seen=new HashSet<string>{Rules.Key(movable,state)};
        Require(AttemptFailure.Classify(movable,state,seen)==AttemptOutcome.Continue,"Unseen legal result did not continue");
        foreach(string key in LegalResultKeys(movable,state))seen.Add(key);
        Require(AttemptFailure.Classify(movable,state,seen)==AttemptOutcome.OnlyRepeatedMoves,"All-seen legal results were not classified as repeated failure");

        var solved=Rules.Apply(state,Rules.Preview(movable,state,1,0));
        Require(AttemptFailure.Classify(movable,solved,seen)==AttemptOutcome.Solved,"Solved state did not take priority over failure");
        Require(!typeof(AttemptFailure).GetMethods().Any(method=>method.Name.IndexOf("Solve",StringComparison.OrdinalIgnoreCase)>=0),"Runtime classifier exposes solver behavior");
    }
    static void VerifyProgress()
    {
        Require(CampaignProgress.Normalize(-4,20)==0&&CampaignProgress.Normalize(99,20)==19&&CampaignProgress.Normalize(6,20)==6,"Progress normalization failed");
        Require(CampaignProgress.NextAfterCompletion(5,4,20)==5&&CampaignProgress.NextAfterCompletion(5,5,20)==6&&CampaignProgress.NextAfterCompletion(19,19,20)==19,"Progress monotonicity failed");
        Require(!CampaignProgress.IsBuiltInCompletion(true,2,20)&&!CampaignProgress.IsBuiltInCompletion(false,20,20)&&CampaignProgress.IsBuiltInCompletion(false,2,20),"Custom/built-in isolation failed");

        bool existed=PlayerPrefs.HasKey(CampaignProgress.Key);int original=existed?PlayerPrefs.GetInt(CampaignProgress.Key):0;
        try{
            PlayerPrefs.DeleteKey(CampaignProgress.Key);PlayerPrefs.Save();Require(CampaignProgress.CurrentIndex(20)==0,"Missing save did not start at level 1");
            PlayerPrefs.SetInt(CampaignProgress.Key,-7);Require(CampaignProgress.CurrentIndex(20)==0&&PlayerPrefs.GetInt(CampaignProgress.Key)==0,"Negative save was not repaired");
            PlayerPrefs.SetInt(CampaignProgress.Key,999);Require(CampaignProgress.CurrentIndex(20)==19&&PlayerPrefs.GetInt(CampaignProgress.Key)==19,"High save was not repaired");
            PlayerPrefs.SetInt(CampaignProgress.Key,5);Require(CampaignProgress.RecordCompletion(4,20)==5&&PlayerPrefs.GetInt(CampaignProgress.Key)==5,"Old-level replay changed progress");
            Require(CampaignProgress.RecordCompletion(6,20)==5&&PlayerPrefs.GetInt(CampaignProgress.Key)==5,"Future-level completion skipped progress");
            Require(CampaignProgress.RecordCompletion(5,20)==6&&PlayerPrefs.GetInt(CampaignProgress.Key)==6,"Current-level completion did not advance exactly once");
            Require(CampaignProgress.RecordCompletion(5,20)==6&&PlayerPrefs.GetInt(CampaignProgress.Key)==6,"Repeated completion advanced twice");
            PlayerPrefs.SetInt(CampaignProgress.Key,19);Require(CampaignProgress.RecordCompletion(19,20)==19&&PlayerPrefs.GetInt(CampaignProgress.Key)==19,"Last level did not saturate");
        }finally{
            if(existed)PlayerPrefs.SetInt(CampaignProgress.Key,original);else PlayerPrefs.DeleteKey(CampaignProgress.Key);PlayerPrefs.Save();
        }
    }
    public static void Run()
    {
        VerifyFailureRules();VerifyProgress();
        Debug.Log("FAILURE PROGRESS VERIFICATION PASSED oneStep=true historyPure=true solvedPriority=true progress=true saveRestored=true");
    }
}
