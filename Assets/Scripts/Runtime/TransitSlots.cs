using System;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class TransitSlots
    {
        public static void Assign(WalkSpace space,State before,State after,Move move)
        {
            if(after.actorSlots==null){
                after.actorSlots=new int[space.Level.groups.Length*4];
                for(int n=0;n<after.queues.Length;n++)for(int r=0;r<after.queues[n].Count;r++)for(int m=0;m<4;m++)after.actorSlots[after.queues[n][r]*4+m]=r*4+m;
            }
            var initial=space.Positions(before);
            foreach(int node in new[]{move.from,move.to}){
                if(!space.Level.nodes[node].transit){
                    if(node==move.to)foreach(int group in move.ids){int row=after.queues[node].IndexOf(group);var seats=Enumerable.Range(0,4).Select(m=>space.Seat(node,row,m,after.queues[node].Count)).ToArray();var members=Match(Enumerable.Range(group*4,4).Select(id=>initial[id]).ToArray(),seats);for(int member=0;member<4;member++)after.actorSlots[group*4+member]=row*4+members[member];}
                    continue;
                }
                if(after.queues[node].Count==0)continue;
                int count=after.queues[node].Count*4;
                var slots=Enumerable.Range(0,count).Select(i=>space.Seat(node,i/4,i%4,count/4)).ToArray();
                var incoming=node==move.to?(space.Centers[move.from]-space.Centers[node]).normalized:space.Facing(node);
                var ranked=Enumerable.Range(0,count).OrderByDescending(i=>Vector3.Dot(slots[i]-space.Centers[node],incoming)).ToArray();
                var residents=before.queues[node].Intersect(after.queues[node]).SelectMany(g=>Enumerable.Range(g*4,4)).ToArray();
                var reserved=ranked.Skip(count-residents.Length).ToArray();
                var assignment=Match(residents.Select(id=>initial[id]).ToArray(),reserved.Select(i=>slots[i]).ToArray());
                for(int i=0;i<residents.Length;i++)after.actorSlots[residents[i]]=reserved[assignment[i]];
                if(node==move.to){
                    var free=ranked.Take(count-residents.Length).Reverse().ToArray();
                    var arrivals=move.ids.SelectMany(g=>Enumerable.Range(g*4,4)).ToArray();
                    for(int i=0;i<arrivals.Length;i++)after.actorSlots[arrivals[i]]=free[i];
                }
            }
        }
        // Rectangular minimum-cost assignment, at most 16 actors/slots.
        // Prefer short paths, then resolve pairwise conflicts before animating.
        static int[] Match(Vector3[] people,Vector3[] slots)
        {
            int n=people.Length,m=slots.Length;var u=new double[n+1];var v=new double[m+1];var p=new int[m+1];var way=new int[m+1];
            for(int i=1;i<=n;i++){
                p[0]=i;int j0=0;var min=Enumerable.Repeat(double.PositiveInfinity,m+1).ToArray();var used=new bool[m+1];
                do{
                    used[j0]=true;int i0=p[j0],j1=0;double delta=double.PositiveInfinity;
                    for(int j=1;j<=m;j++)if(!used[j]){double cost=(people[i0-1]-slots[j-1]).sqrMagnitude-u[i0]-v[j];if(cost<min[j]){min[j]=cost;way[j]=j0;}if(min[j]<delta){delta=min[j];j1=j;}}
                    for(int j=0;j<=m;j++)if(used[j]){u[p[j]]+=delta;v[j]-=delta;}else min[j]-=delta;
                    j0=j1;
                }while(p[j0]!=0);
                do{int j1=way[j0];p[j0]=p[j1];j0=j1;}while(j0!=0);
            }
            var result=new int[n];for(int j=1;j<=m;j++)if(p[j]!=0)result[p[j]-1]=j-1;
            bool safe=true;for(int a=0;a<n;a++)for(int b=0;b<a;b++)if(!Compatible(people[a],slots[result[a]],people[b],slots[result[b]]))safe=false;
            if(safe)return result;
            var choice=Enumerable.Repeat(-1,n).ToArray();var occupied=new bool[m];int budget=20000;
            return Search(people,slots,choice,occupied,0,ref budget)?choice:result;
        }
        static bool Compatible(Vector3 a,Vector3 endA,Vector3 b,Vector3 endB)
        {
            Vector2 start=new Vector2(a.x-b.x,a.z-b.z),end=new Vector2(endA.x-endB.x,endA.z-endB.z),delta=end-start;
            float t=delta.sqrMagnitude<1e-8f?0:Mathf.Clamp01(-Vector2.Dot(start,delta)/delta.sqrMagnitude);
            return (start+delta*t).sqrMagnitude>=(WalkSpace.Separation-.0001f)*(WalkSpace.Separation-.0001f);
        }
        static bool Search(Vector3[] people,Vector3[] slots,int[] choice,bool[] used,int depth,ref int budget)
        {
            if(depth==people.Length)return true;if(--budget<0)return false;
            int next=-1;int[] candidates=null;
            for(int actor=0;actor<people.Length;actor++)if(choice[actor]<0){
                var legal=new System.Collections.Generic.List<int>();
                for(int slot=0;slot<slots.Length;slot++)if(!used[slot]){bool ok=true;for(int other=0;other<people.Length&&ok;other++)if(choice[other]>=0&&!Compatible(people[actor],slots[slot],people[other],slots[choice[other]]))ok=false;if(ok)legal.Add(slot);}
                if(legal.Count==0)return false;
                if(candidates==null||legal.Count<candidates.Length){next=actor;candidates=legal.OrderBy(slot=>(people[actor]-slots[slot]).sqrMagnitude).ToArray();}
            }
            foreach(int slot in candidates){choice[next]=slot;used[slot]=true;if(Search(people,slots,choice,used,depth+1,ref budget))return true;used[slot]=false;choice[next]=-1;if(budget<0)break;}
            return false;
        }
    }
}
