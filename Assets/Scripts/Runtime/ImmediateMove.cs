namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        public bool PreparingMove => false;
        public double LastPlanningSliceMilliseconds => LastPlanningMilliseconds;
        public int LastPlanningFrames => 0;
        public bool RequestMove(int from,int to){return TryMoveSync(from,to);}
        internal void PrepareColdPlanningCheck(){FastMovement.Forget(space);}
        void CancelPendingMove() { }
    }
}
