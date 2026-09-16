using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace StairsCrowd.Runtime
{
    // Fixed-camera 2.5D geometry: one mesh, no textures, no per-frame generation.
    public sealed class FixedTowerBackdrop:MonoBehaviour
    {
        public const int TowerCount=3;
        Mesh mesh;Material material;MeshRenderer towerRenderer;Transform geometry;Camera backdrop;float aspect=-1;
        public int MeshId=>mesh.GetInstanceID();
        public int Triangles=>mesh.triangles.Length/3;
        public long ResourceBytes=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh)+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(material);
        public void SetFocus(bool focused){towerRenderer.enabled=!focused;}
        public void Initialize(Camera main)
        {
            backdrop=GetComponent<Camera>();backdrop.orthographic=true;backdrop.orthographicSize=1;backdrop.nearClipPlane=.1f;backdrop.farClipPlane=30;backdrop.transform.SetPositionAndRotation(new Vector3(0,0,-10),Quaternion.identity);backdrop.cullingMask=1<<30;
            main.cullingMask&=~(1<<30);main.clearFlags=CameraClearFlags.Depth;
            var v=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var anchors=new List<Vector2>();var triangles=new List<int>();float anchorX=0;
            // Background atmosphere shares this draw call and never allocates a texture.
            v.AddRange(new[]{new Vector3(0,-1,1),new Vector3(0,1,1),new Vector3(0,1,1),new Vector3(0,-1,1)});
            anchors.AddRange(new[]{new Vector2(-1,0),new Vector2(-1,0),new Vector2(1,0),new Vector2(1,0)});
            for(int n=0;n<4;n++){colors.Add(Color.white);uv.Add(Vector2.zero);}triangles.AddRange(new[]{0,1,2,0,2,3});
            var light=new Color(.80f,.85f,.75f);var shade=new Color(.35f,.59f,.62f);var top=new Color(.87f,.88f,.77f);var recess=new Color(.28f,.57f,.60f);
            float bottom=0,height=1,visibility=1;
            System.Action<Vector3,Vector3,Vector3,Color> tri=(a,b,c,col)=>{int n=v.Count;v.Add(a-Vector3.right*anchorX);v.Add(b-Vector3.right*anchorX);v.Add(c-Vector3.right*anchorX);for(int k=0;k<3;k++)anchors.Add(new Vector2(anchorX,0));colors.Add(col);colors.Add(col);colors.Add(col);uv.Add(new Vector2(Mathf.Clamp01((a.y-bottom)/height),visibility));uv.Add(new Vector2(Mathf.Clamp01((b.y-bottom)/height),visibility));uv.Add(new Vector2(Mathf.Clamp01((c.y-bottom)/height),visibility));triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);};
            System.Action<Vector3,Vector3,Vector3,Vector3,Color> quad=(a,b,c,d,col)=>{tri(a,b,c,col);tri(a,c,d,col);};
            // Normalized screen anchors: no random seed, clock or level-dependent input.
            var layout=new[]{new Vector4(.24f,.78f,.030f,.22f),new Vector4(.95f,.45f,.040f,.57f),new Vector4(-.95f,-.42f,.035f,.42f)};
            for(int i=0;i<layout.Length;i++){
                var p=layout[i];float x=p.x,y=p.y,w=p.z;bottom=y-p.w;height=p.w;visibility=i==0?.12f:i==2?.13f:.18f;
                anchorX=x;var orientation=Quaternion.LookRotation(-new Vector3(11,20,-22));var right=orientation*Vector3.right;var up=orientation*Vector3.up;
                System.Func<Vector3,Vector3> project=point=>new Vector3(x+Vector3.Dot(point,right),y+Vector3.Dot(point,up),0);
                var a=project(new Vector3(-w,0,-w));var b=project(new Vector3(w,0,-w));var c=project(new Vector3(w,0,w));var d=project(new Vector3(-w,0,w));
                quad(a,new Vector3(a.x,bottom,0),new Vector3(b.x,bottom,0),b,light);quad(b,new Vector3(b.x,bottom,0),new Vector3(c.x,bottom,0),c,shade);quad(a,b,c,d,top);
                // A shallow arched recess on the lit face; fog uses the same height.
                float r=w*.26f,low=bottom+.02f;
                for(int j=0;j<8;j++){float t=j*Mathf.PI/8,u=(j+1)*Mathf.PI/8;var l=project(new Vector3(Mathf.Cos(t)*r,-.15f+Mathf.Sin(t)*r,-w));var rr=project(new Vector3(Mathf.Cos(u)*r,-.15f+Mathf.Sin(u)*r,-w));l.z=rr.z=-.01f;quad(l,new Vector3(l.x,low,-.01f),new Vector3(rr.x,low,-.01f),rr,recess);}
                float cap=w*.48f;var ca=project(new Vector3(-cap,.065f,-cap));var cb=project(new Vector3(cap,.065f,-cap));var cc=project(new Vector3(cap,.065f,cap));var cd=project(new Vector3(-cap,.065f,cap));ca.z=cb.z=cc.z=cd.z=-.02f;
                quad(ca,ca-Vector3.up*.05f,cb-Vector3.up*.05f,cb,light);quad(cb,cb-Vector3.up*.05f,cc-Vector3.up*.05f,cc,shade);
                quad(ca,cb,cc,cd,top);
            }
            mesh=new Mesh{name="Fixed three tower background"};mesh.SetVertices(v);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            mesh.SetUVs(1,anchors);mesh.bounds=new Bounds(Vector3.zero,Vector3.one*20);var go=new GameObject("Fixed tower mesh",typeof(MeshFilter),typeof(MeshRenderer));go.layer=30;geometry=go.transform;geometry.SetParent(transform,false);geometry.localPosition=new Vector3(0,0,10);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Resources.Load<Shader>("FixedTowerBackdrop"));material.SetColor("_Sky",backdrop.backgroundColor);towerRenderer=go.GetComponent<MeshRenderer>();towerRenderer.sharedMaterial=material;towerRenderer.shadowCastingMode=ShadowCastingMode.Off;towerRenderer.receiveShadows=false;towerRenderer.lightProbeUsage=LightProbeUsage.Off;towerRenderer.reflectionProbeUsage=ReflectionProbeUsage.Off;RefreshAspect();
        }
        public void RefreshAspect(){if(!material)return;float value=backdrop.aspect;if(Mathf.Abs(value-aspect)<.00001f)return;aspect=value;material.SetFloat("_Aspect",value);}
        void OnPreCull(){RefreshAspect();}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}





