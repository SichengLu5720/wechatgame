using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        float tutorialExit=-1;
        Vector3 tutorialOrigin;
        public bool IsTutorial => !customPlaying&&CampaignRepository.IsTutorial(board?.Level);
        public bool TutorialExiting => tutorialExit>=0;
        public bool SeamlessTutorial => IsTutorial&&levelIndex<3&&!Home&&!editing;
        void CancelTutorialExit()
        {
            if(TutorialExiting&&scene!=null)scene.root.transform.position=tutorialOrigin;
            tutorialExit=-1;
        }
        bool AdvanceTutorialExit(float delta)
        {
            if(!SeamlessTutorial)return false;
            if(!TutorialExiting){
                // Logical completion happens at click time. Wait for every actor to arrive.
                if(!board.Solved||motion!=null||IsAssembling)return false;
                tutorialExit=0;tutorialOrigin=scene.root.transform.position;
                selected=-1;scene.Highlight(board,-1);
                foreach(var surface in scene.surfaces)surface.enabled=false;
            }
            tutorialExit+=Mathf.Max(0,delta);
            float t=Mathf.Clamp01(tutorialExit/.7f);
            scene.root.transform.position=tutorialOrigin+Vector3.down*(40*t*t*(3-2*t));
            if(t>=1){int next=levelIndex+1;LoadLevel(next);}
            return true;
        }
        void DrawTutorialHint(float w,float h)
        {
            if(!IsTutorial||levelIndex!=0||IsAssembling||Home||PropSelection!=0)return;
            string text=TutorialExiting?"正在收纳，下一关即将升起…":board.Level.tip;
            var hint=Style(17,FontStyle.Normal,TextAnchor.MiddleCenter);
            GUI.Label(new Rect(28,112,w-56,60),text,hint);
            if(levelIndex!=0||board.Solved||motion!=null)return;
            var first=board.Level.solution[0];int node=selected==first.a?first.b:first.a;
            var point=view.WorldToScreenPoint(space.Centers[node]+Vector3.up*2.8f);
            float scale=Screen.width/w;
            var rect=new Rect(Mathf.Clamp(point.x/scale-82,8,w-172),Mathf.Clamp((Screen.height-point.y)/scale-18,185,h-190),164,36);
            Fill(rect,new Color(.96f,.93f,.85f,.95f));
            GUI.Label(rect,selected==first.a?"② 点击集结点":"① 点击红色人群",Style(16,FontStyle.Bold,TextAnchor.MiddleCenter));
        }
    }
}
