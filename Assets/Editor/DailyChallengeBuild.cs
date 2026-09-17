using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the existing scene without regenerating art or changing project settings.
public static class DailyChallengeBuild
{
    public static void Build()
    {
        try {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-dailyBuildPath");
            string path=at>=0?args[at+1]:"Builds/Task002/Windows/StairsCrowd.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("TASK002 WINDOWS BUILD PASSED "+path);EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
