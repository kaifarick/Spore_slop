using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// Optional: snap HoldPoint to humanoid hand midpoint while holding.
    /// Default off — authored HoldPoint (in front of chest) is the stable grab anchor.
    /// Enable after Holding upper-body pose is confirmed, otherwise Idle arms pull the prop into the torso.
    /// </summary>
    public class HoldPointHandFollow : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerGrab grab;
        [Tooltip("When off, HoldPoint stays at its authored local pose (recommended).")]
        [SerializeField] bool followHandsWhileHolding;
        [Tooltip("Local-to-parent offset from the hand midpoint (Z forward toward palms).")]
        [SerializeField] Vector3 offsetFromHands = new Vector3(0f, 0f, 0.1f);

        Transform _leftHand;
        Transform _rightHand;
        Transform _authoredParent;
        Vector3 _authoredLocalPosition;
        Quaternion _authoredLocalRotation;

        void Awake()
        {
            _authoredParent = transform.parent;
            _authoredLocalPosition = transform.localPosition;
            _authoredLocalRotation = transform.localRotation;
        }

        void Start()
        {
            if (grab == null)
                grab = transform.root.GetComponent<PlayerGrab>();

            if (animator == null)
                animator = transform.root.GetComponentInChildren<Animator>();

            if (animator == null || !animator.isHuman)
                return;

            _leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        void LateUpdate()
        {
            if (!followHandsWhileHolding)
            {
                RestoreAuthoredPose();
                return;
            }

            bool holding = grab != null && grab.Held != null;
            if (!holding || _leftHand == null || _rightHand == null || animator == null || !animator.enabled)
            {
                RestoreAuthoredPose();
                return;
            }

            Vector3 midpoint = (_leftHand.position + _rightHand.position) * 0.5f;
            Vector3 offset = transform.parent != null
                ? transform.parent.TransformDirection(offsetFromHands)
                : offsetFromHands;

            transform.position = midpoint + offset;
        }

        void RestoreAuthoredPose()
        {
            if (transform.parent != _authoredParent && _authoredParent != null)
                return;

            transform.localPosition = _authoredLocalPosition;
            transform.localRotation = _authoredLocalRotation;
        }
    }
}
