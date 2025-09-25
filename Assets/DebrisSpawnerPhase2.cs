using UnityEngine;

public class DebrisSpawnerPhase2 : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject debrisPrefab; // Keep the prefab reference
    [SerializeField] private float spawnInterval = 0.5f;

    [Header("Row 1 Settings")]
    [SerializeField] private int row1Columns = 7;
    [SerializeField] private float row1Spacing = 1.5f;
    [SerializeField] private float row1YOffset = 0f;
    [SerializeField] private float row1XOffset = 0f;
    [SerializeField] private int row1SpawnCycles = 5;
    [SerializeField] private Color row1Color = Color.green;

    [Header("Row 2 Settings")]
    [SerializeField] private int row2Columns = 7;
    [SerializeField] private float row2Spacing = 1.5f;
    [SerializeField] private float row2YOffset = 1f;
    [SerializeField] private float row2XOffset = 0f;
    [SerializeField] private int row2SpawnCycles = 3;
    [SerializeField] private Color row2Color = Color.blue;

    [Header("Gizmos Settings")]
    [SerializeField] private bool alwaysShowGizmos = true;

    private float timer;
    private int currentCycle;
    private int currentRow;
    private bool isActive;
    private int row1SpawnCount;
    private int row2SpawnCount;

    void OnEnable()
    {
        ResetSpawner();
    }

    void ResetSpawner()
    {
        timer = 0f;
        currentCycle = 0;
        currentRow = 0;
        row1SpawnCount = 0;
        row2SpawnCount = 0;
        isActive = true;
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnCurrentRow();
            timer = 0f;

            currentRow++;
            if (currentRow >= 2)
            {
                currentRow = 0;
                currentCycle++;

                if (row1SpawnCount >= row1SpawnCycles && row2SpawnCount >= row2SpawnCycles)
                {
                    isActive = false;
                    enabled = false;
                    Debug.Log("Phase 2 spawner completed all cycles");
                }
            }
        }
    }

    void SpawnCurrentRow()
    {
        if (debrisPrefab == null)
        {
            Debug.LogError("Debris prefab not assigned!");
            return;
        }

        if ((currentRow == 0 && row1SpawnCount >= row1SpawnCycles) ||
            (currentRow == 1 && row2SpawnCount >= row2SpawnCycles))
        {
            return;
        }

        int columns = currentRow == 0 ? row1Columns : row2Columns;
        float spacing = currentRow == 0 ? row1Spacing : row2Spacing;
        float xOffset = currentRow == 0 ? row1XOffset : row2XOffset;
        float yOffset = currentRow == 0 ? row1YOffset : row2YOffset;

        // Calculate the center position for this row
        Vector3 rowCenter = transform.position + new Vector3(xOffset, yOffset, 0);

        // Calculate starting position (leftmost point of the row)
        float startX = rowCenter.x - ((columns - 1) * spacing) / 2f;

        for (int col = 0; col < columns; col++)
        {
            // Get debris from pool using the prefab name
            GameObject debris = ObjectPool.SharedInstance.GetPooledObject(debrisPrefab.name);
            if (debris == null)
            {
                Debug.LogWarning($"No available debris of type '{debrisPrefab.name}' in pool!");
                continue;
            }

            Vector3 spawnPos = new Vector3(
                startX + col * spacing,
                rowCenter.y,
                rowCenter.z
            );

            // Set up the debris from pool
            debris.transform.position = spawnPos;
            debris.transform.rotation = Quaternion.identity;
            debris.SetActive(true);
        }

        if (currentRow == 0)
        {
            row1SpawnCount++;
        }
        else
        {
            row2SpawnCount++;
        }
    }

    void OnDrawGizmos()
    {
        // Always draw gizmos regardless of whether the script is enabled
        DrawSpawnGizmos();
    }

    void OnDrawGizmosSelected()
    {
        // Also draw when selected for better visibility
        DrawSpawnGizmos();
    }

    private void DrawSpawnGizmos()
    {
        for (int row = 0; row < 2; row++)
        {
            int columns = row == 0 ? row1Columns : row2Columns;
            float spacing = row == 0 ? row1Spacing : row2Spacing;
            float xOffset = row == 0 ? row1XOffset : row2XOffset;
            float yOffset = row == 0 ? row1YOffset : row2YOffset;

            Gizmos.color = row == 0 ? row1Color : row2Color;

            // Calculate the center position for this row
            Vector3 rowCenter = transform.position + new Vector3(xOffset, yOffset, 0);

            // Calculate starting position (leftmost point of the row)
            float startX = rowCenter.x - ((columns - 1) * spacing) / 2f;

            // Draw the row line
            Vector3 rowStart = new Vector3(startX, rowCenter.y, rowCenter.z);
            Vector3 rowEnd = new Vector3(startX + (columns - 1) * spacing, rowCenter.y, rowCenter.z);
            Gizmos.DrawLine(rowStart, rowEnd);

            for (int col = 0; col < columns; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * spacing,
                    rowCenter.y,
                    rowCenter.z
                );

                // Draw a sphere at each spawn position
                Gizmos.DrawWireSphere(pos, 0.3f);

                // Draw a small cross to make it more visible
                Gizmos.DrawLine(pos - Vector3.right * 0.1f, pos + Vector3.right * 0.1f);
                Gizmos.DrawLine(pos - Vector3.up * 0.1f, pos + Vector3.up * 0.1f);
            }

            // Draw the row center point
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(rowCenter, new Vector3(0.3f, 0.3f, 0.3f));
        }

        // Draw the main transform center indicator
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
    }

    public void RestartSpawning()
    {
        ResetSpawner();
        enabled = true;
    }
}