using System;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class TransitSlots
    {
        public static void Assign(WalkSpace space,State before,State after,Move move,Vector3[] entryPositions=null)
        {
            if(before.actorSlots==null){
                after.actorSlots=new int[space.Level.groups.Length*4];
                for(int n=0;n<before.queues.Length;n++)for(int r=0;r<before.queues[n].Count;r++)for(int m=0;m<4;m++)after.actorSlots[before.queues[n][r]*4+m]=space.Level.nodes[n].transit?WalkSpace.InitialTransitSlot(r*4+m):r*4+m;
            }
            // Source and destination residents retain their physical IDs. Sorting
            // platforms additionally retain their existing logical-row semantics.
            var initial=entryPositions??space.Positions(before);int node=move.to;
            if(!space.Level.nodes[node].transit){
                foreach(int group in move.ids){int row=after.queues[node].IndexOf(group);var seats=Enumerable.Range(0,4).Select(m=>space.Seat(node,row,m,after.queues[node].Count)).ToArray();var members=Match(Enumerable.Range(group*4,4).Select(id=>initial[id]).ToArray(),seats);for(int member=0;member<4;member++)after.actorSlots[group*4+member]=row*4+members[member];}
                return;
            }
            var occupied=new bool[16];
            foreach(int group in before.queues[node])for(int m=0;m<4;m++)occupied[after.actorSlots[group*4+m]]=true;
            var free=Enumerable.Range(0,16).Where(i=>!occupied[i]).ToArray();
            var arrivals=move.ids.SelectMany(g=>Enumerable.Range(g*4,4)).OrderBy(id=>id).ToArray();
            var slots=free.Select(i=>space.Seat(node,i/4,i%4,4)).ToArray();
            var assignment=Match(arrivals.Select(id=>initial[id]).ToArray(),slots);
            for(int i=0;i<arrivals.Length;i++)after.actorSlots[arrivals[i]]=free[assignment[i]];
        }
        // Minimum-total-distance assignment, bounded by 16 people/slots. Iteration
        // in actor/physical-slot ID order deterministically resolves equal costs.
        static int[] Match(Vector3[] people,Vector3[] slots)
        {
            int n=people.Length,m=slots.Length;if(n>m)throw new InvalidOperationException("Insufficient unoccupied presentation slots");var u=new double[n+1];var v=new double[m+1];var p=new int[m+1];var way=new int[m+1];
            for(int i=1;i<=n;i++){
                p[0]=i;int j0=0;var min=Enumerable.Repeat(double.PositiveInfinity,m+1).ToArray();var used=new bool[m+1];
                do{
                    used[j0]=true;int i0=p[j0],j1=0;double delta=double.PositiveInfinity;
                    for(int j=1;j<=m;j++)if(!used[j]){double cost=Vector3.Distance(people[i0-1],slots[j-1])-u[i0]-v[j];if(cost<min[j]){min[j]=cost;way[j]=j0;}if(min[j]<delta){delta=min[j];j1=j;}}
                    for(int j=0;j<=m;j++)if(used[j]){u[p[j]]+=delta;v[j]-=delta;}else min[j]-=delta;
                    j0=j1;
                }while(p[j0]!=0);
                do{int j1=way[j0];p[j0]=p[j1];j0=j1;}while(j0!=0);
            }
            var result=new int[n];for(int j=1;j<=m;j++)if(p[j]!=0)result[p[j]-1]=j-1;
            return result;
        }
    }
}
