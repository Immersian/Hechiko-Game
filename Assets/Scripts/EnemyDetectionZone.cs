using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyDetectionZone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Transform enemyTransform;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private bool followEnemy = true;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Collider Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private string playerTag = "Player"; // Tag for main player collider
    [SerializeField] private bool ignoreWeaponColliders = true; // Option to ignore weapon colliders

    public bool playerInZone { get; private set; }

    private IEnemyController enemyInterface;
    private Transform playerTransform;
    private Collider2D playerMainCollider; // Track main player collider

    public interface IEnemyController
    {
        void OnPlayerDetected();
        void OnPlayerLost();
        bool HasLineOfSightToPlayer();
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (enemyTransform == null)
        {
            enemyTransform = transform.parent;
            Debug.LogWarning("No enemy transform assigned - defaulting to parent", this);
        }

        if (enemyTransform != null)
        {
            enemyInterface = enemyTransform.GetComponent<IEnemyController>();
            if (enemyInterface == null)
            {
                Debug.LogError($"Enemy {enemyTransform.name} doesn't implement IEnemyController!", this);
            }
        }
    }

    private void Update()
    {
        if (followEnemy && enemyTransform != null)
        {
            transform.position = enemyTransform.position + positionOffset;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidPlayerCollider(other)) return;

        // Store reference to main player collider
        if (playerMainCollider == null && other.CompareTag(playerTag))
        {
            playerMainCollider = other;
        }

        playerInZone = true;
        playerTransform = other.transform;

        if (!requireLineOfSight || (enemyInterface != null && enemyInterface.HasLineOfSightToPlayer()))
        {
            enemyInterface?.OnPlayerDetected();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsValidPlayerCollider(other)) return;

        // Only update detection if this is the main player collider
        if (other != playerMainCollider) return;

        if (requireLineOfSight && playerInZone && enemyInterface != null)
        {
            if (enemyInterface.HasLineOfSightToPlayer())
            {
                enemyInterface?.OnPlayerDetected();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidPlayerCollider(other)) return;

        // Only trigger exit if main player collider leaves
        if (other != playerMainCollider) return;

        playerInZone = false;
        enemyInterface?.OnPlayerLost();
    }

    private bool IsValidPlayerCollider(Collider2D collider)
    {
        // Skip if this is a weapon collider and we're ignoring them
        if (ignoreWeaponColliders && collider.gameObject.CompareTag("Player Attack"))
            return false;

        // Check if it's either the main player collider or on player layer
        return collider.CompareTag(playerTag) ||
               ((1 << collider.gameObject.layer) & playerLayer) != 0;
    }

    // Public method to manually set the main player collider
    public void SetMainPlayerCollider(Collider2D collider)
    {
        playerMainCollider = collider;
    }
}