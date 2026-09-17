using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        Transform[] tuningPlatforms; float tuningPlatformScale=-1,tuningPersonScale=-1;
        void PrepareTuningVisuals()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            tuningPlatforms=new Transform[space.Centers.Length];
            for(int n=0;n<tuningPlatforms.Length;n++){
                var node=NodeRoot(n);var renderers=node.GetComponentsInChildren<MeshRenderer>(true);
                var layer=new GameObject("Platform tuning visual").transform;layer.SetParent(node,false);tuningPlatforms[n]=layer;
                foreach(var renderer in renderers){
                    var item=renderer.transform;
                    // Mechanism markers retain their own reveal/retreat hierarchy.
                    if(item.parent!=node)continue;
                    if(item.GetComponent<Collider>()||item.GetComponent<PlatformTag>()){
                        var mesh=item.GetComponent<MeshFilter>();if(!mesh)continue;
                        var clone=new GameObject("Platform surface visual",typeof(MeshFilter),typeof(MeshRenderer));clone.transform.SetParent(item,false);
                        clone.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;var copy=clone.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.enabled=renderer.enabled;
                        copy.shadowCastingMode=renderer.shadowCastingMode;copy.receiveShadows=renderer.receiveShadows;
                        if(platforms[n]==renderer)platforms[n]=copy;Object.DestroyImmediate(renderer);Object.DestroyImmediate(mesh);item=clone.transform;
                    }
                    item.SetParent(layer,true);
                }
            }
            ApplyRuntimeTuning();
#endif
        }
        public void ApplyRuntimeTuning()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            float platform=RuntimeTuning.Platform,person=RuntimeTuning.Person;
            if(tuningPlatforms!=null&&platform!=tuningPlatformScale){foreach(var t in tuningPlatforms)if(t)t.localScale=Vector3.one*platform;tuningPlatformScale=platform;}
            if(pool!=null&&person!=tuningPersonScale){foreach(var p in pool)if(p!=null)ApplyPersonTuning(p);tuningPersonScale=person;}
#endif
        }
        static void ApplyPersonTuning(PersonView p)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(p.tuningVisual){p.tuningVisual.localScale=Vector3.one*RuntimeTuning.Person;p.walk?.VisualScaleChanged();}
#endif
        }
    }
}
