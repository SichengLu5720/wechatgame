using UnityEngine;

namespace StairsCrowd.Runtime
{
    // One coordinate contract: GUI rectangles are top-left, pixels bottom-left.
    public readonly struct GameplayLayout
    {
        public readonly float Scale, Width, Height;
        public readonly Rect Safe, Gear, Title, Props, Play, Hint;
        public Rect PlayPixels => new Rect(Play.x*Scale,(Height-Play.yMax)*Scale,Play.width*Scale,Play.height*Scale);
        public GameplayLayout(float width,float height,Rect safePixels,Rect capsulePixels,bool reserveSystem)
        {
            Scale=Mathf.Max(.01f,Mathf.Min(width/600f,height/900f));Width=width/Scale;Height=height/Scale;
            if(!Valid(safePixels,width,height))safePixels=new Rect(0,0,width,height);
            Safe=new Rect(safePixels.x/Scale,(height-safePixels.yMax)/Scale,safePixels.width/Scale,safePixels.height/Scale);
            float systemBottom=Safe.yMin;
            if(Valid(capsulePixels,width,height))systemBottom=Mathf.Max(systemBottom,(height-capsulePixels.yMin)/Scale);
            else if(reserveSystem)systemBottom+=64;
            float top=systemBottom+16,margin=20;
            Gear=new Rect(Safe.xMin+margin,top,52,52);
            float titleWidth=Mathf.Min(230,Safe.width-2*(margin+64));
            Title=new Rect(Safe.center.x-titleWidth/2,top, titleWidth,54);
            Props=new Rect(Safe.xMin+margin,Safe.yMax-128,Safe.width-2*margin,112);
            Hint=new Rect(Safe.xMin+margin,Title.yMax+8,Safe.width-2*margin,48);
            Play=new Rect(Safe.xMin+margin,Hint.yMax+8,Safe.width-2*margin,Mathf.Max(1,Props.yMin-64-Hint.yMax-8));
        }
        public static bool Valid(Rect r,float width,float height)=>!float.IsNaN(r.x)&&!float.IsNaN(r.y)&&!float.IsNaN(r.width)&&!float.IsNaN(r.height)&&r.width>0&&r.height>0&&r.xMin>=0&&r.yMin>=0&&r.xMax<=width+.1f&&r.yMax<=height+.1f;
        public static GameplayLayout Current => new GameplayLayout(Screen.width,Screen.height,WeChatPlatform.DisplaySafeArea,WeChatPlatform.DisplayCapsule,WeChatPlatform.ReserveSystemArea);
    }
}
