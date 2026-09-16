using System;
using System.Linq;
namespace StairsCrowd.Core
{
    public static class PropActions
    {
        public static bool CanShuffle(Board board,int node)
        {
            if(node<0||node>=board.Level.nodes.Length||board.Solved||Rules.Gated(board.Level,board.Current,node)||Rules.Complete(board.Level,board.Current,node)||board.Level.nodes[node].sticky)return false;
            var original=board.Current.queues[node];
            if(original.Count<2||original.Select(id=>board.Level.groups[id].color).Distinct().Count()<2)return false;
            return true;
        }
        public static bool Shuffle(Board board,int node,Random random)
        {
            if(!CanShuffle(board,node))return false;var original=board.Current.queues[node];
            var next=board.Current.Clone();var q=next.queues[node];
            for(int attempt=0;attempt<24;attempt++){
                for(int i=q.Count-1;i>0;i--){int j=random.Next(i+1),v=q[i];q[i]=q[j];q[j]=v;}
                if(!q.Select(id=>board.Level.groups[id].color).SequenceEqual(original.Select(id=>board.Level.groups[id].color)))break;
            }
            if(q.Select(id=>board.Level.groups[id].color).SequenceEqual(original.Select(id=>board.Level.groups[id].color))){int j=original.FindIndex(id=>board.Level.groups[id].color!=board.Level.groups[original[0]].color);int v=q[0];q[0]=q[j];q[j]=v;}
            next.revealed[q[0]]=true;board.ApplyProp(board.Level,next);return true;
        }
    }
}
