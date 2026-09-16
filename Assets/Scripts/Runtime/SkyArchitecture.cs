using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        void SkyStonework(int node,Vector3 center,float size)
        {
            if(space.Sides[node]!=4)return;ivory.SetFloat("_StoneShade",1);
            var rotation=Quaternion.Euler(0,-space.Rotations[node]*Mathf.Rad2Deg,0);float half=size*.5f;
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){var cap=Box("Jade corner inlay",center+rotation*new Vector3(x*(half-.17f),-.10f,z*(half-.17f)),new Vector3(.32f,.24f,.32f),teal,false);cap.transform.rotation=rotation;}
            // Hairline slab joints only, without changing the walkable surface.
            for(int axis=0;axis<2;axis++)for(int i=0;i<=0;i++){Vector3 a=new Vector3(-half+.25f,.006f,i*size/4),b=new Vector3(half-.25f,.006f,i*size/4);if(axis==1){a=new Vector3(a.z,a.y,a.x);b=new Vector3(b.z,b.y,b.x);}Beam("Stone slab joint",center+rotation*a,center+rotation*b,.014f,.008f,marking);}
            // Solid spandrels with a rounded opening connect the four stone piers.
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            System.Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
            for(int side=0;side<4;side++){var turn=rotation*Quaternion.Euler(0,side*90,0);float radius=half-.48f;
                for(int j=0;j<20;j++){float x=Mathf.Lerp(-radius,radius,j/20f),xx=Mathf.Lerp(-radius,radius,(j+1)/20f);float y=-3.1f+2.35f*Mathf.Sqrt(Mathf.Max(0,1-x*x/(radius*radius))),yy=-3.1f+2.35f*Mathf.Sqrt(Mathf.Max(0,1-xx*xx/(radius*radius)));
                    var a=center+turn*new Vector3(x,-.43f,half-.18f);var b=center+turn*new Vector3(xx,-.43f,half-.18f);var c=center+turn*new Vector3(xx,yy,half-.18f);var d=center+turn*new Vector3(x,y,half-.18f);var back=turn*new Vector3(0,0,-.55f);
                    quad(a,d,c,b);quad(a+back,b+back,c+back,d+back);quad(d,d+back,c+back,c);quad(a,b,b+back,a+back);
                }
            }
            var mesh=new Mesh{name="Ivory arched platform walls"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var wall=new GameObject("Arched stone walls",typeof(MeshFilter),typeof(MeshRenderer));wall.transform.SetParent(activeParent,false);wall.transform.position=Vector3.zero;wall.GetComponent<MeshFilter>().sharedMesh=mesh;wall.GetComponent<MeshRenderer>().sharedMaterial=ivory;
        }
    }
}



