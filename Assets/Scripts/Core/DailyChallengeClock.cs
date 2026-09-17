using System;

namespace StairsCrowd.Core
{
    public enum DailyResult { Pending, Won, Failed }
    [Flags] public enum DailyPause { None=0, Settings=1, Focus=2, Application=4, Platform=8 }

    // Monotonic, injectable time; pausing settles elapsed time before changing the mask.
    public sealed class DailyChallengeClock
    {
        readonly Func<double> now;
        double last;
        public double Remaining {get;private set;}=180;
        public bool Started {get;private set;}
        public DailyResult Result {get;private set;}
        public DailyPause Pauses {get;private set;}
        public DailyChallengeClock(Func<double> time,DailyPause pauses=DailyPause.None){now=time;last=now();Pauses=pauses;}
        public void Tick()
        {
            double current=now(),elapsed=Math.Max(0,current-last);last=current;
            if(!Started||Result!=DailyResult.Pending||Pauses!=DailyPause.None)return;
            Remaining=Math.Max(0,Remaining-elapsed);if(Remaining<=0)Result=DailyResult.Failed;
        }
        public bool Interact(){Tick();if(Result!=DailyResult.Pending||Pauses!=DailyPause.None)return false;Started=true;return true;}
        public void SetPause(DailyPause reason,bool paused){Tick();Pauses=paused?Pauses|reason:Pauses&~reason;}
        public void Win(){if(Result==DailyResult.Pending)Result=DailyResult.Won;}
        public void Fail(){if(Result==DailyResult.Pending)Result=DailyResult.Failed;}
    }
}
