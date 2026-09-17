using UnityEngine;

namespace StairsCrowd.Runtime
{
    // Navigation remains outside this class. Every update uses StairsGame's delta;
    // clip phase follows displacement rather than a global clock or plan timestamp.
    public sealed class CharacterWalkDriver
    {
        readonly CharacterWalkAsset asset;
        readonly PersonView person;
        readonly Transform model;
        readonly Transform[] bones;
        WalkSpace ground;
        Vector3 previousPosition;
        Quaternion facing;
        float phase, blend, heightOffset;
        readonly Foot left = new Foot(), right = new Foot();
        sealed class Foot { public bool locked; public Vector3 anchor; }
        public float Phase => phase;
        public float Blend => blend;
        public bool PresentationSettled => blend == 0 && Mathf.Abs(heightOffset) < .001f && Quaternion.Angle(facing, person.root.rotation) < .1f;
        public SkinnedMeshRenderer Renderer { get; private set; }
        public void VisualScaleChanged(){left.locked=right.locked=false;}

        public CharacterWalkDriver(PersonView person, Transform model, CharacterWalkAsset asset, Material material, WalkSpace ground)
        {
            this.person = person; this.model = model; this.asset = asset; this.ground = ground;
            bones = new Transform[asset.bones.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = new GameObject(asset.bones[i].name).transform;
            for (int i = 0; i < bones.Length; i++) bones[i].SetParent(asset.bones[i].parent < 0 ? model : bones[asset.bones[i].parent], false);
            Renderer = model.gameObject.AddComponent<SkinnedMeshRenderer>();
            Renderer.sharedMesh = asset.mesh; Renderer.sharedMaterial = material;
            var joints = new Transform[asset.joints.Length];
            for (int i = 0; i < joints.Length; i++) joints[i] = bones[asset.joints[i]];
            Renderer.bones = joints; Renderer.rootBone = bones[0]; Renderer.quality = SkinQuality.Bone4;
            Renderer.localBounds = new Bounds(new Vector3(0, .95f, 0), new Vector3(1.1f, 2.2f, 1.1f));
            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false; Renderer.updateWhenOffscreen = false;
            Reset(ground);
        }
        void ReferencePose()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = asset.bones[i].position;
                bones[i].localRotation = asset.bones[i].rotation;
                bones[i].localScale = asset.bones[i].scale;
            }
        }
        public void Reset(WalkSpace nextGround)
        {
            ground = nextGround; previousPosition = person.root.position; facing = person.root.rotation;
            phase = blend = heightOffset = 0; left.locked = right.locked = false;
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
            ReferencePose();
        }
        public void Tick(float delta, float remainingDistance,float movingDelta=-1)
        {
            if (delta <= 0) return;
            Vector3 position = person.root.position;
            float distance = Vector3.ProjectOnPlane(position - previousPosition, Vector3.up).magnitude;
            float navigationRise = position.y - previousPosition.y;
            previousPosition = position;
            bool moving = distance > .000001f;
            // Only moving gait response uses the motion clock. Resting/settling,
            // reveal and world lifecycles retain their ordinary real-time clock.
            if(moving&&movingDelta>=0)delta=movingDelta;
            float visualScale=person.tuningVisual?person.tuningVisual.localScale.x:1;
            phase = Mathf.Repeat(phase + distance / (asset.cycleDistance*visualScale), 1);
            blend = Mathf.MoveTowards(blend, moving ? 1 : 0, delta / (moving ? .16f : .24f));
            facing = Quaternion.Slerp(facing, person.root.rotation, 1 - Mathf.Exp(-delta * 20));
            if (Quaternion.Angle(facing, person.root.rotation) < .1f) facing = person.root.rotation;
            model.rotation = facing;
            float targetHeight = 0;
            if (moving && ground != null && ground.Surface(new Vector2(position.x, position.z), out float floor, out _))
            {
                // The navigation envelope rises to the next tread early. Preserve
                // visual world height before smoothing toward its own support.
                heightOffset -= navigationRise;
                targetHeight = Mathf.Clamp(floor + WalkSpace.FootGap - position.y, -.45f, .1f);
            }
            heightOffset = Mathf.Lerp(heightOffset, targetHeight, 1 - Mathf.Exp(-delta * 22));
            if (Mathf.Abs(heightOffset - targetHeight) < .0005f) heightOffset = targetHeight;
            model.localPosition = model.parent.InverseTransformVector(Vector3.up * heightOffset);
            ReferencePose();
            if (blend == 0) { left.locked = right.locked = false; return; }
            float weight = blend * Mathf.Lerp(.3f, 1, Mathf.SmoothStep(0, 1, remainingDistance / .3f));
            float time = phase * asset.duration;
            foreach (var channel in asset.channels)
            {
                var t = bones[channel.bone]; var value = channel.Sample(time);
                if (channel.path == "translation") t.localPosition = Vector3.Lerp(t.localPosition, (Vector3)value, weight);
                else if (channel.path == "scale") t.localScale = Vector3.Lerp(t.localScale, (Vector3)value, weight);
                else if (channel.path == "rotation") t.localRotation = Quaternion.Slerp(t.localRotation, new Quaternion(value.x, value.y, value.z, value.w), weight);
            }
            // Two quiet weight transfers per cycle. Amplitude is visual only and
            // fades to the exact reference silhouette at rest.
            bones[1].localPosition += Vector3.up * (.009f * (1 - Mathf.Cos(phase * Mathf.PI * 4)) * .5f * weight);
            GroundFoot(asset.leftFoot, left, Mathf.Repeat(phase, .5f), moving, weight);
            GroundFoot(asset.rightFoot, right, Mathf.Repeat(phase + .25f, .5f), moving, weight);
        }
        void GroundFoot(int index, Foot state, float contactPhase, bool moving, float weight)
        {
            var foot = bones[index]; var world = foot.position;
            bool contact = moving && contactPhase <= .1f;
            if (contact && !state.locked) { state.anchor = world; state.locked = true; }
            if (!contact) state.locked = false;
            // Foot lock releases at both boundaries, avoiding a discontinuous snap
            // when contact ends or a new move interrupts the arrival blend.
            float lockWeight = contact ? Mathf.SmoothStep(0, 1, contactPhase / .02f) * Mathf.SmoothStep(0, 1, (.1f - contactPhase) / .02f) : 0;
            foot.rotation = Quaternion.Slerp(foot.rotation, model.rotation, weight);
            Vector3 target = Vector3.Lerp(world, state.anchor, lockWeight);
            if (ground != null && ground.Surface(new Vector2(target.x, target.z), out float floor, out _))
            {
                float lift = person.visual.position.y - person.root.position.y;
                float contactY = floor + WalkSpace.FootGap + lift;
                target.y = Mathf.Max(target.y, contactY);
                if (contact) target.y = Mathf.Lerp(target.y, contactY, lockWeight);
            }
            // Tiny feet are rigid; solve their support on the visual skeleton only.
            foot.position = Vector3.Lerp(world, target, weight);
        }
    }
}
