using System;
using System.Collections;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using StairsCrowd.Core;
using System.Collections.Generic;

namespace StairsCrowd.Runtime
{
    // Same rendered player probe is used before and after character integration.
    public sealed class CharacterWalkProbe : MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public int people, frames, renderers;
            public string state;
            public double frameMeanMs, advanceMeanMs, renderSubmissionMeanMs;
            public long advanceAllocatedBytes, totalAllocatedMemory, monoUsedMemory, draws, triangles;
            public bool drawCounterAvailable;
        }
        [Serializable] sealed class Report { public Sample[] samples; public bool passed, targetDeviceTested, fallback; public string sourceHash,renderMethod; public long sharedCharacterBytes; }
        [Serializable] sealed class PoseEvidence { public float phase; public string file; public Vector3 leftFoot,rightFoot; public int differentPixels; }
        [Serializable] sealed class MotionEvidence { public string sourceHash,renderMethod; public PoseEvidence[] poses; public bool actualFramesDiffer,resetSettled; }
        [Serializable] sealed class StairPose { public string file,direction; public float time,phase,leftClearance,rightClearance; public Vector3 root,leftFoot,rightFoot; }
        [Serializable] sealed class StairEvidence { public string sourceHash,renderMethod; public List<StairPose> poses=new List<StairPose>(); public bool arrivalSettled; }
        static bool FallbackRequested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-characterfallbackprobe")>=0;
        static bool PerformanceOnly=>Array.IndexOf(Environment.GetCommandLineArgs(),"-characterperformanceonly")>=0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PrepareFallbackProbe()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-characterprobe")<0)return;
            bool candidate=Array.IndexOf(Environment.GetCommandLineArgs(),"-charactercandidateprobe")>=0;
            if(!FallbackRequested&&!candidate)return;
            var asset=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            // Player-process-only diagnostic override; never serializes an asset.
            if(asset)asset.runtimeEnabled=!FallbackRequested&&candidate;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-characterprobe") < 0) return;
            var game = UnityEngine.Object.FindFirstObjectByType<StairsGame>();
            if (game) game.gameObject.AddComponent<CharacterWalkProbe>();
        }
        StairsGame game;
        string output;
        IEnumerator Start()
        {
            game = GetComponent<StairsGame>(); game.enabled = false;
            output = Path.Combine(Application.dataPath, FallbackRequested?"../character-probe-fallback":"../character-probe"); Directory.CreateDirectory(output);
            var run = Run();
            while (true)
            {
                bool more;
                try { more = run.MoveNext(); }
                catch (Exception e) { Debug.LogException(e); Application.Quit(1); yield break; }
                if (!more) break;
                yield return run.Current;
            }
            Debug.Log("CHARACTER PROBE PASSED"); Application.Quit(0);
        }
        IEnumerator Run()
        {
            var resource=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            var report = new Report { samples = new Sample[4],fallback=FallbackRequested,sourceHash=resource?resource.sourceHash:"",renderMethod="Explicit offscreen 700x1000 camera.Render each sampled frame; hidden Windows player; CPU submission measured, GPU time/device FPS not asserted",sharedCharacterBytes=resource?Profiler.GetRuntimeMemorySizeLong(resource)+Profiler.GetRuntimeMemorySizeLong(resource.mesh):0 };
            int large = 0;
            for (int i = 0; i < game.catalog.levels.Length; i++) if (game.catalog.levels[i].groups.Length * 4 == 96) { large = i; break; }
            int sample = 0;
            foreach (int level in new[] { 0, large })
            {
                game.LoadLevel(level);
                int guard = 0;
                while (game.IsAssembling && guard++ < 1000) game.Advance(.04f);
                if (game.IsAssembling) throw new Exception("Assembly timeout");
                while (game.IntroVisible) game.ContinueIntro();
                if(FallbackRequested&&game.scene.UsesCharacterWalk)throw new Exception("Fallback scene loaded candidate");
                if(!FallbackRequested&&!game.scene.UsesCharacterWalk)throw new Exception("Candidate scene silently fell back");
                foreach(var p in game.scene.people)if(FallbackRequested?p.walk!=null:p.walk==null)throw new Exception("Wrong character renderer in probe");
                for (int i = 0; i < 20; i++) yield return null;
                yield return Measure(report.samples[sample++] = new Sample { state = "idle" });
                var move = game.board.Level.solution[0];
                if (!game.TryMoveSync(move.a, move.b)) throw new Exception("Probe move rejected");
                yield return Measure(report.samples[sample++] = new Sample { state = "move" });
            }
            report.passed = true;
            File.WriteAllText(Path.Combine(output, "performance.json"), JsonUtility.ToJson(report, true));
            if(game.scene.UsesCharacterWalk&&!PerformanceOnly){yield return CaptureStairs();yield return CaptureMotion();}
        }
        IEnumerator CaptureStairs()
        {
            game.scene.root.SetActive(false);
            var asset=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            var evidence=new StairEvidence{sourceHash=asset.sourceHash,renderMethod="Live player frames, actual CrowdScene stair mesh and FastMovement path, runtime support solver; clearance includes existing presentation lift"};
            foreach(bool descend in new[]{false,true})
            {
                string direction=descend?"down":"up";
                var level=new LevelSpec{nodes=new[]{new NodeSpec{y=descend?2:0,capacity=4,queue=new[]{0}},new NodeSpec{x=8,y=descend?0:2,capacity=4,transit=true,queue=new int[0]}},edges=new[]{new EdgeSpec{a=0,b=1}},groups=new[]{new GroupSpec{id=0,color=0}}};
                var space=new WalkSpace(level,null,true);var state=Rules.Initial(level);
                var exhibit=new CrowdScene(space,game.view);exhibit.Populate(state);
                var plan=FastMovement.Build(space,state,Rules.Preview(level,state,0,1));var track=plan.Track(0);var person=exhibit.people[0];
                foreach(var other in exhibit.people)if(other!=person)other.root.gameObject.SetActive(false);
                person.walk.Renderer.updateWhenOffscreen=true;
                int frames=Mathf.CeilToInt(track.End*60),nextSample=0;
                game.view.orthographicSize=1.65f;
                for(int frame=0;frame<=frames;frame++)
                {
                    float t=Mathf.Min(frame/60f,track.End);var position=track.PlaybackPosition(t);var facing=Vector3.ProjectOnPlane(position-person.root.position,Vector3.up);
                    person.Pose(position,facing,0,t<track.End);person.remainingDistance=track.RemainingDistance(t);person.TickWalk(1f/60);
                    game.view.transform.position=position+new Vector3(-3,2.6f,4);game.view.transform.LookAt(position+Vector3.up*.65f);
                    yield return new WaitForEndOfFrame();
                    if(frame<Mathf.RoundToInt(frames*nextSample/12f))continue;
                    var pose=new StairPose{file="stair-"+direction+"-"+nextSample.ToString("00")+".png",direction=direction,time=t,phase=person.walk.Phase,root=position};
                    foreach(var bone in person.visual.GetComponentsInChildren<Transform>())
                    {
                        if(bone.name!="foot_l"&&bone.name!="foot_r")continue;
                        if(!space.Surface(new Vector2(bone.position.x,bone.position.z),out float floor,out _))throw new Exception("Stair foot outside support");
                        float clearance=bone.position.y-floor-WalkSpace.FootGap-(person.visual.position.y-person.root.position.y);
                        if(bone.name=="foot_l"){pose.leftFoot=bone.position;pose.leftClearance=clearance;}else{pose.rightFoot=bone.position;pose.rightClearance=clearance;}
                    }
                    Capture(pose.file);evidence.poses.Add(pose);nextSample++;
                }
                for(int i=0;i<30;i++){person.TickWalk(1f/60);yield return new WaitForEndOfFrame();}
                if(!person.PresentationSettled)throw new Exception("Stair arrival did not settle");
                Capture("stair-"+direction+"-reset.png");exhibit.Dispose();
            }
            evidence.arrivalSettled=true;File.WriteAllText(Path.Combine(output,"stair-motion-evidence.json"),JsonUtility.ToJson(evidence,true));
        }
        IEnumerator CaptureMotion()
        {
            // A real player-loop frame between poses is essential: synchronous
            // Editor Camera.Render calls do not schedule the skinning update.
            var asset=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            var library=Resources.Load<RefinedArtLibrary>("RefinedArtLibrary");
            game.scene.root.SetActive(false);
            var person=new PersonView{root=new GameObject("Walk evidence exhibit").transform};
            person.visual=new GameObject("Character visual").transform;person.visual.SetParent(person.root,false);
            person.waterBird=new WaterBirdVisual(person,asset.mesh,library.characters[0],null,null,asset,null);
            person.walk.Renderer.updateWhenOffscreen=true;
            game.view.orthographicSize=1.1f;game.view.clearFlags=CameraClearFlags.SolidColor;game.view.backgroundColor=new Color(.23f,.60f,.61f);
            var evidence=new MotionEvidence{sourceHash=asset.sourceHash,renderMethod="Live SkinnedMeshRenderer; one real player-loop frame between samples, no BakeMesh",poses=new PoseEvidence[9]};
            Color32[] first=null;
            for(int frame=0;frame<=120;frame++)
            {
                if(frame>0){person.root.position+=Vector3.forward*(asset.cycleDistance/120);person.TickWalk(1f/120);}
                game.view.transform.position=person.root.position+new Vector3(3,2.5f,5);game.view.transform.LookAt(person.root.position+Vector3.up*.82f);
                yield return new WaitForEndOfFrame();
                if(frame%15!=0)continue;
                string file="live-walk-"+frame.ToString("000")+".png";var pixels=Capture(file);
                int difference=0;if(first==null)first=pixels;else for(int i=0;i<pixels.Length;i++)if(!pixels[i].Equals(first[i]))difference++;
                var transforms=person.visual.GetComponentsInChildren<Transform>();Vector3 left=Vector3.zero,right=Vector3.zero;
                foreach(var t in transforms){if(t.name=="foot_l")left=t.position-person.root.position;if(t.name=="foot_r")right=t.position-person.root.position;}
                evidence.poses[frame/15]=new PoseEvidence{phase=person.walk.Phase,file=file,leftFoot=left,rightFoot=right,differentPixels=difference};
                evidence.actualFramesDiffer|=difference>100;
            }
            if(!evidence.actualFramesDiffer)throw new Exception("Live skinning frames identical despite sampled walk");
            for(int frame=0;frame<30;frame++){person.TickWalk(1f/60);yield return new WaitForEndOfFrame();}
            Capture("live-walk-reset.png");evidence.resetSettled=person.PresentationSettled;
            if(!evidence.resetSettled)throw new Exception("Live arrival did not settle");
            File.WriteAllText(Path.Combine(output,"live-motion-evidence.json"),JsonUtility.ToJson(evidence,true));
            Destroy(person.root.gameObject);
        }
        IEnumerator Measure(Sample sample)
        {
            sample.people = game.scene.people.Length;
            sample.renderers = game.scene.root.GetComponentsInChildren<Renderer>().Length;
            var watch = new System.Diagnostics.Stopwatch();
            var target=RenderTexture.GetTemporary(700,1000,24);
            var originalTarget=game.view.targetTexture;
            try
            {
            using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1))
            using (var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1))
            {
                sample.drawCounterAvailable = draws.Valid;
                for (int i = 0; i < 90; i++)
                {
                    long allocated = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
                    game.Advance(1f / 60); watch.Stop();
                    sample.advanceAllocatedBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
                    sample.advanceMeanMs += watch.Elapsed.TotalMilliseconds;
                    yield return new WaitForEndOfFrame();
                    watch.Restart();game.view.targetTexture=target;game.view.Render();game.view.targetTexture=originalTarget;watch.Stop();
                    sample.renderSubmissionMeanMs+=watch.Elapsed.TotalMilliseconds;
                    sample.frameMeanMs += Time.unscaledDeltaTime * 1000;
                    sample.draws += draws.Valid ? draws.LastValue : 0;
                    sample.triangles += triangles.Valid ? triangles.LastValue : 0;
                    sample.frames++;
                    if (!PerformanceOnly&&(i == 20 || i == 40 || i == 60)) Capture(sample.people + "-" + sample.state + "-" + i + ".png");
                }
            }
            }
            finally{game.view.targetTexture=originalTarget;RenderTexture.ReleaseTemporary(target);}
            sample.frameMeanMs /= sample.frames; sample.advanceMeanMs /= sample.frames;
            sample.renderSubmissionMeanMs/=sample.frames;
            sample.draws /= sample.frames; sample.triangles /= sample.frames;
            sample.totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(); sample.monoUsedMemory = Profiler.GetMonoUsedSizeLong();
        }
        Color32[] Capture(string name)
        {
            var target = RenderTexture.GetTemporary(700, 1000, 24);
            var previous = game.view.targetTexture; var active = RenderTexture.active;
            try
            {
                game.view.targetTexture = target; game.view.Render(); RenderTexture.active = target;
                var image = new Texture2D(700, 1000, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 700, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(output, name), image.EncodeToPNG());var pixels=image.GetPixels32();Destroy(image);return pixels;
            }
            finally { game.view.targetTexture = previous; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
