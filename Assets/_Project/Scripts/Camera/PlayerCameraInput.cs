using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeSlop.PlayerCamera
{
    [RequireComponent(typeof(CinemachinePanTilt))]
    public class PlayerCameraInput : MonoBehaviour
    {
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] bool lockCursorOnPlay;

        CinemachinePanTilt _panTilt;
        InputSystem_Actions _input;

        public CinemachinePanTilt PanTilt => _panTilt;

        void Awake()
        {
            _panTilt = GetComponent<CinemachinePanTilt>();
            _input = new InputSystem_Actions();
        }

        void OnEnable()
        {
            _input.Enable();

            if (lockCursorOnPlay && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            _input.Disable();

            if (lockCursorOnPlay && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void OnDestroy()
        {
            _input.Dispose();
        }

        void Update()
        {
            Vector2 look = _input.Player.Look.ReadValue<Vector2>();
            if (look.sqrMagnitude < 0.0001f)
                return;

            float scale = lookSensitivity;
            if (IsGamepadLook())
                scale *= Time.deltaTime;

            _panTilt.PanAxis.Value += look.x * scale;
            _panTilt.TiltAxis.Value -= look.y * scale;
        }

        bool IsGamepadLook()
        {
            InputControl activeControl = _input.Player.Look.activeControl;
            return activeControl != null && activeControl.device is Gamepad;
        }
    }
}
