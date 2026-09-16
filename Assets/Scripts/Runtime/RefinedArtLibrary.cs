using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed class RefinedArtLibrary : ScriptableObject
    {
        public Mesh character,platform,collectionPlatform,bridge,step;
        public Material architecture;
        public Material[] characters;
        public bool cleanFloor;
    }
}
