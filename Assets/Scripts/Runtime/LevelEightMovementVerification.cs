using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    // Actual down/up dispatch plus an opt-in diagnostic failure reproduction.
    public sealed class LevelEightMovementVerification:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed,baseline,expectedArchitectureException,leakedVerificationGate;
            public int pointerMoves,checks,canceledProbes;public float normalClockAdvance,faultClockAdvance;
        }
        StairsGame game;string output;Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-level8movementtest")>=0)new GameObject("Level 8 pointer verification").AddComponent<LevelEightMovementVerification>();}
        void Require(bool valid,string message){report.checks++;if(!valid)throw new Exception("Level 8 pointer: "+message);}
        void Observe(string message,string stack,LogType type){if(type==LogType.Exception&&message.Contains("Platform architecture missing"))report.expectedArchitectureException=true;}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationOutput");output=at>=0?args[at+1]:"level8-verification";Directory.CreateDirectory(output);report.baseline=Array.IndexOf(args,"-level8baseline")>=0;Application.logMessageReceived+=Observe;
            while(!(game=FindFirstObjectByType<StairsGame>())||game.board==null)yield return null;
            var run=Run();var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(run);bool failed=false;
            while(stack.Count>0){bool more;object next;try{more=stack.Peek().MoveNext();next=more?stack.Peek().Current:null;}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());failed=true;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}
            report.passed=!failed;File.WriteAllText(Path.Combine(output,"pointer.json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=Observe;Application.Quit(failed?1:0);
        }
        IEnumerator Ready()
        {
            for(int i=0;game.IsAssembling&&i<1200;i++){game.Advance(.05f);yield return null;}Require(!game.IsAssembling,"assembly did not finish");while(game.IntroVisible)game.ContinueIntro();yield return null;yield return null;Physics.SyncTransforms();
        }
        Vector2 Point(int node)
        {
            Physics.SyncTransforms();
            for(int radius=0;radius<=5;radius++)for(int x=-radius;x<=radius;x++)for(int z=-radius;z<=radius;z++){
                Vector2 p=game.view.WorldToScreenPoint(game.space.Centers[node]+new Vector3(x*.4f,.05f,z*.4f));
                if(!GameplayLayout.Current.PlayPixels.Contains(p)||game.TuningContains(p))continue;
                int person,platform;bool hp=game.TryPickPerson(p,out person),hg=game.TryPickNode(p,out platform);
                int picked=game.selected>=0&&hg&&(!hp||person!=platform)?platform:hp?person:hg?platform:-1;
                if(picked==node)return p;
            }
            throw new Exception("No visible pointer target for node "+node);
        }
        void Tap(int node){var point=Point(node);game.BeginViewPointer(point);game.EndViewPointer(point);}
        IEnumerator Move(bool fault)
        {
            var step=game.board.Level.solution[0];Tap(step.a);Require(game.selected==step.a,"source selection");Tap(step.b);Require(game.board.Moves==1&&game.motion!=null,"target click did not commit: "+game.message);report.pointerMoves++;
            float clock=game.clock;var initial=game.scene.people.Select(p=>p.root.position).ToArray();
            for(int i=0;i<30;i++)yield return null;
            float advance=game.clock-clock;if(fault)report.faultClockAdvance=advance;else report.normalClockAdvance=advance;
            bool moved=game.scene.people.Where((p,i)=>Vector3.Distance(p.root.position,initial[i])>.01f).Any();
            if(fault&&report.baseline){Require(advance==0&&!moved,"expected stale probe gate was not reproduced");}
            else Require(advance>0&&moved,"committed crowd is frozen");
        }
        IEnumerator Run()
        {
            foreach(float scale in new[]{.75f,1f,1.25f}){
                RuntimeTuning.SetLinked(false);RuntimeTuning.ChangeScale(false,scale);RuntimeTuning.ChangeScale(true,scale);game.TuningOpen=false;
                game.LoadLevel(7);yield return Ready();RefinedRuntimeVerification.Check(game);Require(!HiddenArtVerification.Active&&!game.InterfaceBlocksInput,"unexpected input/verification gate");
                var bounds=game.scene.NodeRoot(0).Find("Platform 0").GetComponent<Collider>().bounds;
                game.scene.ApplyRuntimeTuning();Physics.SyncTransforms();Require(bounds==game.scene.NodeRoot(0).Find("Platform 0").GetComponent<Collider>().bounds,"visual tuning changed gameplay collider");
                yield return Move(false);game.Undo();yield return Ready();Require(game.board.Moves==0&&game.motion==null,"undo");
            }
            if(!report.baseline)foreach(bool destroy in new[]{false,true}){
                game.LoadLevel(7);yield return Ready();
                var canceled=game.gameObject.AddComponent<HiddenArtVerification>();yield return null;
                Require(HiddenArtVerification.Active,"cancel fixture never acquired playback");
                if(destroy)Destroy(canceled);else canceled.enabled=false;
                yield return null;Require(!HiddenArtVerification.Active,"cancel retained playback control");
                game.LoadLevel(6);yield return Ready();
                for(int i=0;i<3;i++)yield return null;
                Require(game.levelIndex==6&&!HiddenArtVerification.Active,"canceled probe continued loading/advancing");
                if(!destroy)Destroy(canceled);report.canceledProbes++;
            }
            game.LoadLevel(7);yield return Ready();
            // Mimic the exact stale architecture lookup failure after the probe
            // takes control. The rename is process-local and restored immediately.
            var node=game.scene.NodeRoot(0);var batch=node.Find("Architecture batch")??node.Find("Platform tuning visual/Architecture batch");Require(batch!=null,"actual architecture missing");
            string name=batch.name;batch.name="Expected verification fault";var probe=game.gameObject.AddComponent<HiddenArtVerification>();
            for(int i=0;i<30&&!report.expectedArchitectureException;i++)yield return null;
            batch.name=name;Require(report.expectedArchitectureException,"expected diagnostic fault did not execute");
            report.leakedVerificationGate=HiddenArtVerification.Active;
            Require(report.leakedVerificationGate==report.baseline,"verification control was not released");
            yield return Move(true);Destroy(probe);
        }
    }
}
