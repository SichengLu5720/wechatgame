using System;
using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public enum FriendLeaderboardState { Loading, Ready, Empty, Failure, ProfileUnavailable }
    public sealed class FriendLeaderboardRow
    {
        public string Id,Nickname;public int Count,Rank;public long Time;public bool Self;
    }
    public static class FriendLeaderboardModel
    {
        public static int Compare(FriendLeaderboardRow a,FriendLeaderboardRow b)
        {int n=b.Count.CompareTo(a.Count);if(n!=0)return n;n=a.Time.CompareTo(b.Time);return n!=0?n:string.CompareOrdinal(a.Id,b.Id);}
        public static List<FriendLeaderboardRow> Fixture(int ownCount,bool empty=false,bool profile=false)
        {
            int[] scores={20,18,18,16,14,14,12,11,10,9,8,6,4,2};var rows=new List<FriendLeaderboardRow>();
            if(!empty)for(int i=0;i<scores.Length;i++)rows.Add(new FriendLeaderboardRow{Id="friend"+i.ToString("00"),Nickname=profile?"微信玩家":i==3?"模拟好友名字很长需要省略显示":"模拟好友"+(i+1).ToString("00"),Count=scores[i],Time=100+i});
            rows.Add(new FriendLeaderboardRow{Id="self",Nickname="微信玩家",Count=ownCount,Time=105,Self=true});
            rows.Sort(Compare);for(int i=0;i<rows.Count;i++)rows[i].Rank=i+1;return rows;
        }
    }
    public readonly struct FriendLeaderboardLayout
    {
        public readonly Rect Entry,Panel,Close,Content;
        public FriendLeaderboardLayout(GameplayLayout layout)
        {
            Entry=new Rect(layout.Gear.x,layout.Gear.yMax+20,126,68);
            float width=Mathf.Min(560,layout.Safe.width-40),top=Mathf.Max(Entry.yMax+24,layout.Safe.y+188);
            float bottom=layout.Safe.yMax-36;
            if(bottom-top<450)top=Mathf.Max(layout.Gear.yMax+16,bottom-450);
            Panel=new Rect(layout.Safe.center.x-width/2,top,width,Mathf.Max(300,bottom-top));
            Close=new Rect(Panel.xMax-84,Panel.y+16,68,68);
            Content=new Rect(Panel.x+20,Panel.y+120,Panel.width-40,Panel.height-142);
        }
        public static FriendLeaderboardLayout Current=>new FriendLeaderboardLayout(GameplayLayout.Current);
    }
}
