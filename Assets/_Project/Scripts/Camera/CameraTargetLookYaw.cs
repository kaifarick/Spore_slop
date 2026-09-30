using Unity.Cinemachine;
using UnityEngine;

namespace SporeSlop.PlayerCamera
{
    /// <summary>
    /// Drives CameraTarget yaw from Look (Pan) so Third Person Follow sits behind the
    /// camera heading. Runs after ragdoll hip follow so position can move while yaw stays look-driven.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class CameraTargetLookYaw : MonoBehaviour
    {
        [SerializeField] Transform cameraTarget;
        [SerializeField] CinemachinePanTilt panTilt;

        void Awake()
        {
            if (cameraTarget == null)
            {
                Transform found = transform.Find("CameraTarget");
                if (found != null)
                    cameraTarget = found;
            }

            if (panTilt == null)
                panTilt = FindFirstObjectByType<CinemachinePanTilt>();
        }

        void LateUpdate()
        {
            if (cameraTarget == null || panTilt == null)
                return;

            cameraTarget.rotation = Quaternion.Euler(0f, panTilt.PanAxis.Value, 0f);
        }
    }
}
