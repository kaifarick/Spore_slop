using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// One-way visual bridge: gameplay state → Animator parameters.
    /// Does not drive movement, grab, or ragdoll (Input → State → Visual).
    ///
    /// Locomotion: <c>Speed</c> follows input target while moving (avoids Idle "slide"
    /// during accel / quick reverse). <c>CycleSpeed</c> matches foot cadence to CC m/s.
    /// <c>IsHolding</c> + UpperBody layer weight (0/1). Arms-only mask keeps Idle torso/legs.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float speedDampTime = 0.08f;
        [SerializeField] float cycleSpeedDampTime = 0.12f;

        [Header("Foot sync (in-place clips)")]
        [Tooltip("Approximate forward speed baked into the Walk clip (m/s). Mixamo walk is often ~1.3–2.")]
        [SerializeField] float walkClipSpeed = 1.5f;
        [Tooltip("Approximate forward speed baked into the Run clip (m/s). Mixamo run is often ~4–6.")]
        [SerializeField] float runClipSpeed = 5f;
        [SerializeField] float minCycleSpeed = 0.5f;
        [SerializeField] float maxCycleSpeed = 2.5f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int CycleSpeedHash = Animator.StringToHash("CycleSpeed");
        static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        static readonly int IsHoldingHash = Animator.StringToHash("IsHolding");
        static readonly int IsRagdolledHash = Animator.StringToHash("IsRagdolled");
        static readonly int IsGettingUpHash = Animator.StringToHash("IsGettingUp");

        PlayerMovement _movement;
        PlayerRagdoll _ragdoll;
        PlayerGrab _grab;
        int _upperBodyLayer = -1;

        void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            _grab = GetComponent<PlayerGrab>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null)
                _upperBodyLayer = animator.GetLayerIndex("UpperBody");
        }

        void Update()
        {
            if (animator == null)
                return;

            bool ragdolled = _ragdoll != null && _ragdoll.IsRagdolled;
            bool gettingUpControl = _ragdoll != null && _ragdoll.IsGettingUp;
            bool gettingUpAnim = _ragdoll != null && _ragdoll.IsGettingUpForAnimator;

            if (ragdolled)
            {
                if (animator.enabled)
                    animator.enabled = false;
                return;
            }

            if (!animator.enabled)
                animator.enabled = true;

            bool holding = _grab != null && _grab.Held != null;

            animator.SetBool(IsRagdolledHash, false);
            animator.SetBool(IsGettingUpHash, gettingUpAnim);
            animator.SetBool(IsGroundedHash, _movement.IsGrounded);
            animator.SetBool(IsHoldingHash, holding);

            // Weight on/off — Empty+WriteDefaults Off left Holding pose stuck after release.
            if (_upperBodyLayer >= 0)
                animator.SetLayerWeight(_upperBodyLayer, holding ? 1f : 0f);

            float speed = gettingUpControl ? 0f : GetLocomotionBlendSpeed();
            animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);

            float cycleSpeed = gettingUpControl ? 1f : ComputeCycleSpeed(_movement.HorizontalSpeed);
            animator.SetFloat(CycleSpeedHash, cycleSpeed, cycleSpeedDampTime, Time.deltaTime);
        }

        /// <summary>
        /// While stick is held, blend from desired speed so quick strafe/reverse does not
        /// dip into Idle (CC still moving, Idle pose = "flying"). Coast uses real velocity.
        /// </summary>
        float GetLocomotionBlendSpeed()
        {
            // Stick held → desired gait (walk/sprint), not current CC speed.
            // Otherwise accel / quick reverse dips Speed near 0 → Idle while still sliding.
            if (_movement.HasMoveInput)
                return _movement.TargetMoveSpeed;

            return _movement.HorizontalSpeed;
        }

        float ComputeCycleSpeed(float horizontalSpeed)
        {
            if (horizontalSpeed < 0.15f)
                return 1f;

            float walk = Mathf.Max(0.05f, walkClipSpeed);
            float run = Mathf.Max(walk, runClipSpeed);
            float walkMove = Mathf.Max(0.05f, _movement.WalkSpeed);
            float sprintMove = Mathf.Max(walkMove, _movement.SprintSpeed);

            float clipRef = horizontalSpeed <= walkMove
                ? walk
                : Mathf.Lerp(walk, run, Mathf.InverseLerp(walkMove, sprintMove, horizontalSpeed));

            return Mathf.Clamp(horizontalSpeed / clipRef, minCycleSpeed, maxCycleSpeed);
        }
    }
}
