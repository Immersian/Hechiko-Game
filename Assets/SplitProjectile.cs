using UnityEngine;

public class SplitProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8f;
    public Vector2 direction = Vector2.right; // Set by BossProjectileSplit

    [Header("Collision Settings")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float destroyDelay = 0.1f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        InitializeMovement();
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        InitializeMovement();
    }

    private void InitializeMovement()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.velocity = direction * speed;

        // Rotate projectile to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;
        if ((groundLayer.value & (1 << otherLayer)) != 0 ||
            (playerLayer.value & (1 << otherLayer)) != 0)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    // Visualize direction in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 1.5f);
    }
}