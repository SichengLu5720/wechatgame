using UnityEngine;

namespace StairsCrowd.Runtime
{
    // All coordinates here affect renderer children only. The navigation root and
    // its original capsule remain owned by CrowdMotion and PersonView.
    public sealed class WaterBirdVisual
    {
        readonly Transform body;
        Renderer[] renderers;readonly MaterialPropertyBlock revealBlock=new MaterialPropertyBlock();
        public void SetReveal(float amount){if(renderers==null)renderers=body.GetComponentsInChildren<Renderer>(true);foreach(var renderer in renderers){renderer.GetPropertyBlock(revealBlock);revealBlock.SetFloat("_RevealTint",amount);renderer.SetPropertyBlock(revealBlock);}}
        bool hidden;
        public void SetHidden(bool value){hidden=value;body.gameObject.SetActive(!hidden);}
        // Compatibility for historical inspection tools; no flow resource is allocated.
        public Mesh FlowMesh => null;
        public float Blend => 0;
        public bool IsWater => false;
        public WaterBirdVisual(PersonView person,Mesh bird,Material material,Mesh trim=null,Material ivory=null)
        {
            var model=new GameObject("Sky character",typeof(MeshFilter),typeof(MeshRenderer));
            body=model.transform;body.SetParent(person.visual,false);
            model.GetComponent<MeshFilter>().sharedMesh=bird;model.GetComponent<MeshRenderer>().sharedMaterial=material;
            if(trim){var detail=new GameObject("Ivory hood and cloak trim",typeof(MeshFilter),typeof(MeshRenderer));detail.transform.SetParent(body,false);detail.GetComponent<MeshFilter>().sharedMesh=trim;detail.GetComponent<MeshRenderer>().sharedMaterial=ivory;}
            Reset();
        }
        public void Reset(){body.gameObject.SetActive(!hidden);body.localScale=Vector3.one;body.localPosition=Vector3.zero;}
    }
}
