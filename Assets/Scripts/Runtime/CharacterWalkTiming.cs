using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class CharacterWalkTiming
    {
        public const float MaximumSpeed = 1.45f;
        public static bool Enabled
        {
            get { var asset=Resources.Load<CharacterWalkAsset>("CharacterWalk/Task003PilgrimCharacter"); return asset && asset.RuntimeReady; }
        }
        // One multiplier for the entire plan preserves every simultaneous relation,
        // slot assignment and spatial collision result, including queued followers.
        public static void Apply(MotionPlan plan)
        {
            float factor = 1;
            foreach(var track in plan.tracks)for(int i=1;i<track.points.Length;i++)
            {
                float seconds=track.times[i]-track.times[i-1];
                if(seconds<=0)continue;
                float distance=Vector3.ProjectOnPlane(track.points[i]-track.points[i-1],Vector3.up).magnitude;
                factor=Mathf.Max(factor,distance/(seconds*MaximumSpeed));
            }
            foreach(var track in plan.tracks)
            {
                track.start*=factor;track.duration*=factor;
                for(int i=0;i<track.times.Length;i++)track.times[i]*=factor;
            }
            plan.duration*=factor;
        }
    }
}
