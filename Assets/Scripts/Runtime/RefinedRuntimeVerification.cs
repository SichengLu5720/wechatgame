using System;
using System.Linq;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    public static class RefinedRuntimeVerification
    {
        public static void Check(StairsGame game)
        {
            var art=Resources.Load<RefinedArtLibrary>("RefinedArtLibrary");
            if(!art||!game.scene.UsesRefinedArt)throw new Exception("Clean refined art is not connected to the game");
            if(!art.architecture.GetTexture("_StoneTex")||art.architecture.GetFloat("_StoneDetail")<=0)throw new Exception("Requested stone texture is missing");
            if(!art.collectionPlatform||art.collectionPlatform==art.platform||art.collectionPlatform.bounds.max.y>.001f||Mathf.Abs(art.collectionPlatform.bounds.size.x-5.2f)>.001f)throw new Exception("Collection rim asset missing or has top pylons");
            for(int n=0;n<game.scene.nodeRoots.Length;n++){
                if(game.space.Sides[n]!=4)continue;
                var batch=game.scene.NodeRoot(n).Find("Architecture batch");if(!batch)throw new Exception("Platform architecture missing");
                var mesh=batch.GetComponent<MeshFilter>().sharedMesh;bool collection=!game.board.Level.nodes[n].transit;
                if(mesh.triangles.Length!=(collection?art.collectionPlatform:art.platform).triangles.Length)throw new Exception("Wrong platform model connected");
                if(!collection)continue;
                if(mesh.bounds.max.y>.001f||game.scene.NodeRoot(n).Find("Collection four towers")||game.scene.NodeRoot(n).Find("Collection white lamps"))throw new Exception("Collection pylons remain");
                if(game.scene.NodeRoot(n).GetComponentsInChildren<Renderer>().Count(r=>r.enabled)!=1||game.scene.NodeRoot(n).GetComponentsInChildren<Light>().Length!=0)throw new Exception("Collection renderer or light budget");
                if(!mesh.colors.Any(c=>Mathf.Abs(c.r-36/255f)<.001f&&Mathf.Abs(c.g-73/255f)<.001f&&Mathf.Abs(c.b-76/255f)<.001f))throw new Exception("Collection teal rim missing");
            }
            // Exclude the retained corner ornaments, and allow the central triangle-fan vertex.
            if(art.platform.vertices.Any(p=>Mathf.Abs(p.y+.001f)<.002f&&Mathf.Abs(p.x)<2.1f&&Mathf.Abs(p.z)<2.1f&&new Vector2(p.x,p.z).sqrMagnitude>.0001f))throw new Exception("Platform has interior slab joints");
            foreach(var person in game.scene.people){
                var model=person.visual.Find("Sky character");if(!model||model.GetComponent<MeshFilter>().sharedMesh!=art.character||model.GetComponentsInChildren<Renderer>(true).Length!=1)throw new Exception("Wrong refined character mesh or renderer count");
            }
            foreach(var p in art.character.vertices)if(new Vector2(p.x,p.z).magnitude*1.1f>WalkSpace.ActorRadius||p.y*1.1f>CrowdScene.PersonHeight)throw new Exception("Refined actor exceeds gameplay envelope");
            foreach(var p in game.scene.platforms)if(p.GetComponent<Collider>()==null||p.GetComponent<PlatformTag>()==null)throw new Exception("Platform click collider removed");
            Debug.Log("CLEAN REFINED GAME ART PASS: stone texture retained, no slab joints, shared character mesh, actor envelope, click colliders");
        }
    }
}
