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

    [Header("Ledge Detection")]
    [SerializeField] private Transform ledgeCheck; // Assign your new ground check empty object here
    [SerializeField] private float ledgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    //[SerializeField] private float chaseLedgeStopDistance = 1f; // How close to get to ledge while chasing
    [SerializeField] private float returnToPatrolDelay = 1f; // Delay before returning to patrol
    private float returnToPatrolTimer = 0f;
    private bool shouldReturnToPatrol = false;
    private bool atLedge = false;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRecoveryTime = 0.3f; // Time after attack before resuming chase
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Alert Settings")]
    [SerializeField] private float alertAnimationDuration = 1f; // Duration of the alerted animation
    private bool isAlerted = false;
    private float alertTimer = 0f;
    private bool hasBeenAlertedThisDetection = false; // NEW: Track if alerted during current detection

    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float recoveryAnimationDelay = 0.1667f; // 1/6th of a second delay
    [SerializeField] private string stunTrigger = "Stunned"; // Animation trigger name for initial stun
    [SerializeField] private string stunBool = "Stun"; // Animation bool name for ongoing stun
    [SerializeField] private string recoveredTrigger = "Recovered"; // Animation trigger for recovery
    private bool isStunned = false;
    private bool isRecovering = false;
    private float stunTimer = 0f;
    private bool hasPlayedStunAnimation = false;

    // Animation parameters
    private const string IS_ATTACKING = "isAttacking";
    private const string ALERTED_TRIGGER = "Alerted";

    [Header("Facing Direction")]
    [SerializeField] private bool facingLeft = true;  // Changed from facingRight to facingLeft
    /*    [SerializeField] private bool flipSprite = true;*/  // Option to flip sprite vs scale

    private float lastFlipTime;
    [SerializeField] private float flipCooldown = 0.5f;

    // Public properties updated for left-facing
    public bool EnemyFacingLeft => facingLeft;
    public bool EnemyFacingRight => !facingLeft;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool enablePitchShift = true;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Tutorial Settings")]
    [SerializeField] private bool enableTutorial = true;
    [SerializeField] private string tutorialObjectTag = "parrytutorial";
    [SerializeField] private float tutorialAlphaNormal = 0.5f;
    [SerializeField] private float tutorialAlphaHighlight = 1f;
    private GameObject tutorialObject;
    private CanvasGroup tutorialCanvasGroup;
    private bool isTutorialActive = false;

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
    [SerializeField] public EnemyDetectionZone detectionZone;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Initialize tutorial object
        if (enableTutorial)
        {
            tutorialObject = GameObject.FindGameObjectWithTag(tutorialObjectTag);
            if (tutorialObject != null)
            {
                tutorialCanvasGroup = tutorialObject.GetComponent<CanvasGroup>();
                if (tutorialCanvasGroup == null)
                {
                    tutorialCanvasGroup = tutorialObject.AddComponent<CanvasGroup>();
                }
                // Set initial alpha to normal
                tutorialCanvasGroup.alpha = tutorialAlphaNormal;
                isTutorialActive = false;
                Debug.Log($"{gameObject.name}: Found tutorial object: {tutorialObject.name}");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: No tutorial object found with tag: {tutorialObjectTag}");
            }
        }

        // Initialize audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

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

        // Set initial radius to normal
        if (detectionZone != null)
        {
            detectionZone.SetNormalRadius();
        }
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
        // Update tutorial state
        UpdateTutorialState();

        if (isStunned || isRecovering)
        {
            HandleStunState();
            return;
        }

        // Handle alerted state
        if (isAlerted)
        {
            HandleAlertedState();
            return;
        }

        // Rest of your existing Update() logic...
        if (TryGetComponent<EnemyDamageHandler>(out var health) && health.isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (shouldReturnToPatrol)
        {
            returnToPatrolTimer += Time.deltaTime;
            if (returnToPatrolTimer >= returnToPatrolDelay)
            {
                ReturnToNearestPatrolPoint();
                shouldReturnToPatrol = false;
            }
        }

        // Check for ledges - only when moving
        if (rb.velocity.magnitude > 0.1f)
        {
            atLedge = !Physics2D.Raycast(ledgeCheck.position, Vector2.down, ledgeCheckDistance, groundLayer);
        }
        else
        {
            atLedge = false;
        }

        if (isChasing)
        {
            // Reset return to patrol timer while chasing
            returnToPatrolTimer = 0f;
            shouldReturnToPatrol = false;

            atLedge = !Physics2D.Raycast(ledgeCheck.position, Vector2.down, ledgeCheckDistance, groundLayer);
        }

        if (player != null)
        {
            playerInSight = PlayerInSight();

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInAttackAnimation = stateInfo.IsTag("Attack");

            if (isAttacking && !isInAttackAnimation)
            {
                isAttacking = false;
                // When attack animation ends, deactivate tutorial
                if (isTutorialActive)
                {
                    SetTutorialAlphaNormal();
                    isTutorialActive = false;
                }
            }

            if (!isAttacking && CanAttackPlayer())
            {
                StartAttack();
            }

            if (isDetectingPlayer && !isAttacking)
            {
                HandleDetectionDelay();
            }
            else if (playerInSight && !isChasing && !isAttacking && !isAlerted)
            {
                // NEW: Check if we've already been alerted during this detection period
                if (!hasBeenAlertedThisDetection)
                {
                    Debug.Log($"{gameObject.name}: Conditions met for starting detection - starting detection process");
                    StartDetection();
                }
                else
                {
                    Debug.Log($"{gameObject.name}: Player in sight but already alerted during this detection - waiting for player to leave zone");
                }
            }
        }

        // Stop movement if at ledge and not chasing player
        if (atLedge && !isChasing && !isWaiting && !isAlerted)
        {
            rb.velocity = Vector2.zero;
            StartWaiting();
            return;
        }

        if (isChasing && !isAttacking && !isAlerted)
        {
            ChasePlayer();
        }
        else if (!isWaiting && !isDetectingPlayer && !isAttacking && !isAlerted)
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

    private void UpdateTutorialState()
    {
        if (!enableTutorial || tutorialCanvasGroup == null) return;

        // If we're attacking and tutorial is active, keep it highlighted
        if (isAttacking && isTutorialActive)
        {
            tutorialCanvasGroup.alpha = tutorialAlphaHighlight;
        }
        // If we're not attacking and tutorial is active, deactivate it
        else if (!isAttacking && isTutorialActive)
        {
            SetTutorialAlphaNormal();
            isTutorialActive = false;
        }
    }

    // ANIMATION EVENT METHOD - Use this for animation events
    public void PlaySoundWithPitchShift(AudioClip clip)
    {
        PlaySoundWithPitchShiftCustom(clip, 1.0f);
    }

    // ANIMATION EVENT METHOD with volume control
    public void PlaySoundWithPitchShiftCustom(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null || audioSource == null) return;

        float pitch = 1.0f;

        if (enablePitchShift)
        {
            pitch = Random.Range(minPitch, maxPitch);
        }

        // Store original pitch
        float originalPitch = audioSource.pitch;

        // Apply new pitch
        audioSource.pitch = pitch;

        // Play the sound
        audioSource.PlayOneShot(clip, volume);

        // Reset pitch after playing
        StartCoroutine(ResetPitchAfterSound(originalPitch));
    }

    // ANIMATION EVENT METHOD - Activate tutorial highlight
    public void ActivateTutorial()
    {
        if (!enableTutorial || tutorialCanvasGroup == null) return;

        SetTutorialAlphaHighlighted();
        isTutorialActive = true;
        Debug.Log($"{gameObject.name}: Tutorial activated and will stay active during attack");
    }

    private IEnumerator ResetPitchAfterSound(float originalPitch)
    {
        // Wait one frame to ensure sound has started playing
        yield return null;

        // Reset pitch to original
        if (audioSource != null)
        {
            audioSource.pitch = originalPitch;
        }
    }

    // Method to play sound without pitch shift (for specific cases)
    public void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    private void SetTutorialAlphaHighlighted()
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = tutorialAlphaHighlight;
        }
    }

    private void SetTutorialAlphaNormal()
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = tutorialAlphaNormal;
        }
    }

    private void HandleAlertedState()
    {
        // Stop movement during alerted animation
        rb.velocity = Vector2.zero;

        // Update timer
        alertTimer += Time.deltaTime;

        // Face the player during alerted state
        if (player != null)
        {
            bool shouldFaceLeft = player.position.x < transform.position.x;
            if (shouldFaceLeft != facingLeft && Time.time >= lastFlipTime + flipCooldown)
            {
                Flip();
            }
        }

        // When alerted animation is complete, start chasing
        if (alertTimer >= alertAnimationDuration)
        {
            isAlerted = false;
            isChasing = true;
        }
    }

    private void HandleStunState()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;

            // Play the initial stun trigger animation if not already played
            if (!hasPlayedStunAnimation)
            {
                animator.SetTrigger(stunTrigger);
                hasPlayedStunAnimation = true;
                // Set the stun bool to true for the ongoing stun animation
                animator.SetBool(stunBool, true);
                Debug.Log($"{gameObject.name}: Stunned, playing stun animation");
            }

            // When stun is over, trigger recovery
            if (stunTimer <= 0)
            {
                Debug.Log($"{gameObject.name}: Stun duration over, starting recovery");
                StartRecovery();
            }
        }
        else if (isRecovering)
        {
            // Recovery state - just wait for the delay to complete
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0)
            {
                Debug.Log($"{gameObject.name}: Recovery complete");
                EndRecovery();
            }
        }
    }

    private void StartRecovery()
    {
        isStunned = false;
        isRecovering = true;
        hasPlayedStunAnimation = false;

        // Set stun bool to false
        animator.SetBool(stunBool, false);

        // Trigger the recovery animation
        animator.SetTrigger(recoveredTrigger);

        // Set timer for recovery delay
        stunTimer = recoveryAnimationDelay;
    }

    private void EndRecovery()
    {
        isRecovering = false;
        ReturnToNearestPatrolPoint();
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
        float dotProduct = Vector2.Dot(toPlayer, facingLeft ? Vector2.right : Vector2.left);

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
                // Instead of immediately chasing, play alerted animation first
                StartAlertedState();
            }
            else
            {
                Debug.Log($"{gameObject.name}: Player lost during detection, returning to normal");
            }
        }
    }

    private void StartAlertedState()
    {
        isAlerted = true;
        alertTimer = 0f;
        animator.SetTrigger(ALERTED_TRIGGER);

        // NEW: Set alert radius when enemy becomes alerted
        if (detectionZone != null)
        {
            detectionZone.SetAlertRadius();
            Debug.Log($"{gameObject.name}: Setting alert radius");
        }

        // NEW: Mark that we've been alerted during this detection period
        hasBeenAlertedThisDetection = true;

        // Stop all movement
        rb.velocity = Vector2.zero;

        // Cancel any current actions
        isDetectingPlayer = false;
        isWaiting = false;
    }

    bool CanAttackPlayer()
    {
        // Add dead check
        if (TryGetComponent<EnemyDamageHandler>(out var health) && health.isDead)
            return false;

        if (player == null || isAttacking || isAlerted) return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool inAttackRange = distanceToPlayer <= attackRange;
        bool offCooldown = Time.time >= lastAttackTime + attackCooldown + attackRecoveryTime;

        return inAttackRange && offCooldown && playerInSight;
    }

    void StartAttack()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero; // Stop moving
        animator.SetTrigger(IS_ATTACKING);
        lastAttackTime = Time.time; // Reset cooldown timer

        // Reset tutorial state - will be activated by animation event
        if (isTutorialActive)
        {
            SetTutorialAlphaNormal();
            isTutorialActive = false;
        }

        Debug.Log($"{gameObject.name}: Starting attack");
    }

    public void OnAttackEnd()
    {
        isAttacking = false;

        // Deactivate tutorial when attack ends
        if (isTutorialActive)
        {
            SetTutorialAlphaNormal();
            isTutorialActive = false;
        }

        Debug.Log($"{gameObject.name}: Attack ended");
        CheckFacePlayerAfterAttack(); // Add this line
    }

    void UpdateAnimations()
    {
        if (isStunned || isRecovering || isAlerted) return; // Don't change animations while stunned, recovering, or alerted

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
        Debug.Log($"{gameObject.name}: Starting wait at point for {waitTimer:F2}s");
    }

    void WaitAtPoint()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0)
        {
            isWaiting = false;
            currentTarget = currentTarget == pointA ? pointB : pointA;
            Debug.Log($"{gameObject.name}: Wait finished, switching to target: {currentTarget.name}");
        }
    }

    void UpdateFacingDirection()
    {
        if (isAttacking || isStunned || isRecovering || isAlerted) return;

        bool shouldFaceLeft = facingLeft;

        // Special case when at ledge - always face toward player if they're on the other side
        if (player != null && (atLedge || isChasing))
        {
            shouldFaceLeft = player.position.x < transform.position.x;
        }
        // Normal movement facing direction
        else if (Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            shouldFaceLeft = rb.velocity.x < 0;
        }

        // Only flip if needed and cooldown has passed
        if (shouldFaceLeft != facingLeft && Time.time >= lastFlipTime + flipCooldown)
        {
            Flip();
        }
    }

    void Patrol()
    {
        // Only check for ledges when actually moving toward a target
        if (Vector2.Distance(transform.position, currentTarget.position) > 0.5f)
        {
            if (atLedge)
            {
                rb.velocity = Vector2.zero;
                Debug.Log($"{gameObject.name}: At ledge during patrol, stopping movement");

                // Face toward patrol point while at ledge
                bool targetOnLeft = currentTarget.position.x < transform.position.x;
                if (targetOnLeft != facingLeft && Time.time >= lastFlipTime + flipCooldown)
                {
                    Flip();
                }

                StartWaiting();
                return;
            }

            Vector2 direction = (currentTarget.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);
        }
        else
        {
            StartWaiting();
        }
    }

    void ChasePlayer()
    {
        if (player == null || isAttacking || isAlerted) return;

        if (Time.time < lastAttackTime + attackRecoveryTime)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (atLedge)
        {
            rb.velocity = Vector2.zero;

            // Face toward player while at ledge
            bool playerOnLeft = player.position.x < transform.position.x;
            if (playerOnLeft != facingLeft && Time.time >= lastFlipTime + flipCooldown)
            {
                Flip();
            }
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * chaseSpeed, rb.velocity.y);
    }

    bool PlayerInSight()
    {
        if (detectionZone == null || !detectionZone.playerInZone || player == null)
        {
            Debug.Log($"{gameObject.name}: PlayerInSight failed - detectionZone: {detectionZone != null}, playerInZone: {detectionZone?.playerInZone}, player: {player != null}");
            return false;
        }

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

        bool hasSight = hit.collider == null || hit.collider.CompareTag("Player");
        Debug.Log($"{gameObject.name}: PlayerInSight check - HasSight: {hasSight}, Hit: {hit.collider?.name}");

        return hasSight;
    }

    Transform GetNearestPatrolPoint()
    {
        return Vector2.Distance(transform.position, pointA.position) <
               Vector2.Distance(transform.position, pointB.position) ? pointA : pointB;
    }

    void GiveUpChase()
    {
        isChasing = false;
        shouldReturnToPatrol = true;
        returnToPatrolTimer = 0f;

        // NEW: Set back to normal radius when giving up chase
        if (detectionZone != null)
        {
            detectionZone.SetNormalRadius();
            Debug.Log($"{gameObject.name}: Setting normal radius (giving up chase)");
        }
    }

    void ReturnToNearestPatrolPoint()
    {
        currentTarget = GetNearestPatrolPoint();
        isWaiting = false;

        // NEW: Set back to normal radius when returning to patrol
        if (detectionZone != null)
        {
            detectionZone.SetNormalRadius();
            Debug.Log($"{gameObject.name}: Setting normal radius (returning to patrol)");
        }
    }

    // Interface implementation
    public void OnPlayerDetected()
    {
        // Don't immediately chase - let the detection system handle it
        Debug.Log($"{gameObject.name}: Player detected in zone");
    }

    public void OnPlayerLost()
    {
        isChasing = false;
        isDetectingPlayer = false;
        isAlerted = false; // Cancel alerted state if player is lost

        // NEW: Set back to normal radius when player is lost
        if (detectionZone != null)
        {
            detectionZone.SetNormalRadius();
            Debug.Log($"{gameObject.name}: Setting normal radius (player lost)");
        }

        // NEW: Reset the alerted flag when player leaves the zone
        hasBeenAlertedThisDetection = false;

        shouldReturnToPatrol = true;
        returnToPatrolTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Shockwave") && !isStunned && !isRecovering)
        {
            if (TryGetComponent<EnemyDamageHandler>(out var health) && health.isDead)
                return;

            StartStun();
        }
    }

    private void StartStun()
    {
        isStunned = true;
        stunTimer = stunDuration;
        hasPlayedStunAnimation = false; // Reset for new stun
        rb.velocity = Vector2.zero; // Stop movement

        // Cancel any current actions
        isChasing = false;
        isDetectingPlayer = false;
        isAttacking = false;
        isWaiting = false;
        isAlerted = false; // Also cancel alerted state

        // Deactivate tutorial if active
        if (isTutorialActive)
        {
            SetTutorialAlphaNormal();
            isTutorialActive = false;
        }

        // NEW: Set back to normal radius when stunned
        if (detectionZone != null)
        {
            detectionZone.SetNormalRadius();
            Debug.Log($"{gameObject.name}: Setting normal radius (stunned)");
        }
    }

    void CheckFacePlayerAfterAttack()
    {
        if (isAttacking || player == null) return;

        // Only check if player is still in attack range after attack ends
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            bool shouldFaceLeft = player.position.x < transform.position.x;

            // Only flip if not already facing the correct direction
            if (shouldFaceLeft != facingLeft)
            {
                Flip();
            }
        }
    }

    public void CancelAttack()
    {
        // Immediately stop attacking state
        isAttacking = false;

        // Reset attack animation if needed
        animator.ResetTrigger(IS_ATTACKING);

        // Deactivate tutorial when attack is cancelled
        if (isTutorialActive)
        {
            SetTutorialAlphaNormal();
            isTutorialActive = false;
        }

        Debug.Log($"{gameObject.name}: Attack cancelled");
    }

    void Flip()
    {
        if (Time.time < lastFlipTime + flipCooldown) return;

        facingLeft = !facingLeft;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        lastFlipTime = Time.time;
        Debug.Log($"{gameObject.name}: Flipped, now facing left: {facingLeft}");
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection zones
        if (facingLeft)
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
        if (ledgeCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + Vector3.down * ledgeCheckDistance);
        }
    }
}