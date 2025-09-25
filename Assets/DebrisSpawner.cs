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
    private bool movingRight = true;
    private int currentSpawnIndex;
    private int patternsCompleted;

    void OnEnable()
    {
        spawnTimer = 0f; // Start spawning immediately
        movingRight = true;
        currentSpawnIndex = 0;
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

        // Get debris from pool
        GameObject debris = ObjectPool.SharedInstance.GetPooledObject();
        if (debris == null)
        {
            Debug.LogWarning("No available debris in pool!");
            return;
        }

        // Calculate spawn position based on current pattern
        float t = (float)currentSpawnIndex / (spawnCount - 1);
        float xOffset;

        if (movingRight)
        {
            // Left to right: index 0 = left, index spawnCount-1 = right
            xOffset = Mathf.Lerp(-spawnWidth / 2, spawnWidth / 2, t);
        }
        else
        {
            // Right to left: index 0 = right, index spawnCount-1 = left
            xOffset = Mathf.Lerp(spawnWidth / 2, -spawnWidth / 2, t);
        }

        Vector3 spawnPos = transform.position + new Vector3(xOffset, yOffset, 0f);

        // Set up the debris
        debris.transform.position = spawnPos;
        debris.transform.rotation = Quaternion.identity;
        debris.SetActive(true);

        // Debug log to see the pattern
        Debug.Log($"Spawned debris at index {currentSpawnIndex}, position: {spawnPos}, direction: {(movingRight ? "Right" : "Left")}");

        // Update position in pattern
        if (movingRight)
        {
            currentSpawnIndex++;
            // Check if we've completed right-moving pattern
            if (currentSpawnIndex >= spawnCount)
            {
                patternsCompleted++;
                if (patternsCompleted < 2)
                {
                    // Switch to left-moving pattern
                    movingRight = false;
                    currentSpawnIndex = 0; // Start from right side
                }
                else if (disableAfterComplete)
                {
                    this.enabled = false;
                    return;
                }
            }
        }
        else
        {
            currentSpawnIndex++;
            // Check if we've completed left-moving pattern
            if (currentSpawnIndex >= spawnCount)
            {
                patternsCompleted++;
                if (patternsCompleted < 2)
                {
                    // Switch back to right-moving pattern (if you want more than 2 patterns)
                    movingRight = true;
                    currentSpawnIndex = 0; // Start from left side
                }
                else if (disableAfterComplete)
                {
                    this.enabled = false;
                    return;
                }
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

        // Draw spawn points for current pattern
        Gizmos.color = Color.yellow;
        if (Application.isPlaying)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                float t = (float)i / (spawnCount - 1);
                float xOffset;

                if (movingRight)
                {
                    xOffset = Mathf.Lerp(-spawnWidth / 2, spawnWidth / 2, t);
                }
                else
                {
                    xOffset = Mathf.Lerp(spawnWidth / 2, -spawnWidth / 2, t);
                }

                Vector3 spawnPoint = center + new Vector3(xOffset, 0, 0);
                Gizmos.DrawWireCube(spawnPoint, new Vector3(0.3f, 0.3f, 0.3f));
            }
        }
    }
}