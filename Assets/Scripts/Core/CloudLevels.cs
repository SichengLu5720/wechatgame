using System;
using System.Collections.Generic;
using System.Linq;

namespace StairsCrowd.Core
{
    public static class CloudLevels
    {
        public static Catalog Create()
        {
            var levels=new[]{Make(0),Make(1),Make(2),Make(3),Make(4),Make(5)};
            return new Catalog{levels=levels};
        }
        static LevelSpec Make(int index)
        {
            var l=new LevelSpec{name=new[]{"雾后色彩","唤醒封印","留下的颜色","同色共鸣","云间回廊","遗迹合奏"}[index],tip="",nodes=new[]{
                N(-3.5f,4.2f,0,false),N(3.5f,4.2f,0,false),N(0,2.8f,2.5f,true),
                N(-3.5f,1.4f,5,true),N(3.5f,1.4f,5,true),N(0,0,7.5f,true)},
                edges=new[]{E(0,2),E(1,2),E(2,3),E(2,4),E(3,5),E(4,5)},solution=new EdgeSpec[0]};
            l.nodes[1].surfaceDirection=1;l.nodes[2].surfaceDirection=3;l.nodes[3].surfaceDirection=3;l.nodes[4].surfaceDirection=2;l.nodes[5].surfaceDirection=2;
            var groups=new List<GroupSpec>();
            Action<int,int[]> put=(node,colors)=>{var ids=new List<int>();foreach(int c in colors){ids.Add(groups.Count);groups.Add(new GroupSpec{id=groups.Count,color=c});}l.nodes[node].queue=ids.ToArray();};
            if(index==0||index==3||index==4){put(0,new[]{0,1,0,1});put(1,new[]{1,0,1,0});}
            else if(index==1){put(0,new[]{0,1,0,1});put(1,new[]{1});put(3,new[]{0});put(4,new[]{1});put(5,new[]{0});l.nodes[1].unlockAfter=1;}
            else {put(0,new[]{0});put(1,new[]{1,0,1,0});put(3,new[]{1});put(4,new[]{0});put(5,new[]{1});l.nodes[0].sticky=true;}
            l.groups=groups.ToArray();
            if(index==0){l.groups[l.nodes[0].queue[1]].hidden=true;l.groups[l.nodes[1].queue[1]].hidden=true;}
            if(index==3||index==5){l.nodes[0].colorRestricted=true;l.nodes[0].targetColor=0;l.nodes[1].colorRestricted=true;l.nodes[1].targetColor=1;}
            if(index==4){l.groups[1].hidden=true;l.groups[2].hidden=true;l.groups[5].hidden=true;l.groups[7].hidden=true;l.nodes[0].colorRestricted=true;}
            if(index==5){l.groups[l.nodes[1].queue[1]].hidden=true;l.groups[l.nodes[1].queue[2]].hidden=true;l.nodes[0].unlockAfter=1;}
            return l;
        }
        public static NodeSpec N(float x,float y,float z,bool transit){return new NodeSpec{x=x,y=y,z=z,transit=transit,capacity=4,queue=new int[0]};}
        public static EdgeSpec E(int a,int b){return new EdgeSpec{a=a,b=b};}
        public static void RevealFronts(LevelSpec l){foreach(var n in l.nodes)if(n.queue.Length>0)l.groups[n.queue[0]].hidden=false;}
        public static bool FrequencyAllowed(LevelSpec l)
        {
            int occupied=l.nodes.Count(n=>n.queue.Length>0),pairs=0;
            foreach(var n in l.nodes){int local=0,run=1;for(int i=1;i<n.queue.Length;i++){run=l.groups[n.queue[i]].color==l.groups[n.queue[i-1]].color?run+1:1;if(run>2)return false;if(run==2)local++;}if(local>1)return false;pairs+=local;}
            return pairs<=Math.Max(1,(int)Math.Ceiling(occupied*.15));
        }
        public static LevelSpec Generate(int seed)
        {
            // Seeded generation is staged: structure, people, mechanisms, then the
            // real solver. A seed therefore reproduces the same accepted board.
            var random=new Random(seed);bool wantPair=random.Next(100)<18;
            for(int attempt=0;attempt<160;attempt++){
                var level=BuildGeneratedShell(random,seed);
                var colors=new[]{0,0,0,0,1,1,1,1};Shuffle(colors,random);
                for(int i=0;i<colors.Length;i++)level.groups[i].color=colors[i];
                foreach(var group in level.groups)group.hidden=false;
                foreach(var node in level.nodes)for(int row=1;row<node.queue.Length;row++)
                    level.groups[node.queue[row]].hidden=random.Next(100)<(row%2==1?32:8);
                if(!FrequencyAllowed(level))continue;
                bool pair=false;foreach(var node in level.nodes)for(int row=1;row<node.queue.Length;row++)
                    pair|=level.groups[node.queue[row]].color==level.groups[node.queue[row-1]].color;
                if(pair!=wantPair)continue;
                if(attempt>22&&random.Next(100)<28)level.nodes[1].colorRestricted=true;
                if(attempt>34&&random.Next(100)<18)level.nodes[2].unlockAfter=1;
                level.Validate();var result=Solver.Solve(level,Rules.Initial(level),22000);
                if(result.status!="solved"||Rules.Completed(level,Rules.Initial(level))>0)continue;
                level.solution=result.moves;return level;
            }
            var fallback=Make(0);fallback.name="云海 "+seed;return fallback;
        }
        static void Shuffle(int[] values,Random random){for(int i=values.Length-1;i>0;i--){int j=random.Next(i+1);int t=values[i];values[i]=values[j];values[j]=t;}}
        static LevelSpec BuildGeneratedShell(Random random,int seed)
        {
            var l=Make(0);l.name="云海 "+seed;l.tip="种子生成";l.solution=new EdgeSpec[0];
            float spread=3.2f+random.Next(0,4)*.45f;
            for(int i=0;i<l.nodes.Length;i++){
                var n=l.nodes[i];n.surfaceDirection=random.Next(4);
                n.x+=(float)(random.NextDouble()-.5)*spread;n.z+=(float)(random.NextDouble()-.5)*spread;
                n.y=i<2?3.6f+(float)random.NextDouble()*1.8f:1.0f+(float)random.NextDouble()*2.6f;
            }
            return l;
        }
    }
}
