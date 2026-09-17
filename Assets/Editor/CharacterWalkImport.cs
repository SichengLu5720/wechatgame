using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Runtime;

// Deliberately narrow, validated glTF contract. Unsupported input fails import.
public static class CharacterWalkImport
{
    const string Input = "artifacts/pilgrim-model/pilgrim-model.zip";
    const string ExpectedHash = "8073C8428386AEEA9323D8AE48E8964C4C12EE5FA11A9F6F1446A5B72FED9B32";
    const string Output = "Assets/Resources/CharacterWalk/Task003PilgrimCharacter.asset";
    [Serializable] sealed class Document { public Node[] nodes; public Geometry[] meshes; public Skin[] skins; public Accessor[] accessors; public View[] bufferViews; public Buffer[] buffers; public Animation[] animations; public SourceMaterial[] materials; }
    [Serializable] sealed class SourceMaterial { public string name; public Pbr pbrMetallicRoughness; }
    [Serializable] sealed class Pbr { public float[] baseColorFactor; public float metallicFactor,roughnessFactor; }
    [Serializable] sealed class Node { public string name; public int[] children; public float[] translation,rotation,scale; }
    [Serializable] sealed class Geometry { public Primitive[] primitives; }
    [Serializable] sealed class Primitive { public Attributes attributes; public int indices,material,mode=4; }
    [Serializable] sealed class Attributes { public int POSITION=-1,NORMAL=-1,COLOR_0=-1,JOINTS_0=-1,WEIGHTS_0=-1; }
    [Serializable] sealed class Skin { public int[] joints; public int inverseBindMatrices; }
    [Serializable] sealed class Accessor { public int bufferView,byteOffset,componentType,count; public string type; public bool normalized; }
    [Serializable] sealed class View { public int buffer,byteOffset,byteLength,byteStride; }
    [Serializable] sealed class Buffer { public string uri; public int byteLength; }
    [Serializable] sealed class Animation { public string name; public Sampler[] samplers; public Channel[] channels; }
    [Serializable] sealed class Sampler { public int input,output; public string interpolation; }
    [Serializable] sealed class Channel { public int sampler; public Target target; }
    [Serializable] sealed class Target { public int node; public string path; }
    static readonly string[] Names={"visual_root","pelvis","spine","cloak_core","neck","hood_shell","crown_inset","trim_front","leg_l","shin_l","foot_l","leg_r","shin_r","foot_r"};
    static readonly int[] Parents={-1,0,1,2,3,4,5,1,1,8,9,1,11,12};

    public static void Run()
    {
        try { Import(); Debug.Log("CHARACTER WALK IMPORT PASSED"); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void Import()
    {
        byte[] bytes;
        using(var archive=ZipFile.OpenRead(Input))
        {
            var entry=archive.GetEntry("pilgrim-reference.glb");Require(entry!=null,"Approved GLB missing from ZIP");
            using(var source=entry.Open())using(var target=new MemoryStream()){source.CopyTo(target);bytes=target.ToArray();}
        }
        string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
        Require(hash==ExpectedHash,"Source differs from user-approved TASK-003 v5 GLB");
        Require(bytes.Length>=28&&BitConverter.ToUInt32(bytes,0)==0x46546C67&&BitConverter.ToUInt32(bytes,4)==2&&BitConverter.ToUInt32(bytes,8)==bytes.Length,"Invalid GLB header");
        int jsonLength=BitConverter.ToInt32(bytes,12),binaryHeader=20+jsonLength;
        Require(BitConverter.ToUInt32(bytes,16)==0x4E4F534A&&binaryHeader+8<=bytes.Length&&BitConverter.ToUInt32(bytes,binaryHeader+4)==0x004E4942,"Invalid GLB chunks");
        var doc=JsonUtility.FromJson<Document>(Encoding.UTF8.GetString(bytes,20,jsonLength));
        int dataLength=BitConverter.ToInt32(bytes,binaryHeader);var data=new byte[dataLength];
        Require(binaryHeader+8+dataLength==bytes.Length,"GLB binary length");System.Buffer.BlockCopy(bytes,binaryHeader+8,data,0,dataLength);
        Require(doc.buffers.Length==1&&string.IsNullOrEmpty(doc.buffers[0].uri)&&data.Length>=doc.buffers[0].byteLength&&data.Length-doc.buffers[0].byteLength<=3,"Buffer length mismatch");
        Require(doc.nodes.Length==14&&doc.skins.Length==1&&doc.meshes.Length==1&&doc.meshes[0].primitives.Length==2,"Mesh/skin/node count");
        Require(doc.materials.Length==2&&doc.materials[0].name=="Warm Ivory"&&doc.materials[1].name=="Coral Cloth","Approved material slots");
        var ivoryFactor=doc.materials[0].pbrMetallicRoughness.baseColorFactor;
        // glTF PBR factors are linear. This project renders in Gamma; vertex
        // colors must contain the corresponding sRGB display values, not linear RGB.
        var ivory=new Color(Mathf.LinearToGammaSpace(ivoryFactor[0]),Mathf.LinearToGammaSpace(ivoryFactor[1]),Mathf.LinearToGammaSpace(ivoryFactor[2]),1);
        var bones=new CharacterWalkAsset.Bone[14];var parents=new int[14];for(int i=0;i<14;i++)parents[i]=-1;
        for(int i=0;i<14;i++)if(doc.nodes[i].children!=null)foreach(int child in doc.nodes[i].children){Require(child>=0&&child<14&&parents[child]<0,"Bad node hierarchy");parents[child]=i;}
        for(int i=0;i<14;i++)
        {
            var node=doc.nodes[i];Require(node.name==Names[i]&&parents[i]==Parents[i],"Frozen skeleton differs at "+node.name);
            bones[i]=new CharacterWalkAsset.Bone{name=node.name,parent=parents[i],position=Mirror(Vec(node.translation,Vector3.zero)),rotation=Rotation(node.rotation),scale=Vec(node.scale,Vector3.one)};
        }
        var primitive=doc.meshes[0].primitives[0];Require(primitive.mode==4,"Expected TRIANGLES");
        var attrs=primitive.attributes;
        var positions=Read(doc,data,attrs.POSITION,3);var normals=Read(doc,data,attrs.NORMAL,3);var joints=Read(doc,data,attrs.JOINTS_0,4);var weights=Read(doc,data,attrs.WEIGHTS_0,4);
        int count=positions.Length/3;Require(count==4946&&normals.Length==count*3&&joints.Length==count*4&&weights.Length==count*4,"Vertex budget/attributes");
        var regions=new int[count];for(int i=0;i<count;i++)regions[i]=-1;
        var allIndices=new List<int>();var faces=new HashSet<string>();
        foreach(var part in doc.meshes[0].primitives)
        {
            var a=part.attributes;Require(part.mode==4&&part.material>=0&&part.material<2&&a.POSITION==attrs.POSITION&&a.NORMAL==attrs.NORMAL&&a.JOINTS_0==attrs.JOINTS_0&&a.WEIGHTS_0==attrs.WEIGHTS_0,"Primitive contract differs");
            var input=Read(doc,data,part.indices,1);Require(input.Length%3==0,"Incomplete triangle");
            for(int i=0;i<input.Length;i+=3)
            {
                int x=(int)input[i],y=(int)input[i+1],z=(int)input[i+2];
                Require(x!=y&&x!=z&&y!=z&&faces.Add(Math.Min(x,Math.Min(y,z))+":"+(x+y+z-Math.Min(x,Math.Min(y,z))-Math.Max(x,Math.Max(y,z)))+":"+Math.Max(x,Math.Max(y,z))),"Overlapping/degenerate material triangle");
                foreach(int index in new[]{x,y,z}){Require(index>=0&&index<count,"Index out of range");Require(regions[index]<0||regions[index]==part.material,"Vertex crosses material boundary");regions[index]=part.material;}
                allIndices.Add(x);allIndices.Add(z);allIndices.Add(y);
            }
        }
        var triangles=allIndices.ToArray();Require(triangles.Length==2842*3,"Missing source triangles");
        var mesh=new Mesh{name="TASK-003 shared pilgrim"};var v=new Vector3[count];var n=new Vector3[count];var c=new Color[count];var uv=new Vector2[count];var surface=new Vector2[count];var bw=new BoneWeight[count];
        for(int i=0;i<count;i++)
        {
            v[i]=new Vector3(-positions[i*3],positions[i*3+1],positions[i*3+2]);n[i]=new Vector3(-normals[i*3],normals[i*3+1],normals[i*3+2]);
            Require(v[i].y>=-.00001f&&v[i].y<=2&&new Vector2(v[i].x,v[i].z).magnitude<=.49001f,"Bind envelope");
            Require(regions[i]>=0,"Source vertex missing from material regions");float mask=regions[i];
            // Translate the exchange dye mask into the existing shader's surface.y.
            // Fixed ivory is kept in vertex RGB; shader properties remain intact.
            c[i]=Color.Lerp(ivory,Color.white,mask);
            uv[i]=new Vector2(v[i].x,v[i].y);surface[i]=new Vector2(.85f,mask);
            int at=i*4;float sum=0;for(int k=0;k<4;k++){Require(joints[at+k]>=0&&joints[at+k]<doc.skins[0].joints.Length&&weights[at+k]>=0,"Invalid joint weight");sum+=weights[at+k];}Require(Mathf.Abs(sum-1)<.0001f,"Weights do not sum to 1");
            bw[i]=new BoneWeight{boneIndex0=(int)joints[at],boneIndex1=(int)joints[at+1],boneIndex2=(int)joints[at+2],boneIndex3=(int)joints[at+3],weight0=weights[at],weight1=weights[at+1],weight2=weights[at+2],weight3=weights[at+3]};
        }
        var matrices=Read(doc,data,doc.skins[0].inverseBindMatrices,16);var bind=new Matrix4x4[doc.skins[0].joints.Length];var mirror=Matrix4x4.Scale(new Vector3(-1,1,1));
        Require(matrices.Length==bind.Length*16&&bind.Length<=20,"Bind matrix budget");
        for(int i=0;i<bind.Length;i++){var m=new Matrix4x4();for(int k=0;k<16;k++)m[k]=matrices[i*16+k];bind[i]=mirror*m*mirror;}
        mesh.vertices=v;mesh.normals=n;mesh.colors=c;mesh.uv=uv;mesh.uv2=surface;mesh.triangles=triangles;mesh.boneWeights=bw;mesh.bindposes=bind;mesh.RecalculateBounds();
        Require(doc.animations.Length==1&&doc.animations[0].name=="walk_a_in_place_v1","Clip key/count");var animation=doc.animations[0];
        var channels=new CharacterWalkAsset.Channel[animation.channels.Length];
        for(int i=0;i<channels.Length;i++)
        {
            var input=animation.channels[i];var sampler=animation.samplers[input.sampler];string path=input.target.path;
            Require(sampler.interpolation=="LINEAR"||sampler.interpolation=="STEP","Unsupported interpolation");Require(path=="translation"||path=="rotation"||path=="scale","Unsupported channel");Require(input.target.node>0&&input.target.node<14,"Root motion/invalid target");
            var times=Read(doc,data,sampler.input,1);int width=path=="rotation"?4:3;var values=Read(doc,data,sampler.output,width);
            Require(times.Length>=2&&values.Length==times.Length*width&&Mathf.Abs(times[0])<.00001f&&Mathf.Abs(times[times.Length-1]-1)<.00001f,"Clip duration/sample count");var converted=new Vector4[times.Length];
            for(int k=0;k<times.Length;k++)
            {
                Require(k==0||times[k]>times[k-1],"Clip keys must increase");int at=k*width;
                if(path=="rotation")converted[k]=new Vector4(values[at],-values[at+1],-values[at+2],values[at+3]);
                else converted[k]=new Vector4(path=="translation"?-values[at]:values[at],values[at+1],values[at+2],0);
            }
            Require(Vector4.Distance(converted[0],converted[converted.Length-1])<.00001f,"Loop seam");
            if(input.target.node==3)foreach(var vKey in converted)Require(path=="scale"&&vKey.x==1&&vKey.z==1&&vKey.y>=.985f&&vKey.y<=1,"Cloak must compress only Y");
            channels[i]=new CharacterWalkAsset.Channel{bone=input.target.node,path=path,step=sampler.interpolation=="STEP",times=times,values=converted};
        }
        // Do not enable assets until all source checks passed. Updating an existing
        // generated asset preserves its GUID and references.
        Directory.CreateDirectory("Assets/Art/CharacterWalk/Source");Directory.CreateDirectory("Assets/Resources/CharacterWalk");
        File.WriteAllBytes("Assets/Art/CharacterWalk/Source/pilgrim-approved-v5.glb",bytes);
        File.WriteAllText("Assets/Art/CharacterWalk/Source/approved-v5-manifest.json","{\"taskId\":\"TASK-003\",\"taskVersion\":5,\"sha256\":\""+hash+"\",\"vertices\":4946,\"triangles\":2842,\"sourceMaterials\":2,\"runtimeMaterialSlots\":1,\"materialConversion\":\"Primitive membership to lossless vertex dye mask; glTF linear ivory factor converted to Gamma sRGB; cloth uses ColorCatalog\"}");
        AssetDatabase.Refresh();var asset=AssetDatabase.LoadAssetAtPath<CharacterWalkAsset>(Output);
        if(!asset){asset=ScriptableObject.CreateInstance<CharacterWalkAsset>();AssetDatabase.CreateAsset(asset,Output);}
        var oldMesh=asset.mesh;
        if(oldMesh)
        {
            // CopySerialized does not reliably replace a Mesh's native vertex
            // buffer when the vertex layout/count changes. Write every channel.
            oldMesh.Clear();oldMesh.vertices=v;oldMesh.normals=n;oldMesh.colors=c;oldMesh.uv=uv;oldMesh.uv2=surface;
            oldMesh.triangles=triangles;oldMesh.boneWeights=bw;oldMesh.bindposes=bind;oldMesh.RecalculateBounds();
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        else{oldMesh=mesh;AssetDatabase.AddObjectToAsset(mesh,asset);}
        Require(oldMesh.vertexCount==count&&oldMesh.triangles.Length==triangles.Length,"Imported mesh differs from source");
        var importedVertices=oldMesh.vertices;for(int i=0;i<count;i++)Require(importedVertices[i]==v[i],"Imported vertex differs from source");
        var importedNormals=oldMesh.normals;for(int i=0;i<count;i++)Require(importedNormals[i]==n[i],"Imported hard-edge normal differs from source");
        var importedTriangles=oldMesh.triangles;for(int i=0;i<triangles.Length;i++)Require(importedTriangles[i]==triangles[i],"Imported triangle differs from source");
        asset.mesh=oldMesh;asset.bones=bones;asset.joints=doc.skins[0].joints;asset.channels=channels;asset.clipKey=animation.name;asset.sourceHash=hash;asset.duration=1;asset.cycleDistance=1.25f;asset.leftFoot=10;asset.rightFoot=13;
        asset.sourceVertexCount=count;asset.sourceTriangleCount=triangles.Length/3;
        asset.runtimeEnabled=true;
        EditorUtility.SetDirty(oldMesh);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
        Debug.Log("CHARACTER IMPORT "+count+" vertices / "+triangles.Length/3+" triangles hash="+hash);
    }
    static Vector3 Vec(float[] v,Vector3 fallback)=>v==null||v.Length==0?fallback:new Vector3(v[0],v[1],v[2]);
    static Vector3 Mirror(Vector3 v)=>new Vector3(-v.x,v.y,v.z);
    static Quaternion Rotation(float[] q)=>q==null||q.Length==0?Quaternion.identity:new Quaternion(q[0],-q[1],-q[2],q[3]);
    static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
    static float[] Read(Document doc,byte[] data,int index,int width)
    {
        Require(index>=0&&index<doc.accessors.Length,"Missing accessor");var a=doc.accessors[index];var view=doc.bufferViews[a.bufferView];
        string type=width==1?"SCALAR":width==3?"VEC3":width==4?"VEC4":"MAT4";Require(a.type==type&&view.buffer==0,"Accessor type/buffer");
        int size=a.componentType==5126||a.componentType==5125?4:a.componentType==5123?2:a.componentType==5121?1:0;Require(size>0,"Unsupported component type");
        int stride=view.byteStride==0?width*size:view.byteStride;var result=new float[a.count*width];
        for(int i=0;i<a.count;i++)for(int k=0;k<width;k++)
        {
            int at=view.byteOffset+a.byteOffset+i*stride+k*size;Require(at>=view.byteOffset&&at+size<=view.byteOffset+view.byteLength&&at+size<=data.Length,"Accessor outside buffer view");
            float value=a.componentType==5126?BitConverter.ToSingle(data,at):a.componentType==5125?BitConverter.ToUInt32(data,at):a.componentType==5123?BitConverter.ToUInt16(data,at):data[at];
            if(a.normalized&&a.componentType!=5126)value/=a.componentType==5123?65535:255;
            Require(!float.IsNaN(value)&&!float.IsInfinity(value),"Nonfinite accessor");result[i*width+k]=value;
        }return result;
    }
}
