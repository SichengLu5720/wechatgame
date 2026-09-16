using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class CloudBuild
{
    public static void BuildGeometryLocal()
    {
        try{NavigationMenu.Bake();LocalArtBuild.RefreshPreview();BuildLocal();}
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void BuildLocal()
    {
        try{
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName="Builds/CloudIteration/Windows/StairsCrowd.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("LOCAL BUILD COMPLETE");EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void Build()
    {
        try{
            Directory.CreateDirectory("artifacts/cloud-iteration");var catalog=CloudLevels.Create();int actions=0;
            foreach(var l in catalog.levels){l.Validate();var solve=Solver.Solve(l,Rules.Initial(l),200000);if(solve.status!="solved")throw new Exception("Unsolvable level "+l.name);l.solution=solve.moves;actions+=solve.moves.Length;}
            File.WriteAllText("Assets/Resources/levels.json",JsonUtility.ToJson(catalog,true));AssetDatabase.Refresh();NavigationMenu.Bake();
            foreach(var l in catalog.levels)LayoutSafety.Validate(new WalkSpace(l));
            typeof(UnityBuild).GetMethod("VerifyShapes",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
            LocalArtBuild.RefreshPreview();
            var level=catalog.levels[4];string code=LevelShare.Encode(level);var copy=LevelShare.Decode(code);if(LevelShare.Encode(copy)!=code)throw new Exception("Share roundtrip");
            try{LevelShare.Decode(code.Substring(0,code.Length-8));throw new Exception("Truncated share accepted");}catch(FormatException){}catch(InvalidDataException){}catch(Exception e){if(e.Message=="Truncated share accepted")throw;}
            File.WriteAllText("artifacts/cloud-iteration/rules.json","{\"passed\":true,\"levels\":6,\"solutionMoves\":"+actions+",\"shareRoundtrip\":true}");
            Directory.CreateDirectory("Builds/CloudIteration/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName="Builds/CloudIteration/Windows/StairsCrowd.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            var mobileReport=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName="Builds/CloudIteration/Cooperative/StairsCrowd.exe",target=BuildTarget.StandaloneWindows64,extraScriptingDefines=new[]{"STAIRS_COOPERATIVE_TEST"},options=BuildOptions.None});
            if(mobileReport.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Cooperative build failed");
            Debug.Log("CLOUD BUILD PASSED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

