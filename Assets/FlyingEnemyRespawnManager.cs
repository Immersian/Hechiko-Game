using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlyingEnemyRespawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject flyingEnemyPrefab;
    public Transform spawnPoint;
    public bool respawnOnPlayerDeath = true;
    public float respawnDelay = 2f;
    public int poolSize = 1; // Added for object pooling

    [Header("Debug")]
    public bool debugMode = true;

    private Queue<GameObject> enemyPool;
    private GameObject currentFlyingEnemyInstance;
    private bool isPoolInitialized = false;
    private bool shouldRespawnOnNextEvent = false;

    void Start()
    {
        InitializePool();
        SpawnFlyingEnemy();

        if (respawnOnPlayerDeath)
        {
            PlayerHealth.OnPlayerRespawn += OnPlayerRespawn;
        }
    }

    void OnDestroy()
    {
        if (respawnOnPlayerDeath)
        {
            PlayerHealth.OnPlayerRespawn -= OnPlayerRespawn;
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
        if (debugMode) Debug.Log($"Initialized flying enemy pool with {poolSize} enemies");
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
        if (flyingEnemyPrefab == null || spawnPoint == null) return null;

        GameObject enemy = Instantiate(flyingEnemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.transform.SetParent(transform);

        // Set up the enemy damage handler with pool reference
        EnemyDamageHandler damageHandler = enemy.GetComponent<EnemyDamageHandler>();
        if (damageHandler != null)
        {
            // Create a wrapper since we don't have the exact same method
            // We'll handle the respawn manager reference differently
            FlyingEnemyRespawnWrapper wrapper = enemy.GetComponent<FlyingEnemyRespawnWrapper>();
            if (wrapper == null)
            {
                wrapper = enemy.AddComponent<FlyingEnemyRespawnWrapper>();
            }
            wrapper.SetRespawnManager(this);
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
        if (debugMode) Debug.Log("Pool exhausted, creating new flying enemy instance");
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

        // Reset health and stun state - FIXED VERSION
        EnemyDamageHandler damageHandler = enemy.GetComponent<EnemyDamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.ResetEnemy(); // This should reset health to maxHealth
        }

        // Reset aerial enemy specific components
        AerialEnemy aerialEnemy = enemy.GetComponent<AerialEnemy>();
        if (aerialEnemy != null)
        {
            // We need to reset the stun state - add a public method to AerialEnemy for this
            // For now, we'll rely on the damage handler reset
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
    }
    private void SpawnFlyingEnemy()
    {
        if (currentFlyingEnemyInstance != null && currentFlyingEnemyInstance.activeInHierarchy)
        {
            // Return current enemy to pool if it's still active
            ReturnEnemyToPool(currentFlyingEnemyInstance);
        }

        GameObject enemy = GetPooledEnemy();
        if (enemy != null)
        {
            enemy.SetActive(true);
            currentFlyingEnemyInstance = enemy;

            if (debugMode) Debug.Log($"Spawned flying enemy from pool at {spawnPoint.position}");
        }
    }

    private void OnPlayerRespawn()
    {
        if (respawnOnPlayerDeath && shouldRespawnOnNextEvent)
        {
            if (debugMode) Debug.Log("Respawning flying enemy due to player respawn");
            StartCoroutine(RespawnFlyingEnemyWithDelay());
            shouldRespawnOnNextEvent = false;
        }
        else if (currentFlyingEnemyInstance == null || !currentFlyingEnemyInstance.activeInHierarchy || IsFlyingEnemyDead())
        {
            if (debugMode) Debug.Log("Respawning flying enemy because none is active");
            StartCoroutine(RespawnFlyingEnemyWithDelay());
        }
    }

    private IEnumerator RespawnFlyingEnemyWithDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnFlyingEnemy();
    }

    private bool IsFlyingEnemyDead()
    {
        if (currentFlyingEnemyInstance == null || !currentFlyingEnemyInstance.activeInHierarchy) return true;

        EnemyDamageHandler damageHandler = currentFlyingEnemyInstance.GetComponent<EnemyDamageHandler>();
        return damageHandler != null && damageHandler.isDead;
    }

    public void OnFlyingEnemyDeath(GameObject deadEnemy)
    {
        if (debugMode) Debug.Log("Flying enemy death detected by spawn manager");

        // If the dead enemy is our current instance, mark it as dead
        if (deadEnemy == currentFlyingEnemyInstance)
        {
            currentFlyingEnemyInstance = null;
            shouldRespawnOnNextEvent = true;
            if (debugMode) Debug.Log("Flying enemy died - will respawn on next player respawn");
        }

        // Return the dead enemy to pool after a delay
        StartCoroutine(ReturnToPoolAfterDelay(deadEnemy));
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject enemy)
    {
        yield return new WaitForSeconds(1.25f); // Small delay before returning to pool
        ReturnEnemyToPool(enemy);
    }

    // Public method to manually trigger respawn
    public void ManualRespawn()
    {
        if (debugMode) Debug.Log("Manual respawn triggered");
        SpawnFlyingEnemy();
    }

    // Public method to get the current flying enemy instance
    public GameObject GetCurrentFlyingEnemy()
    {
        return currentFlyingEnemyInstance;
    }

    // Public method to check if flying enemy is active
    public bool IsFlyingEnemyActive()
    {
        return currentFlyingEnemyInstance != null && currentFlyingEnemyInstance.activeInHierarchy && !IsFlyingEnemyDead();
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

    // Public method to force respawn
    public void ForceRespawn()
    {
        if (debugMode) Debug.Log("Forcing flying enemy respawn");
        StartCoroutine(RespawnFlyingEnemyWithDelay());
    }
}

// Helper component to bridge the respawn manager reference
public class FlyingEnemyRespawnWrapper : MonoBehaviour
{
    private FlyingEnemyRespawnManager respawnManager;

    public void SetRespawnManager(FlyingEnemyRespawnManager manager)
    {
        respawnManager = manager;
    }

    // This method can be called from the EnemyDamageHandler when it dies
    public void NotifyEnemyDeath()
    {
        if (respawnManager != null)
        {
            respawnManager.OnFlyingEnemyDeath(gameObject);
        }
    }
}