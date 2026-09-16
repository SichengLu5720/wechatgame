using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BeakedTubeConcept
{
    const string Folder="Assets/Art/Concepts/BeakedTube";

    public static void Build()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("artifacts/concepts");
        var bodyMaterial=Material("Body",new Color(.25f,.57f,.68f));
        var baseMaterial=Material("Base",new Color(.82f,.76f,.65f));
        var bodyMesh=SaveMesh("StraightTube",TubeMesh());
        var beakMesh=SaveMesh("PointedBeak",BeakMesh());

        var model=new GameObject("Beaked Tube Character");
        var body=new GameObject("Straight tube",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(model.transform,false);body.GetComponent<MeshFilter>().sharedMesh=bodyMesh;body.GetComponent<MeshRenderer>().sharedMaterial=bodyMaterial;
        var beak=new GameObject("Single pointed beak",typeof(MeshFilter),typeof(MeshRenderer));beak.transform.SetParent(model.transform,false);beak.GetComponent<MeshFilter>().sharedMesh=beakMesh;beak.GetComponent<MeshRenderer>().sharedMaterial=bodyMaterial;
        var collider=model.AddComponent<CapsuleCollider>();collider.radius=.38f;collider.height=1.55f;collider.center=Vector3.up*.775f;
        PrefabUtility.SaveAsPrefabAsset(model,Folder+"/BeakedTubeCharacter.prefab");

        var stage=new GameObject("Concept Preview");model.transform.SetParent(stage.transform,false);
        var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pedestal.name="Preview pedestal";pedestal.transform.SetParent(stage.transform,false);pedestal.transform.position=Vector3.down*.09f;pedestal.transform.localScale=new Vector3(.82f,.09f,.82f);pedestal.GetComponent<Renderer>().sharedMaterial=baseMaterial;
        Object.DestroyImmediate(pedestal.GetComponent<Collider>());
        var light=new GameObject("Soft key",typeof(Light)).GetComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.intensity=1.15f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(42,-28,0);
        Render(stage,"artifacts/concepts/beaked-tube-character.png");
        Object.DestroyImmediate(stage);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        AssetDatabase.ExportPackage(new[]{Folder},"artifacts/concepts/BeakedTubeCharacter.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
        Debug.Log("BEAKED TUBE CONCEPT READY");EditorApplication.Exit(0);
    }

    static Material Material(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Stairs/Pastel"));AssetDatabase.CreateAsset(material,path);}material.color=color;material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old)AssetDatabase.DeleteAsset(path);mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh TubeMesh()
    {
        const int sides=16;float[] y={0,.07f,1.28f,1.43f,1.52f};float[] radius={.31f,.38f,.38f,.31f,0};
        var vertices=new List<Vector3>();for(int ring=0;ring<y.Length;ring++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Sin(a)*radius[ring],y[ring],Mathf.Cos(a)*radius[ring]));}
        var triangles=new List<int>();for(int ring=0;ring<y.Length-1;ring++)for(int i=0;i<sides;i++){int a=ring*sides+i,b=ring*sides+(i+1)%sides,c=(ring+1)*sides+i,d=(ring+1)*sides+(i+1)%sides;triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);}
        int bottom=vertices.Count;vertices.Add(Vector3.zero);for(int i=0;i<sides;i++){triangles.Add(bottom);triangles.Add((i+1)%sides);triangles.Add(i);}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh BeakMesh()
    {
        var v=new[]{new Vector3(-.18f,1.08f,.31f),new Vector3(.18f,1.08f,.31f),new Vector3(-.18f,1.30f,.31f),new Vector3(.18f,1.30f,.31f),new Vector3(0,1.17f,.82f)};
        int[] t={0,1,4,1,3,4,3,2,4,2,0,4,0,2,3,0,3,1};var mesh=new Mesh();mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Render(GameObject stage,string output)
    {
        var cameraObject=new GameObject("Preview Camera");cameraObject.transform.SetParent(stage.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.91f,.87f,.79f);camera.orthographic=true;camera.orthographicSize=1.35f;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.transform.position=new Vector3(2.4f,1.8f,4.2f);camera.transform.LookAt(new Vector3(0,.72f,0));
        var target=new RenderTexture(720,720,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var texture=new Texture2D(720,720,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,720,720),0,0);texture.Apply();File.WriteAllBytes(output,texture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);
    }
}
