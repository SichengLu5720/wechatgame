using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed class HiddenArtVerification : MonoBehaviour
    {
        public static bool Active {get;private set;}
        StairsGame game;string output;
        static void Require(bool ok,string reason){if(!ok)throw new Exception("Hidden art: "+reason);}
        void Ready(){int i=0;while(game.Busy&&i++<300)game.Advance(.025f);Require(!game.Busy,"settle timeout");}
        int Check()
        {
            int hidden=0;Mesh shared=null;Material material=null;
            for(int i=0;i<game.scene.people.Length;i++){
                var p=game.scene.people[i];bool concealed=!game.board.Current.revealed[i/4]&&!Rules.Complete(game.board.Level,game.board.Current,p.tag.node);
                Require(p.visual.Find("Sky character").gameObject.activeSelf==!concealed,"ivory or colored model visible in hidden state");
                Require((p.hiddenCharacter&&p.hiddenCharacter.activeSelf)==concealed,"independent hidden model state");
                if(!concealed)continue;hidden++;
                var filter=p.hiddenCharacter.GetComponent<MeshFilter>();var renderer=p.hiddenCharacter.GetComponent<MeshRenderer>();
                if(!shared){shared=filter.sharedMesh;material=renderer.sharedMaterial;}
                Require(filter.sharedMesh==shared&&renderer.sharedMaterial==material,"assets not shared");
                Require(p.hiddenCharacter.GetComponentsInChildren<Renderer>().Length==1,"extra renderers");
                Require(p.hiddenCharacter.GetComponentsInChildren<Collider>().Length==0,"visual changes collision");
                Require(renderer.sharedMaterial.shader.name=="Stairs/HiddenCharacter","wrong shader");
            }
            return hidden;
        }
        float Capture(string name)
        {
            float glyphBrightness=0;
            var target=RenderTexture.GetTemporary(720,1280,24);var active=RenderTexture.active;
            try{
                foreach(var camera in Camera.allCameras.OrderBy(c=>c.depth)){var old=camera.targetTexture;try{camera.targetTexture=target;camera.Render();}finally{camera.targetTexture=old;}}
                RenderTexture.active=target;var t=new Texture2D(720,1280,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,720,1280),0,0);t.Apply();
                foreach(var pixel in t.GetPixels32())if(Math.Abs(pixel.r-pixel.g)<3&&Math.Abs(pixel.r-pixel.b)<3)glyphBrightness=Mathf.Max(glyphBrightness,pixel.r/255f);
                File.WriteAllBytes(Path.Combine(output,name),t.EncodeToPNG());Destroy(t);
            }finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
            return glyphBrightness;
        }
        IEnumerator Start()
        {
            Active=true;game=GetComponent<StairsGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/sky-v7.13-hidden"));Directory.CreateDirectory(output);
            Screen.SetResolution(540,960,FullScreenMode.Windowed);yield return null;yield return null;
            game.LoadLevel(7);Ready();Require(!game.IntroVisible,"removed hints returned");int initial=Check();Require(initial>0,"no hidden test population");
            RefinedRuntimeVerification.Check(game);
            Capture("level-08-hidden.png");
            var person=game.scene.people.First(p=>p.hiddenCharacter&&p.hiddenCharacter.activeSelf);
            var mesh=person.hiddenCharacter.GetComponent<MeshFilter>().sharedMesh;
            foreach(var v in mesh.vertices)Require(new Vector2(v.x,v.z).magnitude*1.1f<=WalkSpace.ActorRadius&&v.y*1.1f<=CrowdScene.PersonHeight,"mesh exceeds envelope");
            Require(mesh.colors.Any(c=>c.r>.99f)&&mesh.colors.Any(c=>c.r<.025f),"glyph missing");
            Require(mesh.colors.All(c=>Mathf.Abs(c.r-c.g)<.001f&&Mathf.Abs(c.r-c.b)<.001f&&(c.r<.025f||c.r>.99f)),"colored or gray trim");
            var oldPos=game.view.transform.position;var oldRot=game.view.transform.rotation;float oldSize=game.view.orthographicSize;
            var center=game.space.Centers[person.tag.node]+Vector3.up*.5f;
            game.view.transform.position=center+(oldPos-center).normalized*25;game.view.transform.LookAt(center);game.view.orthographicSize=5.4f;
            Capture("hidden-platform-closeup.png");
            // Isolated view of the exact prefab used above, rendered by the game camera.
            game.scene.root.SetActive(false);var exhibit=Instantiate(Resources.Load<GameObject>("HiddenCharacter/HiddenCharacter"));
            exhibit.transform.position=Vector3.zero;game.view.transform.position=new Vector3(1.7f,2.3f,5);game.view.transform.LookAt(Vector3.up*.95f);game.view.orthographicSize=1.42f;
            Capture("hidden-model-front.png");float minimum=1,maximum=0;
            for(int frame=0;frame<6;frame++){
                float brightness=Capture("question-pulse-"+frame+".png");minimum=Mathf.Min(minimum,brightness);maximum=Mathf.Max(maximum,brightness);
                yield return new WaitForSecondsRealtime(.4f);
            }
            Require(maximum-minimum<.02f&&minimum>.95f,"question is still breathing");
            File.WriteAllText(Path.Combine(output,"question-static-check.json"),"{\"passed\":true,\"breathingRemoved\":true,\"minimumRenderedBrightness\":"+minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"maximumRenderedBrightness\":"+maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            exhibit.transform.rotation=Quaternion.Euler(0,180,0);Capture("hidden-model-back.png");DestroyImmediate(exhibit);game.scene.root.SetActive(true);
            game.view.transform.SetPositionAndRotation(oldPos,oldRot);game.view.orthographicSize=oldSize;
            var action=game.board.Level.solution[0];Require(game.RequestMove(action.a,action.b),"reveal move");
            Require(game.scene.HasReveal,"no reveal transition");var changing=game.scene.people.First(p=>p.RevealAnimating);Require(changing.RevealTint==0,"reveal flashed color");
            Capture("reveal-start.png");game.Advance(.22f);Require(changing.RevealTint>0&&changing.RevealTint<1,"no intermediate color");Capture("reveal-middle.png");
            game.OpenSettings();float paused=changing.RevealTint;game.Advance(1);Require(changing.RevealTint==paused,"reveal ignored pause");game.CloseSettings();yield return null;yield return null;yield return null;
            game.Undo();Ready();Require(Check()==initial&&!game.scene.HasReveal,"undo during reveal");
            Require(game.RequestMove(action.a,action.b),"retry reveal");Ready();Require(!game.scene.HasReveal,"reveal never finished");Require(Check()<initial,"next row did not reveal");Capture("after-reveal.png");
            game.Undo();Ready();Require(Check()==initial,"undo did not restore black model");game.ResetLevel();Ready();Require(Check()==initial,"reset changed hidden model");
            foreach(var a in game.board.Level.solution){Require(game.RequestMove(a.a,a.b),"solution");Ready();Check();}
            Require(game.board.Solved&&Check()==0,"finished level retains hidden actors");
            File.WriteAllText(Path.Combine(output,"hidden-art-check.json"),"{\"passed\":true,\"independentModel\":true,\"revealUndoReset\":true,\"sharedMeshMaterial\":true,\"renderersPerHiddenActor\":1,\"vertices\":"+mesh.vertexCount+",\"triangles\":"+mesh.triangles.Length/3+",\"meshResourceBytes\":"+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh)+",\"wechatDeviceTested\":false}");
            Active=false;Application.Quit(0);
        }
    }
}
