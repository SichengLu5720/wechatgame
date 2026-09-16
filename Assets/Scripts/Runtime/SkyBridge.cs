using System.Collections.Generic;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        public static float Camber(float t){t=Mathf.Clamp01(t);return .22f*4*t*(1-t);}
        float BridgeLift(Vector3 p)
        {
            foreach(var s in space.Stairs){if(s.polygon!=null||s.steps!=1||Mathf.Abs(p.y-s.start.y)>.5f)continue;float y;if(s.Height(new Vector2(p.x,p.z),out y))return Camber(Vector3.Dot(p-s.start,s.Direction)/s.Length);}
            return 0;
        }
        void SkyBridge(StairSurface s)
        {
            // Keep the verified navigation plane; the render-only offset follows the deck.
            var support=Box("Bridge navigation plane",(s.start+s.end)/2-Vector3.up*.13f,new Vector3(s.width,.26f,s.Length),ivory,true);
            support.transform.rotation=Quaternion.LookRotation(s.Direction);support.GetComponent<Renderer>().enabled=false;
            var vertices=new List<Vector3>();var triangles=new List<int>();var side=Vector3.Cross(Vector3.up,s.Direction);
            System.Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
            const int segments=24;
            for(int i=0;i<segments;i++){
                float t=i/(float)segments,u=(i+1)/(float)segments;var a=Vector3.Lerp(s.start,s.end,t)+Vector3.up*Camber(t);var b=Vector3.Lerp(s.start,s.end,u)+Vector3.up*Camber(u);
                var al=a-side*s.width/2;var ar=a+side*s.width/2;var bl=b-side*s.width/2;var br=b+side*s.width/2;var down=Vector3.down*.26f;
                quad(al,bl,br,ar);quad(al+down,ar+down,br+down,bl+down);quad(al,al+down,bl+down,bl);quad(ar,br,br+down,ar+down);
                if(i==0)quad(al,ar,ar+down,al+down);if(i==segments-1)quad(bl,bl+down,br+down,br);
                Beam("Bridge low edge",al+Vector3.up*.035f,bl+Vector3.up*.035f,.07f,.07f,ivory);Beam("Bridge low edge",ar+Vector3.up*.035f,br+Vector3.up*.035f,.07f,.07f,ivory);
                if(i>0&&i%4==0)Beam("Bridge slab joint",al+Vector3.up*.004f,ar+Vector3.up*.004f,.012f,.007f,marking);
            }
            var mesh=new Mesh{name="Bridge 02 shallow cambered deck"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var model=new GameObject("Bridge 02",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(activeParent,false);model.transform.position=Vector3.zero;model.GetComponent<MeshFilter>().sharedMesh=mesh;model.GetComponent<MeshRenderer>().sharedMaterial=ivory;
        }
    }
}
