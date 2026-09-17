using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        public bool TuningOpen {get;set;}
        bool tuningCaptured,tuningTouchBlocked;int tuningControl=-1;
        int tuningLabelsRevision=-1,tuningLabelsSaved=-1;readonly string[] tuningLabels=new string[6];
        static readonly string[] tuningNumbers=MakeTuningNumbers();
        static string[] MakeTuningNumbers(){if(!RuntimeTuning.Available)return System.Array.Empty<string>();var result=new string[2001];for(int i=0;i<result.Length;i++)result[i]=(i/100f).ToString("F2")+"×";return result;}
        static string TuningNumber(float value)=>tuningNumbers[Mathf.Clamp(Mathf.RoundToInt(value*100),0,2000)];
        bool TuningVisible=>RuntimeTuning.Available&&board!=null&&!Home&&!editing&&!chooseLevel&&!shareOpen&&!libraryOpen&&!InterfaceBlocksInput&&!IntroVisible&&!DailyCalendarOpen&&!(board.Solved&&!Busy);
        public static Rect TuningRect(GameplayLayout layout,bool open)
        {
            float scale=Mathf.Max(1,Mathf.Min(layout.Safe.width*layout.Scale/390f,layout.Height*layout.Scale/844f));
            float width=Mathf.Min(370*scale,layout.Safe.width*layout.Scale-20),height=272*width/370;
            float right=layout.Safe.xMax*layout.Scale-10,top=layout.Title.yMax*layout.Scale+12;
            return open?new Rect(right-width,top,width,height):new Rect(right-68*scale,top,68*scale,44*scale);
        }
        public bool TuningContains(Vector2 screen)=>TuningVisible&&TuningRect(GameplayLayout.Current,TuningOpen).Contains(new Vector2(screen.x,Screen.height-screen.y));
        void ProcessTuningGUIEvent(){var e=Event.current;int touches=Input.touchCount;bool canceled=false;for(int i=0;i<touches;i++)if(Input.GetTouch(i).phase==TouchPhase.Canceled)canceled=true;if(ProcessTuningPointer(e.type,e.mousePosition,touches,canceled)&&(e.isMouse||e.type==EventType.ScrollWheel))e.Use();}
        // Both IMGUI and verification use this gesture state machine. Positions are GUI pixels.
        public bool ProcessTuningPointer(EventType type,Vector2 point,int touches=0,bool canceled=false)
        {
            if(!TuningVisible){CancelTuningGesture();return false;}
            var panel=TuningRect(GameplayLayout.Current,TuningOpen);bool inside=panel.Contains(point),consume=inside||tuningCaptured;
            if(touches>1||canceled){tuningTouchBlocked=true;tuningControl=-1;return consume;}
            // Unity can promote a remaining finger to its mouse pointer. Do not
            // treat that MouseDown as a new gesture after multi-touch/cancellation.
            if(tuningTouchBlocked){if(touches==0)CancelTuningGesture();return consume;}
            float unit=panel.width/(TuningOpen?370:68);var local=(point-panel.position)/unit;
            if(type==EventType.MouseDown&&inside){tuningCaptured=true;tuningControl=ControlAt(local);CancelViewPointer();if(tuningControl>=10)DragTuningSlider(tuningControl,local.x);}
            if(tuningCaptured&&tuningControl>=10&&(type==EventType.MouseDrag||type==EventType.MouseUp))DragTuningSlider(tuningControl,local.x);
            if(type==EventType.MouseUp){int action=tuningCaptured&&tuningControl<10&&inside&&ControlAt(local)==tuningControl?tuningControl:-1;CancelTuningGesture();ActivateTuningControl(action);}
            return consume;
        }
        int ControlAt(Vector2 p)
        {
            if(!TuningOpen)return new Rect(0,0,68,44).Contains(p)?0:-1;
            foreach(int id in tuningControlIds)if(ControlRect(id).Contains(p))return id;return -1;
        }
        static readonly int[] tuningControlIds={1,2,3,4,10,12,14};
        static Rect ControlRect(int id){if(id==1)return new Rect(104,6,78,40);if(id==2)return new Rect(182,6,78,40);if(id==3)return new Rect(308,6,50,40);if(id==4)return new Rect(186,224,172,40);return new Rect(12,72+((id-10)/2)*45,254,42);}
        void DragTuningSlider(int id,float x){float t=Mathf.InverseLerp(24,254,x);TuningChange(id,id==10?(t<=0?.5f:t>=1?20:.5f*Mathf.Pow(40,t)):Mathf.Lerp(.75f,1.25f,t));}
        void ActivateTuningControl(int id)
        {
            if(id<0)return;if(id==0){TuningOpen=true;return;}if(id==3){TuningOpen=false;return;}if(id==1||id==2){RuntimeTuning.SetLinked(id==1);return;}if(id==4){RuntimeTuning.Save();return;}
        }
        bool DrawRuntimeTuning()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(!TuningVisible)return false;InitStyles();
            if(tuningLabelsRevision!=RuntimeTuning.Revision||tuningLabelsSaved!=RuntimeTuning.SaveCount){var w=RuntimeTuning.Working;var s=RuntimeTuning.Saved;tuningLabels[0]=TuningNumber(w.speed);tuningLabels[1]=TuningNumber(w.platform);tuningLabels[2]=TuningNumber(w.person);tuningLabels[3]=RuntimeTuning.HasSaved?TuningNumber(s.speed):"—";tuningLabels[4]=RuntimeTuning.HasSaved?TuningNumber(s.platform):"—";tuningLabels[5]=RuntimeTuning.HasSaved?TuningNumber(s.person):"—";tuningLabelsRevision=RuntimeTuning.Revision;tuningLabelsSaved=RuntimeTuning.SaveCount;}
            var e=Event.current;var panel=TuningRect(GameplayLayout.Current,TuningOpen);var point=e.mousePosition;
            bool inside=panel.Contains(point),consume=tuningCaptured||inside;
            float unit=TuningOpen?panel.width/370:panel.width/68;
            GUI.matrix=Matrix4x4.TRS(new Vector3(panel.x,panel.y,0),Quaternion.identity,Vector3.one*unit);
            var local=(point-panel.position)/unit;
            Round(new Rect(0,0,TuningOpen?370:68,TuningOpen?272:44),14,new Color(.96f,.93f,.85f));
            if(!TuningOpen){DrawTuningButton(new Rect(0,0,68,44),"调试 ‹");}
            else{
                GUI.Label(new Rect(12,8,88,36),"开发调试",button);
                var value=RuntimeTuning.Working;var saved=RuntimeTuning.Saved;
                DrawTuningButton(ControlRect(1),"联动",value.linked);
                DrawTuningButton(ControlRect(2),"独立",!value.linked);
                DrawTuningButton(ControlRect(3),"收起 ›");
                GUI.Label(new Rect(96,48,76,24),"当前",small);GUI.Label(new Rect(280,48,68,24),"已保存",small);
                TuningRow("速度",value.speed,saved.speed,72,10,local,e);
                TuningRow("平台",value.platform,saved.platform,117,12,local,e);
                TuningRow("人物",value.person,saved.person,162,14,local,e);
                GUI.Label(new Rect(12,207,346,22),value.linked?"比例联动 · 移动与动画同步":"独立比例 · 移动与动画同步",small);
                GUI.Label(new Rect(12,233,168,30),!RuntimeTuning.HasSaved?"尚未保存 · 仅本次生效":RuntimeTuning.Dirty?"即时生效 · 未保存":"已保存到此设备",small);
                DrawTuningButton(ControlRect(4),"保存当前参数",true);
            }
            GUI.matrix=Matrix4x4.identity;
            return consume;
#else
            return false;
#endif
        }
        void TuningRow(string label,float value,float saved,float y,int id,Vector2 p,Event e)
        {
            int row=(id-10)/2;GUI.Label(new Rect(14,y-3,65,24),label,body);GUI.Label(new Rect(88,y-3,84,24),tuningLabels[row],button);
            float t=id==10?Mathf.Log(value/.5f)/Mathf.Log(40):Mathf.InverseLerp(.75f,1.25f,value);float thumb=Mathf.Lerp(24,254,t);
            Round(new Rect(24,y+29,230,5),2,new Color(.80f,.83f,.77f));
            if(thumb>24)Round(new Rect(24,y+29,thumb-24,5),2,new Color(.43f,.61f,.56f));
            Round(new Rect(thumb-11,y+20,22,22),11,new Color(.43f,.61f,.56f));
            GUI.Label(new Rect(279,y,76,42),tuningLabels[row+3],small);
        }
        void TuningChange(int id,float value){if(id==10)RuntimeTuning.ChangeSpeed(value);else RuntimeTuning.ChangeScale(id==14,value);scene?.ApplyRuntimeTuning();}
        void DrawTuningButton(Rect r,string text,bool active=false)
        {
            Round(r,8,active?new Color(.43f,.61f,.56f):new Color(.84f,.85f,.79f));GUI.Label(r,text,button);
        }
        void CancelTuningGesture(){tuningCaptured=tuningTouchBlocked=false;tuningControl=-1;}
    }
}
