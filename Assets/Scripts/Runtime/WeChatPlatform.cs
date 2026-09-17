using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class WeChatPlatform
    {
        public static event Action<bool> BackgroundChanged;
        static void NotifyBackground(bool hidden){BackgroundChanged?.Invoke(hidden);}
        public static void CopyText(string text,Action done,Action<string> fail)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            try{WeChatWASM.WX.SetClipboardData(new WeChatWASM.SetClipboardDataOption{data=text,success=_=>done(),fail=_=>fail("复制失败，请长按分享码手动复制")});}catch(Exception){fail("暂时无法复制");}
#else
            GUIUtility.systemCopyBuffer=text;done();
#endif
        }
        public static void PasteText(Action<string> ready,Action<string> fail)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            try{WeChatWASM.WX.GetClipboardData(new WeChatWASM.GetClipboardDataOption{success=result=>ready(result.data),fail=_=>fail("无法读取剪贴板，请手动粘贴")});}catch(Exception){fail("暂时无法粘贴");}
#else
            ready(GUIUtility.systemCopyBuffer);
#endif
        }
        public static void PulseLight(){Pulse("light");}
        public static void Pulse(string strength)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            try{WeChatWASM.WX.VibrateShort(new WeChatWASM.VibrateShortOption{type=strength});}catch(Exception){/* Haptics are optional on unsupported devices. */}
#endif
        }
        public static void Initialize(Action ready)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && WECHAT_MINIGAME
            WeChatWASM.WX.InitSDK(code =>
            {
                Debug.Log("WeChat SDK initialized: " + code);
                WeChatWASM.WX.OnHide(_=>NotifyBackground(true));
                WeChatWASM.WX.OnShow(_=>NotifyBackground(false));
                try{
                    var info=WeChatWASM.WX.GetWindowInfo();
                    if(info.pixelRatio>2)WeChatWASM.WX.SetDevicePixelRatio(2);
                    WeChatWASM.WX.SetPreferredFramesPerSecond(60);
                }catch(Exception error){Debug.LogWarning("Mobile display settings: "+error.Message);}
                ready();
            });
#else
            ready();
#endif
        }
    }
}
