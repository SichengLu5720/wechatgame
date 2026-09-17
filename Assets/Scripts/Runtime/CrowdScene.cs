using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{

    public sealed class PersonView
    {
        public Transform root,visual,tuningVisual,leftLeg,rightLeg,leftArm,rightArm;
        public Transform ModelParent=>tuningVisual?tuningVisual:visual;
        public WaterBirdVisual waterBird;
        public CharacterWalkDriver walk;
        public WalkSpace walkGround;
        public float remainingDistance=float.PositiveInfinity;
        public bool PresentationSettled=>walk==null||walk.PresentationSettled;
        public void TickWalk(float delta,float movingDelta=-1){if(walk!=null)walk.Tick(delta,remainingDistance,movingDelta);}
        ColorReveal colorReveal;
        public bool RevealAnimating=>colorReveal!=null&&colorReveal.Active;
        public bool RevealUsesMask=>colorReveal!=null&&colorReveal.UsesMask;
        public float RevealTint=>colorReveal==null?1:colorReveal.Tint;
        public void ResetReveal(){if(colorReveal!=null)colorReveal.Reset();}
        public void SetHiddenVisual(bool hidden){if(colorReveal==null)colorReveal=new ColorReveal(this);colorReveal.Set(hidden);}
        public void TickReveal(float delta){if(colorReveal!=null)colorReveal.Tick(delta);}
        public GameObject selectionRing,hiddenCharacter;public bool IsSelected {get;private set;}public System.Func<Vector3,float> presentationLift;
        public void SetSelected(bool value){if(IsSelected==value)return;IsSelected=value;selectionRing.SetActive(value);visual.localPosition=Vector3.up*((value?.12f:0)+(presentationLift?.Invoke(root.position)??0));visual.localScale=value?Vector3.one*1.1f:Vector3.one;}
        public CapsuleCollider capsule;public PlatformTag tag;public Renderer[] colored;bool wasWalking;
        public void Pose(Vector3 p,Vector3 direction,float phase,bool walking)
        {
            if(!walking&&!wasWalking&&root.position==p&&direction.sqrMagnitude<.0001f)return;wasWalking=walking;
            root.position=p;visual.localPosition=Vector3.up*((IsSelected?.12f:0)+(presentationLift?.Invoke(p)??0));if(direction.sqrMagnitude>.0001f)root.rotation=Quaternion.LookRotation(direction);
        }
    }
    public sealed partial class CrowdScene
    {
        public const float PersonHeight=2.2f;
        public static readonly Vector3 PersonScale=new Vector3(2.4f,2.25f,2.4f);
        public readonly GameObject root;public readonly WalkSpace space;public readonly Camera camera;public bool HomeFraming;
        public PersonView[] people;public Renderer[] platforms;public readonly List<Collider> surfaces=new List<Collider>();
        public readonly List<Material> materials=new List<Material>();public Material idle,landing,selected,valid,complete,locked;
        PersonView[] pool;int preparedPeople;Mesh birdMesh;readonly List<Mesh> ownedMeshes=new List<Mesh>();
        public int RenderersBeforeBatching {get;private set;} public int RenderersAfterBatching {get;private set;}
        Transform activeParent;readonly Dictionary<int,Transform> stairRoots=new Dictionary<int,Transform>();Material[] palette,selectedPalette;Mesh selectionMesh;Material selectedMystery,selectionInk,marking;int selectedNode=-1;Material mystery,ivory,teal,coral,gold,ink;
        Material platformEdge;Mesh skyTrim;
        readonly Color[] colors=System.Array.ConvertAll(ColorCatalog.Rgb,c=>new Color(((c>>16)&255)/255f,((c>>8)&255)/255f,(c&255)/255f));
        public Material Mat(Color color)
        {
            var m=new Material(Shader.Find("Stairs/Pastel"));m.color=color;m.enableInstancing=true;materials.Add(m);return m;
        }
        System.Collections.IEnumerator construction;
        public bool ConstructionComplete {get;private set;}
        public CrowdScene(WalkSpace space,Camera camera,bool staged=false)
        {
            this.space=space;this.camera=camera;root=new GameObject("Stairs and crowds");
            root.SetActive(!staged);
            construction=Construct();
            if(!staged){while(BuildNextPiece()){}FitCamera();}
        }
        public bool BuildNextPiece()
        {
            if(ConstructionComplete)return false;
            if(construction.MoveNext())return true;
            construction=null;ConstructionComplete=true;return false;
        }
        System.Collections.IEnumerator Construct()
        {
            ivory=Mat(new Color(.94f,.91f,.79f));teal=Mat(new Color(.32f,.65f,.64f));coral=Mat(new Color(.83f,.52f,.47f));gold=Mat(new Color(.87f,.69f,.39f));ink=Mat(new Color(.28f,.36f,.4f));platformEdge=teal;
            idle=ivory;landing=ivory;selected=Mat(new Color(1,.79f,.46f));valid=Mat(new Color(.61f,.85f,.67f));complete=Mat(new Color(.63f,.82f,.57f));locked=Mat(new Color(.6f,.63f,.66f));
            palette=new Material[ColorCatalog.Count];selectedPalette=new Material[ColorCatalog.Count];for(int i=0;i<ColorCatalog.Count;i++){palette[i]=Mat(colors[i]);selectedPalette[i]=Mat(Color.Lerp(colors[i],Color.white,.32f));}
            mystery=Mat(new Color(.25f,.29f,.32f));selectedMystery=Mat(new Color(.55f,.62f,.66f));selectionInk=Mat(new Color(1f,.78f,.08f));marking=Mat(new Color(.58f,.68f,.62f));selectionMesh=SelectionRingMesh();ownedMeshes.Add(selectionMesh);
            SetupRefinedArt();
            yield return null;
            platforms=new Renderer[space.Centers.Length];
            for(int n=0;n<space.Centers.Length;n++){
                var c=space.Centers[n];float size=space.Half(n)*2;bool sorting=!space.Level.nodes[n].transit;var pavilion=new GameObject((sorting?"Sorting Pavilion ":"Transit Pier ")+n);pavilion.transform.SetParent(root.transform,false);pavilion.transform.position=c;activeParent=pavilion.transform;
                var p=Platform(n,"Platform "+n,c-Vector3.up*.13f,new Vector3(size,.26f,size),sorting?idle:landing,true);p.AddComponent<PlatformTag>().node=n;platforms[n]=p.GetComponent<Renderer>();
                if(refined&&space.Sides[n]==4){p.GetComponent<Renderer>().enabled=false;RefinedPlatform(n,c,size);yield return null;continue;}
                Platform(n,"Table edge inlay",c-Vector3.up*.29f,new Vector3(size,.04f,size),sorting?platformEdge:ivory,false);
                Platform(n,"Table underside",c-Vector3.up*.40f,new Vector3(size-.08f,.30f,size-.08f),ivory,false);
                Foundation(n,c,size,sorting);
                SkyStonework(n,c,size);
                if(sorting)Ring(n,c+Vector3.up*.025f,size*.5f-.16f,.10f,platformEdge);
                yield return null;
            }
            foreach(var stair in space.Stairs){if(!stairRoots.ContainsKey(stair.index)){var link=new GameObject("Stair Link "+stair.index);link.transform.SetParent(root.transform,false);link.transform.position=(stair.start+stair.end)/2;stairRoots.Add(stair.index,link.transform);}activeParent=stairRoots[stair.index];
                if(stair.polygon!=null){Connector(stair);yield return null;continue;}
                if(stair.steps==1){if(refined)RefinedBridge(stair);else SkyBridge(stair);yield return null;continue;}
                for(int i=0;i<stair.steps;i++){
                    var tread=stair.Treads[i];var top=tread.Center;float thick=tread.Thickness;
                    var step=Box("Stair "+stair.index+" step "+i,top-Vector3.up*(thick/2),new Vector3(tread.Width,thick,tread.Length),ivory,true);step.transform.rotation=Quaternion.LookRotation(stair.Direction);if(stair.steps==1){step.GetComponent<Renderer>().enabled=false;var surface=PrimitiveGeometry.Create(PrimitiveType.Cube);surface.name="Recessed connector surface";surface.transform.SetParent(step.transform,false);surface.transform.localPosition=new Vector3(0,-.012f/thick,0);Object.DestroyImmediate(surface.GetComponent<Collider>());surface.GetComponent<Renderer>().sharedMaterial=ivory;}
                    if(refined){step.GetComponent<MeshFilter>().sharedMesh=refined.step;step.GetComponent<Renderer>().sharedMaterial=refinedStone;}
                }
                Vector3 side=Vector3.Cross(Vector3.up,stair.Direction)*(stair.width/2+.085f);
                Beam("Stair side band",stair.start+side-Vector3.up*.16f,stair.end+side-Vector3.up*.16f,.13f,.22f,ivory);
                Beam("Stair side band",stair.start-side-Vector3.up*.16f,stair.end-side-Vector3.up*.16f,.13f,.22f,ivory);
                yield return null;
            }
            var batching=BatchArchitecture();while(batching.MoveNext())yield return null;
            BuildCloudWorld();PrepareTuningVisuals();
        }
        void DashedSortingBoundary(int node,Vector3 center)
        {
            const float half=1.71f,radius=.21f,width=.06f;var points=new List<Vector3>();
            if(space.Sides[node]==4){
                var corners=new[]{new Vector2(half-radius,half-radius),new Vector2(-half+radius,half-radius),new Vector2(-half+radius,-half+radius),new Vector2(half-radius,-half+radius)};
                for(int corner=0;corner<4;corner++)for(int i=0;i<=8;i++){
                    float angle=(corner*90+i*90f/8)*Mathf.Deg2Rad;var c=corners[corner];points.Add(new Vector3(c.x+radius*Mathf.Cos(angle),0,c.y+radius*Mathf.Sin(angle)));
                }
            }else{
                float r=half/Mathf.Cos(Mathf.PI/space.Sides[node]);for(int i=0;i<space.Sides[node];i++){float angle=(i+.5f)*Mathf.PI*2/space.Sides[node];points.Add(new Vector3(r*Mathf.Cos(angle),0,r*Mathf.Sin(angle)));}
            }
            points.Add(points[0]);var distances=new float[points.Count];for(int i=1;i<points.Count;i++)distances[i]=distances[i-1]+Vector3.Distance(points[i-1],points[i]);
            float length=distances[distances.Length-1];int dashes=Mathf.RoundToInt(length/.38f);float period=length/dashes;
            System.Func<float,Vector3> at=t=>{for(int i=1;i<points.Count;i++)if(t<=distances[i])return Vector3.Lerp(points[i-1],points[i],(t-distances[i-1])/(distances[i]-distances[i-1]));return points[0];};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int dash=0;dash<dashes;dash++){
                float start=dash*period,end=start+period*.62f;int samples=Mathf.CeilToInt((end-start)/.03f);
                for(int j=0;j<samples;j++){
                    var a=at(Mathf.Lerp(start,end,j/(float)samples));var b=at(Mathf.Lerp(start,end,(j+1)/(float)samples));var side=Vector3.Cross(Vector3.up,(b-a).normalized)*(width/2);int v=vertices.Count;
                    vertices.Add(a-side);vertices.Add(a+side);vertices.Add(b-side);vertices.Add(b+side);triangles.Add(v);triangles.Add(v+2);triangles.Add(v+1);triangles.Add(v+2);triangles.Add(v+3);triangles.Add(v+1);
                }
            }
            var mesh=new Mesh{name="Dashed gathering outline"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var outline=new GameObject("Dashed gathering outline",typeof(MeshFilter),typeof(MeshRenderer));outline.transform.SetParent(activeParent,false);outline.transform.position=center+Vector3.up*.014f;outline.transform.rotation=Quaternion.Euler(0,-space.Rotations[node]*Mathf.Rad2Deg,0);outline.GetComponent<MeshFilter>().sharedMesh=mesh;outline.GetComponent<MeshRenderer>().sharedMaterial=marking;
        }
        void Connector(StairSurface stair)
        {
            var go=new GameObject("Flush edge landing");go.transform.SetParent(activeParent,false);go.transform.position=stair.start;go.layer=8;
            var mesh=PlatformMesh.Connector(stair.polygon.ToArray(),stair.start,WalkGeometryConfig.TreadBaseThickness);ownedMeshes.Add(mesh);var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;surfaces.Add(collider);
            var visual=new GameObject("Ivory landing");visual.transform.SetParent(go.transform,false);visual.transform.localPosition=Vector3.down*.008f;visual.AddComponent<MeshFilter>().sharedMesh=mesh;visual.AddComponent<MeshRenderer>().sharedMaterial=ivory;
            // Follow the actual convex landing outline, including triangular joins.
            for(int i=0;i<stair.polygon.Length;i++){var a=stair.polygon[i];var b=stair.polygon[(i+1)%stair.polygon.Length];Beam("Landing edge band",a-Vector3.up*.16f,b-Vector3.up*.16f,.13f,.22f,ivory);}
        }
        void Foundation(int node,Vector3 c,float size,bool sorting)
        {
            // Substantial corner legs leave the space beneath the tabletop open.
            int sides=space.Sides[node];float width=.78f;
            float cloudFloor=float.PositiveInfinity;foreach(var center in space.Centers)cloudFloor=Mathf.Min(cloudFloor,center.y);
            float height=c.y-cloudFloor+24f;
            var columnMaterial=new Material(Resources.Load<Shader>("CloudColumn"));columnMaterial.color=ivory.color;
            columnMaterial.SetFloat("_FadeTop",cloudFloor-2.5f);columnMaterial.SetFloat("_FadeBottom",cloudFloor-12f);materials.Add(columnMaterial);
            float inset=size*.5f-width*.5f-.17f,radius=inset/Mathf.Cos(Mathf.PI/sides);
            var rotation=Quaternion.Euler(0,-space.Rotations[node]*Mathf.Rad2Deg,0);
            for(int i=0;i<sides;i++){
                float angle=space.Rotations[node]+(i+.5f)*Mathf.PI*2/sides;
                var corner=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                var leg=Box("Cloud column",c+corner-Vector3.up*(.46f+height*.5f),new Vector3(width,height,width),columnMaterial,false);leg.transform.rotation=rotation;
                var collar=Box("Column stone collar",c+corner-Vector3.up*.60f,new Vector3(width+.10f,.26f,width+.10f),ivory,false);collar.transform.rotation=rotation;
            }
        }
        void Beam(string name,Vector3 a,Vector3 b,float width,float height,Material material)
        {
            var beam=Box(name,(a+b)/2,new Vector3(width,height,Vector3.Distance(a,b)),material,false);beam.transform.rotation=Quaternion.LookRotation(b-a);
        }
        GameObject Platform(int node,string name,Vector3 center,Vector3 scale,Material material,bool surface)
        {
            if(space.Sides[node]==4){var box=Box(name,center,scale,material,surface);box.transform.rotation=Quaternion.Euler(0,-space.Rotations[node]*Mathf.Rad2Deg,0);return box;}
            var go=new GameObject(name);go.transform.SetParent(activeParent,false);go.transform.position=center;go.transform.localScale=scale;
            go.transform.rotation=Quaternion.Euler(0,-space.Rotations[node]*Mathf.Rad2Deg,0);
            var mesh=PlatformMesh.Prism(space.Sides[node]);ownedMeshes.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(surface){go.layer=8;var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;surfaces.Add(collider);}return go;
        }
        public void ClearPeople(){if(pool!=null)foreach(var p in pool)if(p!=null){p.SetSelected(false);p.waterBird.Reset();p.remainingDistance=float.PositiveInfinity;p.root.gameObject.SetActive(false);}selectedNode=-1;people=System.Array.Empty<PersonView>();}
        GameObject Box(string name,Vector3 center,Vector3 scale,Material mat,bool surface)
        {
            var go=PrimitiveGeometry.Create(PrimitiveType.Cube);go.name=name;go.transform.SetParent(activeParent?activeParent:root.transform,false);go.transform.position=center;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
            var collider=go.GetComponent<BoxCollider>();if(surface){go.layer=8;surfaces.Add(collider);}else Object.DestroyImmediate(collider);return go;
        }
        Transform Part(Transform parent,string name,PrimitiveType type,Vector3 local,Vector3 scale,Material material)
        {
            var go=PrimitiveGeometry.Create(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        public void RefreshPeople(State state)
        {
            for(int n=0;n<state.queues.Length;n++){bool completed=Rules.Complete(space.Level,state,n);
            foreach(int group in state.queues[n])for(int member=0;member<Rules.MembersPerGroup;member++){
                bool revealed=state.revealed[group]||completed;var p=people[group*Rules.MembersPerGroup+member];p.tag.node=n;
                bool highlighted=n==selectedNode&&IsSelectedGroup(state,n,group);p.SetSelected(highlighted);
                var material=highlighted?(revealed?selectedPalette[space.Level.groups[group].color]:selectedMystery):(revealed?palette[space.Level.groups[group].color]:mystery);
                foreach(var renderer in p.colored)if(renderer.sharedMaterial!=material)renderer.sharedMaterial=material;
                bool hidden=!revealed;
                if(hidden&&!p.hiddenCharacter){
                    var prefab=Resources.Load<GameObject>("HiddenCharacter/HiddenCharacter");
                    if(!prefab)throw new System.InvalidOperationException("Hidden character prefab is missing");
                    p.hiddenCharacter=Object.Instantiate(prefab,p.ModelParent,false);
                }
                p.SetHiddenVisual(hidden);
                if(p.hiddenCharacter){if(hidden){
                    // The independent visual faces the fixed gameplay camera so every glyph reads.
                    var front=Vector3.ProjectOnPlane(camera.transform.position-p.root.position,Vector3.up);
                    if(front.sqrMagnitude>.001f)p.hiddenCharacter.transform.rotation=Quaternion.LookRotation(front);
                }}
            }}
        }
        public void PreparePeople(int count)
        {
            if(pool==null)pool=new PersonView[space.Level.groups.Length*Rules.MembersPerGroup];
            for(int end=Mathf.Min(pool.Length,preparedPeople+count);preparedPeople<end;preparedPeople++){
                int id=preparedPeople;var material=mystery;
                var go=new GameObject("Person "+id);go.SetActive(false);go.transform.SetParent(root.transform,false);go.layer=9;
                var person=new PersonView{root=go.transform,tag=go.AddComponent<PlatformTag>(),presentationLift=BridgeLift,walkGround=space};
                var capsule=go.AddComponent<CapsuleCollider>();capsule.radius=WalkSpace.ActorRadius;capsule.height=PersonHeight;capsule.center=Vector3.up*(PersonHeight/2);capsule.isTrigger=true;person.capsule=capsule;
                person.visual=new GameObject("Character visual").transform;person.visual.SetParent(go.transform,false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                person.tuningVisual=new GameObject("Character tuning scale").transform;person.tuningVisual.SetParent(person.visual,false);ApplyPersonTuning(person);
#endif
                person.selectionRing=new GameObject("Selection ring",typeof(MeshFilter),typeof(MeshRenderer));person.selectionRing.transform.SetParent(go.transform,false);person.selectionRing.transform.localPosition=Vector3.up*.035f;person.selectionRing.GetComponent<MeshFilter>().sharedMesh=selectionMesh;person.selectionRing.GetComponent<MeshRenderer>().sharedMaterial=selectionInk;person.selectionRing.SetActive(false);
                if(!birdMesh){if(refined){birdMesh=refined.character;skyTrim=null;}else{birdMesh=SkyCharacterMesh.Coat();skyTrim=SkyCharacterMesh.Trim();ownedMeshes.Add(birdMesh);ownedMeshes.Add(skyTrim);foreach(var m in palette)m.SetFloat("_ContactTint",1);foreach(var m in selectedPalette)m.SetFloat("_ContactTint",1);mystery.SetFloat("_ContactTint",1);selectedMystery.SetFloat("_ContactTint",1);}}
                person.waterBird=new WaterBirdVisual(person,birdMesh,material,skyTrim,ivory,characterWalk,space);
                var colored=new List<Renderer>();foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))if(renderer.sharedMaterial==mystery)colored.Add(renderer);person.colored=colored.ToArray();pool[id]=person;
            }
        }
        public void Populate(State state)
        {
            PreparePeople(space.Level.groups.Length*Rules.MembersPerGroup);people=pool;var positions=space.Positions(state);
            for(int n=0;n<state.queues.Length;n++)foreach(int group in state.queues[n])for(int member=0;member<Rules.MembersPerGroup;member++){
                int id=group*Rules.MembersPerGroup+member;var person=people[id];person.ResetReveal();person.SetSelected(false);person.visual.localPosition=Vector3.zero;person.root.localScale=Vector3.one;person.Pose(positions[id],space.Facing(n),0,false);person.remainingDistance=float.PositiveInfinity;person.waterBird.Reset();person.root.gameObject.SetActive(true);
            }
            RefreshPeople(state);
        }
        System.Collections.IEnumerator BatchArchitecture()
        {
            RenderersBeforeBatching=root.GetComponentsInChildren<Renderer>(true).Length;
            foreach(Transform piece in root.transform){
                var groups=new Dictionary<Material,List<MeshFilter>>();
                foreach(var filter in piece.GetComponentsInChildren<MeshFilter>(true)){
                    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||filter.GetComponent<PlatformTag>())continue;
                    List<MeshFilter> group;if(!groups.TryGetValue(renderer.sharedMaterial,out group)){group=new List<MeshFilter>();groups.Add(renderer.sharedMaterial,group);}group.Add(filter);
                }
                foreach(var entry in groups){
                    var combine=new CombineInstance[entry.Value.Count];for(int i=0;i<combine.Length;i++)combine[i]=new CombineInstance{mesh=entry.Value[i].sharedMesh,transform=piece.worldToLocalMatrix*entry.Value[i].transform.localToWorldMatrix};
                    var mesh=new Mesh{name="Batched architecture"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(combine,true,true);ownedMeshes.Add(mesh);
                    var batch=new GameObject("Architecture batch",typeof(MeshFilter),typeof(MeshRenderer));batch.transform.SetParent(piece,false);batch.GetComponent<MeshFilter>().sharedMesh=mesh;batch.GetComponent<MeshRenderer>().sharedMaterial=entry.Key;
                    foreach(var filter in entry.Value){var go=filter.gameObject;if(!go.GetComponent<Collider>()&&go.transform.childCount==0)Object.DestroyImmediate(go);else{Object.DestroyImmediate(go.GetComponent<MeshRenderer>());Object.DestroyImmediate(filter);}}
                }
                yield return null;
            }
            RenderersAfterBatching=root.GetComponentsInChildren<Renderer>(true).Length;
        }
        Transform Limb(Transform parent,string name,Vector3 at,float length,float width,Material mat)
        {
            var pivot=new GameObject(name).transform;pivot.SetParent(parent,false);pivot.localPosition=at;
            Part(pivot,name+" mesh",PrimitiveType.Capsule,new Vector3(0,-length/2,0),new Vector3(width,length/2+width/2,width),mat);return pivot;
        }
        public void FitCamera()
        {
            if(FitGameplayCamera())return;
            if(space.Centers.Length==0){camera.orthographic=true;camera.orthographicSize=12;camera.transform.position=new Vector3(11,22,-22);camera.transform.LookAt(Vector3.up*2);orbitDepth=Vector3.Distance(camera.transform.position,Vector3.up*2);camera.nearClipPlane=.1f;camera.farClipPlane=10000;return;}
            var bounds=new Bounds(space.Centers[0],Vector3.one);foreach(var c in space.Centers)bounds.Encapsulate(c);
            var target=bounds.center-Vector3.up*.35f;camera.orthographic=true;camera.transform.position=target+new Vector3(11,20,-22).normalized*Mathf.Max(34,bounds.size.magnitude*2);camera.transform.LookAt(target);orbitDepth=Vector3.Distance(camera.transform.position,target);camera.nearClipPlane=.1f;camera.farClipPlane=Mathf.Max(1000,bounds.size.magnitude*5+100);
            float vertical=0,horizontal=0;for(int node=0;node<space.Centers.Length;node++)foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})foreach(float y in new[]{-2.6f,PersonHeight+.25f}){
                float extent=space.Half(node)/Mathf.Cos(Mathf.PI/space.Sides[node])+.15f;var p=space.Centers[node]+new Vector3(x*extent,y,z*extent)-target;vertical=Mathf.Max(vertical,Mathf.Abs(Vector3.Dot(p,camera.transform.up)));horizontal=Mathf.Max(horizontal,Mathf.Abs(Vector3.Dot(p,camera.transform.right)));}
            float framing=HomeFraming?.47f:camera.rect==new Rect(0,0,1,1)?.68f:1f;
            camera.orthographicSize=Mathf.Max(vertical,horizontal/Mathf.Max(.3f,camera.aspect/framing))*1.04f/framing/PresentationScale;
            if(HomeFraming)camera.transform.position-=camera.transform.up*(camera.orthographicSize*.03f);else if(camera.rect==new Rect(0,0,1,1))camera.transform.position-=camera.transform.up*(camera.orthographicSize*.06f);KeepPresentationVisible();FaceClouds();
        }
        public const float PresentationScale=1.3f;
        Vector3 PortraitDirection()
        {
            var original=new Vector3(11,20,-22);
            // New delivered layouts use one deterministic fixed view chosen at load.
            // No user rotation, animation, navigation or gameplay geometry changes.
            if(HomeFraming||camera.aspect>=.8f||space.Level.provenance==null||space.Level.provenance.generatorVersion!="delivery-1")return original;
            var best=original;float bestSize=float.PositiveInfinity;
            for(int quarter=0;quarter<4;quarter++){
                var direction=Quaternion.AngleAxis(quarter*90,Vector3.up)*original;
                var rotation=Quaternion.LookRotation(-direction.normalized,Vector3.up);
                var right=rotation*Vector3.right;var up=rotation*Vector3.up;
                float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=minX,maxY=maxX;
                for(int n=0;n<space.Centers.Length;n++){
                    float radius=space.Half(n)/Mathf.Cos(Mathf.PI/space.Sides[n]);
                    for(int corner=0;corner<space.Sides[n];corner++)for(int h=0;h<2;h++){
                        float angle=space.Rotations[n]+(corner+.5f)*Mathf.PI*2/space.Sides[n];
                        var p=space.Centers[n]+new Vector3(Mathf.Cos(angle)*radius,h==0?-.26f:PersonHeight+.25f,Mathf.Sin(angle)*radius);
                        float x=Vector3.Dot(p,right),y=Vector3.Dot(p,up);minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                    }
                }
                float size=Mathf.Max((maxX-minX)/(1.92f*Mathf.Max(.1f,camera.aspect)),(maxY-minY)/1.32f);
                if(quarter==0||size<bestSize*.92f){bestSize=size;best=direction;}
            }
            return best;
        }
        // Translate the closer framing first; only reduce zoom if the actual
        // playable mesh cannot fit. Long decorative columns may extend into fog.
        public void KeepPresentationVisible()
        {
            if(FitGameplayCamera())return;
            if(HomeFraming||camera.rect!=new Rect(0,0,1,1)||platforms==null||platforms.Length==0)return;
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=minX,maxY=maxX;
            var right=camera.transform.right;var up=camera.transform.up;var origin=camera.transform.position;
            for(int n=0;n<platforms.Length;n++){
                float radius=space.Half(n)/Mathf.Cos(Mathf.PI/space.Sides[n]);
                for(int corner=0;corner<space.Sides[n];corner++){
                    float angle=space.Rotations[n]+(corner+.5f)*Mathf.PI*2/space.Sides[n];
                    var point=space.Centers[n]+new Vector3(Mathf.Cos(angle)*radius,-.26f,Mathf.Sin(angle)*radius)-origin;
                    float x=Vector3.Dot(point,right),y=Vector3.Dot(point,up);
                    minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                }
                for(int row=0;row<4;row++)for(int member=0;member<4;member++){
                    var point=space.Seat(n,row,member)+Vector3.up*PersonHeight-origin;
                    float x=Vector3.Dot(point,right),y=Vector3.Dot(point,up);
                    minX=Mathf.Min(minX,x-WalkSpace.ActorRadius);maxX=Mathf.Max(maxX,x+WalkSpace.ActorRadius);
                    maxY=Mathf.Max(maxY,y+WalkSpace.ActorRadius);
                }
            }
            float required=Mathf.Max((maxX-minX)/(1.92f*Mathf.Max(.1f,camera.aspect)),(maxY-minY)/1.32f);
            camera.orthographicSize=Mathf.Max(camera.orthographicSize,required);
            float half=camera.orthographicSize,horizontal=half*camera.aspect;
            float shiftX=Mathf.Clamp(0,maxX-horizontal*.96f,minX+horizontal*.96f);
            float shiftY=Mathf.Clamp(0,maxY-half*.72f,minY+half*.60f);
            camera.transform.position+=right*shiftX+up*shiftY;
        }
        bool IsSelectedGroup(State state,int node,int group)
        {
            var queue=state.queues[node];int index=queue.IndexOf(group);return index>=0&&index<Rules.MovableCount(space.Level,state,node);
        }
        static Mesh SelectionRingMesh()
        {
            const int segments=24;var vertices=new Vector3[segments*2];var triangles=new int[segments*6];
            for(int i=0;i<segments;i++){
                float angle=i*Mathf.PI*2/segments;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));vertices[i*2]=direction*.27f;vertices[i*2+1]=direction*.47f;
                int a=i*2,b=(i*2+2)%(segments*2),t=i*6;triangles[t]=a;triangles[t+1]=b;triangles[t+2]=a+1;triangles[t+3]=b;triangles[t+4]=b+1;triangles[t+5]=a+1;
            }
            var mesh=new Mesh{name="Selection ring"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void Highlight(Board board,int source)
        {
            selectedNode=source;
            for(int n=0;n<platforms.Length;n++){
                var material=Rules.Gated(board.Level,board.Current,n)?locked:Rules.Complete(board.Level,board.Current,n)?complete:!board.Level.nodes[n].transit?idle:landing;
                if(platforms[n].sharedMaterial!=material)platforms[n].sharedMaterial=material;
            }
            if(people!=null&&people.Length>0)RefreshPeople(board.Current);
        }
        public void Dispose(){Object.DestroyImmediate(root);foreach(var m in materials)Object.DestroyImmediate(m);foreach(var mesh in ownedMeshes)Object.DestroyImmediate(mesh);}
    }
}

