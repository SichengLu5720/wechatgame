using System;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        int preparedMasks;
        public int ReusedPeople {get;private set;}
        public int PreparedPeople=>preparedPeople;
        public bool PresentationPrepared=>preparedPeople==space.Level.groups.Length*Rules.MembersPerGroup&&preparedMasks==preparedPeople;

        // Only the active level's actors survive a transfer. Architecture owns no actor assets.
        public void TakePeopleFrom(CrowdScene previous)
        {
            if(previous==null||!refined||previous.refined!=refined||previous.pool==null)return;
            pool=new PersonView[space.Level.groups.Length*Rules.MembersPerGroup];
            int count=Math.Min(pool.Length,previous.preparedPeople);
            for(int i=0;i<count;i++){
                var p=previous.pool[i];p.SetSelected(false);p.root.gameObject.SetActive(false);
                p.root.SetParent(root.transform,false);p.presentationLift=BridgeLift;
                p.walkGround=space;p.remainingDistance=float.PositiveInfinity;p.waterBird.Reset();
                p.selectionRing.GetComponent<MeshFilter>().sharedMesh=selectionMesh;
                p.selectionRing.GetComponent<Renderer>().sharedMaterial=selectionInk;
                foreach(var r in p.root.GetComponentsInChildren<Renderer>(true))r.SetPropertyBlock(null);
                foreach(var r in p.colored)r.sharedMaterial=mystery;
                p.ResetReveal();p.visual.localScale=Vector3.one;p.visual.localPosition=Vector3.zero;
                pool[i]=p;previous.pool[i]=null;
            }
            preparedPeople=count;ReusedPeople=count;
            previous.people=Array.Empty<PersonView>();
        }

        public void PreparePresentation(State state,int count)
        {
            PreparePeople(count);
            for(int end=Math.Min(preparedPeople,preparedMasks+count);preparedMasks<end;preparedMasks++){
                var p=pool[preparedMasks];
                if(!state.revealed[preparedMasks/Rules.MembersPerGroup]&&!p.hiddenCharacter){
                    var prefab=Resources.Load<GameObject>("HiddenCharacter/HiddenCharacter");
                    if(!prefab)throw new InvalidOperationException("Missing hidden character prefab");
                    p.hiddenCharacter=UnityEngine.Object.Instantiate(prefab,p.visual,false);
                    p.hiddenCharacter.SetActive(false);
                }
            }
        }
    }

    public sealed partial class StairsGame
    {
        CrowdScene preparedScene;
        int preparedIndex=-1;
        public bool NextSceneReady=>preparedScene!=null&&preparedScene.ConstructionComplete;
        public int ResidentSceneCount=>(scene==null?0:1)+(preparedScene==null?0:1);
        public double MaximumPreparationSliceMs {get;private set;}
        public bool UsedPreparedScene {get;private set;}

        void DiscardPreparedScene()
        {
            if(preparedScene!=null)preparedScene.Dispose();
            preparedScene=null;preparedIndex=-1;
        }
        void PrepareNextScene()
        {
            // Work only while input/movement/intro/assembly is idle. Never build from a move request.
            if(DailyActive||Home||editing||customPlaying||IsAssembling||IntroVisible||motion!=null||PreparingMove||PropSelection!=0||TutorialExiting)return;
            int index=levelIndex+1;if(index>=catalog.levels.Length)return;
            if(preparedIndex==index&&NextSceneReady&&preparedScene.space.Level==catalog.levels[index])return;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            if(preparedScene==null){
                var level=catalog.levels[index];
                var nav=Resources.Load<TextAsset>(CampaignRepository.NavigationPath(level,index));
                var nextSpace=new WalkSpace(level,nav?nav.bytes:null,CampaignRepository.GeometryOnly(level));
                preparedScene=new CrowdScene(nextSpace,view,true);preparedIndex=index;
            }else if(preparedIndex!=index||preparedScene.space.Level!=catalog.levels[index]){DiscardPreparedScene();return;}
            // Each step builds one platform/link or batches one piece; budget is soft, not preemptive.
            while(!preparedScene.ConstructionComplete&&timer.Elapsed.TotalMilliseconds<2)preparedScene.BuildNextPiece();
            MaximumPreparationSliceMs=Math.Max(MaximumPreparationSliceMs,timer.Elapsed.TotalMilliseconds);
        }
        CrowdScene ConsumePreparedScene(int index)
        {
            UsedPreparedScene=false;
            if(preparedIndex!=index||preparedScene==null||!preparedScene.ConstructionComplete||preparedScene.space.Level!=catalog.levels[index]){DiscardPreparedScene();return null;}
            var result=preparedScene;preparedScene=null;preparedIndex=-1;UsedPreparedScene=true;return result;
        }
    }
}
