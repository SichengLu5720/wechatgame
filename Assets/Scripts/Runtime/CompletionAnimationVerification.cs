using System;
using System.Collections;
using System.IO;
using System.Linq;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class CompletionAnimationVerification
    {
        static void Require(bool value,string reason){if(!value)throw new Exception("Completion animation: "+reason);}
        public static IEnumerator Run(StairsGame game)
        {
            var level=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text).levels[0];
            level.name="Completion animation fixture";
            level.groups=Enumerable.Range(0,8).Select(i=>new GroupSpec{id=i,color=i<4?0:1}).ToArray();
            foreach(var node in level.nodes)node.queue=new int[0];
            level.nodes[0].queue=new[]{0,1,2,3};level.nodes[1].queue=new[]{4,5,6};level.nodes[2].queue=new[]{7};
            game.OpenEditor();game.SetEditorDraft(level);game.PlayDraft();
            while(game.Busy){game.Advance(.05f);yield return null;}while(game.IntroVisible)game.ContinueIntro();
            // Initial completion must only remove its incident link.
            for(int i=0;i<60;i++){game.Advance(.05f);yield return null;}
            var scene=game.scene;for(int e=0;e<level.edges.Length;e++)Require(scene.LinkRoot(e)!=null,"missing fixture link "+e);var origins=scene.nodeRoots.Select(t=>t.position).ToArray();
            Require(scene.NodeRoot(0).gameObject.activeSelf,"first collection disappeared");
            RefinedRuntimeVerification.Check(game);
            var architecture=Enumerable.Range(0,level.nodes.Length).Select(n=>scene.NodeRoot(n).Find("Architecture batch").GetComponent<MeshRenderer>()).ToArray();
            var stableMaterials=architecture.Select(r=>r.sharedMaterial).ToArray();
            var stableColors=architecture.Select(r=>r.GetComponent<MeshFilter>().sharedMesh.colors).ToArray();
            Require(scene.CollectionCompleted(0)&&!scene.CollectionCompleted(1),"initial collection state");
            int completionEvents=0;scene.CollectionCompletionChanged+=(node,on)=>{if(node==1)completionEvents++;};
            Capture("collection-rim-before.png");
            Require(!scene.LinkRoot(0).gameObject.activeSelf,"completed collection link remains");
            Require(scene.LinkRoot(1).gameObject.activeSelf&&scene.LinkRoot(2).gameObject.activeSelf,"unrelated links descended early");
            Require(scene.NodeRoot(2).gameObject.activeSelf&&scene.NodeRoot(3).gameObject.activeSelf,"transit descended early");
            var fixedPeople=scene.people.Take(16).Select(p=>p.root.position).ToArray();
            Require(game.TryMoveSync(2,1),"last move rejected");
            Require(game.motion!=null&&scene.NodeRoot(2).gameObject.activeSelf,"final descent started before arrival");
            Require(!scene.CollectionCompleted(1),"collection completed before arrival");
            float animationSeconds=game.motion.duration;int frames=0;
            while(game.Busy&&frames++<500){game.Advance(.025f);for(int n=0;n<2;n++)Require(scene.NodeRoot(n).gameObject.activeSelf&&scene.NodeRoot(n).position==origins[n],"collection moved during descent");yield return null;}
            Require(!game.Busy&&game.board.Solved,"final sequence did not finish");
            Require(scene.CollectionCompleted(0)&&scene.CollectionCompleted(1)&&completionEvents==1,"completion state or event");
            for(int n=0;n<2;n++)Require(architecture[n].sharedMaterial==stableMaterials[n]&&architecture[n].GetComponent<MeshFilter>().sharedMesh.colors.SequenceEqual(stableColors[n]),"completion changed platform color");
            yield return new WaitForEndOfFrame();
            Capture("collection-rim-complete.png");
            Require(!scene.NodeRoot(2).gameObject.activeSelf&&!scene.NodeRoot(3).gameObject.activeSelf,"remaining transit visible");
            for(int e=0;e<level.edges.Length;e++)Require(!scene.LinkRoot(e).gameObject.activeSelf,"remaining link visible");
            foreach(var person in scene.people)Require(person.root.gameObject.activeSelf,"gathered person disappeared");
            for(int i=0;i<16;i++)Require(scene.people[i].root.position==fixedPeople[i],"gathered person moved");
            game.Undo();while(game.Busy){game.Advance(.05f);yield return null;}
            Require(!game.board.Solved&&scene.NodeRoot(2).gameObject.activeSelf&&scene.NodeRoot(3).gameObject.activeSelf,"undo failed to restore transit");
            Require(scene.LinkRoot(1).gameObject.activeSelf&&scene.LinkRoot(2).gameObject.activeSelf,"undo failed to restore links");
            Require(scene.CollectionCompleted(0)&&!scene.CollectionCompleted(1)&&completionEvents==2,"undo did not restore completion state");
            for(int n=0;n<architecture.Length;n++)Require(architecture[n].sharedMaterial==stableMaterials[n]&&architecture[n].GetComponent<MeshFilter>().sharedMesh.colors.SequenceEqual(stableColors[n]),"undo changed platform color");
            Capture("collection-rim-undo.png");
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/sky-v7/completion-check.json"));
            File.WriteAllText(path,"{\"passed\":true,\"collectionsStay\":true,\"peopleStay\":true,\"incidentLinkOnly\":true,\"finalCleanup\":true,\"waitForArrival\":true,\"undo\":true,\"noPylonsOrLights\":true,\"platformColorsUnchanged\":true,\"completionEvent\":true,\"moveAnimationSeconds\":"+animationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
        }
        static void Capture(string name)
        {
            var target=RenderTexture.GetTemporary(720,1280,24);var previous=RenderTexture.active;
            foreach(var camera in Camera.allCameras.OrderBy(c=>c.depth)){var old=camera.targetTexture;camera.targetTexture=target;camera.Render();camera.targetTexture=old;}
            RenderTexture.active=target;var shot=new Texture2D(720,1280,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,720,1280),0,0);shot.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/sky-v7/"+name));Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,shot.EncodeToPNG());UnityEngine.Object.Destroy(shot);
        }
    }
}
