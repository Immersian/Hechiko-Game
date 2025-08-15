using UnityEngine;

public class BossProjectileSplit : MonoBehaviour
{
    [Header("Projectile Prefabs")]
    public GameObject leftProjectilePrefab;  // Assign your left-moving prefab
    public GameObject rightProjectilePrefab; // Assign your right-moving prefab
    public Vector2 spawnOffset = new Vector2(0.5f, 0f); // Offset from center

    [Header("Effects")]
    public GameObject destroyEffect;

    private bool hitPlayer = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            hitPlayer = true;
        }
    }

    private void OnDestroy()
    {
        // Only spawn if this is a real destruction (not scene unload) and didn't hit player
        if (!gameObject.scene.isLoaded || hitPlayer) return;

        SpawnSplitProjectiles();
    }

    private void SpawnSplitProjectiles()
    {
        if (leftProjectilePrefab != null)
        {
            Vector2 leftSpawnPos = (Vector2)transform.position + new Vector2(-spawnOffset.x, spawnOffset.y);
            Instantiate(leftProjectilePrefab, leftSpawnPos, Quaternion.identity);
        }

        if (rightProjectilePrefab != null)
        {
            Vector2 rightSpawnPos = (Vector2)transform.position + new Vector2(spawnOffset.x, spawnOffset.y);
            Instantiate(rightProjectilePrefab, rightSpawnPos, Quaternion.identity);
        }

        if (destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        // Show spawn positions
        Gizmos.DrawWireSphere(transform.position + new Vector3(-spawnOffset.x, spawnOffset.y, 0), 0.2f);
        Gizmos.DrawWireSphere(transform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0), 0.2f);
    }
}