using System;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Every build target validates the same delivered campaign/tutorial/daily inputs.
// No authoring JSON, layout, solver result or runtime asset is written here.
public sealed class NavigationBuildPreflight : IPreprocessBuildWithReport
{
    public int callbackOrder=>-100;
    public void OnPreprocessBuild(BuildReport report)=>Check();
    public static void Check()
    {
        var cached=Directory.GetFiles("Assets/Resources","*.bytes",SearchOption.AllDirectories).Where(p=>p.Replace('\\','/').Contains("navigation/")).ToArray();
        if(cached.Length!=0)throw new BuildFailedException("Offline navigation caches must not be shipped in Resources: "+string.Join(",",cached));
        var results=NavigationDataVerification.Levels(true).Where(x=>x.Key.StartsWith("actual-")||x.Key.StartsWith("daily-")).Select(x=>NavigationDataVerification.Check(x.Key,x.Value)).ToArray();
        Directory.CreateDirectory("artifacts/navigation");
        File.WriteAllText("artifacts/navigation/build-preflight.json",JsonUtility.ToJson(new NavigationDataVerification.Report{passed=results.All(x=>x.error==null),cases=results,resourceBytes=0},true));
        foreach(var result in results)if(result.error!=null)throw new BuildFailedException(result.id+": "+result.error);
        Debug.Log("NAVIGATION BUILD PREFLIGHT PASSED levels="+results.Length);
    }
}
