using System.Linq;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class RevisionCases
    {
        public static LevelSpec Transit()
        {
            return new LevelSpec{name="同色中转",tip="",nodes=new[]{
                new NodeSpec{x=0,y=1.4f,z=2.5f,capacity=4,transit=true,queue=new int[0]},
                new NodeSpec{x=-3.5f,y=2.8f,z=0,capacity=4,queue=new[]{0,1,4,5}},
                new NodeSpec{x=3.5f,y=2.8f,z=0,capacity=4,queue=new[]{2,3,6,7}},
                new NodeSpec{x=-3.5f,y=0,z=5,capacity=4,transit=true,queue=new int[0]},
                new NodeSpec{x=3.5f,y=0,z=5,capacity=4,transit=true,queue=new int[0]}},
                edges=Enumerable.Range(1,4).Select(n=>new EdgeSpec{a=0,b=n}).ToArray(),
                groups=Enumerable.Range(0,8).Select(i=>new GroupSpec{id=i,color=i<4?0:1}).ToArray(),
                solution=new[]{new EdgeSpec{a=1,b=0},new EdgeSpec{a=2,b=0},new EdgeSpec{a=0,b=3},new EdgeSpec{a=1,b=4},new EdgeSpec{a=2,b=4},new EdgeSpec{a=4,b=1},new EdgeSpec{a=3,b=2}}};
        }
    }
}
