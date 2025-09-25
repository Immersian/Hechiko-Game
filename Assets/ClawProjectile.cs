using UnityEngine;

public class ClawProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public Vector2 direction = Vector2.right;

    [Header("Reflection Settings")]
    public LayerMask borderLayer;
    public int maxReflections = 2;

    [Header("Destruction Settings")]
    public string borderTag = "BossBorder";
    public string playerTag = "Player";
    public LayerMask clawLayer;

    private int reflectionCount = 0;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // Try to find it in children if not on root object
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Ensure the collider is set as trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Set initial velocity
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }

        // Set initial sprite flip based on direction
        UpdateSpriteFlip();
    }

    void Update()
    {
        // Maintain constant horizontal movement
        if (rb != null)
        {
            rb.velocity = new Vector2(direction.x * speed, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit border
        if (other.CompareTag(borderTag))
        {
            HandleBorderCollision(other);
            return;
        }

        // Check if hit another claw projectile using layer mask
        if ((clawLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // Check if hit player
        if (other.CompareTag(playerTag))
        {
            Destroy(gameObject);

            // You can add player damage logic here if needed
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(10); // Adjust damage as needed
            }
            return;
        }
    }

    private void HandleBorderCollision(Collider2D borderCollider)
    {
        reflectionCount++;

        // Destroy if reached max reflections
        if (reflectionCount >= maxReflections)
        {
            Destroy(gameObject);
            return;
        }

        // Reflect the projectile
        ReflectProjectile();
    }

    private void ReflectProjectile()
    {
        // Reverse horizontal direction
        direction = new Vector2(-direction.x, 0f);

        // Update velocity with new direction
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }

        // Flip the sprite when reflecting
        UpdateSpriteFlip();
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer != null)
        {
            // Flip the sprite based on movement direction
            // If moving right, don't flip (flipX = false)
            // If moving left, flip (flipX = true)
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    // Public method to set initial direction (used by spawner)
    public void SetDirection(Vector2 newDirection)
    {
        direction = new Vector2(Mathf.Sign(newDirection.x), 0f); // Ensure only horizontal movement
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }

        // Update sprite flip when direction is set
        UpdateSpriteFlip();
    }

    // Public method to set speed
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }
    }

    // Public method to manually set sprite flip if needed
    public void SetSpriteFlip(bool shouldFlip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = shouldFlip;
        }
    }
}