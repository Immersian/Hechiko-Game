using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyDetectionZone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Transform enemyTransform;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private bool followEnemy = true;
    [SerializeField] private bool requireLineOfSight = true; // New flag

    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer; // New layer mask
    public bool playerInZone { get; private set; }

    private IEnemyController enemyInterface;
    private Transform playerTransform;

    public interface IEnemyController
    {
        void OnPlayerDetected();
        void OnPlayerLost();
        bool HasLineOfSightToPlayer(); // New method
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
        if (!IsPlayer(other)) return;

        playerInZone = true;
        playerTransform = other.transform;

        if (!requireLineOfSight || (enemyInterface != null && enemyInterface.HasLineOfSightToPlayer()))
        {
            enemyInterface?.OnPlayerDetected();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        // Continuous check if we require line of sight
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
        if (!IsPlayer(other)) return;

        playerInZone = false;
        enemyInterface?.OnPlayerLost();
    }

    private bool IsPlayer(Collider2D collider)
    {
        return ((1 << collider.gameObject.layer) & playerLayer) != 0;
    }
}