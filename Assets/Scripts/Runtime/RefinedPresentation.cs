using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        RefinedArtLibrary refined;Material refinedStone;CharacterWalkAsset characterWalk;
        public bool UsesCharacterWalk=>characterWalk&&characterWalk.RuntimeReady;
        public bool PresentationSettled {get{if(people!=null)foreach(var p in people)if(!p.PresentationSettled)return false;return true;}}
        public void TickWalks(float delta,float movingDelta=-1){if(people!=null)foreach(var p in people)p.TickWalk(delta,movingDelta);}
        public bool UsesRefinedArt=>refined&&refined.cleanFloor;
        void SetupRefinedArt()
        {
            refined=Resources.Load<RefinedArtLibrary>("RefinedArtLibrary");if(!refined)return;
            characterWalk=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter");
            if(characterWalk&&!characterWalk.Valid){Debug.LogError("TASK-003 character resource incomplete; retaining original character");characterWalk=null;}
            if(characterWalk&&!characterWalk.runtimeEnabled)characterWalk=null;
            if(!refined.cleanFloor||!refined.character||!refined.platform||!refined.collectionPlatform||!refined.bridge||!refined.step||refined.characters.Length!=6)throw new System.InvalidOperationException("Incomplete clean art library");
            refinedStone=new Material(refined.architecture);refinedStone.SetFloat("_GameplayLighting",1);refinedStone.SetFloat("_FogStrength",1);refinedStone.SetColor("_FogColor",GameplayPresentation.Background);
            float bottom=space.Centers.Length==0?0:space.Centers[0].y;foreach(var c in space.Centers)bottom=Mathf.Min(bottom,c.y);
            refinedStone.SetFloat("_FogLevel",bottom-2.5f);refinedStone.SetFloat("_FogDepth",10);materials.Add(refinedStone);
            for(int i=0;i<palette.Length;i++){
                palette[i]=RefinedCharacterMaterial(refined.characters[i]);selectedPalette[i]=RefinedCharacterMaterial(refined.characters[i]);
                palette[i].SetColor("_ClothColor",colors[i]);selectedPalette[i].SetColor("_ClothColor",Color.Lerp(colors[i],Color.white,.20f));
            }
            mystery=RefinedCharacterMaterial(refined.characters[5]);selectedMystery=RefinedCharacterMaterial(refined.characters[5]);
        }
        Material RefinedCharacterMaterial(Material source){var m=new Material(source);m.SetFloat("_GameplayLighting",1);m.SetFloat("_CharacterShadeFloor",.72f);materials.Add(m);return m;}
        GameObject RefinedModel(string name,Mesh mesh,Material material,Vector3 position,Quaternion rotation,Vector3 scale)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(activeParent,false);go.transform.position=position;go.transform.rotation=rotation;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return go;
        }
        void RefinedPlatform(int node,Vector3 center,float size)
        {
            bool collection=!space.Level.nodes[node].transit;
            var mesh=Object.Instantiate(collection?refined.collectionPlatform:refined.platform);mesh.name=collection?"Collection thick rim platform":"Clean transit platform";var v=mesh.vertices;float floor=center.y;
            foreach(var c in space.Centers)floor=Mathf.Min(floor,c.y);
            for(int i=0;i<v.Length;i++)if(v[i].y< -7.7f)v[i].y+=floor-center.y-18;
            mesh.vertices=v;mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            RefinedModel("Refined clean platform",mesh,refinedStone,center,Quaternion.Euler(0,-space.Rotations[node]*Mathf.Rad2Deg,0),new Vector3(size/5.2f,1,size/5.2f));
        }
        void RefinedBridge(StairSurface s)
        {
            var support=Box("Bridge navigation plane",(s.start+s.end)/2-Vector3.up*.13f,new Vector3(s.width,.26f,s.Length),ivory,true);
            support.transform.rotation=Quaternion.LookRotation(s.Direction);support.GetComponent<Renderer>().enabled=false;
            RefinedModel("Refined clean bridge",refined.bridge,refinedStone,s.start,Quaternion.LookRotation(s.Direction),new Vector3(s.width/4.1f,1,s.Length/5.2f));
        }
    }
}
