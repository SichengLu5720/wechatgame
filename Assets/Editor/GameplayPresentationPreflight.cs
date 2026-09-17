using System;
using StairsCrowd.Core;
using StairsCrowd.Runtime;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class GameplayPresentationPreflight:IPreprocessBuildWithReport
{
    public int callbackOrder=>-90;
    public void OnPreprocessBuild(BuildReport report)
    {
        int count=0;
        foreach(string resource in new[]{"tutorial-v1","campaign-v1","levels","daily-v1"}){
            var source=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>(resource).text);
            foreach(var level in source.levels){
                var entry=LevelPresentation.Find(level);
                if(entry==null||entry.sourceSignature!=LevelPresentation.Signature(level))throw new BuildFailedException("Missing/stale fixed camera: "+resource+" / "+level.name+". Run GameplayPresentationDelivery.Generate.");
                var delivered=LevelPresentation.Apply(level);
                if(LevelPresentation.Signature(delivered)!=entry.geometrySignature)throw new BuildFailedException("Invalid delivered geometry: "+level.name);
                foreach(string operation in entry.operations)if(!operation.EndsWith("rotate 90",StringComparison.Ordinal)&&!operation.EndsWith("rotate -90",StringComparison.Ordinal)&&!operation.EndsWith("rotate 180",StringComparison.Ordinal))throw new BuildFailedException("Non-quarter-turn layout: "+level.name);
                count++;
            }
        }
        Debug.Log("PRESENTATION BUILD PREFLIGHT PASSED levels="+count);
    }
    // Compile-time validation of SDK fields used by the player-only display branch.
    // Never invoke native WX calls in an editor/headless verification.
    static Rect CompileSdkFields(WeChatWASM.WindowInfo info,WeChatWASM.ClientRect capsule)
    {
        if(info.safeArea!=null){var s=info.safeArea;WeChatPlatform.WindowRect((float)s.left,(float)s.top-(float)info.screenTop,(float)s.width,(float)s.height,(float)info.windowWidth,(float)info.windowHeight);}
        return capsule==null?default:WeChatPlatform.WindowRect((float)capsule.left,(float)capsule.top,(float)capsule.width,(float)capsule.height,(float)info.windowWidth,(float)info.windowHeight);
    }
}
