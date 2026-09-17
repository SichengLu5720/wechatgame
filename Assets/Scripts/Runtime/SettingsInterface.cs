using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        bool settingsOpen;int settingsConfirm,suppressInputThrough=-1;IslandAudio islandAudio;
        public bool SettingsOpen {get{return settingsOpen;}}
        public bool InterfaceBlocksInput {get{return DailyCalendarOpen||(DailyActive&&DailyClock.Result!=StairsCrowd.Core.DailyResult.Pending)||FailureLocked||PreparingMove||settingsOpen||Time.frameCount<=suppressInputThrough;}}
        static readonly Color UiPaper=new Color(.96f,.93f,.86f),UiInk=new Color(.28f,.36f,.35f),UiSage=new Color(.43f,.61f,.54f),UiMuted=new Color(.70f,.72f,.66f);
        Texture2D uiCircle;
        public void OpenSettings(){if(FailureLocked||DailyCalendarOpen||(DailyActive&&DailyClock.Result!=StairsCrowd.Core.DailyResult.Pending))return;PauseDaily(StairsCrowd.Core.DailyPause.Settings,true);if(FailureLocked)return;CancelPropSelection();CancelViewPointer();settingsConfirm=0;settingsOpen=true;if(Feedback!=null)Feedback.Cancel();}
        public void CloseSettings(){PauseDaily(StairsCrowd.Core.DailyPause.Settings,false);settingsOpen=false;settingsConfirm=0;CancelViewPointer();suppressInputThrough=Time.frameCount+1;}
        void Circle(Rect rect,Color color)
        {
            if(!uiCircle){uiCircle=new Texture2D(96,96,TextureFormat.RGBA32,false);uiCircle.wrapMode=TextureWrapMode.Clamp;var pixels=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48));pixels[y*96+x]=new Color(1,1,1,Mathf.Clamp01(48-d));}uiCircle.SetPixels(pixels);uiCircle.Apply();}
            GUI.color=color;GUI.DrawTexture(rect,uiCircle);GUI.color=Color.white;
        }
        void Round(Rect r,float radius,Color color){radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)/2);Fill(new Rect(r.x+radius,r.y,r.width-2*radius,r.height),color);Fill(new Rect(r.x,r.y+radius,r.width,r.height-2*radius),color);Circle(new Rect(r.x,r.y,radius*2,radius*2),color);Circle(new Rect(r.xMax-radius*2,r.y,radius*2,radius*2),color);Circle(new Rect(r.x,r.yMax-radius*2,radius*2,radius*2),color);Circle(new Rect(r.xMax-radius*2,r.yMax-radius*2,radius*2,radius*2),color);}
        void Stroke(Vector2 a,Vector2 b,float width,Color color){var saved=GUI.matrix;var d=b-a;GUI.matrix=saved*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width/2,d.magnitude,width),color);GUI.matrix=saved;Circle(new Rect(a.x-width/2,a.y-width/2,width,width),color);Circle(new Rect(b.x-width/2,b.y-width/2,width,width),color);}
        void Arrow(Vector2 from,Vector2 to,Color color,float width=3){Stroke(from,to,width,color);var d=(from-to).normalized;var side=new Vector2(-d.y,d.x);Stroke(to,to+d*9+side*6,width,color);Stroke(to,to+d*9-side*6,width,color);}
        void Icon(Rect r,int kind,Color color)
        {
            Vector2 c=r.center;float s=r.width/52f;
            if(kind==0){Vector2 last=c+new Vector2(14,-12)*s;for(int i=1;i<=24;i++){float a=(-40+i*250f/24)*Mathf.Deg2Rad;var p=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*18*s;Stroke(last,p,3*s,color);last=p;}Arrow(c+new Vector2(-6,-14)*s,c+new Vector2(-19,-13)*s,color,3*s);}
            if(kind==1){Arrow(c+new Vector2(-21,15)*s,c+new Vector2(20,-15)*s,color,3*s);Stroke(c+new Vector2(-21,-15)*s,c+new Vector2(-5,-4)*s,3*s,color);Arrow(c+new Vector2(5,4)*s,c+new Vector2(20,15)*s,color,3*s);}
            if(kind==2){var a=c+new Vector2(-20,4)*s;var b=c+new Vector2(1,-5)*s;var d=c+new Vector2(21,4)*s;var e=c+new Vector2(0,14)*s;Stroke(a,b,3*s,color);Stroke(b,d,3*s,color);Stroke(d,e,3*s,color);Stroke(e,a,3*s,color);Stroke(a,a+Vector2.up*7*s,3*s,color);Stroke(e,e+Vector2.up*7*s,3*s,color);Stroke(d,d+Vector2.up*7*s,3*s,color);Stroke(a+Vector2.up*7*s,e+Vector2.up*7*s,3*s,color);Stroke(e+Vector2.up*7*s,d+Vector2.up*7*s,3*s,color);Stroke(c+new Vector2(3,-21)*s,c+new Vector2(3,-9)*s,3*s,color);Stroke(c+new Vector2(-3,-15)*s,c+new Vector2(9,-15)*s,3*s,color);}
            if(kind==3){for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Stroke(c+d*14*s,c+d*21*s,5*s,color);}Circle(new Rect(c.x-17*s,c.y-17*s,34*s,34*s),color);Circle(new Rect(c.x-10*s,c.y-10*s,20*s,20*s),new Color(.16f,.23f,.29f));}
            if(kind==4){Stroke(c+new Vector2(-12,-4)*s,c+new Vector2(-3,-4)*s,3*s,color);Stroke(c+new Vector2(-3,-4)*s,c+new Vector2(8,-14)*s,3*s,color);Stroke(c+new Vector2(8,-14)*s,c+new Vector2(8,14)*s,3*s,color);Stroke(c+new Vector2(8,14)*s,c+new Vector2(-3,4)*s,3*s,color);Stroke(c+new Vector2(-3,4)*s,c+new Vector2(-12,4)*s,3*s,color);Stroke(c+new Vector2(-12,4)*s,c+new Vector2(-12,-4)*s,3*s,color);Stroke(c+new Vector2(16,-7)*s,c+new Vector2(19,0)*s,3*s,color);Stroke(c+new Vector2(19,0)*s,c+new Vector2(16,7)*s,3*s,color);}
            if(kind==5){Round(new Rect(c.x-9*s,c.y-19*s,18*s,38*s),3*s,color);Round(new Rect(c.x-6*s,c.y-15*s,12*s,28*s),1*s,UiPaper);Stroke(c+new Vector2(-17,-9)*s,c+new Vector2(-20,0)*s,2*s,color);Stroke(c+new Vector2(-20,0)*s,c+new Vector2(-17,9)*s,2*s,color);Stroke(c+new Vector2(17,-9)*s,c+new Vector2(20,0)*s,2*s,color);Stroke(c+new Vector2(20,0)*s,c+new Vector2(17,9)*s,2*s,color);}
            if(kind==6){Stroke(c+new Vector2(-5,11)*s,c+new Vector2(-5,-14)*s,3*s,color);Stroke(c+new Vector2(-5,-14)*s,c+new Vector2(14,-18)*s,3*s,color);Stroke(c+new Vector2(14,-18)*s,c+new Vector2(14,7)*s,3*s,color);Circle(new Rect(c.x-17*s,c.y+5*s,13*s,10*s),color);Circle(new Rect(c.x+2*s,c.y+2*s,13*s,10*s),color);}
        }
        bool RoundButton(Rect r,string text,Color color){Round(new Rect(r.x,r.y+3,r.width,r.height),14,new Color(.25f,.3f,.25f,.13f));Round(r,14,color);bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.Label(r,text,button);return hit;}
        void SettingsGear(float w,float top){var r=new Rect(w-76,top,52,52);Circle(new Rect(r.x,r.y+3,r.width,r.height),new Color(.2f,.3f,.25f,.12f));Circle(r,new Color(.16f,.23f,.29f));Icon(new Rect(r.x+9,r.y+9,34,34),3,UiPaper);if(!FailureLocked&&GUI.Button(r,GUIContent.none,GUIStyle.none))OpenSettings();}
        void SettingRow(Rect r,string label,int icon,bool value,System.Action<bool> change)
        {
            Icon(new Rect(r.x+20,r.y+20,40,40),icon,UiInk);GUI.Label(new Rect(r.x+82,r.y,r.width-190,r.height),label,Style(23,FontStyle.Bold));
            var toggle=new Rect(r.xMax-100,r.y+24,72,38);Round(toggle,19,value?UiSage:UiMuted);Circle(new Rect(toggle.x+(value?37:4),toggle.y+4,30,30),UiPaper);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)){change(!value);if(islandAudio){islandAudio.Apply();islandAudio.Play();}if(Feedback!=null)Feedback.Cancel();}
        }
        void DrawSettings(float w,float h)
        {
            Fill(new Rect(0,0,w,h),new Color(.93f,.90f,.83f));float top=Mathf.Max(30,(Screen.height-Screen.safeArea.yMax)/(Screen.width/w)+18);
            var back=new Rect(22,top,64,54);Arrow(new Vector2(67,top+27),new Vector2(39,top+27),UiInk);if(GUI.Button(back,GUIContent.none,GUIStyle.none))CloseSettings();
            GUI.Label(new Rect(94,top,w-120,54),"设置",Style(30,FontStyle.Bold));
            float cw=Mathf.Min(w-48,520),left=(w-cw)/2,y=top+94;
            Round(new Rect(left,y+4,cw,264),20,new Color(.2f,.3f,.25f,.09f));Round(new Rect(left,y,cw,264),20,UiPaper);
            SettingRow(new Rect(left,y+6,cw,84),"音效",4,GameSettings.Sound,v=>GameSettings.Sound=v);
            SettingRow(new Rect(left,y+90,cw,84),"震动",5,GameSettings.Vibration,v=>GameSettings.Vibration=v);
            SettingRow(new Rect(left,y+174,cw,84),"音乐",6,GameSettings.Music,v=>GameSettings.Music=v);
            GUI.Label(new Rect(left+14,y+279,cw-28,32),"偏好设置会自动保存在此设备",Style(14));
            if(!Home){
                y+=332;float bw=(cw-14)/2;
                if(RoundButton(new Rect(left,y,bw,52),"重新开始",UiPaper))settingsConfirm=1;
                if(RoundButton(new Rect(left+bw+14,y,bw,52),customPlaying?"返回编辑":"返回主页",UiPaper))settingsConfirm=2;
                if(settingsConfirm!=0){y+=72;Round(new Rect(left,y,cw,132),18,UiPaper);GUI.Label(new Rect(left+18,y+8,cw-36,43),settingsConfirm==1?"重开本关？道具将恢复各一次。":"离开当前关卡？本局进度不会保留。",Style(17));
                    if(RoundButton(new Rect(left+16,y+65,bw-10,45),"取消",new Color(.87f,.86f,.80f)))settingsConfirm=0;
                    if(RoundButton(new Rect(left+bw+4,y+65,bw-10,45),"确认",new Color(.69f,.80f,.72f))){int action=settingsConfirm;CloseSettings();if(action==1)ResetLevel();else if(customPlaying)OpenEditor();else ReturnHome();}
                }
            }
        }
        void DrawPropButton(Rect r,int kind,string label,int remaining,bool enabled,System.Action action)
        {
            Circle(new Rect(r.x,r.y+5,r.width,r.height),new Color(.26f,.34f,.3f,.15f));Circle(r,enabled?new Color(.60f,.76f,.70f):new Color(.28f,.34f,.39f));
            Circle(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),new Color(.12f,.19f,.26f,.94f));Icon(new Rect(r.center.x-24,r.center.y-25,48,48),kind,enabled?UiPaper:UiMuted);
            var badge=new Rect(r.xMax-24,r.yMax-28,28,28);Circle(badge,remaining>0?UiSage:UiMuted);GUI.Label(badge,remaining.ToString(),new GUIStyle(Style(15,FontStyle.Bold,TextAnchor.MiddleCenter)){normal={textColor=Color.white}});
            GUI.Label(new Rect(r.x-20,r.yMax+8,r.width+40,24),label,NightStyle(15,FontStyle.Bold,TextAnchor.MiddleCenter));
            if(enabled&&GUI.Button(r,GUIContent.none,GUIStyle.none)){CancelViewPointer();action();}
        }
        void DrawPropHUD(float w,float h)
        {
            // Floating controls: no opaque footer or shared tray.
            float scale=Screen.width/w,top=Mathf.Max(16,(Screen.height-Screen.safeArea.yMax)/scale+12),bottomInset=Mathf.Max(14,Screen.safeArea.y/scale+8);
            Round(new Rect(w/2-91,top+3,182,54),18,new Color(.13f,.21f,.28f,.85f));GUI.Label(new Rect(w/2-86,top+3,172,54),DailyActive?"每日挑战  "+DailyTimeText:customPlaying?"自由关卡":"第 "+(levelIndex+1)+" 关",NightStyle(DailyActive?19:24,FontStyle.Bold,TextAnchor.MiddleCenter));SettingsGear(w,top+3);
            GUI.Label(new Rect(24,top+12,100,32),"步数 "+board.Moves,NightStyle(17));
            float y=h-bottomInset-112;
            bool ready=PropReady();float cx=w/2;
            DrawPropButton(new Rect(cx-93,y,76,76),0,"撤销",UndoRemaining,ready&&PropSelection==0&&board.CanUndo&&UndoRemaining>0,()=>UseUndoProp());
            DrawPropButton(new Rect(cx+17,y,76,76),1,"洗混",ShuffleRemaining,ready&&ShuffleRemaining>0,()=>BeginPropSelection(1));
            if(PropSelection!=0){GUI.Label(new Rect(24,top+72,w-48,42),"选择要洗混的平台",NightStyle(24,FontStyle.Bold,TextAnchor.MiddleCenter));if(RoundButton(new Rect(w/2-70,y-110,140,40),"取消",UiPaper))CancelPropSelection();}
            if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(24,y-54,w-48,40),message,NightStyle(16,FontStyle.Normal,TextAnchor.MiddleCenter));
        }
    }
}
