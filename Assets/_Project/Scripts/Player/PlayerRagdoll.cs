using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeSlop.Player
{
    /// <summary>
    /// Bone ragdoll (humanoid): Normal ↔ Ragdoll ↔ GettingUp ↔ Normal.
    ///
    /// GettingUp (linear): Playing → Exiting → Finish (seat CC, restore input once).
    ///
    /// Mixamo Getting Up stores stand-up rise in Root Transform Position (Y). We bake that
    /// into pose (SporeSlop → Setup Player Animation) and keep applyRootMotion false.
    /// Root Y during get-up tracks the floor via <see cref="StickLowestBoneToGround"/>.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerRagdollBody))]
    public class PlayerRagdoll : MonoBehaviour
    {
        enum GetUpPhase
        {
            None,
            Playing,
            Exiting
        }

        const string GettingUpStateName = "GettingUp";
        const float ExitBlendSeconds = 0.35f;
        const float StickDeadzone = 0.02f;
        const float StickMaxStep = 0.2f;

        [Header("References")]
        [Tooltip("Usually Visual — used for facing / tip direction.")]
        [SerializeField] Transform bodyVisual;
        [Tooltip("Kept world-upright while the body tumbles so Cinemachine does not roll.")]
        [SerializeField] Transform cameraTarget;
        [SerializeField] Animator animator;
        [SerializeField] PlayerRagdollBody ragdollBody;

        [Header("Enter Impulse")]
        [SerializeField] float enterTipImpulse = 3.5f;
        [SerializeField] float enterTipTorque = 8f;

        [Header("Fall Trigger")]
        [SerializeField] bool enterOnHardLanding = true;
        [Tooltip("Peak downward speed (m/s) on landing that triggers ragdoll. ~12 ≈ 3.6 m drop at gravity -20.")]
        [SerializeField] float hardLandingSpeed = 12f;

        [Header("Recover")]
        [Tooltip("Seconds after enter before automatic get-up starts.")]
        [SerializeField] float autoRecoverDelay = 2.5f;
        [Tooltip("Fallback if Animator never reports GettingUp finished.")]
        [SerializeField] float gettingUpTimeout = 3.5f;
        [Tooltip("Blend from ragdoll bone pose into GettingUp (avoids snap to clip frame 0).")]
        [SerializeField] float getUpPoseBlendDuration = 0.35f;

        [Header("Debug")]
        [SerializeField] Key debugToggleKey = Key.R;
        [SerializeField] bool logEvents;
        [Tooltip("Log root/hips/stick every frame for the last 15% of GettingUp.")]
        [SerializeField] bool logGetUpTail;

        static readonly RaycastHit[] StandHits = new RaycastHit[8];
        static readonly int IsRagdolledHash = Animator.StringToHash("IsRagdolled");
        static readonly int IsGettingUpHash = Animator.StringToHash("IsGettingUp");

        CharacterController _controller;
        PlayerMovement _movement;
        PlayerInteraction _interaction;
        PlayerGrab _grab;

        Vector3 _cameraTargetLocalPosition;

        float _peakFallSpeed;
        float _recoverAt = -1f;
        float _getUpClipEndsAt = -1f;
        float _exitEndsAt = -1f;
        float _gettingUpStartedAt = -1f;
        float _gettingUpClipLength = 2f;
        bool _isRagdolled;
        bool _getUpStateReached;
        GetUpPhase _getUpPhase;

        public bool IsRagdolled => _isRagdolled;

        /// <summary>Control lock for the whole get-up including the exit blend.</summary>
        public bool IsGettingUp => _getUpPhase != GetUpPhase.None;

        /// <summary>
        /// Animator <c>IsGettingUp</c> only. False during exit blend so Locomotion can start.
        /// </summary>
        public bool IsGettingUpForAnimator => _getUpPhase == GetUpPhase.Playing;

        public void ResetFallTracking()
        {
            _peakFallSpeed = 0f;
        }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _movement = GetComponent<PlayerMovement>();
            _interaction = GetComponent<PlayerInteraction>();
            _grab = GetComponent<PlayerGrab>();

            if (ragdollBody == null)
                ragdollBody = GetComponent<PlayerRagdollBody>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (bodyVisual == null)
            {
                Transform visual = transform.Find("Visual");
                bodyVisual = visual != null ? visual : transform;
            }

            if (cameraTarget == null)
            {
                Transform found = transform.Find("CameraTarget");
                if (found != null)
                    cameraTarget = found;
            }

            if (cameraTarget != null)
                _cameraTargetLocalPosition = cameraTarget.localPosition;

            if (ragdollBody != null)
                ragdollBody.EnsureBuilt(animator);

            if (animator != null)
                animator.applyRootMotion = false;
        }

        void Update()
        {
            UpdateFallTracking();
            UpdateAutoRecover();
            UpdateGettingUp();
            HandleDebugKey();
        }

        void HandleDebugKey()
        {
            if (Keyboard.current == null || !Keyboard.current[debugToggleKey].wasPressedThisFrame)
                return;

            if (_isRagdolled)
            {
                BeginGettingUp();
                return;
            }

            if (_getUpPhase == GetUpPhase.Playing)
            {
                StartGettingUpExit("debug R");
                return;
            }

            if (_getUpPhase == GetUpPhase.Exiting)
            {
                FinishGettingUp("debug R");
                return;
            }

            EnterRagdoll();
        }

        void LateUpdate()
        {
            if (_getUpPhase == GetUpPhase.Playing)
            {
                if (ragdollBody != null)
                    ragdollBody.TickPoseBlend(Time.deltaTime);

                float stickDy = StickLowestBoneToGround();
                float nt = GetGettingUpNormalizedTime();
                if (logGetUpTail && nt >= 0.85f)
                    LogGetUpTail(nt, stickDy);

                UpdateCameraFollowHips();
                return;
            }

            if (_getUpPhase == GetUpPhase.Exiting)
            {
                StickLowestBoneToGround();
                UpdateCameraFollowHips();
                return;
            }

            if (_isRagdolled)
                UpdateCameraFollowHips();
        }

        float GetGettingUpNormalizedTime()
        {
            if (animator == null || !animator.enabled)
                return 0f;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(GettingUpStateName))
                return 0f;

            return Mathf.Clamp01(info.normalizedTime);
        }

        void LogGetUpTail(float normalized, float stickDy)
        {
            float hipsY = float.NaN;
            Transform hips = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (hips != null)
                hipsY = hips.position.y;

            Debug.Log(
                $"[PlayerRagdoll] tail nt={normalized:F3} rootY={transform.position.y:F3} hipsY={hipsY:F3} dStick={stickDy:F4}",
                this);
        }

        void UpdateCameraFollowHips()
        {
            if (cameraTarget == null)
                return;

            Vector3 follow = transform.position;
            if (animator != null && animator.enabled)
            {
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                    follow = hips.position;
            }
            else if (ragdollBody != null && ragdollBody.Hips != null)
            {
                follow = ragdollBody.Hips.position;
            }

            cameraTarget.position = follow + Vector3.up * _cameraTargetLocalPosition.y;
            // Yaw stays look-driven (CameraTargetLookYaw) so Pan and TPF stay aligned while tumbling.
        }

        void UpdateAutoRecover()
        {
            if (!_isRagdolled || _recoverAt < 0f || Time.time < _recoverAt)
                return;

            BeginGettingUp();
        }

        void UpdateGettingUp()
        {
            if (_getUpPhase == GetUpPhase.None || animator == null || !animator.enabled)
                return;

            if (_getUpPhase == GetUpPhase.Exiting)
            {
                if (Time.time >= _exitEndsAt || IsExitBlendDone())
                    FinishGettingUp("exit blend done");
                return;
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

            if (!_getUpStateReached)
            {
                if (info.IsName(GettingUpStateName))
                {
                    _getUpStateReached = true;
                    CacheGettingUpClipLength();
                }
                else if (Time.time >= _getUpClipEndsAt)
                {
                    StartGettingUpExit("never entered GettingUp state");
                }

                return;
            }

            if (Time.time >= _getUpClipEndsAt)
            {
                StartGettingUpExit("clip timeout");
                return;
            }

            if (!info.IsName(GettingUpStateName))
            {
                StartGettingUpExit("left GettingUp state");
                return;
            }

            // Ignore early normalizedTime glitches right after Play().
            if (Time.time - _gettingUpStartedAt < _gettingUpClipLength * 0.5f)
                return;

            if (!animator.IsInTransition(0) && info.normalizedTime >= 0.995f)
                StartGettingUpExit("clip end");
        }

        bool IsExitBlendDone()
        {
            if (animator == null)
                return true;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return !animator.IsInTransition(0) && !info.IsName(GettingUpStateName);
        }

        void CacheGettingUpClipLength()
        {
            if (animator == null)
                return;

            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0 && clips[0].clip != null)
            {
                float speed = Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).speed);
                if (speed < 0.01f)
                    speed = 1f;
                _gettingUpClipLength = Mathf.Max(0.5f, clips[0].clip.length / speed);
            }

            _getUpClipEndsAt = _gettingUpStartedAt + _gettingUpClipLength + getUpPoseBlendDuration + 0.35f;
        }

        void UpdateFallTracking()
        {
            if (_isRagdolled || _getUpPhase != GetUpPhase.None || !enterOnHardLanding)
            {
                if (_isRagdolled || _getUpPhase != GetUpPhase.None)
                    _peakFallSpeed = 0f;
                return;
            }

            if (!_movement.IsGrounded)
            {
                _peakFallSpeed = Mathf.Max(_peakFallSpeed, -_movement.VerticalVelocity);
                return;
            }

            float impactSpeed = _peakFallSpeed;
            _peakFallSpeed = 0f;

            if (impactSpeed < hardLandingSpeed)
                return;

            if (logEvents)
                Debug.Log($"[PlayerRagdoll] Hard landing: {impactSpeed:F1} m/s", this);

            EnterRagdoll();
        }

        public void EnterRagdoll(Vector3? extraVelocity = null)
        {
            if (_isRagdolled || _getUpPhase != GetUpPhase.None)
                return;

            if (ragdollBody == null)
            {
                Debug.LogWarning($"{nameof(PlayerRagdoll)} on {name}: missing {nameof(PlayerRagdollBody)}.", this);
                return;
            }

            ragdollBody.EnsureBuilt(animator);
            if (!ragdollBody.IsBuilt)
            {
                Debug.LogWarning($"{nameof(PlayerRagdoll)} on {name}: ragdoll body failed to build (need humanoid Animator).", this);
                return;
            }

            ForceReleaseInteractions();
            SetGameplayInput(false);

            Vector3 tipDir = GetFlatForward();
            float verticalVelocity = _movement.VerticalVelocity;

            _controller.enabled = false;

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.SetBool(IsGettingUpHash, false);
                animator.SetBool(IsRagdolledHash, true);
                animator.enabled = false;
            }

            Vector3 velocity = tipDir * enterTipImpulse + Vector3.up * Mathf.Max(0f, verticalVelocity);
            if (extraVelocity.HasValue)
                velocity += extraVelocity.Value;

            Vector3 angular = Vector3.Cross(Vector3.up, tipDir) * enterTipTorque;
            ragdollBody.SetRagdollActive(true, velocity, angular);

            _isRagdolled = true;
            ClearGetUpRuntimeState();
            _recoverAt = Time.time + Mathf.Max(0.1f, autoRecoverDelay);

            if (logEvents)
                Debug.Log($"[PlayerRagdoll] Enter (get-up in {autoRecoverDelay:F1}s)", this);
        }

        public void BeginGettingUp()
        {
            if (!_isRagdolled || _getUpPhase != GetUpPhase.None)
                return;

            if (ragdollBody == null || ragdollBody.Hips == null || animator == null)
            {
                RecoverImmediate();
                return;
            }

            Vector3 hipsWorld = ragdollBody.Hips.position;

            ragdollBody.CaptureWorldPose(animator);
            ragdollBody.SetRagdollActive(false, Vector3.zero, Vector3.zero);

            float lyingYaw = ComputeLyingVisualYaw();

            _controller.enabled = false;
            SetGameplayInput(false);

            _isRagdolled = false;
            _getUpPhase = GetUpPhase.Playing;
            _getUpStateReached = false;
            _recoverAt = -1f;
            _gettingUpStartedAt = Time.time;
            _gettingUpClipLength = Mathf.Max(1f, gettingUpTimeout);
            _getUpClipEndsAt = _gettingUpStartedAt + _gettingUpClipLength + 1f;
            _exitEndsAt = -1f;

            // Near hips first — seating standing CC here floats the lying start pose.
            SetPosition(hipsWorld);
            if (bodyVisual != null)
                bodyVisual.localRotation = Quaternion.Euler(0f, lyingYaw, 0f);

            ragdollBody.RestoreWorldPose();
            ragdollBody.CapturePose(animator);

            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.SetBool(IsRagdolledHash, false);
            animator.SetBool(IsGettingUpHash, true);
            animator.Play(GettingUpStateName, 0, 0f);
            animator.Update(0f);
            CacheGettingUpClipLength();
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(GettingUpStateName))
                _getUpStateReached = true;

            ragdollBody.BeginPoseBlend(getUpPoseBlendDuration);
            MatchHipsPosition(hipsWorld);
            StickLowestBoneToGround();

            RestoreCameraTargetLocal();
            UpdateCameraFollowHips();

            if (logEvents)
                Debug.Log("[PlayerRagdoll] GettingUp", this);
        }

        /// <summary>Skip get-up (respawn / fail-safe).</summary>
        public void RecoverImmediate()
        {
            if (!_isRagdolled && _getUpPhase == GetUpPhase.None)
                return;

            Vector3 from = transform.position;
            if (_isRagdolled && ragdollBody != null && ragdollBody.Hips != null)
                from = ragdollBody.Hips.position;
            else if (_getUpPhase != GetUpPhase.None && animator != null)
            {
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                    from = hips.position;
            }

            if (ragdollBody != null)
            {
                ragdollBody.SetRagdollActive(false, Vector3.zero, Vector3.zero);
                ragdollBody.ClearPoseBlend();
            }

            SetPosition(FindStandingPosition(from));
            RestoreCameraTargetLocal();

            _controller.enabled = true;
            _movement.ResetState(transform.position, transform.rotation);

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.enabled = true;
                animator.SetBool(IsRagdolledHash, false);
                animator.SetBool(IsGettingUpHash, false);
            }

            _isRagdolled = false;
            ClearGetUpRuntimeState();
            SetGameplayInput(true);

            if (logEvents)
                Debug.Log("[PlayerRagdoll] RecoverImmediate", this);
        }

        public void Recover() => RecoverImmediate();

        void StartGettingUpExit(string reason)
        {
            if (_getUpPhase != GetUpPhase.Playing)
                return;

            _getUpPhase = GetUpPhase.Exiting;

            if (animator != null)
                animator.SetBool(IsGettingUpHash, false);

            if (ragdollBody != null)
                ragdollBody.ClearPoseBlend();

            _exitEndsAt = Time.time + ExitBlendSeconds;

            if (logEvents || logGetUpTail)
            {
                float hipsY = float.NaN;
                Transform hips = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
                if (hips != null)
                    hipsY = hips.position.y;

                Debug.Log(
                    $"[PlayerRagdoll] exit start ({reason}) rootY={transform.position.y:F3} hipsY={hipsY:F3}",
                    this);
            }
        }

        void FinishGettingUp(string reason)
        {
            if (_getUpPhase == GetUpPhase.None)
                return;

            Vector3 beforeSeat = transform.position;
            SetPosition(FindStandingPosition(transform.position));

            if (logEvents || logGetUpTail)
            {
                float lowY = float.NaN;
                float groundY = float.NaN;
                if (ragdollBody != null && animator != null)
                    ragdollBody.TryGetLowestBoneY(animator, out lowY);
                TryGetGroundY(transform.position, out groundY);

                Debug.Log(
                    $"[PlayerRagdoll] finish ({reason}) beforeY={beforeSeat.y:F3} afterY={transform.position.y:F3} groundY={groundY:F3} lowestBoneY={lowY:F3}",
                    this);
            }

            _controller.enabled = true;
            _movement.ResetState(
                transform.position,
                transform.rotation,
                resetRotationTarget: false,
                verticalVelocity: 0f);

            RestoreCameraTargetLocal();
            ClearGetUpRuntimeState();
            SetGameplayInput(true);
        }

        void ClearGetUpRuntimeState()
        {
            _getUpPhase = GetUpPhase.None;
            _getUpStateReached = false;
            _getUpClipEndsAt = -1f;
            _exitEndsAt = -1f;
            _gettingUpStartedAt = -1f;
            _recoverAt = -1f;
        }

        void SetGameplayInput(bool enabled)
        {
            _movement.SetInputEnabled(enabled);
            if (_interaction != null)
                _interaction.enabled = enabled;
            if (_grab != null)
                _grab.enabled = enabled;
        }

        /// <summary>
        /// Keep lowest probe bone on the floor (up and down). Lift-only stacked float height.
        /// </summary>
        float StickLowestBoneToGround()
        {
            if (animator == null || ragdollBody == null)
                return 0f;
            if (!ragdollBody.TryGetLowestBoneY(animator, out float lowY))
                return 0f;
            if (!TryGetGroundY(transform.position, out float groundY))
                return 0f;

            float error = groundY - lowY;
            if (Mathf.Abs(error) <= StickDeadzone)
                return 0f;

            error = Mathf.Clamp(error, -StickMaxStep, StickMaxStep);
            SetPosition(transform.position + Vector3.up * error);
            return error;
        }

        void MatchHipsPosition(Vector3 worldHips)
        {
            if (animator == null)
                return;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            SetPosition(transform.position + (worldHips - hips.position));
        }

        void SetPosition(Vector3 position)
        {
            bool wasEnabled = _controller != null && _controller.enabled;
            if (wasEnabled)
                _controller.enabled = false;

            transform.position = position;

            if (wasEnabled)
                _controller.enabled = true;
        }

        float ComputeLyingVisualYaw()
        {
            if (animator == null)
                return bodyVisual != null ? bodyVisual.localEulerAngles.y : 0f;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder)
                                     ?? animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder)
                                      ?? animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

            if (hips == null || leftShoulder == null || rightShoulder == null)
                return bodyVisual != null ? bodyVisual.localEulerAngles.y : 0f;

            Vector3 shoulder = rightShoulder.position - leftShoulder.position;
            Vector3 faceDir = Vector3.Cross(shoulder, Vector3.up);
            if (Vector3.Dot(hips.up, Vector3.up) < 0f)
                faceDir = -faceDir;

            faceDir.y = 0f;
            if (faceDir.sqrMagnitude < 0.0001f)
                return bodyVisual != null ? bodyVisual.localEulerAngles.y : 0f;

            return Quaternion.LookRotation(faceDir.normalized, Vector3.up).eulerAngles.y;
        }

        bool TryGetGroundY(Vector3 from, out float groundY)
        {
            groundY = from.y;
            float castHeight = Mathf.Max(_controller != null ? _controller.height : 2f, 2f);
            Vector3 origin = from + Vector3.up * castHeight;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                StandHits,
                castHeight * 2f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = StandHits[i];
                if (hit.collider == null)
                    continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                    continue;
                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                groundY = hit.point.y;
                found = true;
            }

            return found;
        }

        void RestoreCameraTargetLocal()
        {
            if (cameraTarget == null)
                return;

            cameraTarget.localPosition = _cameraTargetLocalPosition;
            cameraTarget.localRotation = Quaternion.identity;
        }

        Vector3 FindStandingPosition(Vector3 from)
        {
            if (!TryGetGroundY(from, out float groundY))
                return from;

            float bottomOffset = _controller.center.y - _controller.height * 0.5f;
            from.y = groundY - bottomOffset;
            return from;
        }

        void ForceReleaseInteractions()
        {
            if (_grab != null)
                _grab.ForceRelease();
        }

        Vector3 GetFlatForward()
        {
            Transform source = bodyVisual != null ? bodyVisual : transform;
            Vector3 forward = source.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }
    }
}
