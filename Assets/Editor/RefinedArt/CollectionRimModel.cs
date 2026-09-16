using UnityEngine;

namespace StairsCrowd.ArtProduction
{
    public static partial class RefinedModels
    {
        // Same footprint and standing plane. A continuous rim replaces all top ornaments.
        public static Mesh CollectionRim(bool supports)
        {
            var m=new RefinedMesh();
            Vector3[] Loop(float half,float y,float corner)
            {
                return new[]{new Vector3(-half+corner,y,-half),new Vector3(half-corner,y,-half),
                    new Vector3(half,y,-half+corner),new Vector3(half,y,half-corner),
                    new Vector3(half-corner,y,half),new Vector3(-half+corner,y,half),
                    new Vector3(-half,y,half-corner),new Vector3(-half,y,-half+corner)};
            }
            void Join(Vector3[] a,Vector3[] b)
            {
                for(int i=0;i<8;i++){int j=(i+1)%8;m.Quad(a[i],b[i],b[j],a[j]);}
            }
            m.Material(Hex("24494C"),.56f);
            var bottom=Loop(2.575f,-.56f,.045f);
            var outerLow=Loop(2.6f,-.535f,.05f);
            var outerHigh=Loop(2.6f,-.025f,.05f);
            var top=Loop(2.575f,0,.045f);
            var innerTop=Loop(2.31f,0,.024f);
            var innerHigh=Loop(2.295f,-.015f,.014f);
            var innerLow=Loop(2.295f,-.56f,.014f);
            Join(bottom,outerLow);Join(outerLow,outerHigh);Join(outerHigh,top);
            Join(top,innerTop);Join(innerTop,innerHigh);Join(innerHigh,innerLow);Join(innerLow,bottom);
            m.Material(LightStone,.8f);
            m.Box(new Vector3(0,-.056f,0),new Vector3(4.61f,.11f,4.61f),.005f);
            m.Material(StoneSide,.85f);
            m.Box(new Vector3(0,-.315f,0),new Vector3(4.61f,.408f,4.61f),.025f);
            if(supports)Arch(m,2.555f,-.56f,-7.8f);
            return m.Finish(supports?"Collection thick teal rim arched supports - no pylons":"Collection thick teal rim deck - no pylons");
        }
    }
}
