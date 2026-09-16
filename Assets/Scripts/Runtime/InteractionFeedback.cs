using System;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed class InteractionFeedback
    {
        readonly Action<string> pulse;
        float cooldown,stepClock,completionEcho;int priority;bool suspended;
        public const float StepInterval=.38f;
        public int SelectionCount {get;private set;}
        public int CompletionCount {get;private set;}
        public int ArrivalCount {get;private set;}
        public int StepCount {get;private set;}
        public int PulseCount {get;private set;}
        public bool IsPlaying {get{return cooldown>0;}}
        public InteractionFeedback(Action pulse=null){this.pulse=pulse==null?(Action<string>)WeChatPlatform.Pulse:(_=>pulse());}
        internal InteractionFeedback(Action<string> pulse){this.pulse=pulse;}
        bool Emit(string strength,int rank,float hold)
        {
            if(suspended||!GameSettings.Vibration||cooldown>0&&priority>=rank)return false;
            priority=rank;cooldown=hold;stepClock=0;PulseCount++;pulse(strength);return true;
        }
        public void Select(){SelectionCount++;Emit("medium",2,.12f);}
        public void Arrive(){ArrivalCount++;Emit("heavy",3,.2f);}
        public void Complete(){CompletionCount++;if(Emit("heavy",4,.4f))completionEcho=.16f;}
        public void Cancel(){cooldown=stepClock=completionEcho=0;priority=0;}
        public void Suspend(bool value){suspended=value;Cancel();}
        public void Tick(float delta)
        {
            if(!GameSettings.Vibration||suspended){Cancel();return;}
            if(completionEcho>0){completionEcho-=Mathf.Max(0,delta);if(completionEcho<=0&&delta<.25f){PulseCount++;pulse("heavy");}}
            cooldown=Mathf.Max(0,cooldown-Mathf.Max(0,delta));if(cooldown==0)priority=0;
        }
        public void Walk(float delta,bool moving)
        {
            if(!moving||suspended||!GameSettings.Vibration){stepClock=0;return;}
            if(cooldown>0){stepClock=0;return;}
            stepClock+=Mathf.Max(0,delta);
            if(stepClock>=StepInterval){stepClock=0;if(Emit("medium",1,.04f))StepCount++;}
        }
    }
}
