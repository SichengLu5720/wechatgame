using System;
namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        bool[] collectionCompleted;
        // Arrival and undo can drive later mechanisms without changing platform visuals.
        public event Action<int,bool> CollectionCompletionChanged;
        public bool CollectionCompleted(int node)=>collectionCompleted!=null&&node>=0&&node<collectionCompleted.Length&&collectionCompleted[node];
        void SetCollectionCompleted(int node,bool completed)
        {
            if(collectionCompleted==null||node<0||node>=collectionCompleted.Length||space.Level.nodes[node].transit||collectionCompleted[node]==completed)return;
            collectionCompleted[node]=completed;
            CollectionCompletionChanged?.Invoke(node,completed);
        }
    }
}
