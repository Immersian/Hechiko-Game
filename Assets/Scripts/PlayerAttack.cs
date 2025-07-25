using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using SupanthaPaul;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float comboResetTime = 0.5f;
    public int maxComboCount = 3;
    public float downwardAttackSpeed = 20f;
    public float groundSlamImpactRadius = 2f;
    public float groundSlamImpactForce = 10f;
    public LayerMask groundSlamAffectedLayers;
    public float groundSlamLockDuration = 0.5f; // Time player is locked after ground slam impact

    [Header("Upward Launch Attack")]
    [SerializeField] private float upwardLaunchForce = 25f;
    [SerializeField] private float launchDelay = 0.1f;
    [SerializeField] private float upwardAttackStaminaCost = 30f; // Added stamina cost
    [SerializeField] private AudioClip upwardLaunchSound;
    [SerializeField] private GameObject upwardLaunchEffect;
    // In PlayerAttack.cs, add this variable:
    public bool isInUpwardAttackRecovery = false;
    private bool isLaunching = false;
    private float lastUpwardAttackTime;

    [Header("Attack Cooldowns")]
    public float[] attackCooldowns = new float[3] { 0.25f, 0.3f, 0.4f };

    [Header("Effects")]
    public AudioClip[] attackSounds;
    public AudioClip downwardAttackSound;
    public AudioClip groundSlamImpactSound;
    public GameObject groundSlamEffect;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Shockwave Settings")]
    [SerializeField] private ShockWaveManager shockWaveManager;

    [Header("Attack Hitbox")]
    [SerializeField] private Collider2D attackHitbox;

    // Component references
    private Animator animator;
    private AudioSource audioSource;

    // State variables
    private float timeSinceAttack;
    private int currentAttack;
    public bool isGroundSlamming = false;
    private bool isInGroundSlamImpact = false;
    private float groundSlamLockTimer = 0f;
    private const string attackTriggerPrefix = "Attack";

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Initialize attack states
        currentAttack = 0;
        timeSinceAttack = attackCooldowns[0];

        // Try to find shockwave manager if not set
        if (shockWaveManager == null)
        {
            shockWaveManager = FindObjectOfType<ShockWaveManager>();
            if (shockWaveManager == null)
            {
                Debug.LogWarning("ShockWaveManager not found in scene!");
            }
        }

        // Safety checks
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference not set in PlayerCombat!");
        }
        if (playerRigidbody == null)
        {
            Debug.LogError("Player Rigidbody2D reference not set in PlayerCombat!");
        }
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
        else
        {
            Debug.LogWarning("Attack hitbox reference not set in PlayerAttack!");
        }
    }

    void Update()
    {
        timeSinceAttack += Time.deltaTime;

        if (InputManager.instance.inputControl.Gameplay.Attack.WasPressedThisFrame())
        {
            if (playerController.canGroundSlam && !playerController.isGrounded)
            {
                StartGroundSlam();
            }
            else if (CanAttack() && !playerController.isDashing && !isGroundSlamming && !isInGroundSlamImpact)
            {
                PerformGroundAttack();
            }
        }
        if (InputManager.instance.inputControl.Gameplay.Special.WasPressedThisFrame()
            && !isGroundSlamming
            && !isInGroundSlamImpact
            && playerController.isGrounded) // Added grounded check
        {
            StartCoroutine(PerformUpwardLaunch());
        }


        // Handle ground slam states
        UpdateGroundSlamState();

        // Update falling animation state
        UpdateFallingAnimation();
    }

    private void UpdateGroundSlamState()
    {
        // Handle ground slam impact lock duration
        if (isInGroundSlamImpact)
        {
            groundSlamLockTimer -= Time.deltaTime;
            if (groundSlamLockTimer <= 0f)
            {
                isInGroundSlamImpact = false;
                playerController.canMove = true;
                playerController.canDash = true;
                playerController.canFlip = true;
            }
        }

        // Check if we hit ground during ground slam
        if (isGroundSlamming && playerController.isGrounded)
        {
            EndGroundSlam();
        }
    }

    private void UpdateFallingAnimation()
    {
        // Set falling animation state
        bool shouldBeFalling = isGroundSlamming && !playerController.isGrounded;
        animator.SetBool("FallingGroundSlam", shouldBeFalling);
    }

    private void StartGroundSlam()
    {
        isGroundSlamming = true;
        playerController.canMove = false;
        playerController.canDash = false;
        playerController.canFlip = false;
        playerController.m_wallGrabbing = false; // Force exit wall grab

        // Apply downward force
        playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, -downwardAttackSpeed);

        // Play sound if available
        if (downwardAttackSound != null)
        {
            audioSource.PlayOneShot(downwardAttackSound);
        }

        // Start falling animation
        animator.SetBool("FallingGroundSlam", true);
    }

    private void EndGroundSlam()
    {
        isGroundSlamming = false;
        isInGroundSlamImpact = true;
        groundSlamLockTimer = groundSlamLockDuration;

        // Switch to impact animation
        animator.SetBool("FallingGroundSlam", false);
        animator.SetTrigger("GroundSlamImpact");

        // Create impact effect
        if (groundSlamEffect != null)
        {
            Instantiate(groundSlamEffect, playerController.groundCheck.position, Quaternion.identity);
        }

        // Play impact sound
        if (groundSlamImpactSound != null)
        {
            audioSource.PlayOneShot(groundSlamImpactSound);
        }

        // Apply impact force to nearby objects
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            playerController.groundCheck.position,
            groundSlamImpactRadius,
            groundSlamAffectedLayers
        );

        foreach (Collider2D hitCollider in hitColliders)
        {
            Rigidbody2D rb = hitCollider.GetComponent<Rigidbody2D>();
            if (rb != null && rb != playerRigidbody)
            {
                Vector2 direction = (hitCollider.transform.position - transform.position).normalized;
                rb.AddForce(direction * groundSlamImpactForce, ForceMode2D.Impulse);
            }
        }

        // Camera shake
        if (playerController.cameraShake != null)
        {
            playerController.cameraShake.ShakeCamera(playerController.shakeIntensity * 1.5f, playerController.shakeTime * 1.2f);
        }
    }
    private IEnumerator PerformUpwardLaunch()
    {
        // Only allow when grounded and has enough stamina
        if (!playerController.isGrounded || playerController.currentStamina < upwardAttackStaminaCost)
            yield break;

        // Start the attack
        isLaunching = true;
        isInUpwardAttackRecovery = true; // New state

        // Consume stamina
        playerController.currentStamina -= upwardAttackStaminaCost;
        playerController.UpdateStaminaBar();

        // Disable movement during wind-up
        playerController.canMove = false;
        playerController.canDash = false;

        // Trigger animation
        animator.SetTrigger("UpwardLaunch");

        // Play sound if available
        if (upwardLaunchSound != null)
        {
            audioSource.PlayOneShot(upwardLaunchSound);
        }

        // Wait for the delay
        yield return new WaitForSeconds(launchDelay);

        // Apply upward force
        playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, 0); // Reset vertical velocity first
        playerRigidbody.AddForce(Vector2.up * upwardLaunchForce, ForceMode2D.Impulse);

        // Create effect if available
        if (upwardLaunchEffect != null)
        {
            Instantiate(upwardLaunchEffect, transform.position, Quaternion.identity);
        }

        // Camera shake
        if (playerController.cameraShake != null)
        {
            playerController.cameraShake.ShakeCamera(playerController.shakeIntensity * 0.8f, playerController.shakeTime * 0.8f);
        }

        // Re-enable movement after a short delay
        yield return new WaitForSeconds(0.1f);
        playerController.canMove = true;
        isLaunching = false;

        // Clear recovery state when landing
        while (!playerController.isGrounded)
        {
            yield return null;
        }
        isInUpwardAttackRecovery = false;
        playerController.canDash = true; // Re-enable dash only after landing
    }

    private bool CanAttack()
    {
        // Reset combo if too much time passed
        if (timeSinceAttack > comboResetTime)
        {
            currentAttack = 0;
        }

        // For first attack or after combo reset
        if (currentAttack == 0)
        {
            return timeSinceAttack >= attackCooldowns[0];
        }

        // For subsequent attacks
        int cooldownIndex = Mathf.Clamp(currentAttack - 1, 0, attackCooldowns.Length - 1);
        return timeSinceAttack >= attackCooldowns[cooldownIndex];
    }

    private void PerformGroundAttack()
    {
        // Advance combo counter
        currentAttack = (currentAttack % maxComboCount) + 1;
        timeSinceAttack = 0f;

        // Trigger animation
        string triggerName = attackTriggerPrefix + currentAttack;
        animator.SetTrigger(triggerName);

        // Play sound if available
        if (attackSounds.Length >= currentAttack && attackSounds[currentAttack - 1] != null)
        {
            audioSource.PlayOneShot(attackSounds[currentAttack - 1]);
        }
    }

    public void TriggerShockwave()
    {
        shockWaveManager.CallShockwave(true);
    }

    // Animation Event - Called at the start of attack animations
    public void OnAttackStart()
    {
        playerController.canMove = false;
        playerController.canDash = false;
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
        }
    }

    public void OnAttackEnd()
    {
        playerController.canMove = true;
        playerController.canDash = true;

        // Disable hitbox when attack ends
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    // In PlayerAttack.cs
    public void OnDashStart()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
            Debug.Log("Dash hitbox enabled"); // For debugging
        }
    }

    public void OnDashEnd()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            Debug.Log("Dash hitbox disabled"); // For debugging
        }
    }

    // Add this for safety in case attack is interrupted
    public void OnDisable()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerController != null && playerController.groundCheck != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(playerController.groundCheck.position, groundSlamImpactRadius);
        }
    }
}