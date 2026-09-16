using System;
using UnityEngine;
using StairsCrowd.Core;
namespace StairsCrowd.Runtime
{
    public static class CampaignRepository
    {
        public static Catalog Load()
        {
            var catalog=LoadOriginal();
            var tutorial=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("tutorial-v1").text);
            if(tutorial.levels.Length!=10)throw new Exception("Tutorial pool is incomplete");
            for(int i=0;i<10;i++){
                tutorial.levels[i].Validate();
                if(tutorial.levels[i].provenance.id!="tutorial-v1-"+(i+1).ToString("00"))throw new Exception("Tutorial order mismatch");
                catalog.levels[i]=tutorial.levels[i];
            }
            return catalog;
        }
        public static bool IsTutorial(LevelSpec level) => level?.provenance?.id?.StartsWith("tutorial-v1-")==true;
        public static bool UsesReferenceLayout(LevelSpec level) => level?.provenance?.generatorVersion=="layout-review-2"||level?.provenance?.generatorVersion=="delivery-2";
        public static bool GeometryOnly(LevelSpec level) => IsTutorial(level)||UsesReferenceLayout(level);
        public static Catalog LoadOriginal()
        {
            var asset=Resources.Load<TextAsset>("campaign-v1");
            if(!asset)return JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text);
            var catalog=JsonUtility.FromJson<Catalog>(asset.text);
            if(catalog==null||catalog.levels==null||catalog.levels.Length!=CampaignSchedule.Count)throw new Exception("Campaign pool is incomplete");
            for(int i=0;i<catalog.levels.Length;i++){
                var l=catalog.levels[i];CampaignValidator.Validate(l);
                if(l.provenance.difficulty!=CampaignSchedule.Difficulty(i+1)||l.provenance.id!="campaign-v1-"+(i+1).ToString("00"))throw new Exception("Campaign order mismatch");
            }
            return catalog;
        }
        public static string NavigationPath(LevelSpec level,int index)
        {return UsesReferenceLayout(level)?"reference-layout-navigation/"+level.provenance.template:level.provenance!=null?"campaign-navigation/"+level.provenance.template:"navigation/level-"+index;}
        public static LevelSpec SelectForEditor(Catalog catalog,int seed)
        {
            int count=Math.Min(CampaignSchedule.Count,catalog.levels.Length);
            var copy=LevelShare.Copy(catalog.levels[(int)((uint)seed%(uint)count)]);copy.provenance=null;return copy;
        }
    }
}
