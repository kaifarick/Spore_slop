using UnityEngine;

namespace SporeSlop.Player
{
    /// <summary>
    /// Y-threshold respawn (ADR-003). Recovers from ragdoll first, then resets movement / fall tracking.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] float fallThresholdY = -10f;
        [SerializeField] float respawnDelay;

        PlayerMovement _movement;
        PlayerRagdoll _ragdoll;
        float _respawnTimer;

        void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _ragdoll = GetComponent<PlayerRagdoll>();
        }

        void Start()
        {
            if (spawnPoint != null)
                _movement.ResetState(spawnPoint.position, spawnPoint.rotation);
        }

        void Update()
        {
            if (_respawnTimer > 0f)
            {
                _respawnTimer -= Time.deltaTime;
                return;
            }

            if (transform.position.y >= fallThresholdY)
                return;

            Respawn();
        }

        void Respawn()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning($"{nameof(PlayerRespawn)} on {name}: spawn point is not assigned.", this);
                return;
            }

            if (_ragdoll != null && _ragdoll.IsRagdolled)
                _ragdoll.Recover();

            _movement.ResetState(spawnPoint.position, spawnPoint.rotation);

            if (_ragdoll != null)
                _ragdoll.ResetFallTracking();

            _respawnTimer = respawnDelay;
        }
    }
}
