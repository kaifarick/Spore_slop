using SporeSlop.Interaction;
using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// Generic interactable detection only (facing + body-height box → nearest capability root).
    /// Does not know Grab/Hold — callers read <see cref="CurrentTarget"/> and check capabilities.
    /// Reach is short and measured to collider surface so grab requires standing against the object.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Usually Visual — player root does not rotate.")]
        [SerializeField] Transform facingSource;
        [SerializeField] float detectRange = 1f;
        [SerializeField] float detectWidth = 1f;
        [SerializeField] float verticalMargin = 0.3f;
        [Tooltip("Used only when no CharacterController is present.")]
        [SerializeField] float originHeight = 1f;
        [SerializeField] float originForwardOffset = 0.2f;
        [Tooltip("Max distance from body center to object surface (ClosestPoint).")]
        [SerializeField] float maxReachDistance = 0.85f;
        [SerializeField] LayerMask detectMask = ~0;
        [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Debug")]
        [SerializeField] bool logTargetChanges;
        [SerializeField] bool drawGizmos = true;

        CharacterController _controller;
        readonly Collider[] _overlapHits = new Collider[16];

        public Transform CurrentTarget { get; private set; }

        void Awake()
        {
            if (facingSource == null)
            {
                Transform visual = transform.Find("Visual");
                facingSource = visual != null ? visual : transform;
            }

            _controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            Transform detected = DetectInteractable();
            if (detected == CurrentTarget)
                return;

            CurrentTarget = detected;

            if (!logTargetChanges)
                return;

            if (CurrentTarget != null)
                Debug.Log($"[PlayerInteraction] Target: {CurrentTarget.name}", CurrentTarget);
            else
                Debug.Log("[PlayerInteraction] Target: none");
        }

        void GetDetectionBox(out Vector3 center, out Vector3 halfExtents, out Quaternion orientation)
        {
            Vector3 forward = GetForward();
            orientation = Quaternion.LookRotation(forward, Vector3.up);

            float centerY;
            float halfHeight;
            if (_controller != null)
            {
                centerY = _controller.bounds.center.y;
                halfHeight = _controller.bounds.extents.y + verticalMargin;
            }
            else
            {
                centerY = transform.position.y + originHeight;
                halfHeight = 1f + verticalMargin;
            }

            halfExtents = new Vector3(detectWidth * 0.5f, halfHeight, detectRange * 0.5f);

            Vector3 basePosition = transform.position;
            basePosition.y = centerY;
            center = basePosition + forward * (originForwardOffset + halfExtents.z);
        }

        Transform DetectInteractable()
        {
            GetDetectionBox(out Vector3 center, out Vector3 halfExtents, out Quaternion orientation);

            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                _overlapHits,
                orientation,
                detectMask,
                triggerInteraction);

            Vector3 reachOrigin = GetReachOrigin();
            float maxReachSq = maxReachDistance * maxReachDistance;
            float bestDistanceSq = float.MaxValue;
            Transform best = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapHits[i];
                if (hit == null)
                    continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Transform root = FindInteractableRoot(hit.transform);
                if (root == null)
                    continue;

                float distanceSq = (hit.ClosestPoint(reachOrigin) - reachOrigin).sqrMagnitude;
                if (distanceSq > maxReachSq || distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                best = root;
            }

            return best;
        }

        Vector3 GetReachOrigin()
        {
            if (_controller != null)
                return _controller.bounds.center;

            Vector3 origin = transform.position;
            origin.y += originHeight;
            return origin;
        }

        /// <summary>Capability root on the hit or a parent. Extend when adding capabilities.</summary>
        static Transform FindInteractableRoot(Transform hitTransform)
        {
            Grabbable grabbable = hitTransform.GetComponentInParent<Grabbable>();
            if (grabbable != null)
                return grabbable.transform;

            return null;
        }

        Vector3 GetForward()
        {
            Transform source = facingSource != null ? facingSource : transform;
            Vector3 forward = source.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            Vector3 center;
            Vector3 halfExtents;
            Quaternion orientation;

            if (Application.isPlaying)
            {
                GetDetectionBox(out center, out halfExtents, out orientation);
            }
            else
            {
                Vector3 forward = GetForward();
                orientation = Quaternion.LookRotation(forward, Vector3.up);
                halfExtents = new Vector3(detectWidth * 0.5f, 1f + verticalMargin, detectRange * 0.5f);
                Vector3 basePosition = transform.position;
                basePosition.y += originHeight;
                center = basePosition + forward * (originForwardOffset + halfExtents.z);
            }

            Gizmos.color = CurrentTarget != null ? Color.green : Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(center, orientation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
