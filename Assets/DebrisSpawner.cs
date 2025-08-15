using UnityEngine;

public class DebrisSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int spawnCount = 5;
    [SerializeField] private float spawnWidth = 10f;
    [SerializeField] private float yOffset = 0f;

    [Header("Behavior")]
    [SerializeField] private bool disableAfterComplete = true;

    private float spawnTimer;
    private bool movingRight = true; // Current direction
    private int currentSpawnIndex;
    private int patternsCompleted; // Track completed patterns

    void OnEnable()
    {
        spawnTimer = spawnInterval;
        currentSpawnIndex = movingRight ? 0 : spawnCount - 1;
        patternsCompleted = 0;
    }

    void Update()
    {
        if (spawnTimer > 0)
        {
            spawnTimer -= Time.deltaTime;
        }
        else
        {
            SpawnDebris();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnDebris()
    {
        if (debrisPrefab == null) return;

        // Calculate normalized position in pattern (0 to 1)
        float t = (float)currentSpawnIndex / (spawnCount - 1);

        // Calculate x position based on direction
        float xOffset;
        if (movingRight)
        {
            // Left to right movement
            xOffset = Mathf.Lerp(-spawnWidth / 2, spawnWidth / 2, t);
        }
        else
        {
            // Right to left movement
            xOffset = Mathf.Lerp(spawnWidth / -2, -spawnWidth / -2, t);
        }

        Vector3 spawnPos = transform.position + new Vector3(xOffset, yOffset, 0f);
        Instantiate(debrisPrefab, spawnPos, Quaternion.identity);

        // Update position in pattern
        currentSpawnIndex += movingRight ? 1 : -1;

        // Check if pattern complete
        if ((movingRight && currentSpawnIndex >= spawnCount) ||
            (!movingRight && currentSpawnIndex < 0))
        {
            patternsCompleted++;

            if (patternsCompleted < 2) // Only do two patterns (right then left)
            {
                // Switch direction
                movingRight = !movingRight;
                currentSpawnIndex = movingRight ? 0 : spawnCount - 1;
            }
            else if (disableAfterComplete)
            {
                this.enabled = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + new Vector3(0, yOffset, 0);
        Vector3 leftPos = center + new Vector3(-spawnWidth / 2, 0, 0);
        Vector3 rightPos = center + new Vector3(spawnWidth / 2, 0, 0);

        Gizmos.DrawLine(leftPos, rightPos);
        Gizmos.DrawSphere(leftPos, 0.2f);
        Gizmos.DrawSphere(rightPos, 0.2f);
        Gizmos.DrawSphere(center, 0.1f);
    }
}