using System.Collections.Generic;
using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// Humanoid bone rigidbodies / colliders / CharacterJoints for tumbling.
    /// Built once from an <see cref="Animator"/> (Mixamo-style avatar). Inactive while animated.
    /// </summary>
    public class PlayerRagdollBody : MonoBehaviour
    {
        [SerializeField] Rigidbody hips;
        [SerializeField] Rigidbody[] parts = System.Array.Empty<Rigidbody>();
        [SerializeField] Collider[] partColliders = System.Array.Empty<Collider>();

        public Rigidbody Hips => hips;
        public bool IsBuilt => parts != null && parts.Length > 0 && hips != null;
        public bool IsBlendingPose => _poseBlend > 0f;

        struct BonePose
        {
            public Transform bone;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        struct WorldBonePose
        {
            public Transform bone;
            public Vector3 position;
            public Quaternion rotation;
        }

        BonePose[] _capturedPose = System.Array.Empty<BonePose>();
        WorldBonePose[] _worldPose = System.Array.Empty<WorldBonePose>();
        float _poseBlend;
        float _poseBlendDuration = 0.4f;

        static readonly HumanBodyBones[] GroundProbeBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.Head,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightLowerArm
        };

        public void EnsureBuilt(Animator animator)
        {
            if (IsBuilt || animator == null || !animator.isHuman)
                return;

            Build(animator);
        }

        public void SetRagdollActive(bool active, Vector3 velocity, Vector3 angularVelocity)
        {
            if (!IsBuilt)
                return;

            for (int i = 0; i < partColliders.Length; i++)
            {
                if (partColliders[i] != null)
                    partColliders[i].enabled = active;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                Rigidbody rb = parts[i];
                if (rb == null)
                    continue;

                if (!active)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }
                else
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }

            if (active && hips != null)
            {
                hips.linearVelocity = velocity;
                hips.angularVelocity = angularVelocity;
            }
        }

        /// <summary>Store humanoid bone world transforms (survives moving the player root).</summary>
        public void CaptureWorldPose(Animator animator)
        {
            _worldPose = BuildWorldPoseList(animator);
        }

        /// <summary>Write stored world transforms back (parents first — HumanBodyBones order).</summary>
        public void RestoreWorldPose()
        {
            for (int i = 0; i < _worldPose.Length; i++)
            {
                WorldBonePose pose = _worldPose[i];
                if (pose.bone == null)
                    continue;

                pose.bone.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        /// <summary>Store humanoid bone locals (call after root is placed and world pose restored).</summary>
        public void CapturePose(Animator animator)
        {
            if (animator == null || !animator.isHuman)
            {
                _capturedPose = System.Array.Empty<BonePose>();
                return;
            }

            var list = new List<BonePose>(24);
            for (HumanBodyBones boneId = HumanBodyBones.Hips; boneId < HumanBodyBones.LastBone; boneId++)
            {
                if (boneId >= HumanBodyBones.LeftThumbProximal)
                    break;

                Transform bone = animator.GetBoneTransform(boneId);
                if (bone == null)
                    continue;

                list.Add(new BonePose
                {
                    bone = bone,
                    localPosition = bone.localPosition,
                    localRotation = bone.localRotation
                });
            }

            _capturedPose = list.ToArray();
        }

        /// <summary>Lowest humanoid probe bone Y, for floor sticking during GettingUp.</summary>
        public bool TryGetLowestBoneY(Animator animator, out float lowestY)
        {
            lowestY = float.MaxValue;
            if (animator == null)
                return false;

            bool found = false;
            for (int i = 0; i < GroundProbeBones.Length; i++)
            {
                Transform bone = animator.GetBoneTransform(GroundProbeBones[i]);
                if (bone == null)
                    continue;

                float y = bone.position.y;
                if (y < lowestY)
                    lowestY = y;
                found = true;
            }

            return found;
        }

        static WorldBonePose[] BuildWorldPoseList(Animator animator)
        {
            if (animator == null || !animator.isHuman)
                return System.Array.Empty<WorldBonePose>();

            var list = new List<WorldBonePose>(24);
            for (HumanBodyBones boneId = HumanBodyBones.Hips; boneId < HumanBodyBones.LastBone; boneId++)
            {
                if (boneId >= HumanBodyBones.LeftThumbProximal)
                    break;

                Transform bone = animator.GetBoneTransform(boneId);
                if (bone == null)
                    continue;

                list.Add(new WorldBonePose
                {
                    bone = bone,
                    position = bone.position,
                    rotation = bone.rotation
                });
            }

            return list.ToArray();
        }

        public void BeginPoseBlend(float duration)
        {
            _poseBlendDuration = Mathf.Max(0.05f, duration);
            _poseBlend = 1f;
            ApplyPoseBlend();
        }

        public void ClearPoseBlend()
        {
            _poseBlend = 0f;
            _capturedPose = System.Array.Empty<BonePose>();
            _worldPose = System.Array.Empty<WorldBonePose>();
        }

        /// <summary>
        /// Call from LateUpdate after Animator. weight 1 = full captured ragdoll pose, 0 = pure animation.
        /// </summary>
        public void TickPoseBlend(float deltaTime)
        {
            if (_poseBlend <= 0f || _capturedPose.Length == 0)
                return;

            ApplyPoseBlend();
            _poseBlend -= deltaTime / _poseBlendDuration;
            if (_poseBlend < 0f)
                _poseBlend = 0f;
        }

        void ApplyPoseBlend()
        {
            float w = _poseBlend;
            for (int i = 0; i < _capturedPose.Length; i++)
            {
                BonePose pose = _capturedPose[i];
                if (pose.bone == null)
                    continue;

                // Animator already wrote the target pose this frame — blend back toward ragdoll.
                pose.bone.localRotation = Quaternion.Slerp(pose.bone.localRotation, pose.localRotation, w);
                pose.bone.localPosition = Vector3.Lerp(pose.bone.localPosition, pose.localPosition, w);
            }
        }

        void Build(Animator animator)
        {
            var built = new List<Rigidbody>();
            var colliders = new List<Collider>();

            Transform hipsT = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            if (spine == null)
                spine = animator.GetBoneTransform(HumanBodyBones.Chest);

            if (hipsT == null)
            {
                Debug.LogWarning($"{nameof(PlayerRagdollBody)} on {name}: no Hips bone.", this);
                return;
            }

            hips = CreateBoxPart(
                hipsT,
                EstimateHipsSize(hipsT, leftUpperLeg, rightUpperLeg, spine),
                mass: 12f,
                connected: null,
                built,
                colliders);

            Rigidbody spineRb = null;
            if (spine != null)
            {
                Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                Transform spineEnd = chest != null ? chest : animator.GetBoneTransform(HumanBodyBones.Neck);
                if (spineEnd == null)
                    spineEnd = animator.GetBoneTransform(HumanBodyBones.Head);
                spineRb = CreateCapsulePart(spine, spineEnd, 0.12f, 8f, hips, built, colliders);
            }

            Rigidbody headParent = spineRb != null ? spineRb : hips;
            Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                Transform headBone = neck != null ? neck : head;
                Transform headEnd = head;
                CreateCapsulePart(headBone, headEnd, 0.1f, 4f, headParent, built, colliders);
            }

            BuildLeg(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, hips, built, colliders);
            BuildLeg(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, hips, built, colliders);

            Rigidbody armParent = spineRb != null ? spineRb : hips;
            BuildArm(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, armParent, built, colliders);
            BuildArm(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, armParent, built, colliders);

            parts = built.ToArray();
            partColliders = colliders.ToArray();
            SetRagdollActive(false, Vector3.zero, Vector3.zero);
        }

        void BuildLeg(
            Animator animator,
            HumanBodyBones upper,
            HumanBodyBones lower,
            HumanBodyBones foot,
            Rigidbody hipsBody,
            List<Rigidbody> built,
            List<Collider> colliders)
        {
            Transform upperT = animator.GetBoneTransform(upper);
            Transform lowerT = animator.GetBoneTransform(lower);
            Transform footT = animator.GetBoneTransform(foot);
            if (upperT == null || lowerT == null)
                return;

            Rigidbody upperRb = CreateCapsulePart(upperT, lowerT, 0.09f, 6f, hipsBody, built, colliders);
            if (footT != null)
                CreateCapsulePart(lowerT, footT, 0.07f, 3f, upperRb, built, colliders);
        }

        void BuildArm(
            Animator animator,
            HumanBodyBones upper,
            HumanBodyBones lower,
            HumanBodyBones hand,
            Rigidbody parent,
            List<Rigidbody> built,
            List<Collider> colliders)
        {
            Transform upperT = animator.GetBoneTransform(upper);
            Transform lowerT = animator.GetBoneTransform(lower);
            Transform handT = animator.GetBoneTransform(hand);
            if (upperT == null || lowerT == null)
                return;

            Rigidbody upperRb = CreateCapsulePart(upperT, lowerT, 0.07f, 3f, parent, built, colliders);
            if (handT != null)
                CreateCapsulePart(lowerT, handT, 0.055f, 2f, upperRb, built, colliders);
        }

        static Vector3 EstimateHipsSize(Transform hipsT, Transform leftUpperLeg, Transform rightUpperLeg, Transform spine)
        {
            float width = 0.28f;
            if (leftUpperLeg != null && rightUpperLeg != null)
                width = Mathf.Max(0.22f, Vector3.Distance(leftUpperLeg.position, rightUpperLeg.position) * 0.9f);

            float height = 0.18f;
            if (spine != null)
                height = Mathf.Max(0.14f, Vector3.Distance(hipsT.position, spine.position) * 0.85f);

            return new Vector3(width, height, width * 0.65f);
        }

        static Rigidbody CreateBoxPart(
            Transform bone,
            Vector3 size,
            float mass,
            Rigidbody connected,
            List<Rigidbody> built,
            List<Collider> colliders)
        {
            Rigidbody rb = GetOrAddRigidbody(bone, mass);
            BoxCollider box = bone.GetComponent<BoxCollider>();
            if (box == null)
                box = bone.gameObject.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.zero;
            box.enabled = false;
            colliders.Add(box);

            ConfigureJoint(bone, connected);
            built.Add(rb);
            return rb;
        }

        static Rigidbody CreateCapsulePart(
            Transform bone,
            Transform end,
            float radius,
            float mass,
            Rigidbody connected,
            List<Rigidbody> built,
            List<Collider> colliders)
        {
            if (bone == null || end == null)
                return null;

            Rigidbody rb = GetOrAddRigidbody(bone, mass);
            CapsuleCollider capsule = bone.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = bone.gameObject.AddComponent<CapsuleCollider>();

            Vector3 localEnd = bone.InverseTransformPoint(end.position);
            float length = localEnd.magnitude;
            if (length < 0.001f)
                localEnd = Vector3.up * 0.2f;

            length = Mathf.Max(localEnd.magnitude, radius * 2.1f);
            int direction = DominantAxis(localEnd);
            capsule.direction = direction;
            capsule.radius = radius;
            capsule.height = length;
            capsule.center = localEnd.normalized * (length * 0.5f);
            capsule.enabled = false;
            colliders.Add(capsule);

            ConfigureJoint(bone, connected);
            built.Add(rb);
            return rb;
        }

        static Rigidbody GetOrAddRigidbody(Transform bone, float mass)
        {
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb == null)
                rb = bone.gameObject.AddComponent<Rigidbody>();

            rb.mass = mass;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.15f;
            rb.useGravity = true;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.None;
            return rb;
        }

        static void ConfigureJoint(Transform bone, Rigidbody connected)
        {
            CharacterJoint joint = bone.GetComponent<CharacterJoint>();
            if (connected == null)
            {
                if (joint != null)
                    Destroy(joint);
                return;
            }

            if (joint == null)
                joint = bone.gameObject.AddComponent<CharacterJoint>();

            joint.connectedBody = connected;
            joint.enableProjection = true;
            joint.enableCollision = false;
            joint.autoConfigureConnectedAnchor = true;

            SoftJointLimit lowTwist = new SoftJointLimit { limit = -40f };
            SoftJointLimit highTwist = new SoftJointLimit { limit = 40f };
            SoftJointLimit swing1 = new SoftJointLimit { limit = 45f };
            SoftJointLimit swing2 = new SoftJointLimit { limit = 25f };
            joint.lowTwistLimit = lowTwist;
            joint.highTwistLimit = highTwist;
            joint.swing1Limit = swing1;
            joint.swing2Limit = swing2;
        }

        static int DominantAxis(Vector3 local)
        {
            Vector3 a = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            if (a.y >= a.x && a.y >= a.z)
                return 1;
            if (a.x >= a.z)
                return 0;
            return 2;
        }
    }
}
