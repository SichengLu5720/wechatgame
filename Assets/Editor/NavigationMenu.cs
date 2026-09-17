using System.IO;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class NavigationMenu
{
    [MenuItem("群岛/更新关卡通行数据")]
    public static void Bake()
    {
        var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text);
        Directory.CreateDirectory("artifacts/navigation/legacy");
        for(int i=0;i<catalog.levels.Length;i++)File.WriteAllBytes("artifacts/navigation/legacy/level-"+i+".bytes",NavigationFactory.ForOfflineValidation(catalog.levels[i]).Bake());
        Debug.Log("离线关卡通行缓存已更新；正式玩法按需生成，不携带密集缓存。");
    }
    [MenuItem("群岛/更新关卡通行数据",true)]
    public static bool CanBake(){return !EditorApplication.isPlayingOrWillChangePlaymode;}
    public static void BakeAllExisting()
    {
        try {
            foreach(var item in NavigationDataVerification.Levels()){
                Directory.CreateDirectory("artifacts/navigation/all");
                File.WriteAllBytes("artifacts/navigation/all/"+item.Key+".bytes",NavigationFactory.ForOfflineValidation(item.Value).Bake());
            }
            EditorApplication.Exit(0);
        }catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
