using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AerialEnemy : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float recoilStrength = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float idealDistance = 7f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float retreatSpeed = 5f;
    [SerializeField] private float deactivateRange = 20f;

    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private string hurtTrigger = "Hurt";
    [SerializeField] private string stunBool = "IsStunned";
    private bool isStunned = false;
    private float stunTimer = 0f;
    private bool hasPlayedHurtAnimation = false;

    [Header("Enemy Repulsion")]
    [SerializeField] private float enemyRepelForce = 5f;
    [SerializeField] private float enemyRepelRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float enemyRepelSmoothness = 2f;

    [Header("Ground Repulsion")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRepelForce = 5f;
    [SerializeField] private float groundCheckDistance = 2f;

    [Header("Ground Repulsion Gizmos")]
    [SerializeField] private bool showGroundChecks = true;
    [SerializeField] private Color groundRayColor = Color.cyan;
    [SerializeField] private Color groundRepelRadiusColor = new Color(0, 1, 1, 0.2f);

    [Header("Improved Ground Repulsion")]
    [SerializeField] private float groundRepelSmoothness = 5f;
    [SerializeField] private float groundRepelMaxForce = 10f;
    [SerializeField] private float groundSafetyMargin = 0.5f;

    [Header("Visuals")]
    [SerializeField] private Transform graphics;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackAnimTrigger = "Attack";

    private Transform player;
    private Rigidbody2D rb;
    private float lastAttackTime;
    private bool facingRight = true;
    private EnemyDamageHandler damageHandler;
    private bool deathCheckInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Set up enemy layer mask if not configured
        if (enemyLayer == 0)
        {
            enemyLayer = LayerMask.GetMask("Default");
        }
    }

    private void Start()
    {
        // Initialize damage handler in Start to ensure it's available
        damageHandler = GetComponent<EnemyDamageHandler>();
        if (damageHandler == null)
        {
            Debug.LogError("EnemyDamageHandler component not found on " + gameObject.name);
        }
        else
        {
            deathCheckInitialized = true;
        }
    }

    private void Update()
    {
        // CHECK IF DEAD FIRST - if dead, stop all processing
        if (IsDead())
        {
            HandleDeathState();
            return;
        }

        if (isStunned)
        {
            HandleStunState();
            return;
        }

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > deactivateRange)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        HandleMovement();
        HandleCombat();
        HandleEnemyRepulsion();
        UpdateFacing();
        HandleGroundRepulsion();
    }

    // Helper method to check if enemy is dead
    private bool IsDead()
    {
        if (!deathCheckInitialized) return false;

        // Try to get the damage handler if it's null (in case it was added later)
        if (damageHandler == null)
        {
            damageHandler = GetComponent<EnemyDamageHandler>();
            if (damageHandler == null) return false;
        }

        return damageHandler.isDead;
    }

    // Handle behavior when enemy is dead
    private void HandleDeathState()
    {
        // Stop all movement
        rb.velocity = Vector2.zero;

        // Reset any attack triggers to prevent attack animation from playing
        if (animator != null)
        {
            animator.ResetTrigger(attackAnimTrigger);
            animator.ResetTrigger(hurtTrigger);
            animator.SetBool(stunBool, false);
        }

        // Don't process any other logic when dead
        return;
    }

    private void HandleStunState()
    {
        // If dead while stunned, stop processing stun logic
        if (IsDead()) return;

        stunTimer -= Time.deltaTime;

        // Play the hurt animation if not already played
        if (!hasPlayedHurtAnimation && animator != null)
        {
            animator.SetTrigger(hurtTrigger);
            animator.SetBool(stunBool, true);
            hasPlayedHurtAnimation = true;
        }

        // Stop movement while stunned
        rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 10f);

        // When stun is over, return to normal
        if (stunTimer <= 0)
        {
            EndStun();
        }
    }

    private void EndStun()
    {
        isStunned = false;
        hasPlayedHurtAnimation = false;

        // Reset stun animation parameters
        if (animator != null && !IsDead()) // Only reset if not dead
        {
            animator.SetBool(stunBool, false);
            animator.ResetTrigger(hurtTrigger);
        }

        Debug.Log("Stun ended - returning to normal behavior");
    }

    private void StartStun()
    {
        // Don't start stun if dead
        if (IsDead()) return;

        isStunned = true;
        stunTimer = stunDuration;
        hasPlayedHurtAnimation = false;

        // Stop any current movement
        rb.velocity = Vector2.zero;

        Debug.Log("Enemy stunned!");
    }

    private void HandleMovement()
    {
        // Don't move if dead
        if (IsDead()) return;

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 moveDirection = toPlayer.normalized;

        if (distance < minDistance)
        {
            rb.velocity = -moveDirection * retreatSpeed;
        }
        else if (distance > idealDistance)
        {
            Vector2 targetPos = (Vector2)player.position - moveDirection * idealDistance;
            rb.velocity = ((targetPos - (Vector2)transform.position).normalized * moveSpeed);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 5f);
        }
    }

    private void HandleEnemyRepulsion()
    {
        // Don't repel other enemies while stunned or dead
        if (isStunned || IsDead()) return;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, enemyRepelRadius, enemyLayer);
        Vector2 totalRepelForce = Vector2.zero;
        int repelCount = 0;

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            if (enemyCollider.gameObject != gameObject &&
                (enemyCollider.CompareTag("Flying Enemy") || enemyCollider.GetComponent<AerialEnemy>() != null))
            {
                Vector2 toEnemy = enemyCollider.transform.position - transform.position;
                float distance = toEnemy.magnitude;

                if (distance > 0.1f)
                {
                    float forceMultiplier = 1 - (distance / enemyRepelRadius);
                    Vector2 repelDirection = -toEnemy.normalized;
                    totalRepelForce += repelDirection * forceMultiplier * enemyRepelForce;
                    repelCount++;
                }
            }
        }

        if (repelCount > 0)
        {
            totalRepelForce /= repelCount;
            rb.AddForce(totalRepelForce * enemyRepelSmoothness * Time.deltaTime, ForceMode2D.Force);
        }
    }

    private void HandleCombat()
    {
        // Don't attack while stunned or dead
        if (isStunned || IsDead()) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance >= minDistance && distance <= idealDistance)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
    }

    private void Attack()
    {
        // Don't attack while stunned or dead
        if (isStunned || IsDead()) return;

        Vector2 attackDir = (player.position - transform.position).normalized;

        if (animator != null)
        {
            animator.SetTrigger(attackAnimTrigger);
        }
        else
        {
            ExecuteAttack(attackDir);
        }
    }

    // Call this from animation event
    public void OnAttackAnimationEvent()
    {
        // Don't execute attack while stunned or dead
        if (isStunned || IsDead()) return;

        Vector2 attackDir = (player.position - transform.position).normalized;
        ExecuteAttack(attackDir);
    }

    private void ExecuteAttack(Vector2 direction)
    {
        // Don't execute attack if dead
        if (IsDead()) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));

        rb.AddForce(-direction * recoilStrength, ForceMode2D.Impulse);
    }

    private void UpdateFacing()
    {
        // Don't update facing while stunned or dead
        if (isStunned || IsDead() || graphics == null) return;

        float xDiff = player.position.x - transform.position.x;
        bool shouldFaceRight = xDiff > 0;

        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            graphics.localScale = new Vector3(facingRight ? -1 : 1, 1, 1);
        }
    }

    private void HandleGroundRepulsion()
    {
        // Don't handle if dead
        if (IsDead()) return;

        Vector2 totalRepelForce = Vector2.zero;
        float closestDistance = float.MaxValue;
        Vector2 closestDirection = Vector2.zero;

        CheckGroundDirection(Vector2.down, ref closestDistance, ref closestDirection);
        CheckGroundDirection(Vector2.up, ref closestDistance, ref closestDirection);
        CheckGroundDirection(Vector2.left, ref closestDistance, ref closestDirection);
        CheckGroundDirection(Vector2.right, ref closestDistance, ref closestDirection);

        if (closestDistance < groundCheckDistance)
        {
            float distanceRatio = 1 - (closestDistance / groundCheckDistance);
            float repulsionStrength = Mathf.Lerp(0, groundRepelMaxForce,
                Mathf.Pow(distanceRatio, groundRepelSmoothness));

            totalRepelForce = closestDirection.normalized * repulsionStrength;
            float upwardBias = Mathf.Lerp(0.2f, 1f, distanceRatio);
            totalRepelForce += Vector2.up * groundRepelForce * upwardBias;

            if (Mathf.Abs(rb.velocity.y) > 0.5f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.9f);
            }

            rb.AddForce(totalRepelForce, ForceMode2D.Force);
        }
        else if (!isStunned)
        {
            rb.AddForce(Vector2.down * 0.5f, ForceMode2D.Force);
        }
    }

    private void CheckGroundDirection(Vector2 direction, ref float closestDistance, ref Vector2 closestDirection)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, groundCheckDistance + groundSafetyMargin, groundLayer);
        if (hit.collider != null && hit.distance < closestDistance)
        {
            closestDistance = hit.distance;
            closestDirection = (Vector2)transform.position - hit.point;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Don't process stun if dead
        if (IsDead()) return;

        if (other.CompareTag("Shockwave") && !isStunned)
        {
            StartStun();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, minDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, idealDistance);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.position, deactivateRange);
        }

        if (showGroundChecks)
        {
            Gizmos.color = groundRayColor;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * groundCheckDistance);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.left * groundCheckDistance);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * groundCheckDistance);

            Gizmos.color = groundRepelRadiusColor;
            Gizmos.DrawWireSphere(transform.position, groundCheckDistance);

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, enemyRepelRadius);
    }
}