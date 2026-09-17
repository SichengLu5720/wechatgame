using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using StairsCrowd.Core;
namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        public const bool PlatformPropEnabled=false;
        public int UndoRemaining {get;private set;}=1;
        public int ShuffleRemaining {get;private set;}=1;
        public int PlatformRemaining {get;private set;}
        public int PropTarget {get;private set;}=-1;
        readonly System.Random propRandom=new System.Random();
        void ResetProps(){CancelPropSelection();ClearPropCandidateCache();UndoRemaining=ShuffleRemaining=1;PlatformRemaining=0;PropTarget=-1;settingsOpen=false;settingsConfirm=0;}
        bool PropReady(){return board!=null&&!FailureLocked&&!Home&&!editing&&!IntroVisible&&!IsAssembling&&!Busy&&!board.Solved&&!settingsOpen&&DailyInput();}
        void RememberPropTarget(int node){if(node>=0&&node<board.Level.nodes.Length&&!Rules.Complete(board.Level,board.Current,node))PropTarget=node;}
        public bool UseUndoProp(){if(FailureLocked||PropSelection!=0||Busy||settingsOpen||Home||editing||IsAssembling||IntroVisible||UndoRemaining==0||!board.CanUndo||!DailyInput(true))return false;Undo();UndoRemaining--;if(islandAudio)islandAudio.Play();return true;}
        public bool UseShuffleProp(){if(!PropReady()||ShuffleRemaining==0)return false;int node=selected>=0?selected:PropTarget;if(!PropActions.CanShuffle(board,node)){message="请选择至少有两种颜色的平台";return false;}if(!DailyInput(true)||!PropActions.Shuffle(board,node,propRandom))return false;ShuffleRemaining--;RefreshPropWorld();EvaluateCurrentBoard();message=FailureLocked?"":"已洗混所选平台";if(islandAudio)islandAudio.Play();return true;}
        public bool UsePlatformProp()
        {
            if(!PlatformPropEnabled||!PropReady()||PlatformRemaining==0)return false;int target=selected>=0?selected:PropTarget;
            if(target<0||target>=board.Level.nodes.Length||Rules.Gated(board.Level,board.Current,target)||Rules.Complete(board.Level,board.Current,target)){message="请先选择一个可用平台";return false;}
            WalkSpace candidateSpace;LevelSpec candidate;
            if(!CachedOrNewCandidate(target,out candidate,out candidateSpace)){message="附近没有可安全连接的位置";return false;}
            var next=board.Current.Clone();Array.Resize(ref next.queues,candidate.nodes.Length);next.queues[next.queues.Length-1]=new List<int>();
            var oldPosition=view.transform.position;var oldRotation=view.transform.rotation;float oldSize=view.orthographicSize;
            board.ApplyProp(candidate,next);PlatformRemaining--;RefreshPropWorld(candidateSpace);EvaluateCurrentBoard();scene.SettleRetired(board.Current);scene.StartAddition(candidate.nodes.Length-1,candidate.edges.Length-1,oldPosition,oldRotation,oldSize);message=FailureLocked?"":"中转平台已加入";if(islandAudio)islandAudio.Play();return true;
        }
        public static bool TryPlatformCandidate(Board source,int target,System.Random random,out LevelSpec result,out WalkSpace resultSpace)
        {
            result=null;resultSpace=null;if(target<0||target>=source.Level.nodes.Length)return false;
            var directions=Enumerable.Range(0,8).ToArray();for(int i=7;i>0;i--){int j=random.Next(i+1),v=directions[i];directions[i]=directions[j];directions[j]=v;}
            var anchor=source.Level.nodes[target];
            foreach(int direction in directions){float angle=direction*Mathf.PI/4;var point=WalkSpace.WorldCenter(anchor)+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*9;
                if(source.Level.nodes.Any(n=>(WalkSpace.WorldCenter(n)-point).sqrMagnitude<49))continue;
                var copy=LevelShare.Copy(source.Level);int count=copy.nodes.Length;Array.Resize(ref copy.nodes,count+1);copy.nodes[count]=new NodeSpec{x=point.x/1.4f,y=1f+(float)random.NextDouble()*2.6f,z=-point.z/1.96f,capacity=4,transit=true,queue=new int[0]};
                Array.Resize(ref copy.edges,copy.edges.Length+1);copy.edges[copy.edges.Length-1]=new EdgeSpec{a=target,b=count};copy.solution=new EdgeSpec[0];
                try{copy.Validate();var candidate=new WalkSpace(copy);LayoutSafety.Validate(candidate);result=copy;resultSpace=candidate;return true;}catch(Exception){/* Bounded search; failures never mutate source state. */}
            }return false;
        }
        void RefreshPropWorld(WalkSpace prepared=null)
        {
            CancelPending();if(space.Level!=board.Level){if(scene!=null)scene.Dispose();space=prepared??new WalkSpace(board.Level);scene=new CrowdScene(space,view);scene.HomeFraming=false;SetViewport();scene.FitCamera();}
            SyncCompletionFeedback(true);Rebuild();WarmMoves();PropTarget=-1;
        }
    }
}
