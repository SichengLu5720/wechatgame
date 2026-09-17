using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    // Opt-in local player verification, with all preference contents restored.
    public sealed class RuntimeTuningVerification:MonoBehaviour
    {
        StairsGame game;int checks,phaseSamples;string output,original;bool hadKey;long allocated,tuningAllocated,uiAllocated,highSpeedAllocated;double movingMs,highSpeedMs;float maxPhaseError;int uiFrames;
        [Serializable] sealed class Report {public bool passed,development,wechatDeviceTested;public int checks,phaseSamples,panelEvents,upSamples,downSamples,turnSamples;public long movingAllocatedBytes,parameterChangeAllocatedBytes,panelAllocatedBytes,dragAllocatedBytes,highSpeedAllocatedBytes;public float maximumPhaseError,maximumArcPhaseError;public double advanceMs;public Performance[] highSpeedPerformance;}
        [Serializable] sealed class Performance {public float realFrameDelta;public int samples;public long allocatedBytes;public double meanMs,p95Ms,maxMs;}
        Performance[] performance;int upSamples,downSamples,turnSamples;float maxArcPhaseError;long dragAllocated;bool measureUI;Func<bool> drawBoard;
        void OnGUI(){if(measureUI){long bytes=GC.GetAllocatedBytesForCurrentThread();drawBoard();uiAllocated+=GC.GetAllocatedBytesForCurrentThread()-bytes;uiFrames++;}}
        IEnumerator Pointer(EventType type,Vector2 point){game.ProcessTuningPointer(type,point);yield return null;}
        IEnumerator Tap(Vector2 point){yield return Pointer(EventType.MouseDown,point);yield return Pointer(EventType.MouseUp,point);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tuningtest")>=0)new GameObject("Runtime tuning verification").AddComponent<RuntimeTuningVerification>();}
        void Check(bool value,string reason){if(!value)throw new Exception("TUNING: "+reason);checks++;}
        IEnumerator Start()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../tuning-check"));Directory.CreateDirectory(output);
            while(!(game=FindFirstObjectByType<StairsGame>())||game.board==null)yield return null;
            Screen.SetResolution(391,844,FullScreenMode.Windowed);yield return null;yield return null;Screen.SetResolution(390,844,FullScreenMode.Windowed);yield return null;yield return null;
            game.enabled=false;drawBoard=(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),game,typeof(StairsGame).GetMethod("DrawRuntimeTuning",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance));hadKey=PlayerPrefs.HasKey(RuntimeTuning.Key);original=PlayerPrefs.GetString(RuntimeTuning.Key,"");
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());bool passed=true;
            while(stack.Count>0){bool more;object next;try{more=stack.Peek().MoveNext();next=more?stack.Peek().Current:null;}catch(Exception e){Debug.LogException(e);passed=false;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}
            if(hadKey&&Array.IndexOf(Environment.GetCommandLineArgs(),"-tuningClearTestPreference")<0)PlayerPrefs.SetString(RuntimeTuning.Key,original);else PlayerPrefs.DeleteKey(RuntimeTuning.Key);PlayerPrefs.Save();RuntimeTuning.Reload();
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{passed=passed,development=RuntimeTuning.Available,checks=checks,phaseSamples=phaseSamples,upSamples=upSamples,downSamples=downSamples,turnSamples=turnSamples,movingAllocatedBytes=allocated,parameterChangeAllocatedBytes=tuningAllocated,panelAllocatedBytes=uiAllocated,dragAllocatedBytes=dragAllocated,highSpeedAllocatedBytes=highSpeedAllocated,panelEvents=uiFrames,maximumPhaseError=maxPhaseError,maximumArcPhaseError=maxArcPhaseError,advanceMs=movingMs,highSpeedPerformance=performance},true));
            Debug.Log("RUNTIME TUNING VERIFICATION "+(passed?"PASSED":"FAILED")+" "+checks);Application.Quit(passed?0:1);
        }
        void Settle(){int count=0;while(game.IsAssembling&&count++<2000)game.Advance(.04f);Check(!game.IsAssembling,"assembly");while(game.IntroVisible)game.ContinueIntro();}
        IEnumerator Capture(string name){game.enabled=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;yield return null;game.enabled=false;}
        IEnumerator Run()
        {
            PlayerPrefs.SetString(RuntimeTuning.Key,"{\"version\":1,\"speed\":20,\"platform\":0.75,\"person\":1.25,\"linked\":false}");PlayerPrefs.Save();RuntimeTuning.Reload();
            if(!RuntimeTuning.Available){Check(RuntimeTuning.Speed==1&&RuntimeTuning.Platform==1&&RuntimeTuning.Person==1,"release ignores saved overrides");RuntimeTuning.ChangeSpeed(2);RuntimeTuning.Save();Check(RuntimeTuning.SaveCount==0,"release cannot write");game.LoadLevel(6);Settle();Check(!game.scene.people[0].tuningVisual,"release has no visual layer");yield return Capture("release");yield break;}
            Check(RuntimeTuning.Speed==20&&RuntimeTuning.Platform==.75f&&RuntimeTuning.Person==1.25f,"load saved20");
            PlayerPrefs.DeleteKey(RuntimeTuning.Key);RuntimeTuning.Reload();Check(RuntimeTuning.Speed==1.25f&&RuntimeTuning.Working.linked,"default");
            RuntimeTuning.ChangeSpeed(1.5f);PlayerPrefs.Save();Check(!PlayerPrefs.HasKey(RuntimeTuning.Key),"other settings Save cannot persist working");
            RuntimeTuning.Save();string saved=PlayerPrefs.GetString(RuntimeTuning.Key);RuntimeTuning.ChangeSpeed(2);PlayerPrefs.Save();Check(PlayerPrefs.GetString(RuntimeTuning.Key)==saved&&RuntimeTuning.Dirty,"unsaved isolation");RuntimeTuning.Reload();Check(RuntimeTuning.Speed==1.5f&&!RuntimeTuning.Dirty,"roundtrip");
            foreach(string bad in new[]{"bad json","{}","{\"version\":9}","{\"version\":1,\"speed\":999,\"platform\":1,\"person\":1}"}){PlayerPrefs.SetString(RuntimeTuning.Key,bad);RuntimeTuning.Reload();Check(RuntimeTuning.Speed==1.25f,"invalid fallback");}
            RuntimeTuning.SetLinked(false);RuntimeTuning.ChangeScale(false,.8f);RuntimeTuning.ChangeScale(true,1.2f);RuntimeTuning.SetLinked(true);float ratio=RuntimeTuning.Person/RuntimeTuning.Platform;
            RuntimeTuning.ChangeScale(false,1.25f);Check(Mathf.Abs(RuntimeTuning.Person-1.25f)<.00001f&&Mathf.Abs(RuntimeTuning.Person/RuntimeTuning.Platform-ratio)<.00001f,"linked upper clamp preserves ratio");
            RuntimeTuning.ChangeScale(true,.75f);Check(Mathf.Abs(RuntimeTuning.Platform-.75f)<.00001f&&Mathf.Abs(RuntimeTuning.Person/RuntimeTuning.Platform-ratio)<.00001f,"linked lower clamp preserves ratio");
            RuntimeTuning.SetLinked(false);RuntimeTuning.ChangeScale(false,.75f);RuntimeTuning.ChangeScale(true,1.11f);RuntimeTuning.SetLinked(true);RuntimeTuning.ChangeScale(false,1.25f);Check(RuntimeTuning.Working.Valid,"linked boundary remains persistable after float rounding");
            foreach(float width in new[]{375f,390f,700f})foreach(bool open in new[]{false,true}){var layout=new GameplayLayout(width,1000,new Rect(12,34,width-24,946),new Rect(),true);var rect=StairsGame.TuningRect(layout,open);Check(rect.xMin>=12&&rect.xMax<=width-12&&rect.yMin>=layout.Title.yMax*layout.Scale&&rect.yMax<layout.Props.yMin*layout.Scale,"safe area "+width);if(open)Check(Mathf.Abs(rect.height-272*rect.width/370)<.001f,"visual and input rect agree at narrow safe width");}
            game.LoadLevel(13);Settle();var projection=game.view.projectionMatrix;var camera=game.view.worldToCameraMatrix;
            var colliders=game.scene.root.GetComponentsInChildren<Collider>(true);var matrices=new Matrix4x4[colliders.Length];for(int i=0;i<colliders.Length;i++)matrices[i]=colliders[i].transform.localToWorldMatrix;
            var roots=new Vector3[game.scene.people.Length];for(int i=0;i<roots.Length;i++)roots[i]=game.scene.people[i].root.position;
            RuntimeTuning.SetLinked(false);
            foreach(float size in new[]{.75f,1f,1.25f}){RuntimeTuning.ChangeScale(false,size);RuntimeTuning.ChangeScale(true,size);game.scene.ApplyRuntimeTuning();Physics.SyncTransforms();
                for(int i=0;i<colliders.Length;i++)Check(colliders[i].transform.localToWorldMatrix==matrices[i],"collider unchanged");
                for(int i=0;i<roots.Length;i++)Check(game.scene.people[i].root.position==roots[i]&&Mathf.Abs(game.scene.people[i].tuningVisual.localScale.x-size)<.0001f,"root/visual separation");
                Check(projection==game.view.projectionMatrix&&camera==game.view.worldToCameraMatrix,"camera unchanged");
            }
            var panel=StairsGame.TuningRect(GameplayLayout.Current,true);game.TuningOpen=true;var inside=new Vector2(panel.center.x,Screen.height-panel.center.y);
            game.BeginViewPointer(inside);game.EndViewPointer(inside);Check(!game.PressPreviewActive&&game.selected==-1,"panel press no selection");
            var action=game.board.Level.solution[0];Vector2 source=game.view.WorldToScreenPoint(game.space.Centers[action.a]);game.TuningOpen=false;
            game.BeginViewPointer(source);game.TuningOpen=true;game.MoveViewPointer(inside);game.EndViewPointer(source);Check(!game.PressPreviewActive&&game.selected==-1,"cross into panel cancels");
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(700,1000)}){Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return null;yield return null;game.TuningOpen=false;yield return Capture("collapsed-"+size.x);game.TuningOpen=true;RuntimeTuning.SetLinked(true);yield return Capture("linked-"+size.x);RuntimeTuning.SetLinked(false);yield return Capture("independent-"+size.x);RuntimeTuning.Save();yield return Capture("saved-"+size.x);}
            game.TuningOpen=false;
            foreach(float speed in new[]{.5f,1f,1.25f,2f,5f,10f,20f})foreach(float dt in new[]{1f/120,1f/60,1f/30,.05f}){RuntimeTuning.ChangeSpeed(speed);game.LoadLevel(6);Settle();var a=game.board.Level.solution[0];Check(game.TryMoveSync(a.a,a.b),"start move");float start=game.clock,end=game.motion.duration,elapsed=0;
                int frames=0;while(game.motion!=null&&frames++<20000){CheckPhase(dt);elapsed+=dt;}
                Check(game.motion==null&&Mathf.Abs(elapsed-(end-start)/speed)<dt+.004f,"speed/arrival "+speed);
                for(int i=0;i<100;i++)game.Advance(.01f);Check(game.scene.PresentationSettled,"settled");game.Undo();Check(game.board.Moves==0,"undo");game.ResetLevel();Settle();Check(game.board.Moves==0,"reset");
            }
            game.LoadLevel(13);Settle();var move=game.board.Level.solution[0];RuntimeTuning.ChangeSpeed(.5f);Check(game.TryMoveSync(move.a,move.b),"mid-speed start");CheckPhase(.2f);RuntimeTuning.ChangeSpeed(20);float before=game.clock;CheckPhase(.1f);Check(game.clock>=before,"mid-speed monotonic");int guard=0;while(game.motion!=null&&guard++<5000)CheckPhase(.2f);Check(game.motion==null,"mid-speed arrives");
            foreach(float frameDelta in new[]{.05f,.2f,1f}){game.LoadLevel(6);Settle();RuntimeTuning.ChangeSpeed(20);var route=game.board.Level.solution[0];Check(game.TryMoveSync(route.a,route.b),"20x low frame move");int frameGuard=0;while(game.motion!=null&&frameGuard++<1000)CheckPhase(frameDelta);Check(game.motion==null,"20x low frame arrives");}
            game.LoadLevel(6);Settle();RuntimeTuning.ChangeSpeed(20);var first=game.board.Level.solution[0];Check(game.TryMoveSync(first.a,first.b),"fast to slow move");CheckPhase(.01f);RuntimeTuning.ChangeSpeed(.5f);while(game.motion!=null)CheckPhase(.05f);RuntimeTuning.ChangeSpeed(20);
            for(int routeIndex=1;routeIndex<Mathf.Min(4,game.board.Level.solution.Length);routeIndex++){var route=game.board.Level.solution[routeIndex];Check(game.TryMoveSync(route.a,route.b),"consecutive20 move");while(game.motion!=null)CheckPhase(.05f);}game.Undo();game.ResetLevel();Settle();Check(game.board.Moves==0,"consecutive undo/reset");
            game.LoadLevel(13);Settle();RuntimeTuning.ChangeSpeed(20);var appendA=game.board.Level.solution[0];var appendB=game.board.Level.solution[1];Check(game.TryMoveSync(appendA.a,appendA.b),"append first");CheckPhase(.001f);Check(game.TryMoveSync(appendB.a,appendB.b),"append while moving");while(game.motion!=null)CheckPhase(.05f);Check(game.board.Moves==2,"appended moves arrive");game.Undo();Check(game.board.Moves==1,"appended undo");game.ResetLevel();Settle();
            Check(upSamples>0&&downSamples>0&&turnSamples>0,"up/down/turn path coverage");
            // Large real-frame deltas exercise turn/end crossings while substeps preserve displacement phase.
            RuntimeTuning.ChangeSpeed(1.25f);game.LoadLevel(13);Settle();Check(game.scene.people.Length==96,"96-person fixture");Check(game.TryMoveSync(move.a,move.b),"performance move");
            for(int i=0;i<4;i++)game.Advance(.001f);var watch=new System.Diagnostics.Stopwatch();long bytes=GC.GetAllocatedBytesForCurrentThread();watch.Start();for(int i=0;i<100;i++)game.Advance(.001f);watch.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;movingMs=watch.Elapsed.TotalMilliseconds/100;Check(allocated==0,"96 moving steady GC");
            RuntimeTuning.SetLinked(false);RuntimeTuning.ChangeScale(true,1);game.scene.ApplyRuntimeTuning();bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++){RuntimeTuning.ChangeSpeed(i%2==0?1:2);RuntimeTuning.ChangeScale(true,i%2==0?1:1.01f);RuntimeTuning.ChangeScale(false,i%2==0?1:1.01f);game.scene.ApplyRuntimeTuning();game.Advance(.001f);}tuningAllocated=GC.GetAllocatedBytesForCurrentThread()-bytes;Check(tuningAllocated==0,"96 moving with parameter changes GC");
            game.LoadLevel(13);Settle();RuntimeTuning.ChangeSpeed(20);Check(game.TryMoveSync(move.a,move.b),"20x 96-person performance move");game.Advance(.001f);watch.Reset();bytes=GC.GetAllocatedBytesForCurrentThread();watch.Start();for(int i=0;i<8;i++)game.Advance(1f/60);watch.Stop();highSpeedAllocated=GC.GetAllocatedBytesForCurrentThread()-bytes;highSpeedMs=watch.Elapsed.TotalMilliseconds/8;Check(highSpeedAllocated==0,"20x moving steady GC");
            performance=new[]{MeasurePerformance(1f/60),MeasurePerformance(.05f)};
            var previousNow=game.DailyNow;var previousToday=game.DailyToday;double dailyTime=100;game.DailyNow=()=>dailyTime;game.DailyToday=()=>new DateTime(2097,1,1);game.ReturnHome();game.OpenDailyCalendar();Check(game.StartDailyChallenge(game.DailyToday()),"daily fixture starts");Settle();game.DailyClock.SetPause(game.DailyClock.Pauses,false);Check(game.DailyClock.Interact(),"daily clock starts");double remaining=game.DailyClock.Remaining;dailyTime+=.05;RuntimeTuning.ChangeSpeed(20);game.Advance(.05f);game.SendMessage("TickDaily");Check(Math.Abs(game.DailyClock.Remaining-(remaining-.05))<.00001&&Time.timeScale==1,"20x leaves daily real clock unchanged");game.DailyNow=previousNow;game.DailyToday=previousToday;
            Debug.Log("TUNING NON-UI CHECKS PASSED checks="+checks+" phaseError="+maxPhaseError+" movingBytes="+allocated+" tuningBytes="+tuningAllocated);
            game.LoadLevel(13);Settle();RuntimeTuning.ChangeSpeed(1.25f);
            // Closing the daily calendar's settings layer deliberately suppresses
            // input through the following real frame; Advance does not consume it.
            yield return null;yield return null;Check(!game.InterfaceBlocksInput,"input fixture past modal suppression");yield return InputChecks();
            game.TuningOpen=true;var dragPanel=StairsGame.TuningRect(GameplayLayout.Current,true);float unit=dragPanel.width/370;
            foreach(float y in new[]{103f,148f,193f}){game.ProcessTuningPointer(EventType.MouseDown,dragPanel.position+new Vector2(24,y)*unit);long dragStart=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<240;i++)game.ProcessTuningPointer(EventType.MouseDrag,dragPanel.position+new Vector2(24+i%231,y)*unit);dragAllocated+=GC.GetAllocatedBytesForCurrentThread()-dragStart;game.ProcessTuningPointer(EventType.MouseUp,dragPanel.position+new Vector2(254,y)*unit);}Check(dragAllocated==0,"sustained three slider drag GC");
            game.TuningOpen=true;game.enabled=true;yield return null;yield return null;measureUI=true;
            for(int i=0;i<60;i++){RuntimeTuning.ChangeSpeed(i%2==0?.5f:20);game.Advance(.001f);yield return null;}measureUI=false;game.enabled=false;Check(uiFrames>0&&uiAllocated==0,"panel draw/parameter label steady GC");
        }
        IEnumerator InputChecks()
        {
            game.TuningOpen=false;yield return Tap(StairsGame.TuningRect(GameplayLayout.Current,false).center);Check(game.TuningOpen,"touch entry opens");
            var ui=StairsGame.TuningRect(GameplayLayout.Current,true);float us=ui.width/370;var low=ui.position+new Vector2(24,103)*us;var high=ui.position+new Vector2(254,103)*us;
            int saves=RuntimeTuning.SaveCount;yield return Pointer(EventType.MouseDown,low);Check(RuntimeTuning.Speed==.5f,"slider lower endpoint");yield return Pointer(EventType.MouseDrag,high);Check(RuntimeTuning.Speed==20,"slider drag reaches20 before release");
            yield return Pointer(EventType.MouseDrag,ui.position+new Vector2(450,103)*us);Check(RuntimeTuning.Speed==20,"captured slider outside clamp");yield return Pointer(EventType.MouseUp,high);Check(RuntimeTuning.SaveCount==saves,"slider never saves");
            yield return Pointer(EventType.MouseDown,ui.position+Vector2.down*(ui.height+40));yield return Pointer(EventType.MouseUp,low);Check(RuntimeTuning.Speed==20,"cross in cannot activate");
            yield return Pointer(EventType.MouseDown,low);game.SendMessage("OnApplicationFocus",false);game.SendMessage("OnApplicationFocus",true);yield return Pointer(EventType.MouseDrag,high);yield return Pointer(EventType.MouseUp,high);Check(RuntimeTuning.Speed==.5f,"focus loss cancels further changes");
            game.ProcessTuningPointer(EventType.MouseDown,low);game.ProcessTuningPointer(EventType.MouseDrag,high,2);game.ProcessTuningPointer(EventType.MouseDrag,high);game.ProcessTuningPointer(EventType.MouseUp,high);Check(RuntimeTuning.Speed==.5f,"multi-touch cancels further changes");
            game.ProcessTuningPointer(EventType.MouseDown,low,1);game.ProcessTuningPointer(EventType.MouseDrag,high,2);game.ProcessTuningPointer(EventType.MouseDown,high,1);Check(RuntimeTuning.Speed==.5f,"remaining finger cannot restart canceled gesture");game.ProcessTuningPointer(EventType.MouseUp,high,0);
            game.ProcessTuningPointer(EventType.MouseDown,low,1);game.ProcessTuningPointer(EventType.MouseDrag,high,1,true);game.ProcessTuningPointer(EventType.MouseDown,high,1);Check(RuntimeTuning.Speed==.5f,"system touch cancellation blocks until all released");game.ProcessTuningPointer(EventType.MouseUp,high,0);
            RuntimeTuning.SetLinked(false);yield return Tap(ui.position+new Vector2(24,148)*us);yield return Tap(ui.position+new Vector2(254,193)*us);Check(RuntimeTuning.Platform==.75f&&RuntimeTuning.Person==1.25f,"independent scale sliders");
            RuntimeTuning.ChangeScale(true,.75f);RuntimeTuning.SetLinked(true);yield return Pointer(EventType.MouseDown,ui.position+new Vector2(24,148)*us);yield return Pointer(EventType.MouseDrag,ui.position+new Vector2(254,148)*us);yield return Pointer(EventType.MouseUp,ui.position+new Vector2(254,148)*us);Check(RuntimeTuning.Platform==1.25f&&RuntimeTuning.Person==1.25f,"linked scale drag");
            yield return Tap(high);
            yield return Tap(ui.position+new Vector2(265,244)*us);Check(!RuntimeTuning.Dirty,"explicit save button");
            yield return Tap(ui.position+new Vector2(334,26)*us);Check(!game.TuningOpen,"touch closes");
        }
        void CheckPhase(float delta)
        {
            var plan=game.motion;if(plan==null){game.Advance(delta);return;}int count=game.scene.people.Length;var phase=new float[count];var positions=new Vector3[count];
            for(int i=0;i<count;i++){phase[i]=game.scene.people[i].walk.Phase;positions[i]=game.scene.people[i].root.position;}
            float end=Mathf.Min(game.clock+delta*RuntimeTuning.Speed,plan.duration),motionDelta=end-game.clock;int steps=Mathf.Clamp(Mathf.CeilToInt(motionDelta*120),1,4096);float time=game.clock;var asset=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            var arcPhase=(float[])phase.Clone();foreach(var track in plan.tracks){double distance=0;for(int k=1;k<track.points.Length;k++){double a=track.start+track.times[k-1],b=track.start+track.times[k];double overlap=Math.Max(0,Math.Min(end,b)-Math.Max(game.clock,a));if(b>a)distance+=Vector3.ProjectOnPlane(track.points[k]-track.points[k-1],Vector3.up).magnitude*overlap/(b-a);if(overlap>0){var direction=track.points[k]-track.points[k-1];if(direction.y>.001f)upSamples++;if(direction.y<-.001f)downSamples++;if(k+1<track.points.Length&&Vector3.Cross(Vector3.ProjectOnPlane(direction,Vector3.up).normalized,Vector3.ProjectOnPlane(track.points[k+1]-track.points[k],Vector3.up).normalized).sqrMagnitude>.0001f)turnSamples++;}}arcPhase[track.actor]=Mathf.Repeat(arcPhase[track.actor]+(float)(distance/(asset.cycleDistance*RuntimeTuning.Person)),1);}
            for(int k=0;k<steps;k++){time=k==steps-1?end:Mathf.Min(time+motionDelta/steps,plan.duration);foreach(var track in plan.tracks){var p=track.PlaybackPosition(time);phase[track.actor]=Mathf.Repeat(phase[track.actor]+Vector3.ProjectOnPlane(p-positions[track.actor],Vector3.up).magnitude/(asset.cycleDistance*RuntimeTuning.Person),1);positions[track.actor]=p;}}
            game.Advance(delta);foreach(var track in plan.tracks){phaseSamples++;var actor=game.scene.people[track.actor];float error=Mathf.Abs(Mathf.DeltaAngle(phase[track.actor]*360,actor.walk.Phase*360))/360;maxPhaseError=Mathf.Max(maxPhaseError,error);Check(error<.001f,"phase follows actual displacement");float arcError=Mathf.Abs(Mathf.DeltaAngle(arcPhase[track.actor]*360,actor.walk.Phase*360))/360;maxArcPhaseError=Mathf.Max(maxArcPhaseError,arcError);Check(arcError<.005f,"independent polyline arc phase "+arcError);Check(Vector3.Distance(actor.root.position,positions[track.actor])<.005f,"sampled turn/stair position");}
        }
        Performance MeasurePerformance(float delta)
        {
            var times=new double[80];var watch=new System.Diagnostics.Stopwatch();long total=0;RuntimeTuning.ChangeSpeed(20);
            for(int i=0;i<times.Length;i++){if(i==0||game.motion==null){game.LoadLevel(13);Settle();var route=game.board.Level.solution[0];Check(game.TryMoveSync(route.a,route.b),"performance restart");game.Advance(.001f);}watch.Reset();long before=GC.GetAllocatedBytesForCurrentThread();watch.Start();game.Advance(delta);watch.Stop();total+=GC.GetAllocatedBytesForCurrentThread()-before;times[i]=watch.Elapsed.TotalMilliseconds;}
            double sum=0;foreach(var value in times)sum+=value;Array.Sort(times);Check(total==0,"20x real-frame performance GC");return new Performance{realFrameDelta=delta,samples=times.Length,allocatedBytes=total,meanMs=sum/times.Length,p95Ms=times[75],maxMs=times[79]};
        }
    }
}
