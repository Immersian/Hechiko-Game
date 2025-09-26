using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyRespawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public bool respawnOnCheckpoint = true; // Changed from respawnOnPlayerDeath
    public float respawnDelay = 2f;
    public int poolSize = 1; // Fixed to 1 as requested

    [Header("References")]
    public Transform pointA;
    public Transform pointB;
    public EnemyDetectionZone detectionZone; // Drag the Detection Zone object here

    [Header("Debug")]
    public bool debugMode = true;

    private Queue<GameObject> enemyPool;
    private GameObject currentEnemyInstance;
    private Transform currentEnemyTransform;
    private bool isPoolInitialized = false;
    private bool shouldRespawnOnNextEvent = false; // New flag to control respawning

    void Start()
    {
        InitializePool();
        SpawnEnemy();

        // Subscribe to checkpoint respawn event instead of player death
        if (respawnOnCheckpoint)
        {
            PlayerHealth.OnPlayerRespawn += OnCheckpointRespawn;
        }
    }

    void OnDestroy()
    {
        if (respawnOnCheckpoint)
        {
            PlayerHealth.OnPlayerRespawn -= OnCheckpointRespawn;
        }

        ClearPool();
    }

    private void InitializePool()
    {
        if (isPoolInitialized) return;

        enemyPool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledEnemy();
        }

        isPoolInitialized = true;
        if (debugMode) Debug.Log($"Initialized enemy pool with {poolSize} enemies");
    }

    private void ClearPool()
    {
        if (enemyPool == null) return;

        while (enemyPool.Count > 0)
        {
            GameObject enemy = enemyPool.Dequeue();
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        enemyPool.Clear();
        isPoolInitialized = false;
    }

    private GameObject CreatePooledEnemy()
    {
        if (enemyPrefab == null || spawnPoint == null) return null;

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.transform.SetParent(transform);

        // Set up the enemy damage handler with pool reference
        EnemyDamageHandler damageHandler = enemy.GetComponent<EnemyDamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.SetRespawnManager(this);
        }

        enemy.SetActive(false);
        enemyPool.Enqueue(enemy);

        return enemy;
    }

    private GameObject GetPooledEnemy()
    {
        // Try to find an available enemy in the pool
        foreach (GameObject enemy in enemyPool)
        {
            if (!enemy.activeInHierarchy)
            {
                return enemy;
            }
        }

        // If no available enemy found, create a new one and add to pool
        if (debugMode) Debug.Log("Pool exhausted, creating new enemy instance");
        return CreatePooledEnemy();
    }

    private void ReturnEnemyToPool(GameObject enemy)
    {
        if (enemy == null) return;

        // Reset enemy state
        ResetEnemyState(enemy);
        enemy.SetActive(false);

        // Only add back to pool if it's not already there
        if (!enemyPool.Contains(enemy))
        {
            enemyPool.Enqueue(enemy);
        }
    }

    private void ResetEnemyState(GameObject enemy)
    {
        if (enemy == null) return;

        // Reset position and rotation
        enemy.transform.position = spawnPoint.position;
        enemy.transform.rotation = spawnPoint.rotation;

        // Reset health
        EnemyDamageHandler damageHandler = enemy.GetComponent<EnemyDamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.ResetEnemy();
        }

        // Reset rigidbody
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        // Re-enable colliders
        Collider2D[] colliders = enemy.GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }

        // Re-enable movement
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.enabled = true;
        }
    }

    private void SpawnEnemy()
    {
        if (currentEnemyInstance != null && currentEnemyInstance.activeInHierarchy)
        {
            // Return current enemy to pool if it's still active
            ReturnEnemyToPool(currentEnemyInstance);
        }

        GameObject enemy = GetPooledEnemy();
        if (enemy != null)
        {
            enemy.SetActive(true);
            currentEnemyInstance = enemy;
            currentEnemyTransform = enemy.transform;

            // Set the detection zone reference in the enemy's movement script
            SetEnemyDetectionReference(currentEnemyInstance);

            // Assign patrol points
            AssignPatrolPoints(currentEnemyInstance);

            // Tell the detection zone to follow this specific enemy instance
            if (detectionZone != null)
            {
                detectionZone.SetTargetEnemy(currentEnemyTransform);
                if (debugMode) Debug.Log($"Detection zone set to follow: {currentEnemyInstance.name}");
            }

            if (debugMode) Debug.Log($"Spawned enemy from pool at {spawnPoint.position}");
        }
    }

    private void SetEnemyDetectionReference(GameObject enemyInstance)
    {
        EnemyMovement enemyMovement = enemyInstance.GetComponent<EnemyMovement>();
        if (enemyMovement != null && detectionZone != null)
        {
            enemyMovement.detectionZone = detectionZone;
            if (debugMode) Debug.Log($"Set detection zone reference in {enemyInstance.name}");
        }
    }

    private void AssignPatrolPoints(GameObject enemyInstance)
    {
        EnemyMovement enemyMovement = enemyInstance.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.pointA = pointA;
            enemyMovement.pointB = pointB;
        }
    }

    // Changed from OnPlayerRespawn to OnCheckpointRespawn
    private void OnCheckpointRespawn()
    {
        if (debugMode) Debug.Log("Checkpoint respawn event received");

        // Only respawn if we have the flag set or if there's no active enemy
        if (respawnOnCheckpoint && shouldRespawnOnNextEvent)
        {
            if (debugMode) Debug.Log("Respawning enemy due to checkpoint interaction");
            StartCoroutine(RespawnEnemyWithDelay());
            shouldRespawnOnNextEvent = false; // Reset the flag
        }
        else if (currentEnemyInstance == null || !currentEnemyInstance.activeInHierarchy || IsEnemyDead())
        {
            if (debugMode) Debug.Log("Respawning enemy because none is active");
            StartCoroutine(RespawnEnemyWithDelay());
        }
    }

    private IEnumerator RespawnEnemyWithDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnEnemy();
    }

    private bool IsEnemyDead()
    {
        if (currentEnemyInstance == null || !currentEnemyInstance.activeInHierarchy) return true;
        EnemyDamageHandler damageHandler = currentEnemyInstance.GetComponent<EnemyDamageHandler>();
        return damageHandler != null && damageHandler.isDead;
    }

    public void OnEnemyDeath(GameObject deadEnemy)
    {
        if (debugMode) Debug.Log("Enemy death detected by spawn manager");

        // Clear the target when enemy dies
        if (detectionZone != null)
        {
            detectionZone.ClearTargetEnemy();
        }

        // If the dead enemy is our current instance, mark it as dead
        if (deadEnemy == currentEnemyInstance)
        {
            currentEnemyInstance = null;
            currentEnemyTransform = null;

            // DON'T auto-respawn here - wait for checkpoint interaction
            // Just set the flag so we respawn on next checkpoint interaction
            shouldRespawnOnNextEvent = true;
            if (debugMode) Debug.Log("Enemy died - will respawn on next checkpoint interaction");
        }

        // Return the dead enemy to pool after a delay
        StartCoroutine(ReturnToPoolAfterDelay(deadEnemy));
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject enemy)
    {
        yield return new WaitForSeconds(2.5f); // Small delay before returning to pool
        ReturnEnemyToPool(enemy);
    }

    void Update()
    {
        // Keep the detection zone updated with the current enemy position
        if (detectionZone != null && currentEnemyTransform != null && currentEnemyInstance != null && currentEnemyInstance.activeInHierarchy)
        {
            detectionZone.SetTargetEnemy(currentEnemyTransform);
        }
    }

    public int GetActiveEnemyCount()
    {
        int count = 0;
        if (enemyPool == null) return count;

        foreach (GameObject enemy in enemyPool)
        {
            if (enemy.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    public int GetPoolSize()
    {
        return enemyPool != null ? enemyPool.Count : 0;
    }

    // Public method to force respawn (can be called from checkpoint if needed)
    public void ForceRespawn()
    {
        if (debugMode) Debug.Log("Forcing enemy respawn");
        StartCoroutine(RespawnEnemyWithDelay());
    }
}