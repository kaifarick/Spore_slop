using Unity.Cinemachine;
using UnityEngine;

namespace SporeSlop.Player
{
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

        [Header("Rotation")]
        [SerializeField] float rotationSpeed = 12f;
        [Tooltip("Rotates toward move direction. Use a Visual child — not the Player root — so Cinemachine does not spin.")]
        [SerializeField] Transform rotationTarget;

        [Header("References")]
        [SerializeField] CinemachinePanTilt viewYawSource;
        [SerializeField] Transform cameraTransform;

        CharacterController _controller;
        InputSystem_Actions _input;
        Vector3 _horizontalVelocity;
        float _verticalVelocity;
        bool _inputEnabled = true;

        public float VerticalVelocity => _verticalVelocity;
        public bool IsGrounded => _controller.isGrounded;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = new InputSystem_Actions();

            if (rotationTarget == null)
                rotationTarget = transform;

            if (viewYawSource == null)
                viewYawSource = FindFirstObjectByType<CinemachinePanTilt>();

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

            UpdateGroundedVerticalVelocity();
            HandleJump();
            ApplyGravity();
            MoveHorizontal();
            ApplyMovement();
        }

        void UpdateGroundedVerticalVelocity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = groundedStickForce;
        }

        void HandleJump()
        {
            if (_controller.isGrounded && _input.Player.Jump.WasPressedThisFrame())
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        void ApplyGravity()
        {
            if (!_controller.isGrounded)
                _verticalVelocity += gravity * Time.deltaTime;
        }

        void MoveHorizontal()
        {
            Vector2 moveInput = _input.Player.Move.ReadValue<Vector2>();
            Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
            float targetSpeed = _input.Player.Sprint.IsPressed() ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = moveDirection * targetSpeed;

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

            Quaternion yawRotation = viewYawSource != null
                ? Quaternion.Euler(0f, viewYawSource.PanAxis.Value, 0f)
                : GetCameraYawRotation();

            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            return (forward * moveInput.y + right * moveInput.x).normalized;
        }

        Quaternion GetCameraYawRotation()
        {
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
            _controller.Move(motion * Time.deltaTime);
        }

        public void ResetState(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);

            if (rotationTarget != null && rotationTarget != transform)
                rotationTarget.localRotation = Quaternion.identity;

            _controller.enabled = true;

            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = groundedStickForce;
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
