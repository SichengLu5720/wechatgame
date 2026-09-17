using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Runtime;

// Model-independent fixtures only. Never imports or enables production art.
public static class CharacterWalkCodeVerification
{
    public static void Run()
    {
        try
        {
            Channels();Timing();Lifecycle();
            var rejected=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            Require(!rejected||!rejected.runtimeEnabled,"Rejected model must remain disabled");
            Require(!CharacterWalkTiming.Enabled,"Fallback must retain original timing");
            Directory.CreateDirectory("artifacts");
            File.WriteAllText("artifacts/task003-code-only-verification.json","{\"passed\":true,\"channels\":true,\"uniformTiming\":true,\"phaseContinuity\":true,\"reset\":true,\"zeroDelta\":true,\"zeroSteadyAllocation\":true,\"rejectedArtDisabled\":true,\"productionArtUsed\":false}");
            Debug.Log("CHARACTER WALK CODE ONLY VERIFICATION PASSED");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Channels()
    {
        var curve=new CharacterWalkAsset.Channel{path="translation",times=new[]{0f,.5f,1f},values=new[]{Vector4.zero,Vector4.one,Vector4.zero}};
        Require(Vector4.Distance(curve.Sample(.25f),Vector4.one*.5f)<.00001f,"Linear sample");
        Require(curve.Sample(-1)==Vector4.zero&&curve.Sample(2)==Vector4.zero,"Boundary sample");
        curve.step=true;Require(curve.Sample(.25f)==Vector4.zero,"Step sample");Require(curve.Sample(.5f)==Vector4.one,"Step exact key sample");
        curve.step=false;curve.path="rotation";var q=Quaternion.Euler(20,0,0);curve.values=new[]{new Vector4(0,0,0,1),new Vector4(q.x,q.y,q.z,q.w),new Vector4(0,0,0,1)};
        var value=curve.Sample(.25f);Require(Quaternion.Angle(new Quaternion(value.x,value.y,value.z,value.w),Quaternion.Euler(10,0,0))<.01f,"Quaternion sample");
    }
    static void Timing()
    {
        var plan=new MotionPlan{duration=1};
        plan.tracks.Add(new ActorTrack{start=.1f,duration=.9f,points=new[]{Vector3.zero,Vector3.right*2,Vector3.right*2+Vector3.forward*3},times=new[]{0f,.3f,.9f}});
        var original=new Vector3[101];for(int i=0;i<=100;i++)original[i]=plan.tracks[0].Position(i/100f);
        CharacterWalkTiming.Apply(plan);
        for(int i=0;i<=100;i++)Require(Vector3.Distance(original[i],plan.tracks[0].Position(plan.duration*i/100))<.00001f,"Timing changes path");
        Require(plan.tracks[0].RemainingDistance(plan.duration)==0,"Arrival remainder");
        Require(Mathf.Abs(plan.tracks[0].RemainingDistance(0)-5)<.00001f,"Path remainder");
    }
    static void Lifecycle()
    {
        // Empty render mesh, numeric test bones: not a character or art candidate.
        var asset=ScriptableObject.CreateInstance<CharacterWalkAsset>();asset.mesh=new Mesh();asset.bones=new CharacterWalkAsset.Bone[14];asset.joints=new int[14];
        for(int i=0;i<14;i++){asset.bones[i]=new CharacterWalkAsset.Bone{name="fixture-"+i,parent=i==0?-1:0,position=Vector3.zero,rotation=Quaternion.identity,scale=Vector3.one};asset.joints[i]=i;}
        asset.channels=new[]{new CharacterWalkAsset.Channel{bone=1,path="translation",times=new[]{0f,1f},values=new[]{Vector4.zero,Vector4.zero}}};asset.leftFoot=10;asset.rightFoot=13;
        var person=new PersonView{root=new GameObject("Numeric fixture").transform};person.visual=new GameObject("Fixture visual").transform;person.visual.SetParent(person.root,false);
        var model=new GameObject("Empty renderer fixture").transform;model.SetParent(person.visual,false);
        person.walk=new CharacterWalkDriver(person,model,asset,null,null);
        person.walk.Renderer.enabled=false;
        for(int i=0;i<100;i++){person.root.position+=Vector3.forward*.00625f;person.TickWalk(.01f);}
        Require(Mathf.Abs(person.walk.Phase-.5f)<.0001f,"Displacement phase");
        float phase=person.walk.Phase;person.TickWalk(0);Require(person.walk.Phase==phase,"Zero delta changed state");
        for(int i=0;i<30;i++)person.TickWalk(.01f);Require(person.PresentationSettled,"Stop does not settle");
        person.root.position+=Vector3.forward*.125f;person.TickWalk(.1f);Require(Mathf.Abs(person.walk.Phase-.6f)<.0001f,"Continuation restarted phase");
        for(int i=0;i<30;i++){person.root.position+=Vector3.forward*.01f;person.TickWalk(.01f);}
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<500;i++){person.root.position+=Vector3.forward*.01f;person.TickWalk(.01f);}Require(GC.GetAllocatedBytesForCurrentThread()-before==0,"Steady allocation");
        person.walk.Reset(null);Require(person.walk.Phase==0&&person.walk.Blend==0&&person.PresentationSettled,"Reset incomplete");
        StairContinuity(person);
        UnityEngine.Object.DestroyImmediate(person.root.gameObject);UnityEngine.Object.DestroyImmediate(asset.mesh);UnityEngine.Object.DestroyImmediate(asset);
    }
    static void StairContinuity(PersonView person)
    {
        var level=new StairsCrowd.Core.LevelSpec
        {
            nodes=new[]{new StairsCrowd.Core.NodeSpec{capacity=4,queue=new[]{0}},new StairsCrowd.Core.NodeSpec{x=8,y=2,capacity=4,transit=true,queue=new int[0]}},
            edges=new[]{new StairsCrowd.Core.EdgeSpec{a=0,b=1}},groups=new[]{new StairsCrowd.Core.GroupSpec{id=0,color=0}}
        };
        var space=new WalkSpace(level,null,true);var state=StairsCrowd.Core.Rules.Initial(level);
        var plan=FastMovement.Build(space,state,StairsCrowd.Core.Rules.Preview(level,state,0,1));CharacterWalkTiming.Apply(plan);
        var track=plan.Track(0);person.root.position=plan.initial[0];person.walk.Reset(space);
        float previous=person.walk.Renderer.transform.position.y,maximum=0;
        for(float t=1f/60;t<=track.End+1f/60;t+=1f/60)
        {
            person.root.position=track.PlaybackPosition(t);person.TickWalk(1f/60);
            float y=person.walk.Renderer.transform.position.y;maximum=Mathf.Max(maximum,Mathf.Abs(y-previous));previous=y;
        }
        Require(maximum<.15f,"Stair envelope jump leaked to visual root");
        for(int i=0;i<60;i++)person.TickWalk(1f/60);Require(person.PresentationSettled,"Stair settle");
        person.walk.Reset(null);Require(person.PresentationSettled,"Clear old ground state");
        Debug.Log("CHARACTER WALK NUMERIC STAIR MAX FRAME RISE "+maximum);
    }
    static void Require(bool value,string message){if(!value)throw new Exception("TASK-003 code: "+message);}
}
