using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using StairsCrowd.Runtime;

public static class FriendLeaderboardVerification
{
    // Compile the exact bridge signatures against the installed editor SDK. Never call them in mock runs.
    static void CompileSdkContract(Texture texture)
    {
        WeChatWASM.WX.GetOpenDataContext().PostMessage("{}");
        WeChatWASM.WX.ShowOpenData(texture,0,0,100,100);
        WeChatWASM.WX.HideOpenData();
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception("FRIEND LEADERBOARD: "+message);}
    public static void Verify()
    {
        try {
            var record=new FriendLeaderboardRecord{completedCount=6,achievedAtMs=100,marker=new string('a',32)};
            Check(ReferenceEquals(record,FriendLeaderboardProgress.Advance(record,5,6,20,200)),"replay increased score");
            Check(ReferenceEquals(record,FriendLeaderboardProgress.Advance(record,7,6,20,200)),"future level skipped score");
            var raised=FriendLeaderboardProgress.Advance(record,6,6,20,90);Check(raised.completedCount==7&&raised.achievedAtMs==101,"nonmonotonic clock");
            var extreme=new FriendLeaderboardRecord{completedCount=6,achievedAtMs=9007199254740990L,marker=record.marker};Check(ReferenceEquals(extreme,FriendLeaderboardProgress.Advance(extreme,6,6,20,100)),"timestamp overflow corrupted record");
            var last=new FriendLeaderboardRecord{completedCount=19,achievedAtMs=200,marker=record.marker};
            var final=FriendLeaderboardProgress.Advance(last,19,19,20,300);Check(final.completedCount==20,"last level lost");
            Check(ReferenceEquals(final,FriendLeaderboardProgress.Advance(final,19,19,20,400)),"last replay retimed");
            var rows=FriendLeaderboardModel.Fixture(6);Check(rows.Count==15&&rows.Find(r=>r.Self).Rank==12,"fixture identity or ties");
            for(int i=1;i<rows.Count;i++)Check(FriendLeaderboardModel.Compare(rows[i-1],rows[i])<=0,"wrong sort");
            foreach(var size in new[]{new Vector2(700,1000),new Vector2(390,844),new Vector2(320,568)})foreach(bool safe in new[]{false,true}) {
                var layout=new GameplayLayout(size.x,size.y,new Rect(0,safe?24:0,size.x,size.y-(safe?90:0)),safe?new Rect(size.x-100,size.y-92,90,34):default,safe);
                var ui=new FriendLeaderboardLayout(layout);
                Check(layout.Safe.Contains(ui.Panel.min)&&layout.Safe.Contains(ui.Panel.max),"panel outside safe area");
                Check(ui.Entry.y>=layout.Gear.yMax&&ui.Entry.yMax<=ui.Panel.y,"entry overlap");
                Check(ui.Content.height>250&&ui.Close.width*layout.Scale>=35,"unusable controls");
            }
            Directory.CreateDirectory("artifacts/task008");File.WriteAllText("artifacts/task008/logic-tests.json","{\"passed\":true,\"cases\":[\"replay\",\"future\",\"clock\",\"last-level\",\"ties\",\"six-layouts\"]}");
            Debug.Log("FRIEND LEADERBOARD VERIFICATION PASSED");EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void BuildWindows()
    {
        try {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName="Builds/FriendLeaderboard/StairsCrowd.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Leaderboard Windows build failed");
            Debug.Log("FRIEND LEADERBOARD WINDOWS BUILD PASSED");EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void VerifyDeployment()
    {
        try {
            string output="artifacts/task008/export-fixture";Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"game.json"),"{\"plugins\":{\"Layout\":{},\"UnityPlugin\":{}},\"deviceOrientation\":\"portrait\"}");
            WeChatBuild.DeployFriendLeaderboard(output);
            string result=File.ReadAllText(Path.Combine(output,"game.json"));Check(result.Contains("UnityPlugin")&&!result.Contains("Layout"),"plugin isolation");
            EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
