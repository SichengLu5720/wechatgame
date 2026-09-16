using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class LocalArtBuild
{
    public static void Verify(){NavigationMenu.Bake();UnityBuild.VerifyAlternatives();}
    public static void Build(){NavigationMenu.Bake();RefreshPreview();WeChatBuild.BuildCooperativeTest();}
    [MenuItem("群岛/更新本地美术预览")]
    public static void RefreshPreview()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StairsCrowd.unity");
        var old=GameObject.Find("Editor Preview");if(old)UnityEngine.Object.DestroyImmediate(old);
        var camera=Camera.main;if(!camera){camera=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";}
        camera.backgroundColor=new Color(.9f,.85f,.8f);camera.clearFlags=CameraClearFlags.SolidColor;
        var level=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text).levels[0];
        var preview=new CrowdScene(new WalkSpace(level,Resources.Load<TextAsset>("navigation/level-0").bytes),camera);
        preview.Populate(Rules.Initial(level));preview.root.name="Editor Preview";preview.root.tag="EditorOnly";
        Directory.CreateDirectory("Assets/Art/GeneratedPreview");AssetDatabase.Refresh();
        var meshes=new Dictionary<Mesh,Mesh>();var materials=new Dictionary<Material,Material>();
        Func<Mesh,Mesh> keepMesh=mesh=>{
            if(!mesh||AssetDatabase.Contains(mesh))return mesh;
            Mesh saved;if(meshes.TryGetValue(mesh,out saved))return saved;
            saved=Persist(mesh,"Assets/Art/GeneratedPreview/Mesh-"+meshes.Count.ToString("000")+".asset");meshes.Add(mesh,saved);return saved;
        };
        foreach(var filter in preview.root.GetComponentsInChildren<MeshFilter>(true))filter.sharedMesh=keepMesh(filter.sharedMesh);
        foreach(var collider in preview.root.GetComponentsInChildren<MeshCollider>(true))collider.sharedMesh=keepMesh(collider.sharedMesh);
        foreach(var renderer in preview.root.GetComponentsInChildren<Renderer>(true)){
            var slots=renderer.sharedMaterials;
            for(int i=0;i<slots.Length;i++){
                var material=slots[i];if(!material||AssetDatabase.Contains(material))continue;
                Material saved;if(!materials.TryGetValue(material,out saved)){saved=Persist(material,"Assets/Art/GeneratedPreview/Material-"+materials.Count.ToString("000")+".mat");materials.Add(material,saved);}slots[i]=saved;
            }
            renderer.sharedMaterials=slots;
        }
        Action<GameObject,string> prefab=(go,name)=>{var position=go.transform.position;var tag=go.tag;go.transform.position=Vector3.zero;go.tag="Untagged";PrefabUtility.SaveAsPrefabAsset(go,"Assets/Art/Prefabs/"+name+".prefab");go.transform.position=position;go.tag=tag;};
        foreach(Transform child in preview.root.transform){if(child.name=="Sorting Pavilion 0")prefab(child.gameObject,"SortingPavilion");if(child.name=="Transit Pier 2")prefab(child.gameObject,"TransitPier");if(child.name=="Stair Link 0")prefab(child.gameObject,"ConnectedStairway");}
        prefab(preview.people[0].root.gameObject,"CloakedWalker");
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Debug.Log("LOCAL ART PREVIEW UPDATED");
    }
    static T Persist<T>(T source,string path) where T:UnityEngine.Object
    {
        var saved=AssetDatabase.LoadAssetAtPath<T>(path);
        if(saved){EditorUtility.CopySerialized(source,saved);EditorUtility.SetDirty(saved);}else{saved=UnityEngine.Object.Instantiate(source);saved.name=source.name;AssetDatabase.CreateAsset(saved,path);}return saved;
    }
}