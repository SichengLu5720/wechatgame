using System;
using System.Collections.Generic;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Offline delivery proxy, not a human readability score. Rays use the shared
    // generated tread descriptions; no second staircase height formula is kept.
    public static class GameplayScreenSafety
    {
        [Serializable] public sealed class Result
        {
            public int[] platformSamples,crowdHeads,stairSamples;
            public bool Reachable { get { foreach(int n in platformSamples)if(n==0)return false;foreach(int n in crowdHeads)if(n==0)return false;foreach(int n in stairSamples)if(n==0)return false;return true; } }
        }
        static bool Visible(WalkSpace s,Vector3 p,Vector3 toward,int ownNode,int ownEdge,bool crowd)
        {
            p+=Vector3.up*.035f;
            for(int n=0;n<s.Centers.Length;n++)if(n!=ownNode){
                float t=(s.Centers[n].y-p.y)/toward.y;
                if(t>.02f){var at=p+toward*t;if(s.Inside(n,new Vector2(at.x,at.z)))return false;}
                if(crowd)for(int row=0;row<4;row++)for(int member=0;member<4;member++){
                    var head=s.Seat(n,row,member)+Vector3.up*(CrowdScene.PersonHeight*.8f);
                    var delta=head-p;float along=Vector3.Dot(delta,toward);
                    if(along>.05f&&(delta-toward*along).sqrMagnitude<WalkSpace.ActorRadius*WalkSpace.ActorRadius*1.21f)return false;
                }
            }
            foreach(var stair in s.Stairs)if(stair.index!=ownEdge){
                if(stair.polygon!=null){float t=(stair.start.y-p.y)/toward.y;if(t>.02f){var at=p+toward*t;float y;if(stair.Height(new Vector2(at.x,at.z),out y))return false;}continue;}
                var side=Vector3.Cross(Vector3.up,stair.Direction);
                foreach(var tread in stair.Treads){float t=(tread.Height-p.y)/toward.y;if(t<=.02f)continue;var delta=p+toward*t-tread.Center;
                    if(Mathf.Abs(Vector3.Dot(delta,stair.Direction))<tread.Length*.5f&&Mathf.Abs(Vector3.Dot(delta,side))<tread.Width*.5f)return false;
                }
            }
            return true;
        }
        public static Result Analyze(WalkSpace s,Vector3 direction)
        {
            var d=direction.normalized;var r=new Result{platformSamples=new int[s.Centers.Length],crowdHeads=new int[s.Centers.Length],stairSamples=new int[s.Stairs.Length]};
            for(int n=0;n<s.Centers.Length;n++){
                for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++){
                    var p=s.Centers[n]+new Vector3(x,0,z)*(s.Half(n)*.26f);
                    if(s.Inside(n,new Vector2(p.x,p.z))&&Visible(s,p,d,n,-1,false))r.platformSamples[n]++;
                }
                for(int row=0;row<4;row++)for(int m=0;m<4;m++)if(Visible(s,s.Seat(n,row,m)+Vector3.up*(CrowdScene.PersonHeight*.8f),d,n,-1,true))r.crowdHeads[n]++;
            }
            for(int i=0;i<s.Stairs.Length;i++){
                var stair=s.Stairs[i];var side=Vector3.Cross(Vector3.up,stair.Direction);
                for(int t=1;t<=15;t++)for(int x=-1;x<=1;x++){
                    var p=Vector3.Lerp(stair.start,stair.end,t/16f)+side*x*stair.width*.3f;float y;
                    if(stair.Height(new Vector2(p.x,p.z),out y)){p.y=y;if(Visible(s,p,d,-1,stair.index,false))r.stairSamples[i]++;}
                }
            }
            return r;
        }
        public static bool NoWorse(Result baseline,Result candidate)
        {
            if(!candidate.Reachable)return false;
            return NoWorse(baseline.platformSamples,candidate.platformSamples)&&NoWorse(baseline.crowdHeads,candidate.crowdHeads)&&NoWorse(baseline.stairSamples,candidate.stairSamples);
        }
        static bool NoWorse(int[] before,int[] after){if(before.Length!=after.Length)return false;for(int i=0;i<before.Length;i++)if(after[i]<before[i])return false;return true;}
    }
}
