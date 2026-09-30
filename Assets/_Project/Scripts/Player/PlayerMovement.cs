using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// CharacterController locomotion: walk / sprint / jump / gravity.
    /// Player root does not yaw — <see cref="rotationTarget"/> (Visual) faces move direction.
    /// Grounding uses Move collision flags, plus a downward ray (ignoring self) to snap out of
    /// floor penetration. Unity 6 requires CharacterController.includeLayers to be set explicitly.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 7f;
        [SerializeField] float acceleration = 12f;
        [SerializeField] float deceleration = 14f;

        [Header("Jump & Gravity")]
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float groundedStickForce = -2f;
        [SerializeField] float maxFallSpeed = 25f;
        [Tooltip("Feet within this distance above ground count as grounded; below ground snaps up.")]
        [SerializeField] float groundSnapDistance = 0.2f;
        [SerializeField] LayerMask groundMask = ~0;

        [Header("Rotation")]
        [SerializeField] float rotationSpeed = 12f;
        [Tooltip("Usually the Visual child — not the player root (Cinemachine follows the root).")]
        [SerializeField] Transform rotationTarget;

        [Header("References")]
        [SerializeField] Transform cameraTransform;

        static readonly RaycastHit[] GroundHits = new RaycastHit[8];

        CharacterController _controller;
        InputSystem_Actions _input;
        PlayerGrab _grab;
        Vector3 _horizontalVelocity;
        float _verticalVelocity;
        bool _grounded = true;
        bool _inputEnabled = true;
        bool _hasMoveInput;
        float _targetMoveSpeed;

        public float VerticalVelocity => _verticalVelocity;
        public float HorizontalSpeed => _horizontalVelocity.magnitude;
        public bool IsGrounded => _grounded;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        /// <summary>True while Move stick/keys are pressed (even if velocity is still ramping).</summary>
        public bool HasMoveInput => _hasMoveInput;
        /// <summary>Desired planar speed from input (0 / walk / sprint) — for animation blend.</summary>
        public float TargetMoveSpeed => _targetMoveSpeed;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = new InputSystem_Actions();
            _grab = GetComponent<PlayerGrab>();

            // Prefab should have Include Layers = Everything; enforce here for Unity 6 safety.
            _controller.includeLayers = ~0;
            _controller.excludeLayers = 0;

            if (rotationTarget == null)
                rotationTarget = transform;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void OnEnable()
        {
            if (_inputEnabled)
                _input.Enable();
        }

        void OnDisable()
        {
            _input.Disable();
        }

        void OnDestroy()
        {
            _input.Dispose();
        }

        void Update()
        {
            if (!_inputEnabled)
                return;

            HandleJump();
            ApplyGravity();
            MoveHorizontal();
            ApplyMovement();
            ResolveGround();
        }

        void HandleJump()
        {
            if (_grounded && _input.Player.Jump.WasPressedThisFrame())
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        void ApplyGravity()
        {
            if (_grounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = groundedStickForce;
                return;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            if (_verticalVelocity < -maxFallSpeed)
                _verticalVelocity = -maxFallSpeed;
        }

        void MoveHorizontal()
        {
            Vector2 moveInput = _input.Player.Move.ReadValue<Vector2>();
            _hasMoveInput = moveInput.sqrMagnitude >= 0.01f;
            Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
            _targetMoveSpeed = !_hasMoveInput
                ? 0f
                : (_input.Player.Sprint.IsPressed() ? sprintSpeed : walkSpeed);
            Vector3 targetVelocity = moveDirection * _targetMoveSpeed;

            float rate = targetVelocity.sqrMagnitude > 0.01f ? acceleration : deceleration;
            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                rate * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                rotationTarget.rotation = Quaternion.Slerp(
                    rotationTarget.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }

        Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.01f)
                return Vector3.zero;

            Quaternion yawRotation = GetCameraYawRotation();
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            return (forward * moveInput.y + right * moveInput.x).normalized;
        }

        Quaternion GetCameraYawRotation()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform == null)
                return Quaternion.identity;

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return Quaternion.identity;

            return Quaternion.LookRotation(forward.normalized);
        }

        void ApplyMovement()
        {
            Vector3 motion = _horizontalVelocity;
            motion.y = _verticalVelocity;
            CollisionFlags flags = _controller.Move(motion * Time.deltaTime);
            _grounded = (flags & CollisionFlags.Below) != 0;
        }

        void ResolveGround()
        {
            float bottomOffset = GetBottomOffset();
            float feetY = transform.position.y + bottomOffset;
            Vector3 rayOrigin = new Vector3(
                transform.position.x,
                feetY + _controller.height + 0.5f,
                transform.position.z);
            float rayLength = _controller.height + groundSnapDistance + 2.5f;

            if (!TryGetGroundHit(rayOrigin, rayLength, out RaycastHit hit))
                return;

            float distanceFeetToGround = feetY - hit.point.y;
            if (distanceFeetToGround > groundSnapDistance)
                return;

            _grounded = true;
            if (_verticalVelocity < 0f)
                _verticalVelocity = groundedStickForce;

            if (distanceFeetToGround >= -0.001f)
                return;

            TeleportKeepController(new Vector3(
                transform.position.x,
                hit.point.y - bottomOffset,
                transform.position.z));
        }

        bool TryGetGroundHit(Vector3 origin, float length, out RaycastHit bestHit)
        {
            bestHit = default;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHits,
                length,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = GroundHits[i];
                if (hit.collider == null)
                    continue;

                // CharacterController is a raycast target — ignoring self prevents upward snap loops.
                if (IsOwnCollider(hit.collider))
                    continue;

                // Held props sit near the body — snapping onto them lifts the player onto the object.
                if (IsHeldCollider(hit.collider))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }

            return found;
        }

        bool IsOwnCollider(Collider col)
        {
            return col.transform == transform || col.transform.IsChildOf(transform);
        }

        bool IsHeldCollider(Collider col)
        {
            return _grab != null && _grab.Held != null && BelongsTo(_grab.Held.transform, col);
        }

        static bool BelongsTo(Transform root, Collider col)
        {
            return col.transform == root || col.transform.IsChildOf(root);
        }

        float GetBottomOffset()
        {
            return _controller.center.y - _controller.height * 0.5f;
        }

        void TeleportKeepController(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }

        /// <param name="resetRotationTarget">
        /// When true (respawn), Visual faces root forward.
        /// When false (end of GettingUp), keep current Visual yaw.
        /// </param>
        /// <param name="verticalVelocity">
        /// Initial vertical velocity. Default stick force; use 0 after GettingUp so the first
        /// Move does not embed into the floor and trigger ResolveGround pop-up.
        /// </param>
        public void ResetState(
            Vector3 position,
            Quaternion rotation,
            bool resetRotationTarget = true,
            float? verticalVelocity = null)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);

            if (resetRotationTarget && rotationTarget != null && rotationTarget != transform)
                rotationTarget.localRotation = Quaternion.identity;

            _controller.enabled = true;

            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = verticalVelocity ?? groundedStickForce;
            _grounded = true;
            _hasMoveInput = false;
            _targetMoveSpeed = 0f;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (enabled)
                _input.Enable();
            else
                _input.Disable();
        }

        public void SetCameraTransform(Transform transform)
        {
            cameraTransform = transform;
        }
    }
}
