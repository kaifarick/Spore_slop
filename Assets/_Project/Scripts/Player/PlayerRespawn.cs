using UnityEngine;

namespace SporeSlop.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] float fallThresholdY = -10f;
        [SerializeField] float respawnDelay;

        PlayerMovement _movement;
        float _respawnTimer;

        void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
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

            _movement.ResetState(spawnPoint.position, spawnPoint.rotation);
            _respawnTimer = respawnDelay;
        }
    }
}
