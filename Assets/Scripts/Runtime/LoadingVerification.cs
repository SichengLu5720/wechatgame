using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class LoadingVerification:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed,wechatDeviceTested;
            public int transitions,seamlessTransitions,reusedPeople,maxResidentScenes;
            public double maxPreparationSliceMs;
            public string cloudFormat;
            public long cloudResourceBytes;
            public int cloudTextureDataBytes=128*128;
            public List<double> coldLoadMs=new List<double>(),preparedLoadMs=new List<double>();
            public List<int> meshCounts=new List<int>(),materialCounts=new List<int>(),actorCounts=new List<int>();
        }
        StairsGame game;string output;readonly Report report=new Report();
        static void Check(bool ok,string message){if(!ok)throw new Exception("Loading verification: "+message);}
        IEnumerator Start()
        {
            game=GetComponent<StairsGame>();game.enabled=false;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../loading-check"));Directory.CreateDirectory(output);
            Screen.SetResolution(540,960,FullScreenMode.Windowed);yield return null;
            var run=Run();while(true){bool next;try{next=run.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Debug.LogException(e);Application.Quit(1);yield break;}if(!next)break;yield return run.Current;}
            report.passed=true;report.maxPreparationSliceMs=game.MaximumPreparationSliceMs;
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Debug.Log("LOADING VERIFICATION PASSED");Application.Quit(0);
        }
        void Settle()
        {
            int steps=0;while(game.IsAssembling&&steps++<1000)game.Advance(.025f);
            Check(!game.IsAssembling,"assembly timeout");while(game.IntroVisible)game.ContinueIntro();
            if(game.Home){Check(game.scene.people.Length==0,"home shows gameplay actors");return;}
            Check(game.scene.people.Length==game.board.Level.groups.Length*4,"population size");
            foreach(var p in game.scene.people){Check(p.root.gameObject.activeInHierarchy,"actor inactive");foreach(var r in p.colored)Check(r.sharedMaterial&&r.GetComponent<MeshFilter>().sharedMesh,"disposed actor resource");}
        }
        void Prepare()
        {
            var position=game.view.transform.position;var rotation=game.view.transform.rotation;var size=game.view.orthographicSize;
            int frames=0;while(!game.NextSceneReady&&frames++<1000){game.Advance(.017f);Check(game.ResidentSceneCount<=2,"more than two scenes");}
            Check(game.NextSceneReady,"preparation timeout");report.maxResidentScenes=Math.Max(report.maxResidentScenes,game.ResidentSceneCount);
            Check(game.view.transform.position==position&&game.view.transform.rotation==rotation&&game.view.orthographicSize==size,"preparation moved active camera");
            Check(UnityEngine.Object.FindObjectsByType<PlatformTag>(FindObjectsSortMode.None).All(t=>t.transform.IsChildOf(game.scene.root.transform)),"prepared level accepts raycasts");
        }
        void Load(int index,bool prepared)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();game.LoadLevel(index);double ms=timer.Elapsed.TotalMilliseconds;
            Check(game.UsedPreparedScene==prepared,"unexpected preparation consumption");
            (prepared?report.preparedLoadMs:report.coldLoadMs).Add(ms);Settle();report.transitions++;
        }
        void CheckHidden()
        {
            foreach(var p in game.scene.people){int id=Array.IndexOf(game.scene.people,p);bool hidden=!game.board.Current.revealed[id/4]&&!Rules.Complete(game.board.Level,game.board.Current,p.tag.node);
                Check((p.hiddenCharacter&&p.hiddenCharacter.activeSelf)==hidden,"stale hidden state after reuse");
                Check(p.visual.Find("Sky character").gameObject.activeSelf==!hidden,"stale color model visibility");
                Check(!p.IsSelected&&!p.selectionRing.activeSelf,"selection survived level transfer");
            }
        }
        IEnumerator Run()
        {
            var noise=Resources.Load<Texture2D>("CloudNoise");Check(noise&&noise.width==128&&noise.height==128&&!noise.isReadable&&noise.mipmapCount==1,"noise texture import");
            report.cloudFormat=noise.format.ToString();report.cloudResourceBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(noise);
            // Four identical cycles detect leaked native meshes/materials and stale person references.
            for(int cycle=0;cycle<4;cycle++){
                Load(6,false);Prepare();var actor=game.scene.people[0].root;Load(7,true);
                Check(game.scene.people[0].root==actor,"actor recreated");report.reusedPeople+=game.scene.ReusedPeople;CheckHidden();
                var step=game.board.Level.solution[0];Check(game.RequestMove(step.a,step.b),"reused hidden row cannot move");
                game.Advance(.22f);Check(game.scene.HasReveal,"hidden color reveal missing");game.Undo();Settle();CheckHidden();game.ResetLevel();Settle();CheckHidden();
                Prepare();actor=game.scene.people[0].root;Load(8,true);Check(game.scene.people[0].root==actor,"second transfer lost actor");CheckHidden();report.reusedPeople+=game.scene.ReusedPeople;
                game.ReturnHome();Settle();Check(game.ResidentSceneCount==1,"home retained preload");
                GC.Collect();GC.WaitForPendingFinalizers();yield return null;
                report.meshCounts.Add(Resources.FindObjectsOfTypeAll<Mesh>().Length);report.materialCounts.Add(Resources.FindObjectsOfTypeAll<Material>().Length);
                report.actorCounts.Add(Resources.FindObjectsOfTypeAll<PlatformTag>().Count(p=>p.GetComponent<CapsuleCollider>()));
            }
            Check(report.meshCounts.Skip(1).Distinct().Count()==1&&report.materialCounts.Skip(1).Distinct().Count()==1&&report.actorCounts.Distinct().Count()==1,"native objects accumulate across cycles");
            // Abandon partially constructed preloads and enter/leave editor.
            Load(6,false);game.Advance(.017f);game.OpenEditor();Check(game.ResidentSceneCount==1,"editor retained next level");game.ReturnHome();Settle();
            Load(0,false);
            for(int level=0;level<3;level++){
                Prepare();var actor=game.scene.people[0].root;
                foreach(var step in game.board.Level.solution){Check(game.RequestMove(step.a,step.b),"tutorial move");int guard=0;while(game.motion!=null&&guard++<1000)game.Advance(.025f);Check(game.motion==null,"tutorial movement timeout");}
                Check(game.board.Solved,"tutorial completion");game.Advance(.1f);Check(game.TutorialExiting,"tutorial exit did not begin");
                game.OpenSettings();var origin=game.scene.root.transform.position;game.Advance(1);Check(game.levelIndex==level&&game.scene.root.transform.position==origin,"pause advanced transition");game.CloseSettings();
                yield return null;yield return null;yield return null;
                int frames=0;while(game.levelIndex==level&&frames++<100)game.Advance(.025f);
                Check(game.levelIndex==level+1&&game.IsAssembling&&game.UsedPreparedScene,"seamless next-level rise");Settle();Check(game.scene.people[0].root==actor,"seamless transition recreated actor");report.seamlessTransitions++;
            }
            Load(7,false);Settle();CheckHidden();yield return new WaitForEndOfFrame();Capture();
        }
        void Capture()
        {
            var target=RenderTexture.GetTemporary(540,960,24);var previous=RenderTexture.active;
            try{foreach(var camera in Camera.allCameras.OrderBy(c=>c.depth)){var old=camera.targetTexture;try{camera.targetTexture=target;camera.Render();}finally{camera.targetTexture=old;}}
                RenderTexture.active=target;var texture=new Texture2D(540,960,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,540,960),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,"level-08.png"),texture.EncodeToPNG());Destroy(texture);
            }finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
        }
    }
}
