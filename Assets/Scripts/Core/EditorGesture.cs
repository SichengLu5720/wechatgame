namespace StairsCrowd.Core
{
    // Two five-finger double taps; every tap must finish with all fingers lifted.
    public sealed class EditorGesture
    {
        int previous,peak,taps;float began,last;bool invalid;
        public void Reset(){previous=peak=taps=0;began=last=0;invalid=false;}
        public bool Sample(float time,int contacts,bool canceled=false,bool moved=false)
        {
            // Only measure the gap before the next tap begins, never while it is held.
            if(taps>0&&previous==0&&time-last>(taps==2?1.8f:.85f))taps=0;
            if(previous==0&&contacts>0){began=time;peak=0;invalid=false;}
            if(contacts>0){peak=System.Math.Max(peak,contacts);invalid|=contacts>5||canceled||moved||time-began>.85f||(peak<5&&time-began>.35f);}
            invalid|=canceled||moved;
            bool open=false;
            if(previous>0&&contacts==0){
                if(!invalid&&peak==5&&time-began<=.85f){taps++;last=time;if(taps==4){open=true;taps=0;}}
                else taps=0;
            }
            previous=contacts;return open;
        }
    }
}
