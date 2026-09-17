using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class RuntimeTuningBuild
{
    [Serializable] sealed class Source {public string path,sha256;}
    [Serializable] sealed class Manifest {public bool development;public string unityVersion;public Source[] sources;}
    [MenuItem("群岛/开发调试/构建 Windows")]
    public static void DevelopmentWindows()=>Build(BuildTarget.StandaloneWindows64,true,"Builds/RuntimeTuningV2Dev/StairsCrowd.exe");
    public static void ReleaseWindows()=>Build(BuildTarget.StandaloneWindows64,false,"Builds/RuntimeTuningV2Release/StairsCrowd.exe");
    [MenuItem("群岛/开发调试/构建 WebGL")]
    public static void DevelopmentWebGL()=>Build(BuildTarget.WebGL,true,"Builds/RuntimeTuningV2WebGL");
    static void Build(BuildTarget target,bool development,string path)
    {
        try{var files=Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories).OrderBy(p=>p).ToArray();var hashes=files.Select(p=>new Source{path=p.Replace('\\','/'),sha256=Hash(p)}).ToArray();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/StairsCrowd.unity"},locationPathName=path,target=target,options=development?BuildOptions.Development:BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            foreach(var source in hashes)if(Hash(source.path)!=source.sha256)throw new Exception("Source changed during build: "+source.path);
            File.WriteAllText(Path.Combine(target==BuildTarget.WebGL?path:Path.GetDirectoryName(path),"source-manifest.json"),JsonUtility.ToJson(new Manifest{development=development,unityVersion=Application.unityVersion,sources=hashes},true));
            Debug.Log("RUNTIME TUNING BUILD PASSED "+path);if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
    static string Hash(string path){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
}
