using UnityEngine;
using System.Collections;

public class EnemyRespawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public bool respawnOnPlayerDeath = true;
    public float respawnDelay = 2f;

    [Header("References")]
    public Transform pointA;
    public Transform pointB;
    public EnemyDetectionZone detectionZone; // Drag the Detection Zone object here

    [Header("Debug")]
    public bool debugMode = true;

    private GameObject currentEnemyInstance;
    private Transform currentEnemyTransform;

    void Start()
    {
        SpawnEnemy();

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

    private void SpawnEnemy()
    {
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance);
        }

        if (enemyPrefab != null && spawnPoint != null)
        {
            currentEnemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            currentEnemyInstance.transform.SetParent(transform);
            currentEnemyTransform = currentEnemyInstance.transform;

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

            if (debugMode) Debug.Log($"Spawned enemy at {spawnPoint.position}");
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

    private void OnPlayerRespawn()
    {
        if (respawnOnPlayerDeath && (currentEnemyInstance == null || IsEnemyDead()))
        {
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
        if (currentEnemyInstance == null) return true;
        EnemyDamageHandler damageHandler = currentEnemyInstance.GetComponent<EnemyDamageHandler>();
        return damageHandler != null && damageHandler.isDead;
    }

    public void OnEnemyDeath()
    {
        if (debugMode) Debug.Log("Enemy death detected by spawn manager");

        // Clear the target when enemy dies
        if (detectionZone != null)
        {
            detectionZone.ClearTargetEnemy();
        }
    }

    void Update()
    {
        // Keep the detection zone updated with the current enemy position
        if (detectionZone != null && currentEnemyTransform != null)
        {
            detectionZone.SetTargetEnemy(currentEnemyTransform);
        }
    }
}