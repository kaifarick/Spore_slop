using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SporeSlop.PlayerCamera
{
    /// <summary>
    /// Look → Cinemachine Pan/Tilt. Armed only after scene load + cursor lock + settled Look delta.
    /// </summary>
    [RequireComponent(typeof(CinemachinePanTilt))]
    public class PlayerCameraInput : MonoBehaviour
    {
        [SerializeField] float lookSensitivity = 0.15f;
        [Tooltip("Lock and hide cursor while this component is enabled in Play.")]
        [SerializeField] bool lockCursorOnPlay = true;
        [Tooltip("No Look while cursor is unlocked (Editor Play + menus).")]
        [SerializeField] bool requireCursorLockForLook = true;
        [Tooltip("Look magnitude below this counts as settled after load.")]
        [SerializeField] float settleLookSqrMagnitude = 0.25f;
        [Tooltip("Consecutive settled frames required before Look arms.")]
        [SerializeField] int settleQuietFrames = 5;

        CinemachinePanTilt _panTilt;
        InputSystem_Actions _input;
        bool _lookArmed;
        int _quietFrames;

        public CinemachinePanTilt PanTilt => _panTilt;

        void Awake()
        {
            _panTilt = GetComponent<CinemachinePanTilt>();
            _input = new InputSystem_Actions();
        }

        void OnEnable()
        {
            _input.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyCursorLock(true);
            DisarmLook();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _input.Disable();
            ApplyCursorLock(false);
        }

        void OnDestroy()
        {
            _input?.Dispose();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!Application.isPlaying || !hasFocus)
                return;

            ApplyCursorLock(true);
            DisarmLook();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyCursorLock(true);
            DisarmLook();
        }

        void Update()
        {
            Vector2 look = _input.Player.Look.ReadValue<Vector2>();

            if (!_lookArmed)
            {
                SnapAxesToCenter();
                TryArmLook(look);
                return;
            }

            if (!CanReceiveLook() || look.sqrMagnitude < 0.0001f)
                return;

            float scale = lookSensitivity;
            if (IsGamepadLook())
                scale *= Time.deltaTime;

            _panTilt.PanAxis.Value += look.x * scale;
            _panTilt.TiltAxis.Value -= look.y * scale;
        }

        void TryArmLook(Vector2 look)
        {
            if (!CanReceiveLook() || look.sqrMagnitude > settleLookSqrMagnitude)
            {
                _quietFrames = 0;
                return;
            }

            _quietFrames++;
            if (_quietFrames < settleQuietFrames)
                return;

            _lookArmed = true;
            SnapAxesToCenter();
        }

        bool CanReceiveLook()
        {
            if (!Application.isFocused)
                return false;

            if (requireCursorLockForLook && Cursor.lockState != CursorLockMode.Locked)
                return false;

            return true;
        }

        void DisarmLook()
        {
            _lookArmed = false;
            _quietFrames = 0;
            SnapAxesToCenter();
        }

        void SnapAxesToCenter()
        {
            if (_panTilt == null)
                return;

            _panTilt.PanAxis.Value = _panTilt.PanAxis.Center;
            _panTilt.TiltAxis.Value = _panTilt.TiltAxis.Center;
        }

        void ApplyCursorLock(bool lockCursor)
        {
            if (!lockCursorOnPlay || !Application.isPlaying)
                return;

            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        bool IsGamepadLook()
        {
            InputControl activeControl = _input.Player.Look.activeControl;
            return activeControl != null && activeControl.device is Gamepad;
        }
    }
}
