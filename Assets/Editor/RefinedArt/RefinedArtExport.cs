using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using StairsCrowd.ArtProduction;

public static partial class RefinedArtExport
{
    static string Folder="Assets/Art/RefinedV1", Output="artifacts/refined-assets-v1";static bool clean;
    public static void RunClean(){clean=true;Folder="Assets/Art/RefinedV2";Output="artifacts/refined-assets-v2";Run();}
    static readonly string[] Names={"Red","Blue","Green","Yellow","Purple","Black"};
    static readonly string[] Hexes={"CE5550","347FA3","528572","D7AA45","8061A0","304A50"};
    static readonly Dictionary<string,Mesh> Meshes=new Dictionary<string,Mesh>();
    static readonly Dictionary<string,GameObject> Prefabs=new Dictionary<string,GameObject>();
    static Material surface;static Camera camera;static Light sun;static GameObject stage;
    static readonly List<string> captures=new List<string>();
    static readonly CultureInfo CI=CultureInfo.InvariantCulture;
    static void Require(bool condition,string message){if(!condition)throw new Exception("Refined assets: "+message);}
    public static void Run()
    {
        try{
            Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Prefabs");Directory.CreateDirectory(Folder+"/Scenes");Directory.CreateDirectory(Output+"/renders");Directory.CreateDirectory(Output+"/obj");AssetDatabase.Refresh();
            if(clean&&!File.Exists(Folder+"/Materials/IvoryStone-v1.png")){File.Copy("Assets/Art/RefinedV1/Materials/IvoryStone-v1.png",Folder+"/Materials/IvoryStone-v1.png",true);AssetDatabase.Refresh();}
            var grain=Grain();var stoneImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Materials/IvoryStone-v1.png");
            Require(stoneImporter!=null,"limestone texture missing");stoneImporter.sRGBTexture=false;stoneImporter.mipmapEnabled=true;stoneImporter.wrapMode=TextureWrapMode.Repeat;stoneImporter.maxTextureSize=512;stoneImporter.textureCompression=TextureImporterCompression.Compressed;stoneImporter.SaveAndReimport();
            surface=MakeMaterial("Stone_Jade_Gold",Color.white,grain);surface.SetTexture("_StoneTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Materials/IvoryStone-v1.png"));surface.SetFloat("_StoneDetail",.80f);surface.SetFloat("_Detail",.65f);EditorUtility.SetDirty(surface);
            SaveMesh("Pilgrim",RefinedModels.Character());SaveMesh("PlatformTower",RefinedModels.Platform(true,clean));SaveMesh("PlatformDeck",RefinedModels.Platform(false,clean));SaveMesh("Stair",RefinedModels.Stairs());SaveMesh("Bridge02",RefinedModels.Bridge(clean));SaveMesh("DistantTower",RefinedModels.Tower());
            for(int i=0;i<Names.Length;i++)MakePrefab("Pilgrim_"+Names[i],Meshes["Pilgrim"],MakeMaterial("Cloth_"+Names[i],RefinedModels.Hex(Hexes[i]),grain));
            foreach(var key in new[]{"PlatformTower","PlatformDeck","Stair","Bridge02","DistantTower"})MakePrefab(key,Meshes[key],surface);
            if(clean){SaveMesh("StepUnit",RefinedModels.StepUnit());var lib=AssetDatabase.LoadAssetAtPath<StairsCrowd.Runtime.RefinedArtLibrary>("Assets/Resources/RefinedArtLibrary.asset");lib=lib?UnityEngine.Object.Instantiate(lib):ScriptableObject.CreateInstance<StairsCrowd.Runtime.RefinedArtLibrary>();lib.character=Meshes["Pilgrim"];lib.platform=Meshes["PlatformTower"];lib.bridge=Meshes["Bridge02"];lib.step=Meshes["StepUnit"];lib.architecture=surface;lib.characters=Names.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Cloth_"+n+".mat")).ToArray();lib.cleanFloor=true;Persist(lib,"Assets/Resources/RefinedArtLibrary.asset");}
            AssetDatabase.SaveAssets();Validate();
            PrepareScene();CaptureLineup();CaptureCharacterViews();CaptureModules();CaptureHero();
            AssetDatabase.ExportPackage(new[]{Folder},Output+"/SkyIslands-RefinedAssets-"+(clean?"v2":"v1")+".unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
            WriteReport();
            Debug.Log("REFINED ART EXPORT PASS");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Texture2D Grain()
    {
        const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGB24,true,true);var pixels=new Color[size*size];
        // Tileable low contrast grain, shared by stone and cloth. No runtime synthesis.
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float xx=x/(float)size,yy=y/(float)size;
            float g=.5f;for(int i=1;i<=8;i++){int a=new[]{3,7,13,19,29,31,41,53}[i-1],b=new[]{5,11,3,17,7,23,13,31}[i-1];g+=Mathf.Sin(2*Mathf.PI*(a*xx+b*yy)+i*.71f)*.014f;}
            pixels[y*size+x]=new Color(g,g,g,1);
        }
        texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(Folder+"/Materials/SurfaceGrain.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.Refresh();
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Materials/SurfaceGrain.png");importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Compressed;importer.maxTextureSize=256;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Materials/SurfaceGrain.png");
    }
    static Material MakeMaterial(string name,Color color,Texture2D grain)
    {
        var path=Folder+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Stairs/RefinedSurface")){name=name};AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_ClothColor",color);mat.SetTexture("_Grain",grain);mat.SetFloat("_Detail",.65f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
    }
    static void SaveMesh(string name,Mesh generated)
    {
        string path=Folder+"/Meshes/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(generated,saved);UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(saved);}else{saved=generated;AssetDatabase.CreateAsset(saved,path);}Meshes[name]=saved;ExportObj(name,saved);
    }
    static void MakePrefab(string name,Mesh mesh,Material mat)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
        Prefabs[name]=PrefabUtility.SaveAsPrefabAsset(go,Folder+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);
    }
    static void ExportObj(string name,Mesh mesh)
    {
        using(var writer=new StreamWriter(Output+"/obj/"+name+".obj")){
            writer.WriteLine("# Sky Islands Refined V1. Geometry interchange; Unity material is authoritative.");writer.WriteLine("o "+name);
            foreach(var v in mesh.vertices)writer.WriteLine(string.Format(CI,"v {0} {1} {2}",v.x,v.y,v.z));
            foreach(var v in mesh.uv)writer.WriteLine(string.Format(CI,"vt {0} {1}",v.x,v.y));
            foreach(var v in mesh.normals)writer.WriteLine(string.Format(CI,"vn {0} {1} {2}",v.x,v.y,v.z));
            var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int a=t[i]+1,b=t[i+1]+1,c=t[i+2]+1;writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");}
        }
    }
    static void Validate()
    {
        foreach(var pair in Meshes){var mesh=pair.Value;Require(mesh.vertexCount>0&&mesh.vertexCount<65536,pair.Key+" index budget");Require(mesh.colors.Length==mesh.vertexCount&&mesh.uv2.Length==mesh.vertexCount,pair.Key+" material channels");Require(mesh.vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),pair.Key+" nonfinite vertex");Require(mesh.triangles.Length/3<10000,pair.Key+" triangle budget");}
        var c=Meshes["Pilgrim"].bounds;Require(c.size.x<1.02f&&c.size.z<1.02f&&c.min.y>=-.001f&&c.max.y<2.05f,"character envelope");
        Require((Meshes["PlatformTower"].bounds.size-Meshes["PlatformDeck"].bounds.size).x==0,"platform width mismatch");
        var bridge=Meshes["Bridge02"];Require(bridge.bounds.size.y<.65f,"Bridge 02 exaggerated arch");
        Require(Prefabs.Count==11,"missing variants");foreach(var p in Prefabs.Values){Require(p.GetComponentsInChildren<Renderer>().Length==1,"renderer budget");Require(p.GetComponentsInChildren<Collider>().Length==0,"unexpected collider");Require(p.GetComponentsInChildren<MonoBehaviour>().Length==0,"unexpected runtime behaviour");}
    }
    static void PrepareScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        camera=new GameObject("Art review camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RefinedModels.Hex("3F9DA6");camera.nearClipPlane=.1f;camera.farClipPlane=160;camera.allowHDR=false;
        sun=new GameObject("Warm key",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.975f,.925f);sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(47,145,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.4f;sun.shadowNormalBias=.22f;sun.shadowStrength=.62f;
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=100;QualitySettings.antiAliasing=4;QualitySettings.shadowCascades=2;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.64f,.64f);RenderSettings.fog=false;
        stage=new GameObject("Asset exhibition");
    }
    static void ClearStage(){foreach(Transform child in stage.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);}
    static GameObject Put(string prefab,Vector3 position,float rotation=0,float scale=1)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(Prefabs[prefab]);go.transform.SetParent(stage.transform);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,rotation,0);go.transform.localScale=Vector3.one*scale;return go;
    }
    static void Ground(float y,Color color)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Studio floor";go.transform.SetParent(stage.transform);go.transform.position=new Vector3(0,y-.04f,0);go.transform.localScale=new Vector3(150,.08f,150);
        var mat=new Material(Shader.Find("Stairs/RefinedSurface")){name="Review backdrop"};mat.SetColor("_ClothColor",color);mat.SetFloat("_Detail",0);
        // Built-in cube colors default to white; use a vertex colored offline mesh.
        var m=new RefinedMesh();m.Material(color,1);m.Box(Vector3.zero,new Vector3(150,.08f,150),.01f);go.transform.localScale=Vector3.one;go.GetComponent<MeshFilter>().sharedMesh=m.Finish("Review ground");go.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void Frame(Vector3 target,Vector3 direction,float size){camera.transform.position=target+direction.normalized*45;camera.transform.LookAt(target);camera.orthographicSize=size;}
    static void Capture(string name,int w,int h)
    {
        var rt=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var old=camera.targetTexture;camera.targetTexture=rt;camera.aspect=w/(float)h;camera.Render();var before=RenderTexture.active;RenderTexture.active=rt;
        var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Output+"/renders/"+name+".png",tex.EncodeToPNG());
        var pixels=tex.GetPixels32();Require(pixels.Any(p=>p.r>180)&&pixels.Any(p=>p.r<100),"render empty or wrong exposure: "+name);Require(pixels.Count(p=>p.r>220&&p.b>220&&p.g<70)<pixels.Length/100,"missing shader: "+name);
        UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=before;camera.targetTexture=old;RenderTexture.ReleaseTemporary(rt);captures.Add(name);
    }
    static void CaptureLineup()
    {
        ClearStage();Ground(0,RefinedModels.Hex("DDD6C6"));for(int i=0;i<6;i++)Put("Pilgrim_"+Names[i],new Vector3(-(i-2.5f)*1.62f,0,0),-12);
        Frame(new Vector3(0,1,0),new Vector3(0,2.1f,8),2.65f);Capture("01-six-characters",1920,1000);
    }
    static void CaptureCharacterViews()
    {
        ClearStage();Ground(0,RefinedModels.Hex("DDD6C6"));for(int i=0;i<4;i++)Put("Pilgrim_Red",new Vector3(-(i-1.5f)*1.7f,0,0),new[]{0f,90f,180f,-40f}[i]);
        Frame(new Vector3(0,1.03f,0),new Vector3(0,1.5f,8),2.1f);Capture("02-character-turnaround",1800,1000);
    }
    static void CaptureModules()
    {
        ClearStage();Ground(-8,RefinedModels.Hex("3F9DA6"));Put("PlatformTower",Vector3.zero);
        Frame(new Vector3(0,-2.3f,0),new Vector3(9,7,12),5.9f);Capture("03-platform-tower",1400,1600);
        ClearStage();Ground(-.5f,RefinedModels.Hex("3F9DA6"));Put("PlatformDeck",Vector3.zero);Put("Pilgrim_Blue",new Vector3(1.25f,0,.7f),20);
        Frame(new Vector3(0,.1f,0),new Vector3(7,7,10),3.95f);Capture("04-platform-detail",1400,1200);
        ClearStage();Ground(-.85f,RefinedModels.Hex("3F9DA6"));Put("Stair",Vector3.zero);
        Frame(new Vector3(0,1.15f,2.6f),new Vector3(8,6,-11),4.9f);Capture("05-stairs-module",1500,1100);
        ClearStage();Ground(-1,RefinedModels.Hex("3F9DA6"));Put("Bridge02",Vector3.zero);
        Frame(new Vector3(0,0,2.6f),new Vector3(9,7,-11),4.6f);Capture("06-bridge02-module",1600,1100);
        Frame(new Vector3(0,0,2.6f),new Vector3(10,.4f,0),3.1f);Capture("07-bridge02-side",1600,600);
    }
    static void CaptureHero()
    {
        ClearStage();camera.backgroundColor=RefinedModels.Hex("3F9DA6");
        Put("PlatformTower",new Vector3(0,2.4f,0));Put("PlatformTower",new Vector3(0,0,10.4f));Put("PlatformTower",new Vector3(10.4f,0,10.4f));
        Put("Stair",new Vector3(0,0,7.8f),180);Put("Bridge02",new Vector3(2.6f,0,10.4f),90);
        for(int i=0;i<3;i++)Put("Pilgrim_Blue",new Vector3((i-1)*1.0f,2.4f,.3f),22);
        for(int i=0;i<8;i++)Put("Pilgrim_Red",new Vector3((i%4-1.5f)*1.0f,0,10+(i/4)*1.05f),-10);
        for(int i=0;i<4;i++)Put("Pilgrim_Green",new Vector3(9.8f+(i%2)*1.08f,0,9.8f+(i/2)*1.08f),15);
        var towerMat=new Material(surface){name="Distant desaturated stone"};towerMat.SetFloat("_FogStrength",1);towerMat.SetFloat("_FogLevel",8);towerMat.SetFloat("_FogDepth",15);towerMat=Persist(towerMat,Folder+"/Materials/DistantStone.mat");
        var exhibitMat=new Material(surface){name="Exhibition height haze"};exhibitMat.SetFloat("_FogStrength",1);exhibitMat=Persist(exhibitMat,Folder+"/Materials/ExhibitionStone.mat");
        foreach(var r in stage.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial==surface)r.sharedMaterial=exhibitMat;
        // Background-only material uses the same geometry shader; color is baked in
        // a separate shared mesh to keep all distant copies batched.
        var distant=UnityEngine.Object.Instantiate(Meshes["DistantTower"]);distant.name="Low contrast distant tower";var colors=distant.colors;
        for(int i=0;i<colors.Length;i++){var c=Color.Lerp(RefinedModels.Hex("3F9DA6"),colors[i],.27f);c.a=colors[i].a;colors[i]=c;}distant.colors=colors;
        distant=Persist(distant,Folder+"/Meshes/DistantTower_Fogged.asset");
        foreach(var p in new[]{new Vector3(-8,-1,-6),new Vector3(4,-2,-10),new Vector3(14,-3,-7),new Vector3(-10,-4,6),new Vector3(23,-3,8)}){var t=Put("DistantTower",p,0,1.0f);t.GetComponent<MeshFilter>().sharedMesh=distant;t.GetComponent<MeshRenderer>().sharedMaterial=towerMat;t.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
        Frame(new Vector3(4,-.2f,4.8f),new Vector3(12,15,19),13.6f);Capture("08-scene-landscape",1920,1500);
        Frame(new Vector3(3.6f,-1.4f,5.0f),new Vector3(12,15,19),18.6f);Capture("09-scene-portrait",1080,1920);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Folder+"/Scenes/AssetReview.unity");
    }
    static void WriteReport()
    {
        var rows=new List<string>();long total=0;
        foreach(var p in Meshes){long bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(p.Value);total+=bytes;rows.Add("{\"name\":\""+p.Key+"\",\"triangles\":"+p.Value.triangles.Length/3+",\"vertices\":"+p.Value.vertexCount+",\"meshBytes\":"+bytes+"}");}
        long textureBytes=0;foreach(var key in new[]{"_StoneTex","_Grain"}){var texture=surface.GetTexture(key);if(texture)textureBytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);}
        File.WriteAllText(Output+"/asset-report.json","{\"status\":\"DRAFT\",\"exportPassed\":true,\"renderChecksPassed\":true,\"prefabs\":11,\"renderersPerPrefab\":1,\"sharedCharacterMesh\":true,\"grainTextureSize\":256,\"stoneTextureImportSize\":512,\"textureResourceBytes\":"+textureBytes+",\"meshResourceBytes\":"+total+",\"wechatDeviceTested\":false,\"gameAssetsReplaced\":false,\"meshes\":["+string.Join(",",rows)+"],\"screenshots\":["+string.Join(",",captures.Select(s=>"\""+s+".png\""))+"]}");
    }
    static T Persist<T>(T source,string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing){EditorUtility.CopySerialized(source,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(source);return existing;}
        AssetDatabase.CreateAsset(source,path);return source;
    }
}
