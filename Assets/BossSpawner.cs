using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public Transform spawnPoint;
    
    [Header("Respawn Settings")]
    public bool respawnBossOnPlayerRespawn = true;
    
    private GameObject currentBossInstance;

    void Start()
    {
        // Subscribe to respawn event
        PlayerHealth.OnPlayerRespawn += OnPlayerRespawn;
        
        // Spawn boss at start
        SpawnBoss();
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        PlayerHealth.OnPlayerRespawn -= OnPlayerRespawn;
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("Boss prefab not assigned in BossSpawner!");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // Instantiate boss
        currentBossInstance = Instantiate(bossPrefab, spawnPosition, 
            spawnPoint != null ? spawnPoint.rotation : Quaternion.identity);
        
        Debug.Log("Boss spawned at: " + spawnPosition);
    }

    private void OnPlayerRespawn()
    {
        if (respawnBossOnPlayerRespawn)
        {
            ResetBoss();
        }
    }

    public void ResetBoss()
    {
        // Destroy current boss if it exists
        if (currentBossInstance != null)
        {
            Destroy(currentBossInstance);
            currentBossInstance = null;
        }
        
        // Spawn a fresh boss
        SpawnBoss();
        
        Debug.Log("Boss reset - fresh instance spawned");
    }

    // For manual control if needed
    [ContextMenu("Test Boss Reset")]
    private void TestReset()
    {
        ResetBoss();
    }

    // Public method to destroy boss without respawning (for when boss is defeated)
    public void DestroyBossPermanently()
    {
        if (currentBossInstance != null)
        {
            Destroy(currentBossInstance);
            currentBossInstance = null;
        }
        
        // Unsubscribe from respawn events so boss doesn't come back
        PlayerHealth.OnPlayerRespawn -= OnPlayerRespawn;
    }
}