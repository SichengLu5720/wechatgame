using System;
using System.Linq;
using System.Collections.Generic;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        readonly List<int> introKinds=new List<int>();int introPage;RenderTexture introTexture;GameObject introModel;Camera introCamera;
        public bool IntroVisible {get{return !Home&&!editing&&!IsAssembling&&introPage<introKinds.Count;}}
        static readonly string[] MechanismNames={"帘幕","封印基座","滞留花园","共鸣祭坛"};
        static readonly string[] MechanismRules={"深色人群只隐藏颜色。移走最外排后，下一排立即揭晓。","每完成一个集结点，数字减一。归零后，这里的平台才能通行。","站在这里的人无法搬出。集满 16 个同色人，解除束缚。","任何颜色均可停留。只有指定颜色集满 16 人才能完成。"};
        void ConfigureIntro()
        {
            DisposeIntroPreview();introKinds.Clear();introPage=0;if(Home||board.Level.nodes.Length==0||IsTutorial||(!customPlaying&&levelIndex>0))return;
            if(board.Level.groups.Any(g=>g.hidden)||board.Level.nodes.Any(n=>n.curtain))introKinds.Add(0);
            if(board.Level.nodes.Any(n=>n.unlockAfter>0))introKinds.Add(1);
            if(board.Level.nodes.Any(n=>n.sticky))introKinds.Add(2);
            if(board.Level.nodes.Any(n=>n.colorRestricted))introKinds.Add(3);
            if(introKinds.Count==0)introKinds.Add(-1);
        }
        public void ContinueIntro(){if(!IntroVisible)return;introPage++;DisposeIntroPreview();}
        void DisposeIntroPreview()
        {
            if(introCamera)Destroy(introCamera.gameObject);if(introModel)Destroy(introModel);if(introTexture){introTexture.Release();Destroy(introTexture);}introCamera=null;introModel=null;introTexture=null;
        }
        void PrepareIntroPreview(int kind)
        {
            if(introTexture)return;int node=0;
            for(int i=0;i<board.Level.nodes.Length;i++){var n=board.Level.nodes[i];if(kind==0&&n.queue.Any(id=>board.Level.groups[id].hidden)||kind==1&&n.unlockAfter>0||kind==2&&n.sticky||kind==3&&n.colorRestricted){node=i;break;}}
            introModel=new GameObject("Mechanism exhibit");introModel.transform.position=new Vector3(1000,0,0);
            var platform=Instantiate(scene.NodeRoot(node).gameObject,introModel.transform);platform.transform.localPosition=Vector3.zero;
            // The exhibit uses the game's actual platform and crowd assets.
            for(int row=0;row<board.Current.queues[node].Count;row++)for(int m=0;m<4;m++){
                int id=board.Current.queues[node][row]*4+m;if(id>=scene.people.Length)continue;
                var person=Instantiate(scene.people[id].root.gameObject,introModel.transform);person.transform.localPosition=space.SeatFor(board.Current,node,row,m)-space.Centers[node];person.transform.localScale=Vector3.one;
            }
            foreach(var transform in introModel.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=10;
            foreach(var col in introModel.GetComponentsInChildren<Collider>(true))col.enabled=false;
            introCamera=new GameObject("Mechanism camera",typeof(Camera)).GetComponent<Camera>();introCamera.enabled=false;introCamera.cullingMask=1<<10;view.cullingMask&=~(1<<10);
            introCamera.clearFlags=CameraClearFlags.SolidColor;introCamera.backgroundColor=new Color(0,0,0,0);introCamera.orthographic=true;introCamera.orthographicSize=4.5f/CrowdScene.PresentationScale;introCamera.nearClipPlane=.1f;introCamera.farClipPlane=40;
            var target=introModel.transform.position+Vector3.down*.4f;introCamera.transform.position=target+new Vector3(7,11,-12);introCamera.transform.LookAt(target);
            introTexture=new RenderTexture(512,512,16,RenderTextureFormat.ARGB32);introCamera.targetTexture=introTexture;introCamera.Render();
        }
        bool SafeToRetreat(int node)
        {
            if(motion==null)return true;
            foreach(var track in motion.tracks){if(track.End<=clock||scene.people[track.actor].tag.node==node)continue;
                for(int i=1;i<track.points.Length;i++)if(track.start+track.times[i]>clock){var p=track.points[i];foreach(var stair in space.Stairs){if(stair.a!=node&&stair.b!=node)continue;float y;if(stair.Height(new Vector2(p.x,p.z),out y))return false;}}
            }return true;
        }
        void DrawCloudGUI()
        {
            if(board==null)return;InitStyles();drawingModal=false;GUI.skin.font=font;
            float scale=Mathf.Min(Screen.width/600f,Screen.height/900f),w=Screen.width/scale,h=Screen.height/scale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            if(settingsOpen){DrawSettings(w,h);GUI.matrix=Matrix4x4.identity;return;}
            if(editing){DrawEditor(w,h);GUI.matrix=Matrix4x4.identity;return;}
            if(Home){
                DrawHomeEditorButton(scale);SettingsGear(w,Mathf.Max(64,(Screen.height-Screen.safeArea.yMax)/scale+12));
                if(startHero){float heroW=Mathf.Min(w-56,320),heroH=heroW*startHero.height/startHero.width,maxH=h*.18f;if(heroH>maxH){heroH=maxH;heroW=heroH*startHero.width/startHero.height;}GUI.DrawTexture(new Rect((w-heroW)/2,h*.025f,heroW,heroH),startHero,ScaleMode.ScaleToFit,true);}
                if(Button(new Rect(w/2-110,h*.82f,220,52),"开始",true,new Color(.68f,.78f,.69f)))StartGame();
                GUI.matrix=Matrix4x4.identity;return;
            }
            DrawPropHUD(w,h);
            DrawTutorialHint(w,h);
            if(board.Solved&&!Busy&&!IntroVisible&&!SeamlessTutorial){Fill(new Rect(w/2-200,h*.36f,400,180),new Color(.96f,.93f,.85f));GUI.Label(new Rect(w/2-170,h*.36f+20,340,45),"集合完成",title);GUI.Label(new Rect(w/2-170,h*.36f+68,340,30),"用了 "+board.Moves+" 步",body);
                if(Button(new Rect(w/2-170,h*.36f+117,340,44),customPlaying?"返回编辑":"下一关",true,new Color(.68f,.8f,.7f))){if(customPlaying)OpenEditor();else LoadLevel((levelIndex+1)%builtInCount);}}
            if(IntroVisible){
                int kind=introKinds[introPage];PrepareIntroPreview(kind);Fill(new Rect(0,0,w,h),new Color(.12f,.19f,.22f,.70f));
                float panelW=Mathf.Min(w-44,480),left=(w-panelW)/2,top=h*.15f,panelH=Mathf.Min(h*.7f,650);Fill(new Rect(left,top,panelW,panelH),new Color(.96f,.93f,.86f));
                var center=Style(27,FontStyle.Bold,TextAnchor.MiddleCenter);var textCenter=Style(17,FontStyle.Normal,TextAnchor.MiddleCenter);
                string caption=kind>=0&&levelIndex==kind&&!customPlaying?"新机制":"本关机制";
                GUI.Label(new Rect(left+24,top+18,panelW-48,28),caption,Style(14,FontStyle.Normal,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(left+24,top+52,panelW-48,46),kind<0?"同色集合":MechanismNames[kind],center);
                float picture=Mathf.Min(panelW-70,panelH-245);GUI.DrawTexture(new Rect(w/2-picture/2,top+104,picture,picture),introTexture,ScaleMode.ScaleToFit,true);
                GUI.Label(new Rect(left+30,top+panelH-142,panelW-60,64),kind<0?"集结点每次移出一排。落脚点同色人群整批移动，集满 16 人完成。":MechanismRules[kind],textCenter);
                if(Button(new Rect(left+32,top+panelH-62,panelW-64,43),introPage+1<introKinds.Count?"继续  "+(introPage+1)+" / "+introKinds.Count:"继续",true,new Color(.68f,.79f,.7f)))ContinueIntro();
            }
            GUI.matrix=Matrix4x4.identity;if(!IntroVisible&&!IsAssembling)scene.ShowMechanismLabels(board.Current,font);
        }
    }
}

