using System;
using System.Linq;
using System.Collections.Generic;
using StairsCrowd.Core;
using UnityEngine;
using Object=UnityEngine.Object;

namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        readonly List<Transform> cloudBanks=new List<Transform>();
        public void FaceClouds(){foreach(var cloud in cloudBanks)if(cloud)cloud.rotation=camera.transform.rotation;}
        public Transform[] nodeRoots;Vector3[] nodeOrigins;readonly Dictionary<int,Vector3> linkOrigins=new Dictionary<int,Vector3>();
        GameObject[] seals,gardens;float[] retreat,sealFade,linkRetreat;bool finalRetreat;float retreatFloor;const float RetreatDuration=2f;public bool HasRetreat {get{return retreat!=null&&(retreat.Any(t=>t>=0&&t<RetreatDuration)||linkRetreat.Any(t=>t>=0&&t<RetreatDuration));}}
        public bool IsRetiring(int node){return retreat!=null&&retreat[node]>=0;}
        public Transform NodeRoot(int node){return nodeRoots[node];}
        void BuildCloudWorld()
        {
            nodeRoots=platforms.Select(p=>p.transform.parent).ToArray();nodeOrigins=nodeRoots.Select(p=>p.position).ToArray();retreatFloor=(space.Centers.Length==0?2:space.Centers.Min(c=>c.y))-32;
            foreach(var pair in stairRoots)linkOrigins[pair.Key]=pair.Value.position;
            retreat=Enumerable.Repeat(-1f,nodeRoots.Length).ToArray();linkRetreat=Enumerable.Repeat(-1f,space.Level.edges.Length).ToArray();sealFade=new float[nodeRoots.Length];seals=new GameObject[nodeRoots.Length];gardens=new GameObject[nodeRoots.Length];
            for(int n=0;n<nodeRoots.Length;n++){
                var spec=space.Level.nodes[n];var c=space.Centers[n];
                if(spec.unlockAfter>0){seals[n]=MarkerRoot(n,"Seal");activeParent=seals[n].transform;
                    for(int side=0;side<4;side++){float angle=side*Mathf.PI*.5f+space.Rotations[n];var v=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));Box("Seal stone",c+v*1.94f+Vector3.up*.4f,new Vector3(.28f,.85f,.28f),ink,false);}
                    Ring(n,c+Vector3.up*.38f,2.0f,.085f,ink);BatchMarker(seals[n]);
                }
                if(spec.sticky){gardens[n]=MarkerRoot(n,"Root garden");activeParent=gardens[n].transform;
                    for(int r=0;r<4;r++)for(int m=0;m<4;m++){var p=space.Seat(n,r,m);Box("Root tile",p-Vector3.up*.01f,new Vector3(.47f,.025f,.47f),gold,false);}
                    BatchMarker(gardens[n]);
                }
                if(spec.colorRestricted){var mark=MarkerRoot(n,"Color altar");activeParent=mark.transform;Ring(n,c+Vector3.up*.025f,1.84f,.1f,palette[spec.targetColor]);BatchMarker(mark);}
            }
            collectionCompleted=new bool[nodeRoots.Length];activeParent=null;
            var shader=Resources.Load<Shader>("Cloud");if(!shader)return;
            var cloudMat=new Material(shader);materials.Add(cloudMat);cloudMat.SetFloat("_Ceiling",(space.Centers.Length==0?2:space.Centers.Min(c=>c.y))-2f);
            cloudMat.SetTexture("_NoiseTex",Resources.Load<Texture2D>("CloudNoise"));
            // The original alpha is zero outside these bounds for every possible noise value.
            // Preserve UVs: trim invisible corners instead of shrinking the visible cloud.
            var mesh=new Mesh{name="Soft cloud quad",vertices=new[]{new Vector3(-.84f,-.88f,0),new Vector3(.84f,-.88f,0),new Vector3(-.84f,.88f,0),new Vector3(.84f,.88f,0)},uv=new[]{new Vector2(.08f,.06f),new Vector2(.92f,.06f),new Vector2(.08f,.94f),new Vector2(.92f,.94f)},triangles=new[]{0,2,1,2,3,1}};mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            float low=(space.Centers.Length==0?2:space.Centers.Min(c=>c.y))-6.5f;
            for(int i=0;i<8;i++){
                var cloud=new GameObject("Cloud bank",typeof(MeshFilter),typeof(MeshRenderer));cloud.transform.SetParent(root.transform,false);
                var anchor=space.Centers.Length==0?Vector3.zero:space.Centers[i%space.Centers.Length];cloud.transform.position=new Vector3(anchor.x+Mathf.Sin(i*2.4f)*8,low-(i%3)*2.2f,anchor.z+Mathf.Cos(i*2.4f)*8);
                cloudBanks.Add(cloud.transform);cloud.transform.rotation=camera.transform.rotation;cloud.transform.localScale=new Vector3(8+(i%3)*1.5f,3.2f+(i%2)*.8f,1);
                cloud.GetComponent<MeshFilter>().sharedMesh=mesh;cloud.GetComponent<MeshRenderer>().sharedMaterial=cloudMat;
                cloud.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;cloud.GetComponent<MeshRenderer>().receiveShadows=false;
            }
        }
        GameObject MarkerRoot(int n,string name){var go=new GameObject(name);go.transform.SetParent(nodeRoots[n],false);return go;}
        void Ring(int node,Vector3 c,float radius,float width,Material material)
        {
            int sides=space.Sides[node];float r=radius/Mathf.Cos(Mathf.PI/sides);
            for(int i=0;i<sides;i++){float a=space.Rotations[node]+(i+.5f)*Mathf.PI*2/sides,b=space.Rotations[node]+(i+1.5f)*Mathf.PI*2/sides;
                Beam("Mechanism rim",c+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),c+new Vector3(Mathf.Cos(b)*r,0,Mathf.Sin(b)*r),width,.045f,material);}
        }
        void BatchMarker(GameObject go)
        {
            var filters=go.GetComponentsInChildren<MeshFilter>(true);if(filters.Length==0)return;var material=filters[0].GetComponent<Renderer>().sharedMaterial;
            var mesh=new Mesh{name=go.name};mesh.CombineMeshes(filters.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());ownedMeshes.Add(mesh);
            foreach(var f in filters)Object.DestroyImmediate(f.gameObject);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        public void RestoreWorld(State state)
        {
            for(int n=0;n<nodeRoots.Length;n++)SetCollectionCompleted(n,Rules.Complete(space.Level,state,n));
            addingNode=addingEdge=-1;finalRetreat=false;for(int e=0;e<linkRetreat.Length;e++)linkRetreat[e]=-1;
            for(int n=0;n<nodeRoots.Length;n++){retreat[n]=-1;sealFade[n]=0;nodeRoots[n].gameObject.SetActive(true);nodeRoots[n].position=nodeOrigins[n];nodeRoots[n].localScale=Vector3.one;if(seals[n]){seals[n].SetActive(Rules.Gated(space.Level,state,n));seals[n].transform.localPosition=Vector3.zero;}if(gardens[n]){gardens[n].SetActive(true);gardens[n].transform.localScale=Vector3.one;}}
            foreach(var pair in stairRoots){pair.Value.gameObject.SetActive(true);pair.Value.position=linkOrigins[pair.Key];pair.Value.localScale=Vector3.one;}
            foreach(var surface in surfaces)surface.enabled=true;
        }
        public Transform LinkRoot(int edge){Transform link;return stairRoots.TryGetValue(edge,out link)?link:null;}
        void StartLinkRetreat(int edge){if(linkRetreat[edge]>=0)return;linkRetreat[edge]=0;var link=LinkRoot(edge);if(link)foreach(var col in link.GetComponentsInChildren<Collider>())col.enabled=false;}
        // Completed collection platforms and their residents stay on display.
        public void StartRetreat(int node){if(space.Level.nodes[node].transit)return;SetCollectionCompleted(node,true);for(int e=0;e<space.Level.edges.Length;e++){var link=space.Level.edges[e];if(link.a==node||link.b==node)StartLinkRetreat(e);}}
        public void StartFinalRetreat(){if(finalRetreat)return;finalRetreat=true;for(int e=0;e<linkRetreat.Length;e++)StartLinkRetreat(e);for(int n=0;n<retreat.Length;n++)if(space.Level.nodes[n].transit){retreat[n]=0;foreach(var col in nodeRoots[n].GetComponentsInChildren<Collider>())col.enabled=false;}}
        public void TickWorld(State state,float delta)
        {
            TickAddition(delta);
            for(int n=0;n<nodeRoots.Length;n++){
                if(seals[n]&&seals[n].activeSelf&&!Rules.Gated(space.Level,state,n)){sealFade[n]+=delta;seals[n].transform.localPosition=Vector3.down*(sealFade[n]*2);if(sealFade[n]>=.4f)seals[n].SetActive(false);}
                if(retreat[n]<0)continue;retreat[n]=Mathf.Min(RetreatDuration,retreat[n]+delta);float t=Mathf.Clamp01((retreat[n]-.35f)/(RetreatDuration-.35f)),ease=t*t*t*(10+t*(-15+6*t)),distance=(nodeOrigins[n].y-retreatFloor)*ease;
                nodeRoots[n].position=nodeOrigins[n]-Vector3.up*distance;
                if(retreat[n]>=RetreatDuration)nodeRoots[n].gameObject.SetActive(false);
            }
            foreach(var pair in stairRoots){float t=linkRetreat[pair.Key];if(t<0)continue;t=linkRetreat[pair.Key]=Mathf.Min(RetreatDuration,t+Mathf.Max(0,delta));float u=Mathf.Clamp01((t-.35f)/(RetreatDuration-.35f)),ease=u*u*u*(10+u*(-15+6*u));pair.Value.position=linkOrigins[pair.Key]-Vector3.up*((linkOrigins[pair.Key].y-retreatFloor)*ease);if(t>=RetreatDuration)pair.Value.gameObject.SetActive(false);}
        }
        public void ShowMechanismLabels(State state,Font font)
        {
            var style=new GUIStyle{font=font,fontSize=Mathf.RoundToInt(Mathf.Max(15,Screen.height*.018f)),alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.2f,.28f,.3f)}};
            for(int n=0;n<nodeRoots.Length;n++)if(Rules.Gated(space.Level,state,n)&&!IsRetiring(n)){
                var p=camera.WorldToScreenPoint(space.Centers[n]+Vector3.up*2.45f);var rect=new Rect(p.x-19,Screen.height-p.y-18,38,36);GUI.color=new Color(.96f,.94f,.87f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(rect,(space.Level.nodes[n].unlockAfter-Rules.Completed(space.Level,state)).ToString(),style);
            }
        }
    }
}


