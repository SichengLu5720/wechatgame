using System;
using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        public bool FriendLeaderboardOpen {get;private set;}
        public FriendLeaderboardState LeaderboardState {get;private set;}
        public List<FriendLeaderboardRow> LeaderboardRows {get;private set;}=new List<FriendLeaderboardRow>();
        readonly WeChatLeaderboard leaderboardBridge=new WeChatLeaderboard();
        FriendLeaderboardState mockTarget=FriendLeaderboardState.Ready;
        float leaderboardReadyAt,leaderboardScroll;bool leaderboardSuspended;
        public float LeaderboardScroll {get=>leaderboardScroll;set=>leaderboardScroll=value;}
        FriendLeaderboardRecord leaderboardRecord;
        void InitializeLeaderboard()
        {
            leaderboardRecord=FriendLeaderboardProgress.Read(builtInCount);
            leaderboardBridge.Sync(leaderboardRecord);
            WeChatPlatform.BackgroundChanged+=LeaderboardBackground;
        }
        void SyncLeaderboard()
        {
            leaderboardRecord=FriendLeaderboardProgress.Read(builtInCount);leaderboardBridge.Sync(leaderboardRecord);
        }
        public void OpenFriendLeaderboard()
        {
            if(!Home||editing||settingsOpen||DailyCalendarOpen||Time.frameCount<=suppressInputThrough)return;
            FriendLeaderboardOpen=true;CancelPropSelection();CancelViewPointer();Feedback?.Cancel();
            RetryFriendLeaderboard();
        }
        public void CloseFriendLeaderboard()
        {
            FriendLeaderboardOpen=false;leaderboardBridge.Hide();CancelViewPointer();suppressInputThrough=Time.frameCount+1;
        }
        public void RetryFriendLeaderboard()
        {
            leaderboardRecord=FriendLeaderboardProgress.Read(builtInCount);
            LeaderboardState=FriendLeaderboardState.Loading;leaderboardScroll=0;leaderboardReadyAt=Time.unscaledTime+.6f;
            if(WeChatLeaderboard.Available){var layout=FriendLeaderboardLayout.Current;leaderboardBridge.Layout(layout.Content,GameplayLayout.Current.Scale);leaderboardBridge.Open(leaderboardRecord);}
        }
        public void SetLeaderboardMock(FriendLeaderboardState target)
        {
            if(WeChatLeaderboard.Available)return;mockTarget=target;if(FriendLeaderboardOpen)RetryFriendLeaderboard();
        }
        void LeaderboardBackground(bool hidden)
        {
            leaderboardSuspended=hidden;
            if(hidden)leaderboardBridge.Hide();
            else if(FriendLeaderboardOpen)RetryFriendLeaderboard();
            else SyncLeaderboard();
        }
        void TickLeaderboard()
        {
            if(!FriendLeaderboardOpen||leaderboardSuspended)return;
            if(Input.GetKeyDown(KeyCode.Escape)){CloseFriendLeaderboard();return;}
            if(WeChatLeaderboard.Available) {
                var layout=FriendLeaderboardLayout.Current;
                // Relayout recreates the SDK texture hook; reopen after WXDestroy.
                leaderboardBridge.Layout(layout.Content,GameplayLayout.Current.Scale);
            } else if(LeaderboardState==FriendLeaderboardState.Loading&&Time.unscaledTime>=leaderboardReadyAt&&mockTarget!=FriendLeaderboardState.Loading) {
                LeaderboardState=mockTarget;
                LeaderboardRows=FriendLeaderboardModel.Fixture(leaderboardRecord?.completedCount??0,mockTarget==FriendLeaderboardState.Empty,mockTarget==FriendLeaderboardState.ProfileUnavailable);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(!WeChatLeaderboard.Available&&Input.GetKeyDown(KeyCode.F8))SetLeaderboardMock((FriendLeaderboardState)(((int)mockTarget+1)%5));
#endif
        }
        void DrawLeaderboardEntry()
        {
            var rect=FriendLeaderboardLayout.Current.Entry;
            if(RoundButton(rect,"排行榜",new Color(.19f,.29f,.32f)))OpenFriendLeaderboard();
            GUI.Label(rect,"排行榜",new GUIStyle(button){normal={textColor=UiPaper}});
        }
        void DrawFriendLeaderboard(float width,float height)
        {
            var layout=FriendLeaderboardLayout.Current;var p=layout.Panel;
            Fill(new Rect(0,0,width,height),new Color(.06f,.12f,.14f,.64f));Round(p,24,UiPaper);
            GUI.Label(new Rect(p.x+28,p.y+16,p.width-128,46),"好友排行",Style(32,FontStyle.Bold));
            GUI.Label(new Rect(p.x+28,p.y+67,p.width-60,32),"同分时，先达到者在前",Style(20));
            if(RoundButton(layout.Close,"×",new Color(.89f,.88f,.80f))){CloseFriendLeaderboard();return;}
            if(WeChatLeaderboard.Available&&!leaderboardBridge.Failed){if(leaderboardBridge.Texture)GUI.DrawTexture(layout.Content,leaderboardBridge.Texture,ScaleMode.StretchToFill,true);return;}
            if(WeChatLeaderboard.Available)LeaderboardState=FriendLeaderboardState.Failure;
            DrawLeaderboardMock(layout.Content);
        }
        string LeaderboardName(string name,float width)
        {
            var style=Style(22);if(style.CalcSize(new GUIContent(name)).x<=width)return name;
            while(name.Length>0&&style.CalcSize(new GUIContent(name+"…")).x>width)name=name.Substring(0,name.Length-1);
            return name+"…";
        }
        void DrawLeaderboardRow(Rect rect,FriendLeaderboardRow row,bool fixedSelf=false,bool known=true)
        {
            bool self=row.Self||fixedSelf;Round(rect,12,self?new Color(.85f,.90f,.82f):new Color(.98f,.97f,.93f));
            GUI.Label(new Rect(rect.x,rect.y,48,68),known?row.Rank.ToString():"—",Style(26,FontStyle.Bold,TextAnchor.MiddleCenter));
            Circle(new Rect(rect.x+50,rect.y+12,44,44),new Color(.77f,.84f,.79f));Circle(new Rect(rect.x+65,rect.y+20,14,14),UiInk);Round(new Rect(rect.x+59,rect.y+36,26,13),6,UiInk);
            float nameWidth=Mathf.Max(20,rect.width-260);GUI.Label(new Rect(rect.x+110,rect.y+(self?8:0),nameWidth,self?34:68),LeaderboardName(row.Nickname,nameWidth),Style(22));
            if(self)GUI.Label(new Rect(rect.x+110,rect.y+38,nameWidth+26,24),known?"我":LeaderboardState==FriendLeaderboardState.Loading?"名次加载中":"名次暂不可用",Style(16));
            GUI.Label(new Rect(rect.xMax-154,rect.y,140,68),"已通关 "+row.Count+" 关",Style(20,FontStyle.Normal,TextAnchor.MiddleRight));
        }
        void DrawLeaderboardMock(Rect content)
        {
            GUI.BeginGroup(content);float w=content.width,h=content.height,fixedY=h-68,top=0;
            bool ready=LeaderboardState==FriendLeaderboardState.Ready||LeaderboardState==FriendLeaderboardState.ProfileUnavailable;
            if(LeaderboardState==FriendLeaderboardState.ProfileUnavailable||LeaderboardState==FriendLeaderboardState.Empty){GUI.Label(new Rect(0,0,w-118,68),"部分资料暂不可用",Style(20));if(RoundButton(new Rect(w-110,0,102,68),"重试",new Color(.85f,.89f,.83f)))RetryFriendLeaderboard();top=80;}
            var viewport=new Rect(0,top,w,fixedY-28-top);
            if(ready){
                float total=LeaderboardRows.Count*76-8,max=Mathf.Max(0,total-viewport.height);
                if(Event.current.type==EventType.ScrollWheel&&viewport.Contains(Event.current.mousePosition)){leaderboardScroll+=Event.current.delta.y*30;Event.current.Use();}
                if(Event.current.type==EventType.MouseDrag&&viewport.Contains(Event.current.mousePosition)){leaderboardScroll-=Event.current.delta.y;Event.current.Use();}
                leaderboardScroll=Mathf.Clamp(leaderboardScroll,0,max);
                GUI.BeginGroup(viewport);
                for(int i=0;i<LeaderboardRows.Count;i++){float y=i*76-leaderboardScroll;if(y+68>=0&&y<=viewport.height)DrawLeaderboardRow(new Rect(0,y,w-8,68),LeaderboardRows[i]);}
                GUI.EndGroup();
                if(max>0){float thumb=Mathf.Max(25,viewport.height*viewport.height/total);Round(new Rect(w-3,top+(viewport.height-thumb)*leaderboardScroll/max,3,thumb),1,UiSage);}
            } else {
                float y=viewport.center.y-30;GUI.Label(new Rect(0,y,w,44),LeaderboardState==FriendLeaderboardState.Loading?"正在加载好友排行…":LeaderboardState==FriendLeaderboardState.Empty?"暂无可显示的好友成绩":"好友排行加载失败",Style(24,FontStyle.Normal,TextAnchor.MiddleCenter));
                if(LeaderboardState==FriendLeaderboardState.Failure){GUI.Label(new Rect(0,y+45,w,32),"请稍后重试",Style(20,FontStyle.Normal,TextAnchor.MiddleCenter));if(RoundButton(new Rect(w/2-85,y+96,170,68),"重试",new Color(.89f,.67f,.60f))){if(!WeChatLeaderboard.Available)mockTarget=FriendLeaderboardState.Ready;RetryFriendLeaderboard();}}
            }
            Fill(new Rect(0,fixedY-14,w-8,1),new Color(.82f,.85f,.78f));
            var self=LeaderboardRows.Find(r=>r.Self);bool known=(ready||LeaderboardState==FriendLeaderboardState.Empty)&&self!=null;
            DrawLeaderboardRow(new Rect(0,fixedY,w-8,68),known?self:new FriendLeaderboardRow{Self=true,Nickname="微信玩家",Count=leaderboardRecord?.completedCount??0},true,known);
            GUI.EndGroup();
        }
        void DisposeLeaderboard(){WeChatPlatform.BackgroundChanged-=LeaderboardBackground;leaderboardBridge.Dispose();}
    }
}
