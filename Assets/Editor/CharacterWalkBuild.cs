using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Task-specific build: intentionally does not regenerate architecture or legacy art.
public static class CharacterWalkBuild
{
    public static void VerifyFallback()
    {
        var asset=Resources.Load<StairsCrowd.Runtime.CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
        bool original=asset&&asset.runtimeEnabled;
        try { if(asset)asset.runtimeEnabled=false;UnityBuild.Verify(); }
        finally { if(asset)asset.runtimeEnabled=original; }
    }
    public static void VerifyCandidate()
    {
        var asset=Resources.Load<StairsCrowd.Runtime.CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
        if(!asset||!asset.Valid){Debug.LogError("Missing candidate asset");EditorApplication.Exit(1);return;}
        bool original=asset.runtimeEnabled;
        try
        {
            // This process-only override verifies natural-walk timing without
            // changing the default project or serializing candidate approval.
            asset.runtimeEnabled=true;Debug.Log("VERIFY CANDIDATE HASH "+asset.sourceHash);
            UnityBuild.Verify();
        }
        finally{if(asset)asset.runtimeEnabled=original;}
    }
    public static void ImportVerifyAndBuild()
    {
        try { CharacterWalkImport.Import(); CharacterWalkVerification.Check(); Build(); }
        catch(Exception e) { DisableCandidate(); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void VerifyAndBuild()
    {
        try { CharacterWalkVerification.Check(); Build(); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void Build()
    {
        try
        {
            string folder = "Builds/CharacterWalk";
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-characterBuildFolder");
            if (at >= 0 && at + 1 < args.Length) folder = args[at + 1];
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/StairsCrowd.unity" },
                locationPathName = Path.Combine(folder, "StairsCrowd.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Character build: " + report.summary.result);
            Debug.Log("CHARACTER WALK WINDOWS BUILD PASSED " + folder);
            DisableCandidate();
            EditorApplication.Exit(0);
        }
        catch (Exception e) { DisableCandidate(); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void DisableCandidate()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-enableCharacterWalkCandidate")<0)return;
        var asset=AssetDatabase.LoadAssetAtPath<StairsCrowd.Runtime.CharacterWalkAsset>("Assets/Resources/CharacterWalk/Task003PilgrimCharacter.asset");
        if(!asset)return;
        asset.runtimeEnabled=false;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
    }
}
