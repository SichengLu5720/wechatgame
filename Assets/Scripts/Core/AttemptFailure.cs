using System;
using System.Collections.Generic;

namespace StairsCrowd.Core
{
    public enum AttemptOutcome
    {
        Continue,
        Solved,
        NoLegalMoves,
        OnlyRepeatedMoves,
        TimedOut
    }

    public static class AttemptFailure
    {
        // This deliberately inspects one move only. It never searches for a solution.
        public static AttemptOutcome Classify(LevelSpec level,State state,ISet<string> seen)
        {
            if(level==null)throw new ArgumentNullException(nameof(level));
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(seen==null)throw new ArgumentNullException(nameof(seen));
            if(Rules.AllGathered(level,state))return AttemptOutcome.Solved;

            bool hasLegalMove=false;
            for(int from=0;from<level.nodes.Length;from++)for(int to=0;to<level.nodes.Length;to++){
                var move=Rules.Preview(level,state,from,to);if(!move.ok)continue;
                hasLegalMove=true;
                var next=Rules.Apply(state,move);
                if(!seen.Contains(Rules.Key(level,next)))return AttemptOutcome.Continue;
            }
            return hasLegalMove?AttemptOutcome.OnlyRepeatedMoves:AttemptOutcome.NoLegalMoves;
        }
    }
}
