using SporeSlop.Interaction;
using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// Grab / hold / release / throw for <see cref="Grabbable"/> on <see cref="PlayerInteraction.CurrentTarget"/>.
    /// Hold = kinematic + reparent to HoldPoint (ADR-006).
    /// Interact = grab or drop; Attack while holding = throw along character facing.
    /// </summary>
    [RequireComponent(typeof(PlayerInteraction))]
    public class PlayerGrab : MonoBehaviour
    {
        [SerializeField] Transform holdPoint;
        [Tooltip("Usually Visual — throw goes along this forward (flat). Falls back to HoldPoint.")]
        [SerializeField] Transform facingSource;
        [Tooltip("Throw speed applied as VelocityChange (mass-independent).")]
        [SerializeField] float throwSpeed = 12f;
        [Tooltip("Extra up blend on throw direction (0 = pure character forward).")]
        [SerializeField] float throwUpwardBias = 0.2f;
        [SerializeField] bool logEvents;

        PlayerInteraction _interaction;
        InputSystem_Actions _input;
        Grabbable _held;

        public Grabbable Held => _held;

        void Awake()
        {
            _interaction = GetComponent<PlayerInteraction>();
            _input = new InputSystem_Actions();

            if (facingSource == null)
            {
                Transform visual = transform.Find("Visual");
                if (visual != null)
                    facingSource = visual;
            }
        }

        void OnEnable()
        {
            _input.Enable();
        }

        void OnDisable()
        {
            ForceRelease();
            _input.Disable();
        }

        void OnDestroy()
        {
            _input.Dispose();
        }

        public void ForceRelease()
        {
            if (_held != null)
                Drop();
        }

        void Update()
        {
            if (_held != null && _input.Player.Attack.WasPressedThisFrame())
            {
                Throw();
                return;
            }

            if (!_input.Player.Interact.WasPressedThisFrame())
                return;

            if (_held != null)
            {
                Drop();
                return;
            }

            if (_interaction.CurrentTarget == null)
                return;

            Grabbable target = _interaction.CurrentTarget.GetComponent<Grabbable>();
            if (target != null)
                Grab(target);
        }

        void Grab(Grabbable target)
        {
            if (holdPoint == null)
            {
                Debug.LogWarning($"{nameof(PlayerGrab)} on {name}: holdPoint is not assigned.", this);
                return;
            }

            _held = target;
            _held.SetHeld(true);

            if (_held.Body != null)
            {
                _held.Body.linearVelocity = Vector3.zero;
                _held.Body.angularVelocity = Vector3.zero;
                _held.Body.isKinematic = true;
            }

            foreach (Collider col in _held.Colliders)
                col.enabled = false;

            Transform heldTransform = _held.transform;
            heldTransform.SetParent(holdPoint, worldPositionStays: false);
            heldTransform.localPosition = Vector3.zero;
            heldTransform.localRotation = Quaternion.identity;

            if (logEvents)
                Debug.Log($"[PlayerGrab] Grabbed: {_held.name}", _held);
        }

        void Drop()
        {
            DetachHeld(applyThrowVelocity: false);
        }

        void Throw()
        {
            DetachHeld(applyThrowVelocity: true);
        }

        void DetachHeld(bool applyThrowVelocity)
        {
            if (_held == null)
                return;

            Grabbable released = _held;
            released.transform.SetParent(null, worldPositionStays: true);

            foreach (Collider col in released.Colliders)
                col.enabled = true;

            if (released.Body != null)
            {
                released.Body.isKinematic = false;
                released.Body.linearVelocity = Vector3.zero;
                released.Body.angularVelocity = Vector3.zero;

                if (applyThrowVelocity)
                {
                    Vector3 direction = GetThrowDirection();
                    released.Body.AddForce(direction * throwSpeed, ForceMode.VelocityChange);
                }
            }

            released.SetHeld(false);
            _held = null;

            if (logEvents)
            {
                string action = applyThrowVelocity ? "Thrown" : "Released";
                Debug.Log($"[PlayerGrab] {action}: {released.name}", released);
            }
        }

        Vector3 GetThrowDirection()
        {
            Transform source = facingSource != null ? facingSource : holdPoint;
            Vector3 forward = source != null ? source.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = source != null ? source.forward : Vector3.forward;

            Vector3 direction = forward.normalized + Vector3.up * throwUpwardBias;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.up;

            return direction.normalized;
        }
    }
}
