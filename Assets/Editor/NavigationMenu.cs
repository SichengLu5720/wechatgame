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
        Directory.CreateDirectory("Assets/Resources/navigation");
        for(int i=0;i<catalog.levels.Length;i++)File.WriteAllBytes("Assets/Resources/navigation/level-"+i+".bytes",new WalkSpace(catalog.levels[i]).Bake());
        AssetDatabase.Refresh();Debug.Log("关卡通行数据已更新。");
    }
    [MenuItem("群岛/更新关卡通行数据",true)]
    public static bool CanBake(){return !EditorApplication.isPlayingOrWillChangePlaymode;}
    public static void BakeAllExisting()
    {
        try {
            Bake();var campaign=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("campaign-v1").text);
            var done=new System.Collections.Generic.HashSet<string>();
            foreach(var level in campaign.levels)if(done.Add(level.provenance.template))File.WriteAllBytes("Assets/Resources/campaign-navigation/"+level.provenance.template+".bytes",new WalkSpace(level).Bake());
            AssetDatabase.Refresh();EditorApplication.Exit(0);
        }catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
