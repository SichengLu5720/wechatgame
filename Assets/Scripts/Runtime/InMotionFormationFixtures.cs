using System.Linq;
using StairsCrowd.Core;
namespace StairsCrowd.Runtime
{
    public static class InMotionFormationFixtures
    {
        public static LevelSpec Create(int groups,int residents)
        {
            return new LevelSpec{name="Formation verification",groups=Enumerable.Range(0,groups+residents).Select(i=>new GroupSpec{id=i,color=0}).ToArray(),
                nodes=new[]{new NodeSpec{x=0,z=0,y=2,transit=true,capacity=4,queue=Enumerable.Range(0,groups).ToArray()},
                    new NodeSpec{x=0,z=12,y=0,transit=true,capacity=4,queue=Enumerable.Range(groups,residents).ToArray()},
                    new NodeSpec{x=12,z=12,y=2,transit=true,capacity=4,queue=new int[0]}},
                edges=new[]{new EdgeSpec{a=0,b=1},new EdgeSpec{a=1,b=2}},solution=new EdgeSpec[0]};
        }
    }
}
