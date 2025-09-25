using UnityEngine;

public class ClawSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject clawProjectilePrefab;
    public Transform spawnPoint;
    public float projectileSpeed = 10f;

    [Header("Direction Settings")]
    public bool shootRight = true;
    public bool shootLeft = false;

    [Header("Animation Event Settings")]
    public bool useAnimationEvents = true;

    private void Start()
    {
    }

    // This method can be called by animation events
    public void SpawnClawProjectile()
    {
        if (clawProjectilePrefab == null)
        {
            Debug.LogWarning("Claw projectile prefab not assigned!");
            return;
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Spawn right projectile if enabled
        if (shootRight)
        {
            SpawnSingleProjectile(Vector2.right, false); // Don't flip for right
        }

        // Spawn left projectile if enabled
        if (shootLeft)
        {
            SpawnSingleProjectile(Vector2.left, true); // Flip for left
        }
    }

    private void SpawnSingleProjectile(Vector2 direction, bool flipSprite)
    {
        GameObject claw = Instantiate(clawProjectilePrefab, spawnPoint.position, Quaternion.identity);
        ClawProjectile clawScript = claw.GetComponent<ClawProjectile>();

        if (clawScript != null)
        {
            clawScript.SetDirection(direction);
            clawScript.SetSpeed(projectileSpeed);
        }
        else
        {
            Debug.LogWarning("Spawned claw doesn't have ClawProjectile component!");
        }

        // Flip the sprite if shooting left
        if (flipSprite)
        {
            SpriteRenderer spriteRenderer = claw.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                // Try to find SpriteRenderer in children if not on root object
                spriteRenderer = claw.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
    }

    // Public methods to change direction at runtime
    public void SetShootRight(bool shouldShootRight)
    {
        shootRight = shouldShootRight;
    }

    public void SetShootLeft(bool shouldShootLeft)
    {
        shootLeft = shouldShootLeft;
    }

    public void SetShootDirection(bool shootRight, bool shootLeft)
    {
        this.shootRight = shootRight;
        this.shootLeft = shootLeft;
    }

    // Method to toggle between single directions
    public void SetSingleDirection(Vector2 direction)
    {
        shootRight = direction.x > 0;
        shootLeft = direction.x < 0;
    }

    // Method for animation events to set specific directions
    public void SetDirectionFromAnimation(int directionInt)
    {
        switch (directionInt)
        {
            case 0: // Right only
                SetShootDirection(true, false);
                break;
            case 1: // Left only
                SetShootDirection(false, true);
                break;
            case 2: // Both directions
                SetShootDirection(true, true);
                break;
            default:
                SetShootDirection(true, false);
                break;
        }
    }
}