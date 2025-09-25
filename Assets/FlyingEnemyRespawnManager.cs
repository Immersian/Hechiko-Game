using UnityEngine;
using System.Collections;

public class FlyingEnemyRespawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject flyingEnemyPrefab;
    public Transform spawnPoint;
    public bool respawnOnPlayerDeath = true;
    public float respawnDelay = 2f;

    [Header("Debug")]
    public bool debugMode = true;

    private GameObject currentFlyingEnemyInstance;

    void Start()
    {
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
    }

    private void SpawnFlyingEnemy()
    {
        if (currentFlyingEnemyInstance != null)
        {
            Destroy(currentFlyingEnemyInstance);
        }

        if (flyingEnemyPrefab != null && spawnPoint != null)
        {
            currentFlyingEnemyInstance = Instantiate(flyingEnemyPrefab, spawnPoint.position, spawnPoint.rotation);
            currentFlyingEnemyInstance.transform.SetParent(transform);

            if (debugMode) Debug.Log($"Spawned flying enemy at {spawnPoint.position}");
        }
    }

    private void OnPlayerRespawn()
    {
        if (respawnOnPlayerDeath && (currentFlyingEnemyInstance == null || IsFlyingEnemyDead()))
        {
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
        if (currentFlyingEnemyInstance == null) return true;

        // Adjust this component name based on your flying enemy's damage handler
        EnemyDamageHandler damageHandler = currentFlyingEnemyInstance.GetComponent<EnemyDamageHandler>();

        // Check either component based on what your flying enemy uses
        if (damageHandler != null)
            return damageHandler.isDead;
        return false;
    }

    public void OnFlyingEnemyDeath()
    {
        if (debugMode) Debug.Log("Flying enemy death detected by spawn manager");

        // Optional: Add any flying enemy specific cleanup here
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
        return currentFlyingEnemyInstance != null && !IsFlyingEnemyDead();
    }
}