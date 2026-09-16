using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StairsCrowd.ArtProduction;

public static partial class RefinedArtExport
{
    static readonly Color CollectionTeal=RefinedModels.Hex("24494C");
    static Mesh SolidPylon()
    {
        var m=new RefinedMesh();
        m.Material(CollectionTeal,.56f);m.Box(new Vector3(0,.09f,0),new Vector3(.66f,.18f,.66f),.038f);
        m.Material(RefinedModels.Gold,.38f);m.Box(new Vector3(0,.20f,0),new Vector3(.61f,.045f,.61f),.014f);
        m.Material(CollectionTeal,.56f);m.Box(new Vector3(0,.68f,0),new Vector3(.54f,.92f,.54f),.045f);
        m.Material(RefinedModels.Gold,.38f);m.Box(new Vector3(0,1.125f,0),new Vector3(.57f,.042f,.57f),.014f);
        m.Material(CollectionTeal,.53f);m.Box(new Vector3(0,1.177f,0),new Vector3(.59f,.062f,.59f),.022f);
        // Eight clean roof facets and a tiny flat apex, no window, lens or light insert.
        var ring=new[]{new Vector3(-.225f,1.208f,-.28f),new Vector3(.225f,1.208f,-.28f),new Vector3(.28f,1.208f,-.225f),new Vector3(.28f,1.208f,.225f),new Vector3(.225f,1.208f,.28f),new Vector3(-.225f,1.208f,.28f),new Vector3(-.28f,1.208f,.225f),new Vector3(-.28f,1.208f,-.225f)};
        for(int i=0;i<8;i++){
            int j=(i+1)%8;var a=new Vector3(ring[i].x*.13f,1.43f,ring[i].z*.13f);var b=new Vector3(ring[j].x*.13f,1.43f,ring[j].z*.13f);
            m.Quad(ring[i],a,b,ring[j]);m.Tri(new Vector3(0,1.43f,0),b,a);
        }
        return m.Finish("Solid octagonal cap collection pylon - no light");
    }
    static Mesh CollectionBase(bool tower)
    {
        var mesh=RefinedModels.Platform(tower,true);var c=mesh.colors;var v=mesh.vertices;var uv=mesh.uv2;
        for(int i=0;i<v.Length;i++){
            bool fascia=v[i].y>=-.481f&&v[i].y<=-.074f;
            if(fascia){c[i]=CollectionTeal;uv[i]=new Vector2(.56f,0);}
        }
        mesh.colors=c;mesh.uv2=uv;return mesh;
    }
    static Mesh CombineCollection(Mesh deck,Mesh pylons)
    {
        var mesh=new Mesh();mesh.CombineMeshes(new[]{new CombineInstance{mesh=deck,transform=Matrix4x4.identity},new CombineInstance{mesh=pylons,transform=Matrix4x4.identity}});return mesh;
    }
    public static void RunCollection()
    {
        try{
            Folder="Assets/Art/CollectionSolidV1";Output="artifacts/collection-solid-v1";
            foreach(string sub in new[]{"Meshes","Materials","Prefabs","Scenes"})Directory.CreateDirectory(Folder+"/"+sub);
            Directory.CreateDirectory(Output+"/renders");Directory.CreateDirectory(Output+"/obj");AssetDatabase.Refresh();
            // Reuse the existing production shader and compressed stone maps.
            surface=Persist(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/RefinedV2/Materials/Stone_Jade_Gold.mat")),Folder+"/Materials/CollectionStone.mat");surface.name="Collection stone teal gold - static";surface.SetFloat("_GameplayLighting",0);surface.SetFloat("_FogStrength",0);
            SaveMesh("SolidPylon",SolidPylon());
            var combines=new List<CombineInstance>();for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)combines.Add(new CombineInstance{mesh=Meshes["SolidPylon"],transform=Matrix4x4.Translate(new Vector3(x*2.4f,.17f,z*2.4f))});
            var pylons=new Mesh();pylons.CombineMeshes(combines.ToArray());SaveMesh("FourPylons",pylons);
            SaveMesh("CollectionBase",CollectionBase(true));SaveMesh("CollectionDeckBase",CollectionBase(false));
            SaveMesh("CollectionPlatform",CombineCollection(Meshes["CollectionBase"],Meshes["FourPylons"]));
            SaveMesh("CollectionDeck",CombineCollection(Meshes["CollectionDeckBase"],Meshes["FourPylons"]));
            SaveMesh("Stair",RefinedModels.Stairs());
            foreach(var name in new[]{"SolidPylon","FourPylons","CollectionPlatform","CollectionDeck","Stair"})MakePrefab(name,Meshes[name],surface);
            var kit=new GameObject("Collection with independent staircase");
            var platform=(GameObject)PrefabUtility.InstantiatePrefab(Prefabs["CollectionPlatform"]);platform.name="Platform";platform.transform.SetParent(kit.transform,false);
            var stair=(GameObject)PrefabUtility.InstantiatePrefab(Prefabs["Stair"]);stair.name="StairAttachment";stair.transform.SetParent(kit.transform,false);stair.transform.localPosition=new Vector3(0,-2.4f,7.8f);stair.transform.localRotation=Quaternion.Euler(0,180,0);
            Prefabs["CollectionWithStair"]=PrefabUtility.SaveAsPrefabAsset(kit,Folder+"/Prefabs/CollectionWithStair.prefab");UnityEngine.Object.DestroyImmediate(kit);
            AssetDatabase.SaveAssets();CheckCollectionAssets();PrepareScene();
            ClearStage();Put("CollectionDeck",Vector3.zero);Ground(-.6f,RefinedModels.Hex("3F9DA6"));Frame(new Vector3(0,.30f,0),new Vector3(7,8,11),4.05f);Capture("01-platform-empty",1600,1400);
            ClearStage();Put("SolidPylon",Vector3.zero);Ground(-.01f,RefinedModels.Hex("DDD6C6"));Frame(Vector3.up*.76f,new Vector3(4,2.8f,6),1.15f);Capture("02-solid-pylon-detail",1200,1400);
            ClearStage();sun.shadows=LightShadows.None;var exhibit=Put("CollectionWithStair",Vector3.zero);AddCollectionPeople(exhibit,16);Frame(new Vector3(0,-1.4f,2),new Vector3(5,8,12),7.7f);Capture("03-before-descent",1400,1600);
            var originalPosition=exhibit.transform.Find("Platform").position;var beforeColor=surface.GetColor("_ClothColor");
            exhibit.transform.Find("StairAttachment").localPosition+=Vector3.down*3.8f;Capture("04-after-descent",1400,1600);
            Require(exhibit.transform.Find("Platform").position==originalPosition&&surface.GetColor("_ClothColor")==beforeColor,"completion changes platform or material");
            ClearStage();Put("CollectionDeck",new Vector3(3.4f,0,0));var transit=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RefinedV2/Prefabs/PlatformDeck.prefab");var t=(GameObject)PrefabUtility.InstantiatePrefab(transit);t.transform.SetParent(stage.transform,false);t.transform.position=new Vector3(-3.4f,0,0);Ground(-.6f,RefinedModels.Hex("3F9DA6"));Frame(new Vector3(0,.2f,0),new Vector3(0,9,13),4.1f);Capture("05-transit-collection-comparison",1920,1100);
            ClearStage();exhibit=Put("CollectionWithStair",Vector3.zero);AddCollectionPeople(exhibit,16);Frame(new Vector3(0,-1,2),new Vector3(5,8,12),7.4f);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Folder+"/Scenes/CollectionReview.unity");AssetDatabase.SaveAssets();
            AssetDatabase.ExportPackage(new[]{Folder},Output+"/CollectionSolidV1.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
            var rows=Meshes.Select(p=>"{\"name\":\""+p.Key+"\",\"triangles\":"+p.Value.triangles.Length/3+",\"vertices\":"+p.Value.vertexCount+"}");
            File.WriteAllText(Output+"/asset-report.json","{\"status\":\"DRAFT\",\"exportPassed\":true,\"actualUnityRenders\":true,\"platformWidth\":5.2,\"pylonCount\":4,\"lights\":0,\"stateColorChange\":false,\"stairIndependent\":true,\"platformRenderers\":1,\"gameIntegrated\":false,\"meshes\":["+string.Join(",",rows)+"]}");
            Debug.Log("REFINED ART EXPORT PASS");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void AddCollectionPeople(GameObject parent,int count)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RefinedV2/Prefabs/Pilgrim_Red.prefab");
        for(int i=0;i<count;i++){var p=(GameObject)PrefabUtility.InstantiatePrefab(prefab);p.transform.SetParent(parent.transform,false);p.transform.localPosition=new Vector3((i%4-1.5f)*1.12f,0,(i/4-1.5f)*1.12f);}
    }
    static void CheckCollectionAssets()
    {
        Require(Meshes["FourPylons"].vertexCount==Meshes["SolidPylon"].vertexCount*4,"four equal pylons");
        foreach(var pair in Meshes){var m=pair.Value;Require(m.subMeshCount==1&&m.vertexCount<65536&&m.triangles.Length/3<10000,"mesh budget");Require(m.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"nonfinite geometry");Require(m.colors.Length==m.vertexCount&&m.uv2.Length==m.vertexCount,"surface data");}
        foreach(var p in Prefabs.Values){Require(p.GetComponentsInChildren<Light>(true).Length==0&&p.GetComponentsInChildren<MonoBehaviour>(true).Length==0&&p.GetComponentsInChildren<Collider>(true).Length==0,"unexpected runtime component");}
        Require(Prefabs["CollectionPlatform"].GetComponentsInChildren<Renderer>().Length==1,"platform renderer budget");
        Require(Meshes["CollectionBase"].vertices.All(v=>!(Mathf.Abs(v.y+.001f)<.002f&&Mathf.Abs(v.x)<2.1f&&Mathf.Abs(v.z)<2.1f&&new Vector2(v.x,v.z).sqrMagnitude>.0001f)),"floor grid lines returned");
        var deck=Meshes["CollectionBase"];for(int i=0;i<deck.vertexCount;i++){var v=deck.vertices[i];if(Mathf.Abs(v.y+.001f)<.002f&&Mathf.Abs(v.x)<2.49f&&Mathf.Abs(v.z)<2.49f)Require(deck.colors[i].r>.75f&&deck.colors[i].g>.75f,"ivory top was recolored");}
    }
}
