using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class EnemyMovement : MonoBehaviour, EnemyDetectionZone.IEnemyController
{
    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    [Tooltip("Minimum wait time at patrol points")]
    public float minWaitTime = 1f;
    [Tooltip("Maximum wait time at patrol points")]
    public float maxWaitTime = 3f;

    [Header("Detection Settings")]
    [SerializeField] private float maxDetectionDistance = 10f;
    [SerializeField] private float minDetectionTime = 0.2f;
    [SerializeField] private float maxDetectionTime = 2f;
    [SerializeField] private float frontDetectionMultiplier = 0.7f; // Faster detection in front
    [SerializeField] private float rearDetectionMultiplier = 1.5f; // Slower detection behind

    [Header("Chase Settings")]
    public float chaseSpeed = 4f;
    public float visionRange = 5f;
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;
    public Transform raycastOrigin; // Assign your raycast empty object here

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRecoveryTime = 0.3f; // Time after attack before resuming chase
    private float lastAttackTime;
    private bool isAttacking = false;

    // Animation parameter
    private const string IS_ATTACKING = "isAttacking";

    [Header("Facing Direction")]
    [SerializeField] private bool facingRight = true;  // Serialized field for Inspector

    // Public properties for easy access
    public bool EnemyFacingRight => facingRight;
    public bool EnemyFacingLeft => !facingRight;

    private bool isDetectingPlayer = false;
    private float currentDetectionTime;
    private float detectionTimer;
    private bool playerInSight = false;

    private Transform currentTarget;
    private bool isChasing = false;
    private Transform player;
    private Rigidbody2D rb;
    private float waitTimer;
    private bool isWaiting = false;
    private Animator animator;

    // Animation parameters
    private const string IS_WALKING = "isWalking";
    private const string IS_RUNNING = "isRunning";

    [Header("Detection")]
    [SerializeField] private EnemyDetectionZone detectionZone;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (detectionZone == null)
        {
            detectionZone = GetComponentInChildren<EnemyDetectionZone>();
            if (detectionZone == null)
            {
                Debug.LogWarning("No detection zone assigned or found!", this);
            }
        }

        currentTarget = pointA;
        FindPlayer();
    }

    void FindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Ensure player exists and has correct tag/layer.");
        }
    }

    void Update()
    {
        if (player != null)
        {
            playerInSight = PlayerInSight();

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInAttackAnimation = stateInfo.IsTag("Attack");

            if (isAttacking && !isInAttackAnimation)
            {
                isAttacking = false;
            }

            if (!isAttacking && CanAttackPlayer())
            {
                StartAttack();
            }

            if (isDetectingPlayer && !isAttacking)
            {
                HandleDetectionDelay();
            }
            else if (playerInSight && !isChasing && !isAttacking)
            {
                StartDetection();
            }
        }

        if (isChasing && !isAttacking)
        {
            ChasePlayer();
        }
        else if (!isWaiting && !isDetectingPlayer && !isAttacking)
        {
            Patrol();
        }
        else if (isWaiting)
        {
            WaitAtPoint();
        }

        UpdateAnimations();
        UpdateFacingDirection();
    }
    public bool HasLineOfSightToPlayer()
    {
        return PlayerInSight();
    }

    void StartDetection()
    {
        isDetectingPlayer = true;
        rb.velocity = Vector2.zero; // Stop moving while detecting
        animator.SetBool(IS_WALKING, false);

        // Calculate detection time based on distance and direction
        float distance = Vector2.Distance(transform.position, player.position);
        float normalizedDistance = Mathf.Clamp01(distance / maxDetectionDistance);

        // Base time (longer when farther)
        currentDetectionTime = Mathf.Lerp(minDetectionTime, maxDetectionTime, normalizedDistance);

        // Apply direction modifier
        float directionFactor = GetDirectionFactor();
        currentDetectionTime *= directionFactor;

        detectionTimer = currentDetectionTime;
    }

    float GetDirectionFactor()
    {
        if (player == null) return 1f;

        Vector2 toPlayer = (player.position - transform.position).normalized;
        float dotProduct = Vector2.Dot(toPlayer, facingRight ? Vector2.right : Vector2.left);

        // Player is in front (dot product > 0)
        if (dotProduct > 0.3f) // Using a threshold for "front"
        {
            return frontDetectionMultiplier;
        }
        // Player is behind (dot product < 0)
        else if (dotProduct < -0.3f)
        {
            return rearDetectionMultiplier;
        }
        // Player is to the side
        return 1f;
    }

    void HandleDetectionDelay()
    {
        detectionTimer -= Time.deltaTime;

        if (detectionTimer <= 0 || !playerInSight)
        {
            isDetectingPlayer = false;

            if (playerInSight)
            {
                isChasing = true;
            }
        }
    }

    bool CanAttackPlayer()
    {
        if (player == null || isAttacking) return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool inAttackRange = distanceToPlayer <= attackRange;
        bool offCooldown = Time.time >= lastAttackTime + attackCooldown + attackRecoveryTime; // Include recovery time

        return inAttackRange && offCooldown && playerInSight;
    }

    void StartAttack()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero; // Stop moving
        animator.SetTrigger(IS_ATTACKING);
        lastAttackTime = Time.time; // Reset cooldown timer
    }

    public void OnAttackEnd()
    {
        isAttacking = false; // Immediately stop attacking
    }

    void UpdateAnimations()
    {
        bool isMoving = rb.velocity.magnitude > 0.1f && !isAttacking;
        animator.SetBool(IS_WALKING, isMoving && !isChasing);
        animator.SetBool(IS_RUNNING, isMoving && isChasing);
    }
    void StartWaiting()
    {
        isWaiting = true;
        rb.velocity = Vector2.zero;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
        animator.SetBool(IS_WALKING, false);
    }

    void WaitAtPoint()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0)
        {
            isWaiting = false;
            currentTarget = currentTarget == pointA ? pointB : pointA;
        }
    }
    void UpdateFacingDirection()
    {
        if (rb.velocity.x > 0 && !facingRight)
        {
            Flip();
        }
        else if (rb.velocity.x < 0 && facingRight)
        {
            Flip();
        }
    }

    void Patrol()
    {
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        if (Vector2.Distance(transform.position, currentTarget.position) < 0.5f)
        {
            StartWaiting();
        }
    }


    void ChasePlayer()
    {
        if (player == null || isAttacking) return;

        // Don't chase if still in recovery phase after attack
        if (Time.time < lastAttackTime + attackRecoveryTime)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Stop chasing if in attack range (attack will handle the rest)
        if (distanceToPlayer <= attackRange)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * chaseSpeed, rb.velocity.y);
    }

    bool PlayerInSight()
    {
        if (detectionZone == null || !detectionZone.playerInZone || player == null)
            return false;

        // Use the raycast origin position if assigned, otherwise use enemy position
        Vector2 origin = raycastOrigin != null ? raycastOrigin.position : transform.position;
        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(origin, player.position);

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction,
            distance,
            obstacleLayer);

        Debug.DrawRay(origin, direction * distance,
                     hit.collider == null ? Color.green : Color.red);

        return hit.collider == null || hit.collider.CompareTag("Player");
    }

    Transform GetNearestPatrolPoint()
    {
        return Vector2.Distance(transform.position, pointA.position) <
               Vector2.Distance(transform.position, pointB.position) ? pointA : pointB;
    }

    // Interface implementation
    public void OnPlayerDetected()
    {
        isChasing = true;
        Debug.Log("Player detected in zone");
    }
    public void OnPlayerLost()
    {
        isChasing = false;
        isDetectingPlayer = false;
        Debug.Log("Player left detection zone");
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection zones
        if (facingRight)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawRay(transform.position, Vector2.right * 2f);
            Gizmos.color = new Color(1, 0, 0, 0.1f);
            Gizmos.DrawRay(transform.position, Vector2.left * 1f);
        }
        else
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawRay(transform.position, Vector2.left * 2f);
            Gizmos.color = new Color(1, 0, 0, 0.1f);
            Gizmos.DrawRay(transform.position, Vector2.right * 1f);
        }
    }
}