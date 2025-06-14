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
    [SerializeField] private float upwardAttackCooldown = 1f;
    [SerializeField] private AudioClip upwardLaunchSound;
    [SerializeField] private GameObject upwardLaunchEffect;
    private float lastUpwardAttackTime;
    private bool isLaunching = false;

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

        // Safety checks
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference not set in PlayerCombat!");
        }
        if (playerRigidbody == null)
        {
            Debug.LogError("Player Rigidbody2D reference not set in PlayerCombat!");
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
            && Time.time > lastUpwardAttackTime + upwardAttackCooldown
            && !isGroundSlamming
            && !isInGroundSlamImpact)
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
        // Start the attack
        isLaunching = true;
        lastUpwardAttackTime = Time.time;

        // Disable movement during wind-up
        playerController.canMove = false;
        playerController.canDash = false;

        // Trigger animation (you'll need to create this)
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
        playerController.canDash = true;
        isLaunching = false;
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

    // Animation Event - Called at the start of attack animations
    public void OnAttackStart()
    {
        playerController.canMove = false;
        playerController.canDash = false;
    }

    // Animation Event - Called at the end of attack animations
    public void OnAttackEnd()
    {
        playerController.canMove = true;
        playerController.canDash = true;
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