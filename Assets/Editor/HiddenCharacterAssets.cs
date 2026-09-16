using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using StairsCrowd.Runtime;

public static class HiddenCharacterAssets
{
    public static void Export()
    {
        try {
            const string folder="Assets/Resources/HiddenCharacter";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var generated=HiddenCharacterMesh.Create();var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/HiddenCharacter.asset");
            if(mesh){EditorUtility.CopySerialized(generated,mesh);UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(mesh);}else{mesh=generated;AssetDatabase.CreateAsset(mesh,folder+"/HiddenCharacter.asset");}
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/HiddenCharacter.mat");
            if(!material){material=new Material(Shader.Find("Stairs/HiddenCharacter")){name="Opaque black with painted white glyph",enableInstancing=true};AssetDatabase.CreateAsset(material,folder+"/HiddenCharacter.mat");}
            var go=new GameObject("Hidden character",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            PrefabUtility.SaveAsPrefabAsset(go,folder+"/HiddenCharacter.prefab");UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();
            Debug.Log("HIDDEN ASSET EXPORT PASS triangles="+mesh.triangles.Length/3);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
