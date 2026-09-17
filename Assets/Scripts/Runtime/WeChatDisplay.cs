using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static partial class WeChatPlatform
    {
        static Rect normalizedSafe,normalizedCapsule;
        static bool hasSafe,hasCapsule;
        public static int DisplayRevision {get;private set;}
        public static bool ReserveSystemArea {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
                return true;
#else
                return false;
#endif
            }
        }
        static Rect ScreenRect(Rect r)=>new Rect(r.x*Screen.width,r.y*Screen.height,r.width*Screen.width,r.height*Screen.height);
        public static Rect DisplaySafeArea=>hasSafe?ScreenRect(normalizedSafe):Screen.safeArea;
        public static Rect DisplayCapsule=>hasCapsule?ScreenRect(normalizedCapsule):default;
        // The SDK reports CSS/window pixels. Normalize once; Unity may cap DPR at two.
        public static Rect WindowRect(float left,float top,float width,float height,float windowWidth,float windowHeight)=>new Rect(left/windowWidth,1-(top+height)/windowHeight,width/windowWidth,height/windowHeight);
        public static void RefreshDisplay()
        {
            hasSafe=hasCapsule=false;
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            try {
                var info=WeChatWASM.WX.GetWindowInfo();
                float w=(float)info.windowWidth,h=(float)info.windowHeight;
                if(w>0&&h>0){
                    var s=info.safeArea;
                    if(s!=null){normalizedSafe=WindowRect((float)s.left,(float)s.top-(float)info.screenTop,(float)s.width,(float)s.height,w,h);hasSafe=GameplayLayout.Valid(normalizedSafe,1,1);}
                    var c=WeChatWASM.WX.GetMenuButtonBoundingClientRect();
                    if(c!=null){normalizedCapsule=WindowRect((float)c.left,(float)c.top,(float)c.width,(float)c.height,w,h);hasCapsule=GameplayLayout.Valid(normalizedCapsule,1,1);}
                }
            }catch(Exception e){Debug.LogWarning("Display geometry unavailable; using safe-area fallback: "+e.Message);}
#endif
            DisplayRevision++;
        }
    }
}
