using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyDetectionZone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private bool followEnemy = true;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Collider Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool ignoreWeaponColliders = true;

    [Header("Alert Settings")]
    [SerializeField] private float normalRadius = 13.92f;
    [SerializeField] private float alertRadius = 20f;
    private CircleCollider2D zoneCollider;

    public bool playerInZone { get; private set; }

    private Transform targetEnemy; // The specific enemy instance to follow
    private IEnemyController enemyInterface;
    private Transform playerTransform;
    private Collider2D playerMainCollider;

    public interface IEnemyController
    {
        void OnPlayerDetected();
        void OnPlayerLost();
        bool HasLineOfSightToPlayer();
    }

    private void Start()
    {
        zoneCollider = GetComponent<CircleCollider2D>();
        if (zoneCollider == null)
        {
            Debug.LogError("EnemyDetectionZone requires a CircleCollider2D!", this);
            return;
        }

        zoneCollider.isTrigger = true;
        zoneCollider.radius = normalRadius; // Set initial radius
    }

    // Public method for spawn manager to set which enemy to follow
    public void SetTargetEnemy(Transform enemyTransform)
    {
        if (enemyTransform != targetEnemy)
        {
            targetEnemy = enemyTransform;

            if (targetEnemy != null)
            {
                enemyInterface = targetEnemy.GetComponent<IEnemyController>();
                if (enemyInterface == null)
                {
                    Debug.LogError($"Enemy {targetEnemy.name} doesn't implement IEnemyController!", this);
                }
                else
                {
                    Debug.Log($"Detection zone now following: {targetEnemy.name}");
                }
            }
        }
    }

    public void ClearTargetEnemy()
    {
        targetEnemy = null;
        enemyInterface = null;
        playerInZone = false;
    }

    private void Update()
    {
        if (followEnemy && targetEnemy != null)
        {
            // Follow the specific enemy instance set by the spawn manager
            transform.position = targetEnemy.position + positionOffset;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyInterface == null) return;
        if (!IsValidPlayerCollider(other)) return;

        if (playerMainCollider == null && other.CompareTag(playerTag))
        {
            playerMainCollider = other;
        }

        playerInZone = true;
        playerTransform = other.transform;

        if (!requireLineOfSight || enemyInterface.HasLineOfSightToPlayer())
        {
            enemyInterface.OnPlayerDetected();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (enemyInterface == null) return;
        if (!IsValidPlayerCollider(other)) return;

        if (other != playerMainCollider) return;

        if (requireLineOfSight && playerInZone)
        {
            if (enemyInterface.HasLineOfSightToPlayer())
            {
                enemyInterface.OnPlayerDetected();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (enemyInterface == null) return;
        if (!IsValidPlayerCollider(other)) return;

        if (other != playerMainCollider) return;

        playerInZone = false;
        enemyInterface.OnPlayerLost();
    }

    private bool IsValidPlayerCollider(Collider2D collider)
    {
        if (ignoreWeaponColliders && collider.gameObject.CompareTag("Player Attack"))
            return false;

        return collider.CompareTag(playerTag) || ((1 << collider.gameObject.layer) & playerLayer) != 0;
    }
    public void SetAlertRadius()
    {
        if (zoneCollider != null)
        {
            zoneCollider.radius = alertRadius;
        }
    }

    public void SetNormalRadius()
    {
        if (zoneCollider != null)
        {
            zoneCollider.radius = normalRadius;
        }
    }

    // Visual debug
    private void OnDrawGizmos()
    {
        if (targetEnemy != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetEnemy.position);
            Gizmos.DrawWireSphere(targetEnemy.position, 0.3f);
        }
    }
}