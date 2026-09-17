using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WeChatWASM;
using LitJson;

public static class WeChatBuild
{
    [MenuItem("群岛/微信小游戏/配置并打开转换面板")]
    public static void Open()
    {
        Configure();
        WXEditorWin.Open();
    }

    static void PrepareGeometry()
    {
        Directory.CreateDirectory("Assets/Resources/Geometry");
        foreach(var type in new[]{PrimitiveType.Cube,PrimitiveType.Sphere,PrimitiveType.Capsule,PrimitiveType.Cylinder}){
            string path="Assets/Resources/Geometry/"+type+".asset";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(path))continue;
            var temporary=GameObject.CreatePrimitive(type);
            var mesh=UnityEngine.Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
            mesh.name=type.ToString();
            AssetDatabase.CreateAsset(mesh,path);
            UnityEngine.Object.DestroyImmediate(temporary);
        }
        // Our meshes use Stairs/Pastel. Keeping every Standard variant forces
        // hundreds of MB of shader state into the WebAssembly heap on first use.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        for(int i=shaders.arraySize-1;i>=0;i--){
            var shader=shaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if(shader&&(shader.name=="Standard"||shader.name=="Standard (Specular setup)")){
                shaders.GetArrayElementAtIndex(i).objectReferenceValue=null;
                shaders.DeleteArrayElementAtIndex(i);
            }
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
    public static void Configure()
    {
        PrepareGeometry();
        var config=WXConvertCore.config;
        config.ProjectConf.projectName="群岛";
        config.ProjectConf.relativeDST="Builds/WeChat";
        config.ProjectConf.DST=Path.GetFullPath("Builds/WeChat");
        string appId=Environment.GetEnvironmentVariable("STAIRS_WECHAT_APPID");
        if(!string.IsNullOrWhiteSpace(appId)){
            if(!System.Text.RegularExpressions.Regex.IsMatch(appId,"^wx[0-9a-fA-F]{16}$"))
                throw new Exception("AppID must be wx followed by 16 hexadecimal characters.");
            config.ProjectConf.Appid=appId;
        }
        config.ProjectConf.Orientation=(WXScreenOritation)0;
        config.ProjectConf.assetLoadType=1;
        config.ProjectConf.compressDataPackage=true;
        config.ProjectConf.HideAfterCallMain=true;
        config.ProjectConf.bgImageSrc="Assets/Resources/start_hero.png";
        config.CompileOptions.Webgl2=true;
        config.CompileOptions.fbslim=false;
        config.CompileOptions.DevelopBuild=false;
        config.CompileOptions.AutoProfile=false;
        config.CompileOptions.enableProfileStats=false;
        config.CompileOptions.enableRenderAnalysis=false;
        config.CompileOptions.showMonitorSuggestModal=false;
        string node=Environment.GetEnvironmentVariable("STAIRS_NODE_PATH");
        if(!string.IsNullOrEmpty(node))config.CompileOptions.CustomNodePath=node;
        config.SDKOptions.PreloadWXFont=false;
        config.SDKOptions.UseFriendRelation=true;
        EditorUtility.SetDirty(config);
        var target=NamedBuildTarget.WebGL;
        var defines=PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s=>!string.IsNullOrWhiteSpace(s)).ToList();
        if(!defines.Contains("WECHAT_MINIGAME"))defines.Add("WECHAT_MINIGAME");
        PlayerSettings.SetScriptingDefineSymbols(target,string.Join(";",defines));
        PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.WebGL.initialMemorySize=128;
        PlayerSettings.WebGL.maximumMemorySize=512;
        PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching=false;
        PlayerSettings.WebGL.threadsSupport=false;
        PlayerSettings.runInBackground=false;
        PlayerSettings.colorSpace=ColorSpace.Gamma;
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/StairsCrowd.unity",true)};
        string source="Packages/com.qq.weixin.minigame/WebGLTemplates/WXTemplate2022";
        string destination="Assets/WebGLTemplates/WXTemplate2022";
        foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){
            if(file.EndsWith(".meta"))continue;
            string output=Path.Combine(destination,file.Substring(source.Length+1));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.Copy(file,output,true);
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("artifacts/wechat");
        File.WriteAllText("artifacts/wechat/integration.txt",
            "WXSDK changelog version: 0.1.34\nOfficial commit: d288776c50578926c732496882bd6ab6684c778c\n" +
            "AppID configured: "+(!string.IsNullOrEmpty(config.ProjectConf.Appid))+"\n" +
            "Output: Builds/WeChat/minigame\nPortrait; WebGL2; packaged first assets; bundled CJK font; cooperative move cache.\n");
        Debug.Log("WECHAT CONFIGURATION PASSED");
    }

    [MenuItem("群岛/微信小游戏/导出小游戏")]
    public static void Export()
    {
        try{
            Configure();
            var result=WXConvertCore.DoExport(Array.IndexOf(Environment.GetCommandLineArgs(),"-wechatConvertOnly")<0);
            if(result!=WXConvertCore.WXExportError.SUCCEED)throw new Exception("WeChat export failed: "+result);
            string output="Builds/WeChat/minigame";
            foreach(string file in new[]{"game.js","game.json","project.config.json"})
                if(!File.Exists(Path.Combine(output,file)))throw new Exception("Missing export: "+file);
            DeployFriendLeaderboard(output);
            File.WriteAllText("artifacts/wechat/export.txt","Converted successfully. AppID and device verification are required before upload.\n");
            Debug.Log("WECHAT EXPORT PASSED");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }

    // The SDK template is not a production leaderboard. Deploy only our two modules.
    public static void DeployFriendLeaderboard(string output)
    {
        string root=Path.GetFullPath(output),destination=Path.GetFullPath(Path.Combine(root,"open-data"));
        if(!destination.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Invalid open data output");
        foreach(string file in new[]{"index.js","model.js"})if(!File.Exists(Path.Combine("Assets/WeChat/LeaderboardOpenData",file)))throw new Exception("Missing leaderboard module");
        var game=JsonMapper.ToObject(File.ReadAllText(Path.Combine(root,"game.json")));
        game["openDataContext"]="open-data";
        if(game.ContainsKey("plugins")&&game["plugins"].ContainsKey("Layout"))game["plugins"].Remove("Layout");
        if(Directory.Exists(destination))Directory.Delete(destination,true);
        Directory.CreateDirectory(destination);
        foreach(string file in new[]{"index.js","model.js"})File.Copy(Path.Combine("Assets/WeChat/LeaderboardOpenData",file),Path.Combine(destination,file));
        File.WriteAllText(Path.Combine(root,"game.json"),game.ToJson());
        VerifyFriendLeaderboardExport(root);
    }

    public static void VerifyFriendLeaderboardExport(string output)
    {
        var game=JsonMapper.ToObject(File.ReadAllText(Path.Combine(output,"game.json")));
        if(!game.ContainsKey("openDataContext")||(string)game["openDataContext"]!="open-data")throw new Exception("Leaderboard context missing");
        string directory=Path.Combine(output,"open-data");
        var files=Directory.GetFiles(directory,"*",SearchOption.AllDirectories);
        if(files.Length!=2||!File.Exists(Path.Combine(directory,"model.js"))||!File.Exists(Path.Combine(directory,"index.js")))throw new Exception("Unexpected open data payload");
        foreach(string file in files){string source=File.ReadAllText(file);if(source.Contains("console.")||source.Contains("Math.random")||source.Contains("getGroupCloudStorage")||source.Contains("requirePlugin"))throw new Exception("SDK sample or private data logging found");}
        Debug.Log("FRIEND LEADERBOARD OPEN DATA EXPORT PASSED");
    }

    public static void BuildCooperativeTest()
    {
        try{
            Directory.CreateDirectory("Builds/WeChatVerification/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},
                locationPathName="Builds/WeChatVerification/Windows/StairsCrowd.exe",
                target=BuildTarget.StandaloneWindows64,
                extraScriptingDefines=new[]{"STAIRS_COOPERATIVE_TEST"},
                options=BuildOptions.None
            });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Cooperative test build failed");
            Debug.Log("COOPERATIVE TEST BUILD PASSED");
            EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
