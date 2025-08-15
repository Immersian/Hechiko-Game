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
    public float groundSlamLockDuration = 0.5f;

    [Header("Upward Launch Attack")]
    [SerializeField] private float upwardLaunchForce = 25f;
    [SerializeField] private float launchDelay = 0.1f;
    [SerializeField] private float upwardAttackStaminaCost = 30f;
    [SerializeField] private AudioClip upwardLaunchSound;
    [SerializeField] private GameObject upwardLaunchEffect;
    public bool isInUpwardAttackRecovery = false;
    private bool isLaunching = false;
    private float lastUpwardAttackTime;

    [Header("Attack Cooldowns")]
    public float[] attackCooldowns = new float[3] { 0.25f, 0.3f, 0.4f };

    [Header("Dash Cooldown After Attack")]
    [SerializeField] private float postAttackDashCooldown = 0.3f; // Separate cooldown for dash after attacking
    private float timeSinceLastAttack = 0f;

    [Header("Effects")]
    public AudioClip[] attackSounds;
    public AudioClip downwardAttackSound;
    public AudioClip groundSlamImpactSound;
    public GameObject groundSlamEffect;
    //[SerializeField] private Vector2 soundPitchRange = new Vector2(0.9f, 1.1f);

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Shockwave Settings")]
    [SerializeField] private ShockWaveManager shockWaveManager;

    [Header("Attack Hitbox")]
    [SerializeField] private Collider2D attackHitbox;

    [Header("Sound Settings")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f); // Random pitch range
    [SerializeField] private float pitchShiftDuration = 0.1f; // How long the pitch shift lasts

    // Component references
    private Coroutine currentPitchShiftCoroutine;

    private Animator animator;
    private AudioSource audioSource;
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

        currentAttack = 0;
        timeSinceAttack = attackCooldowns[0];

        if (shockWaveManager == null)
        {
            shockWaveManager = FindObjectOfType<ShockWaveManager>();
            if (shockWaveManager == null)
            {
                Debug.LogWarning("ShockWaveManager not found in scene!");
            }
        }

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
        timeSinceLastAttack += Time.deltaTime;

        if (InputManager.instance.inputControl.Gameplay.Attack.WasPressedThisFrame())
        {
            if (playerController.canGroundSlam && !playerController.isGrounded)
            {
                StartGroundSlam();
            }
            // Only allow ground attacks when grounded
            else if (playerController.isGrounded && CanAttack() && !playerController.isDashing && !isGroundSlamming && !isInGroundSlamImpact)
            {
                PerformGroundAttack();
            }
        }

        // Rest of your Update method remains the same...
        if (InputManager.instance.inputControl.Gameplay.Special.WasPressedThisFrame()
            && !isGroundSlamming
            && !isInGroundSlamImpact
            && playerController.isGrounded)
        {
            StartCoroutine(PerformUpwardLaunch());
        }

        UpdateGroundSlamState();
        UpdateFallingAnimation();
    }

    private void UpdateGroundSlamState()
    {
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

        if (isGroundSlamming && playerController.isGrounded)
        {
            EndGroundSlam();
        }
    }

    private void UpdateFallingAnimation()
    {
        bool shouldBeFalling = isGroundSlamming && !playerController.isGrounded;
        animator.SetBool("FallingGroundSlam", shouldBeFalling);
    }

    private void StartGroundSlam()
    {
        isGroundSlamming = true;
        playerController.canMove = false;
        playerController.canDash = false;
        playerController.canFlip = false;
        playerController.m_wallGrabbing = false;

        playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, -downwardAttackSpeed);

        if (downwardAttackSound != null)
        {
            //audioSource.pitch = Random.Range(soundPitchRange.x, soundPitchRange.y);
            audioSource.PlayOneShot(downwardAttackSound);
        }

        animator.SetBool("FallingGroundSlam", true);
    }

    private void EndGroundSlam()
    {
        isGroundSlamming = false;
        isInGroundSlamImpact = true;
        groundSlamLockTimer = groundSlamLockDuration;

        animator.SetBool("FallingGroundSlam", false);
        animator.SetTrigger("GroundSlamImpact");

        if (groundSlamEffect != null)
        {
            Instantiate(groundSlamEffect, playerController.groundCheck.position, Quaternion.identity);
        }

        if (groundSlamImpactSound != null)
        {
            //audioSource.pitch = Random.Range(soundPitchRange.x, soundPitchRange.y);
            audioSource.PlayOneShot(groundSlamImpactSound);
        }

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

        if (playerController.cameraShake != null)
        {
            playerController.cameraShake.ShakeCamera(playerController.shakeIntensity * 1.5f, playerController.shakeTime * 1.2f);
        }
    }

    private IEnumerator PerformUpwardLaunch()
    {
        if (!playerController.isGrounded || playerController.currentStamina < upwardAttackStaminaCost)
            yield break;
        isLaunching = true;
        isInUpwardAttackRecovery = true;

        playerController.currentStamina -= upwardAttackStaminaCost;
        playerController.UpdateStaminaBar();

        playerController.canMove = false;
        playerController.canDash = false;

        animator.SetTrigger("UpwardLaunch");

        if (upwardLaunchSound != null)
        {
            PlayWithRandomPitch(upwardLaunchSound);
        }

        if (upwardLaunchSound != null)
        {
            //audioSource.pitch = Random.Range(soundPitchRange.x, soundPitchRange.y);
            audioSource.PlayOneShot(upwardLaunchSound);
        }

        yield return new WaitForSeconds(launchDelay);

        playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, 0);
        playerRigidbody.AddForce(Vector2.up * upwardLaunchForce, ForceMode2D.Impulse);

        if (upwardLaunchEffect != null)
        {
            Instantiate(upwardLaunchEffect, transform.position, Quaternion.identity);
        }

        if (playerController.cameraShake != null)
        {
            playerController.cameraShake.ShakeCamera(playerController.shakeIntensity * 0.8f, playerController.shakeTime * 0.8f);
        }

        yield return new WaitForSeconds(0.1f);
        playerController.canMove = true;
        isLaunching = false;

        while (!playerController.isGrounded)
        {
            yield return null;
        }
        isInUpwardAttackRecovery = false;
        playerController.canDash = true;
    }

    private bool CanAttack()
    {
        // Only allow ground attacks when player is grounded
        if (!playerController.isGrounded)
            return false;

        if (timeSinceAttack > comboResetTime)
        {
            currentAttack = 0;
        }

        if (currentAttack == 0)
        {
            return timeSinceAttack >= attackCooldowns[0];
        }

        int cooldownIndex = Mathf.Clamp(currentAttack - 1, 0, attackCooldowns.Length - 1);
        return timeSinceAttack >= attackCooldowns[cooldownIndex];
    }
    private void PerformGroundAttack()
    {
        if (!playerController.isGrounded)
            return;

        currentAttack = (currentAttack % maxComboCount) + 1;
        timeSinceAttack = 0f;
        timeSinceLastAttack = 0f; // Reset the dash cooldown timer

        string triggerName = attackTriggerPrefix + currentAttack;
        animator.SetTrigger(triggerName);
    }
    public bool IsInPostAttackDashCooldown
    {
        get { return timeSinceLastAttack < postAttackDashCooldown; }
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
        // Play attack sound with randomized pitch
        if (attackSounds.Length >= currentAttack && attackSounds[currentAttack - 1] != null)
        {
            PlayWithRandomPitch(attackSounds[currentAttack - 1]);
        }


    }
    private void PlayWithRandomPitch(AudioClip clip)
    {
        // Stop any existing pitch shift
        if (currentPitchShiftCoroutine != null)
        {
            StopCoroutine(currentPitchShiftCoroutine);
        }

        // Set random pitch and play sound
        float randomPitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.pitch = randomPitch;
        audioSource.PlayOneShot(clip);

        // Start coroutine to reset pitch
        currentPitchShiftCoroutine = StartCoroutine(ResetPitch());
    }
    private IEnumerator ResetPitch()
    {
        yield return new WaitForSeconds(pitchShiftDuration);
        audioSource.pitch = 1.0f; // Reset to default pitch
        currentPitchShiftCoroutine = null;
    }

    public void OnAttackEnd()
    {
        playerController.canMove = true;
        playerController.canDash = true;

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    public void OnDashStart()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
        }
    }

    public void OnDashEnd()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    public void OnDisable()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    public void TriggerShockwave()
    {
        shockWaveManager.CallShockwave(true);
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