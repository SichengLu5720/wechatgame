using System;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Immutable shared data. Instances own only transforms and small playback state.
    public sealed class CharacterWalkAsset : ScriptableObject
    {
        [Serializable] public struct Bone
        {
            public string name;
            public int parent;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        [Serializable] public sealed class Channel
        {
            public int bone;
            public string path;
            public bool step;
            public float[] times;
            public Vector4[] values;
            public Vector4 Sample(float time)
            {
                if (time <= times[0]) return values[0];
                int last = times.Length - 1;
                if (time >= times[last]) return values[last];
                int lo = 1, hi = last;
                while (lo < hi) { int mid = (lo + hi) / 2; if (times[mid] < time) lo = mid + 1; else hi = mid; }
                if (step) return values[time >= times[lo] ? lo : lo - 1];
                float u = (time - times[lo - 1]) / (times[lo] - times[lo - 1]);
                if (path != "rotation") return Vector4.LerpUnclamped(values[lo - 1], values[lo], u);
                var a = values[lo - 1]; var b = values[lo];
                var q = Quaternion.SlerpUnclamped(new Quaternion(a.x, a.y, a.z, a.w), new Quaternion(b.x, b.y, b.z, b.w), u);
                return new Vector4(q.x, q.y, q.z, q.w);
            }
        }
        public Mesh mesh;
        public Bone[] bones;
        public int[] joints;
        public Channel[] channels;
        public string clipKey, sourceHash;
        public int sourceVertexCount,sourceTriangleCount;
        // Explicit integration switch; unapproved/rejected art stays on fallback.
        public bool runtimeEnabled;
        public bool RuntimeReady => runtimeEnabled && Valid;
        public float duration = 1, cycleDistance = 1.25f;
        public int leftFoot, rightFoot;
        public bool Valid => mesh && bones != null && bones.Length == 14 && joints != null && channels != null && channels.Length > 0 && clipKey == "walk_a_in_place_v1";
    }
}
