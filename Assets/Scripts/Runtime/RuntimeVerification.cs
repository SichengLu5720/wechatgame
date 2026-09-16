using System.Collections;
using System.IO;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed class RuntimeVerification:MonoBehaviour
    {
        public static bool Active => false;
        IEnumerator Start(){var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/fast-runtime"));Directory.CreateDirectory(output);var game=GetComponent<StairsGame>();game.catalog=CampaignRepository.LoadOriginal();yield return FastResponseVerification.Run(game,output);Application.Quit(0);}
    }
}
