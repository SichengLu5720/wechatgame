using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using StairsCrowd.Core;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        static readonly Color NightBackground=GameplayPresentation.Background;
        Camera nightBackdrop;public FixedTowerBackdrop TowerBackground {get;private set;}
        public int PropSelection {get;private set;}
        public bool FindingPropTargets {get;private set;}
        public readonly HashSet<int> PropTargets=new HashSet<int>();
        readonly Dictionary<int,LevelSpec> propLevels=new Dictionary<int,LevelSpec>();
        readonly Dictionary<int,WalkSpace> propSpaces=new Dictionary<int,WalkSpace>();
        readonly HashSet<int> searchedPropTargets=new HashSet<int>();
        LevelSpec propCacheLevel;
        public int PropCandidateSearches {get;private set;}
        public double PropCandidateMilliseconds {get;private set;}
        readonly Dictionary<int,GUIStyle> nightStyles=new Dictionary<int,GUIStyle>();
        int selectionEpoch;
        GUIStyle NightStyle(int size,FontStyle weight=FontStyle.Normal,TextAnchor anchor=TextAnchor.MiddleLeft){int key=(size*4+(int)weight)*16+(int)anchor;GUIStyle s;if(!nightStyles.TryGetValue(key,out s)){s=Style(size,weight,anchor);s.normal.textColor=UiPaper;nightStyles[key]=s;}return s;}
        public bool BeginPropSelection(int kind)
        {
            if(kind==2&&!PlatformPropEnabled)return false;
            if(kind==PropSelection){CancelPropSelection();return true;}
            if(!PropReady()||(kind!=1&&kind!=2)||(kind==1?ShuffleRemaining:PlatformRemaining)==0)return false;
            if(!DailyInput(true))return false;
            CancelPropSelection();CancelViewPointer();selected=-1;PropTarget=-1;scene.Highlight(board,-1);
            PropSelection=kind;FindingPropTargets=true;SetNightFocus();StartCoroutine(FindPropTargets(selectionEpoch));return true;
        }
        IEnumerator FindPropTargets(int epoch)
        {
            var readyTargets=new HashSet<int>();
            for(int n=0;n<board.Level.nodes.Length;n++){
                if(epoch!=selectionEpoch)yield break;
                if(PropSelection==1){if(PropActions.CanShuffle(board,n))readyTargets.Add(n);}
                else if(!Rules.Gated(board.Level,board.Current,n)&&!Rules.Complete(board.Level,board.Current,n)){
                    LevelSpec level;WalkSpace geometry;
                    if(CachedOrNewCandidate(n,out level,out geometry))readyTargets.Add(n);
                }
                message="正在检查可选平台…";yield return null;
            }
            if(epoch!=selectionEpoch)yield break;PropTargets.UnionWith(readyTargets);FindingPropTargets=false;SetNightFocus();
            message=PropTargets.Count==0?"当前没有可用平台，取消不会消耗道具":"点击白色描边的平台；再次点击道具可取消";
        }
        void SetNightFocus(){var c=PropSelection==0?NightBackground:new Color(.007f,.012f,.02f);view.backgroundColor=c;if(nightBackdrop)nightBackdrop.backgroundColor=c;if(TowerBackground)TowerBackground.SetFocus(PropSelection!=0);scene.SetPropFocus(PropSelection==0?null:PropTargets,board.Current,PropSelection==1);}
        public void CancelPropSelection()
        {
            selectionEpoch++;PropSelection=0;FindingPropTargets=false;PropTargets.Clear();
            if(propSpaces.Count>24)ClearPropCandidateCache();
            if(scene!=null&&view&&board!=null)SetNightFocus();message="";
        }
        bool CachedOrNewCandidate(int target,out LevelSpec level,out WalkSpace geometry)
        {
            if(!ReferenceEquals(propCacheLevel,board.Level)){ClearPropCandidateCache();propCacheLevel=board.Level;}
            level=null;geometry=null;
            if(searchedPropTargets.Contains(target))return propLevels.TryGetValue(target,out level)&&propSpaces.TryGetValue(target,out geometry);
            var timer=System.Diagnostics.Stopwatch.StartNew();PropCandidateSearches++;
            bool found=TryPlatformCandidate(board,target,propRandom,out level,out geometry);
            PropCandidateMilliseconds+=timer.Elapsed.TotalMilliseconds;searchedPropTargets.Add(target);
            if(found){propLevels[target]=level;propSpaces[target]=geometry;}return found;
        }
        void ClearPropCandidateCache(){propCacheLevel=null;searchedPropTargets.Clear();propLevels.Clear();propSpaces.Clear();}
        public bool ChoosePropTarget(int node)
        {
            if(PropSelection==0||FindingPropTargets||!PropTargets.Contains(node))return false;
            int kind=PropSelection;PropTarget=node;selected=-1;
            bool success=kind==1?UseShuffleProp():UsePlatformProp();
            if(success){CancelPropSelection();message=kind==1?"已洗混所选平台":"中转平台正在升起";}
            return success;
        }
    }
    public sealed partial class CrowdScene
    {
        GameObject[] propRims;
        readonly MaterialPropertyBlock focusBlock=new MaterialPropertyBlock();
        readonly Dictionary<Transform,Renderer[]> focusRenderers=new Dictionary<Transform,Renderer[]>();
        int focusPeopleCount=-1;
        void FocusRenderers(Transform parent,float value)
        {
            Renderer[] renderers;if(!focusRenderers.TryGetValue(parent,out renderers)){renderers=parent.GetComponentsInChildren<Renderer>(true);focusRenderers[parent]=renderers;}
            foreach(var renderer in renderers){
                renderer.GetPropertyBlock(focusBlock);focusBlock.SetFloat("_PropDim",value);renderer.SetPropertyBlock(focusBlock);
            }
        }
        public void SetPropFocus(HashSet<int> targets,State state,bool illuminateTransit=false)
        {
            bool focus=targets!=null;
            if(propRims==null){
                propRims=new GameObject[nodeRoots.Length];
                for(int n=0;n<nodeRoots.Length;n++){propRims[n]=MarkerRoot(n,"Prop target rim");activeParent=propRims[n].transform;Ring(n,space.Centers[n]+Vector3.up*.07f,space.Half(n)-.05f,.085f,selectionInk);BatchMarker(propRims[n]);}
                activeParent=null;
            }
            int count=people==null?0:people.Length;if(focusPeopleCount!=count){focusPeopleCount=count;focusRenderers.Clear();}
            FocusRenderers(root.transform,focus?.12f:1f);
            for(int n=0;n<nodeRoots.Length;n++){
                bool eligible=focus&&targets.Contains(n);propRims[n].SetActive(eligible);
                if(eligible||(focus&&illuminateTransit&&space.Level.nodes[n].transit&&!Rules.Gated(space.Level,state,n)&&!IsRetiring(n))){FocusRenderers(nodeRoots[n],1);foreach(int group in state.queues[n])for(int m=0;m<4;m++)if(group*4+m<people.Length)FocusRenderers(people[group*4+m].root,1);}
            }
        }
        int addingNode=-1,addingEdge=-1;float additionClock;const float AdditionDuration=1.4f;
        public bool HasAddition {get{return addingNode>=0;}}
        Vector3 additionCameraStart,additionCameraEnd;Quaternion additionRotationStart,additionRotationEnd;float additionSizeStart,additionSizeEnd;
        public void SettleRetired(State state){for(int n=0;n<retreat.Length;n++)if(Rules.Complete(space.Level,state,n))StartRetreat(n);if(Rules.AllGathered(space.Level,state)&&Rules.Completed(space.Level,state)==space.Level.GoalCount)StartFinalRetreat();for(int n=0;n<retreat.Length;n++)if(retreat[n]>=0)retreat[n]=RetreatDuration;for(int e=0;e<linkRetreat.Length;e++)if(linkRetreat[e]>=0)linkRetreat[e]=RetreatDuration;TickWorld(state,0);}
        public void StartAddition(int node,int edge,Vector3 oldPosition,Quaternion oldRotation,float oldSize){additionCameraStart=oldPosition;additionRotationStart=oldRotation;additionSizeStart=oldSize;additionCameraEnd=camera.transform.position;additionRotationEnd=camera.transform.rotation;additionSizeEnd=camera.orthographicSize;addingNode=node;addingEdge=edge;additionClock=0;TickAddition(0);}
        void TickAddition(float delta)
        {
            if(addingNode<0)return;additionClock=Mathf.Min(AdditionDuration,additionClock+Mathf.Max(0,delta));
            float cameraEase=Mathf.SmoothStep(0,1,additionClock/AdditionDuration);camera.transform.position=Vector3.Lerp(additionCameraStart,additionCameraEnd,cameraEase);camera.transform.rotation=Quaternion.Slerp(additionRotationStart,additionRotationEnd,cameraEase);camera.orthographicSize=Mathf.Lerp(additionSizeStart,additionSizeEnd,cameraEase);FaceClouds();
            float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(additionClock/1.1f));
            nodeRoots[addingNode].position=nodeOrigins[addingNode]+Vector3.down*(9*(1-u));
            Transform link;if(stairRoots.TryGetValue(addingEdge,out link)){
                float v=Mathf.SmoothStep(0,1,Mathf.Clamp01((additionClock-.25f)/1.15f));
                link.position=linkOrigins[addingEdge]+Vector3.down*(5*(1-v));link.localScale=new Vector3(1,Mathf.Max(.001f,v),1);
            }
            if(additionClock>=AdditionDuration){nodeRoots[addingNode].position=nodeOrigins[addingNode];addingNode=-1;addingEdge=-1;}
        }
    }
}
