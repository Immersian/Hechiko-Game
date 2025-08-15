using UnityEngine;

public class DebrisSpawnerPhase2 : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private float spawnInterval = 0.5f; // Time between row spawns
    [SerializeField] private int columns = 7;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float rowSeparation = 1f; // Vertical space between rows
    [SerializeField] private int greenSpawnCycles = 5; // Green row spawn count
    [SerializeField] private int blueSpawnCycles = 3;  // Blue row spawn count

    [Header("Gizmos Colors")]
    [SerializeField] private Color row1Color = Color.green;
    [SerializeField] private Color row2Color = Color.blue;

    private float timer;
    private int currentCycle;
    private int currentRow; // 0 or 1
    private bool isActive;
    private int greenSpawnCount;
    private int blueSpawnCount;

    void OnEnable()
    {
        ResetSpawner();
    }

    void ResetSpawner()
    {
        timer = 0f;
        currentCycle = 0;
        currentRow = 0;
        greenSpawnCount = 0;
        blueSpawnCount = 0;
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

            // Move to next row or cycle
            currentRow++;
            if (currentRow >= 2)
            {
                currentRow = 0;
                currentCycle++;

                // Check if both rows have completed their cycles
                if (greenSpawnCount >= greenSpawnCycles && blueSpawnCount >= blueSpawnCycles)
                {
                    isActive = false;
                    enabled = false;
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

        // Skip if this row has completed its cycles
        if ((currentRow == 0 && greenSpawnCount >= greenSpawnCycles) ||
            (currentRow == 1 && blueSpawnCount >= blueSpawnCycles))
        {
            return;
        }

        float startX = -((columns - 1) * spacing) / 2f + xOffset;
        float rowY = transform.position.y + yOffset + (currentRow * rowSeparation);

        for (int col = 0; col < columns; col++)
        {
            if ((col + currentRow) % 2 == 0) // Optional pattern
            {
                Vector3 spawnPos = new Vector3(
                    startX + col * spacing,
                    rowY,
                    transform.position.z
                );
                Instantiate(debrisPrefab, spawnPos, Quaternion.identity);
            }
        }

        // Increment the appropriate counter
        if (currentRow == 0)
        {
            greenSpawnCount++;
        }
        else
        {
            blueSpawnCount++;
        }
    }

    void OnDrawGizmos()
    {
        float startX = -((columns - 1) * spacing) / 2f + xOffset;

        for (int row = 0; row < 2; row++)
        {
            Gizmos.color = row == 0 ? row1Color : row2Color;
            float rowY = transform.position.y + yOffset + (row * rowSeparation);

            for (int col = 0; col < columns; col++)
            {
                if ((col + row) % 2 == 0)
                {
                    Vector3 pos = new Vector3(
                        startX + col * spacing,
                        rowY,
                        transform.position.z
                    );
                    Gizmos.DrawWireSphere(pos, 0.3f);
                }
            }
        }
    }

    public void RestartSpawning()
    {
        ResetSpawner();
        enabled = true;
    }
}