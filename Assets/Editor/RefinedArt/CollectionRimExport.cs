using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StairsCrowd.ArtProduction;

public static partial class RefinedArtExport
{
    public static void RunCollectionRim()
    {
        try{
            Folder="Assets/Art/CollectionRimV1";Output="artifacts/collection-rim-v1";
            foreach(string sub in new[]{"Meshes","Materials","Prefabs","Scenes"})Directory.CreateDirectory(Folder+"/"+sub);
            Directory.CreateDirectory(Output+"/renders");Directory.CreateDirectory(Output+"/obj");AssetDatabase.Refresh();
            surface=Persist(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/RefinedV2/Materials/Stone_Jade_Gold.mat")),Folder+"/Materials/CollectionStone.mat");
            surface.name="Collection ivory and solid teal - no state tint";surface.SetFloat("_GameplayLighting",0);surface.SetFloat("_FogStrength",0);EditorUtility.SetDirty(surface);
            SaveMesh("CollectionPlatform",RefinedModels.CollectionRim(true));SaveMesh("CollectionDeck",RefinedModels.CollectionRim(false));SaveMesh("Stair",RefinedModels.Stairs());
            foreach(string key in new[]{"CollectionPlatform","CollectionDeck","Stair"})MakePrefab(key,Meshes[key],surface);
            var kit=new GameObject("Collection with independent staircase");
            var platform=(GameObject)PrefabUtility.InstantiatePrefab(Prefabs["CollectionPlatform"]);platform.name="Platform";platform.transform.SetParent(kit.transform,false);
            var stair=(GameObject)PrefabUtility.InstantiatePrefab(Prefabs["Stair"]);stair.name="StairAttachment";stair.transform.SetParent(kit.transform,false);stair.transform.localPosition=new Vector3(0,-2.4f,7.8f);stair.transform.localRotation=Quaternion.Euler(0,180,0);
            Prefabs["CollectionWithStair"]=PrefabUtility.SaveAsPrefabAsset(kit,Folder+"/Prefabs/CollectionWithStair.prefab");UnityEngine.Object.DestroyImmediate(kit);
            AssetDatabase.SaveAssets();CheckCollectionRim();PrepareScene();
            ClearStage();Put("CollectionDeck",Vector3.zero);Ground(-.62f,RefinedModels.Hex("3F9DA6"));Frame(new Vector3(0,0,0),new Vector3(7,8,11),3.9f);Capture("01-platform-empty",1600,1400);
            Frame(new Vector3(2.1f,-.22f,2.1f),new Vector3(5,4,8),1.2f);Capture("02-thick-rim-detail",1200,1200);
            ClearStage();sun.shadows=LightShadows.None;var exhibit=Put("CollectionWithStair",Vector3.zero);AddCollectionPeople(exhibit,16);Frame(new Vector3(0,-1.4f,2),new Vector3(5,8,12),7.7f);Capture("03-before-descent",1400,1600);
            var fixedPlatform=exhibit.transform.Find("Platform");var before=fixedPlatform.localToWorldMatrix;var materials=fixedPlatform.GetComponent<Renderer>().sharedMaterials;
            exhibit.transform.Find("StairAttachment").localPosition+=Vector3.down*3.8f;Capture("04-after-descent",1400,1600);
            Require(before==fixedPlatform.localToWorldMatrix&&materials.SequenceEqual(fixedPlatform.GetComponent<Renderer>().sharedMaterials),"platform changed during stair descent");
            ClearStage();Put("CollectionDeck",new Vector3(3.4f,0,0));var transit=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RefinedV2/Prefabs/PlatformDeck.prefab");var t=(GameObject)PrefabUtility.InstantiatePrefab(transit);t.transform.SetParent(stage.transform,false);t.transform.position=new Vector3(-3.4f,0,0);Ground(-.62f,RefinedModels.Hex("3F9DA6"));Frame(new Vector3(0,.1f,0),new Vector3(0,9,13),4.1f);Capture("05-transit-collection-comparison",1920,1100);
            ClearStage();exhibit=Put("CollectionWithStair",Vector3.zero);AddCollectionPeople(exhibit,16);Frame(new Vector3(0,-1,2),new Vector3(5,8,12),7.4f);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Folder+"/Scenes/CollectionReview.unity");AssetDatabase.SaveAssets();
            AssetDatabase.ExportPackage(new[]{Folder},Output+"/CollectionRimV1.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
            var rows=Meshes.Select(p=>"{\"name\":\""+p.Key+"\",\"triangles\":"+p.Value.triangles.Length/3+",\"vertices\":"+p.Value.vertexCount+"}");
            File.WriteAllText(Output+"/asset-report.json","{\"status\":\"DRAFT\",\"exportPassed\":true,\"actualUnityRenders\":true,\"platformWidth\":5.2,\"borderWidth\":0.30,\"borderDepth\":0.56,\"pylonCount\":0,\"lights\":0,\"stateColorChange\":false,\"stairIndependent\":true,\"platformRenderers\":1,\"gameIntegrated\":false,\"meshes\":["+string.Join(",",rows)+"]}");
            Debug.Log("REFINED ART EXPORT PASS");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void CheckCollectionRim()
    {
        foreach(var pair in Meshes){var m=pair.Value;Require(m.subMeshCount==1&&m.vertexCount<65536&&m.triangles.Length/3<10000,"mesh budget");Require(m.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"nonfinite geometry");Require(m.colors.Length==m.vertexCount&&m.uv2.Length==m.vertexCount,"surface data");}
        foreach(var p in Prefabs.Values)Require(p.GetComponentsInChildren<Light>(true).Length==0&&p.GetComponentsInChildren<MonoBehaviour>(true).Length==0&&p.GetComponentsInChildren<Collider>(true).Length==0,"unexpected runtime component");
        foreach(string key in new[]{"CollectionPlatform","CollectionDeck"}){
            var m=Meshes[key];Require(Prefabs[key].GetComponentsInChildren<Renderer>().Length==1,"renderer budget");
            Require(m.bounds.max.y<=.001f,"top decoration remains");Require(Mathf.Abs(m.bounds.size.x-5.2f)<.001f&&Mathf.Abs(m.bounds.size.z-5.2f)<.001f,"footprint changed");
            var vertices=m.vertices;var colors=m.colors;var normals=m.normals;
            for(int i=0;i<vertices.Length;i++){
                var v=vertices[i];if(v.y>-.003f&&Mathf.Abs(v.x)<2.29f&&Mathf.Abs(v.z)<2.29f){Require(colors[i].r>.75f&&colors[i].g>.75f,"ivory floor recolored");Require(new Vector2(v.x,v.z).sqrMagnitude<.0001f,"floor seams returned");}
                if(v.y>-.003f&&Mathf.Abs(v.x)>2.4f)Require(normals[i].y>.6f,"rim face reversed");
            }
        }
    }
}
