using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // The bridge carries only the current player's score and presentation commands.
    // No friend data callback exists in the Unity domain.
    public sealed class WeChatLeaderboard:IDisposable
    {
        [Serializable] sealed class Message {public string type="islands-friends-v1",command;public FriendLeaderboardRecord record;public float width,height;}
        Texture2D texture;Rect pixels;int revision=-1;bool showing;
        FriendLeaderboardRecord current;
        public static bool Available {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
                return true;
#else
                return false;
#endif
            }
        }
        public bool Failed {get;private set;}
        public Texture Texture=>texture;
        void Send(Message message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            WeChatWASM.WX.GetOpenDataContext().PostMessage(JsonUtility.ToJson(message));
#endif
        }
        public void Sync(FriendLeaderboardRecord record)
        {if(!Available||record==null)return;try{Send(new Message{command="sync",record=record});}catch(Exception){Failed=true;}}
        public void Open(FriendLeaderboardRecord record)
        {current=record;Failed=false;try{Send(new Message{command="open",record=record});}catch(Exception){Failed=true;}}
        public void Layout(Rect rect,float scale)
        {
            if(!Available||Failed)return;
            var next=new Rect(Mathf.Round(rect.x*scale),Mathf.Round(rect.y*scale),Mathf.Round(rect.width*scale),Mathf.Round(rect.height*scale));
            if(showing&&next==pixels&&revision==WeChatPlatform.DisplayRevision)return;
            try {
                bool reopen=showing;Hide();pixels=next;revision=WeChatPlatform.DisplayRevision;
                if(!texture)texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
                WeChatWASM.WX.GetOpenDataContext();
                WeChatWASM.WX.ShowOpenData(texture,(int)pixels.x,(int)pixels.y,(int)pixels.width,(int)pixels.height);
#endif
                showing=true;Send(new Message{command="layout",width=rect.width,height=rect.height});
                if(reopen)Open(current);
            }catch(Exception){Failed=true;}
        }
        public void Hide()
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            if(showing)try{WeChatWASM.WX.HideOpenData();}catch(Exception){Failed=true;}
#endif
            showing=false;
        }
        public void Dispose(){Hide();if(texture)UnityEngine.Object.Destroy(texture);texture=null;}
    }
}
