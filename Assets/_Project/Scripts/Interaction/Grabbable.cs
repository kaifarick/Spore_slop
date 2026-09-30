using UnityEngine;

namespace SporeSlop.Interaction
{
    /// <summary>
    /// Capability marker: this entity can be targeted for grab interactions.
    /// </summary>
    [DisallowMultipleComponent]
    public class Grabbable : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public Collider[] Colliders { get; private set; }
        public bool IsHeld { get; private set; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            if (Body == null)
                Body = GetComponentInParent<Rigidbody>();

            Colliders = GetComponentsInChildren<Collider>();
        }

        /// <summary>
        /// Set by the holder (e.g. PlayerGrab) when this object is picked up or released.
        /// </summary>
        public void SetHeld(bool held)
        {
            IsHeld = held;
        }
    }
}
