using UnityEngine;

public class AerialShooting : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] public GameObject bullet;
    [SerializeField] public Transform bulletPos;
    [SerializeField] public float bulletRange = 10f;
    [SerializeField] public float bulletCooldown = 2f;

    [Header("Donut Zone Settings")]
    [SerializeField] public float innerRadius = 3f; // Avoidance zone
    [SerializeField] public float outerRadius = 7f; // Shooting zone
    [SerializeField] public float approachSpeed = 3f;
    [SerializeField] public float retreatSpeed = 5f;
    [SerializeField] public float detectionRange = 15f;

    [Header("Ground Repulsion Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRepelForce = 5f;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float groundRepelRadius = 1.5f;

    [Header("Sprite Flipping")]
    [SerializeField] private bool enableFlipping = true;
    [SerializeField] private Transform spriteTransform; // Assign the sprite transform in inspector
    [SerializeField] private float flipThreshold = 0.1f; // Minimum x difference to trigger flip
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Visualization")]
    [SerializeField] public bool showGizmos = true;
    [SerializeField] public Color innerZoneColor = Color.red;
    [SerializeField] public Color outerZoneColor = Color.yellow;
    [SerializeField] public Color detectionColor = Color.blue;
    [SerializeField] public Color groundRepelColor = Color.green;

    private float timer;
    private GameObject player;
    private Rigidbody2D rb;
    private bool hasDetectedPlayer = false;
    private bool isFacingRight = true;
    private bool isInShootingZone = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        // Only act if player is within detection range
        if (distance < detectionRange)
        {
            HandleMovement(distance);
            HandleShooting(distance);

            // Handle sprite flipping if enabled
            if (enableFlipping && spriteTransform != null)
            {
                HandleSpriteFlip();
            }
        }

        HandleGroundRepulsion();
    }

    private void HandleGroundRepulsion()
    {
        // Check for ground in all directions
        RaycastHit2D hitDown = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitUp = Physics2D.Raycast(transform.position, Vector2.up, groundCheckDistance, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, Vector2.left, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, Vector2.right, groundCheckDistance, groundLayer);

        // Apply repulsion force based on ground proximity
        if (hitDown.collider != null)
        {
            rb.AddForce(Vector2.up * groundRepelForce, ForceMode2D.Force);
        }
        if (hitUp.collider != null)
        {
            rb.AddForce(Vector2.down * groundRepelForce, ForceMode2D.Force);
        }
        if (hitLeft.collider != null)
        {
            rb.AddForce(Vector2.right * groundRepelForce, ForceMode2D.Force);
        }
        if (hitRight.collider != null)
        {
            rb.AddForce(Vector2.left * groundRepelForce, ForceMode2D.Force);
        }

        // Additional sphere cast for nearby ground
        Collider2D[] nearbyGround = Physics2D.OverlapCircleAll(transform.position, groundRepelRadius, groundLayer);
        foreach (Collider2D ground in nearbyGround)
        {
            Vector2 repelDirection = (transform.position - ground.transform.position).normalized;
            rb.AddForce(repelDirection * groundRepelForce * 0.5f, ForceMode2D.Force);
        }
    }

    private void HandleMovement(float distance)
    {
        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;

        // Calculate base movement (donut zone behavior)
        Vector2 desiredVelocity = Vector2.zero;

        if (distance < innerRadius)
        {
            // In avoidance zone - move away from player
            desiredVelocity = -directionToPlayer * retreatSpeed;
            isInShootingZone = false;
        }
        else if (distance > outerRadius)
        {
            // Outside shooting zone - move toward outer radius
            Vector2 targetPos = (Vector2)player.transform.position + directionToPlayer * outerRadius;
            desiredVelocity = (targetPos - (Vector2)transform.position).normalized * approachSpeed;
            isInShootingZone = false;
        }
        else
        {
            // In shooting zone - maintain position
            desiredVelocity = Vector2.zero;
            isInShootingZone = true;
        }

        // Apply movement with ground repulsion influence
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, Time.deltaTime * 5f);
    }
    private void HandleSpriteFlip()
    {
        if (player == null) return;

        float xDifference = player.transform.position.x - transform.position.x;

        // Only flip if the difference exceeds our threshold
        if (Mathf.Abs(xDifference) > flipThreshold)
        {
            bool shouldFaceRight = xDifference > 0;

            // Only flip if needed
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
    }

    private void FlipSprite(bool faceRight)
    {
        isFacingRight = faceRight;

        // Use whichever flipping method works for your setup
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !faceRight;
        }
        else if (spriteTransform != null)
        {
            Vector3 newScale = spriteTransform.localScale;
            newScale.x = Mathf.Abs(newScale.x) * (faceRight ? 1 : -1);
            spriteTransform.localScale = newScale;
        }
    }
    private void HandleShooting(float distance)
    {
        // Check if player is within shooting range
        if (distance <= bulletRange)
        {
            // First detection - shoot immediately
            if (!hasDetectedPlayer)
            {
                Shoot();
                timer = 0; // Reset cooldown timer
                hasDetectedPlayer = true;
            }

            // Subsequent shots with cooldown
            timer += Time.deltaTime;
            if (timer > bulletCooldown)
            {
                timer = 0;
                Shoot();
            }
        }
        else
        {
            // Player out of range
            hasDetectedPlayer = false;
            timer = Mathf.Max(0, timer - Time.deltaTime * 0.5f); // Gradually reduce timer
        }
    }

    void Shoot()
    {
        if (player != null)
        {
            Vector2 direction = (player.transform.position - bulletPos.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Instantiate(bullet, bulletPos.position, Quaternion.Euler(0, 0, angle));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        if (player != null)
        {
            // Draw detection range
            Gizmos.color = detectionColor;
            Gizmos.DrawWireSphere(player.transform.position, detectionRange);

            // Draw outer zone (shooting area)
            Gizmos.color = outerZoneColor;
            Gizmos.DrawWireSphere(player.transform.position, outerRadius);

            // Draw inner zone (avoidance area)
            Gizmos.color = innerZoneColor;
            Gizmos.DrawWireSphere(player.transform.position, innerRadius);
        }

        // Draw ground repulsion indicators
        Gizmos.color = groundRepelColor;
        Gizmos.DrawWireSphere(transform.position, groundRepelRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * groundCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * groundCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * groundCheckDistance);
    }
}