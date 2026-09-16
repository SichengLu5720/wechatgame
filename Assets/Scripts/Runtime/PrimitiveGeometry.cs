using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class PrimitiveGeometry
    {
        static readonly Dictionary<PrimitiveType,Mesh> meshes=new Dictionary<PrimitiveType,Mesh>();
        public static GameObject Create(PrimitiveType type)
        {
            Mesh mesh;
            if(!meshes.TryGetValue(type,out mesh)){
                mesh=Resources.Load<Mesh>("Geometry/"+type);
#if UNITY_EDITOR
                if(!mesh)return GameObject.CreatePrimitive(type);
#endif
                if(!mesh)throw new System.InvalidOperationException("Missing baked primitive: "+type);
                meshes.Add(type,mesh);
            }
            var go=new GameObject(type.ToString(),typeof(MeshFilter),typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            // Only cubes retain a collider; decorative parts have none.
            if(type==PrimitiveType.Cube)go.AddComponent<BoxCollider>();
            return go;
        }
    }
}