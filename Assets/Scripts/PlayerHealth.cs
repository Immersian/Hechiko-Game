using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SupanthaPaul;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;
    public bool isInvulnerable;
    [SerializeField] private float invulnerabilityDuration = 1f;
    private float invulnerabilityTimer = 0f;

    [Header("Health Bars")]
    public RectTransform healthBar1;
    private float healthBarFullWidth;

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash damageFlashEffect;
    [SerializeField] private PlayerController playerController;

    [Header("Camera Shake")]
    [SerializeField] public CameraShake cameraShake;
    [SerializeField] public float shakeIntensity = 5;
    [SerializeField] public float shakeTime = 0.1f;

    [Header("Death Animation")]
    [SerializeField] private string deathTrigger = "Die"; // Animation trigger parameter

    [Header("Respawn Settings")]
    [SerializeField] private CrossFade crossFade;
    [SerializeField] private float fadeInDelay = 1f; // Delay before starting fade to black
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDelay = 1f; // Time screen stays black before fading out
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float respawnStabilizeTime = 0.1f; // Time to stabilize player after respawn
    private Vector3 checkpointPosition;

    void Start()
    {
        currentHealth = maxHealth;
        checkpointPosition = transform.position; // Set initial checkpoint to starting position

        if (healthBar1 != null)
        {
            healthBarFullWidth = healthBar1.sizeDelta.x;
            UpdateHealthBars();
        }

        if (damageFlashEffect == null)
        {
            damageFlashEffect = GetComponent<SimpleFlash>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (crossFade == null)
        {
            crossFade = FindObjectOfType<CrossFade>();
        }
    }

    void Update()
    {
        if (isDead) return; // Don't update invulnerability if dead

        if (invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
            isInvulnerable = true;
        }
        else
        {
            isInvulnerable = false;
        }
    }

    private void UpdateHealthBars()
    {
        float healthPercentage = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector2 newSize = new Vector2(healthBarFullWidth * healthPercentage, healthBar1.sizeDelta.y);
        healthBar1.sizeDelta = newSize;
    }

    public event Action<int> OnTakeDamage;

    public void TakeDamage(int damageAmount, GameObject damageSource = null)
    {
        if (isDead) return;

        bool isSpecialAttack = (damageSource != null && damageSource.CompareTag("SpecialAttack"));

        // Only check invulnerability if it's NOT a special attack AND we're not currently invulnerable
        // Special attacks bypass dash invulnerability but not post-hit invulnerability
        if (isInvulnerable) return;

        // Interrupt dash if active
        if (playerController != null)
        {
            playerController.InterruptDash();
            playerController.InterruptHealing();
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        // ALWAYS apply invulnerability after being hit, even from special attacks
        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        OnTakeDamage?.Invoke(damageAmount);

        if (damageFlashEffect != null)
        {
            damageFlashEffect.CallHurtFlash();
        }

        UpdateHealthBars();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (isDead) return; // Can't heal if dead

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        UpdateHealthBars();
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died!");

        // Disable the entire PlayerController component first
        if (playerController != null)
        {
            playerController.enabled = false; // This stops all controller logic
        }

        // Play death animation
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false; // Prevent animation movement
            animator.SetTrigger(deathTrigger);
        }

        // Disable any attack components
        PlayerAttack playerAttack = GetComponentInChildren<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }

        // Disable parry/blocking
        ParryScript parryScript = GetComponentInChildren<ParryScript>();
        if (parryScript != null)
        {
            parryScript.enabled = false;
        }

        // Stop all movement - Use Kinematic instead of Static
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Better for controlled objects
        }

        // Disable colliders to prevent further damage/interaction
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        // Start the respawn sequence
        StartCoroutine(RespawnSequence());

        // Trigger any death events
        OnDeath?.Invoke();
    }

    private IEnumerator RespawnSequence()
    {
        // Wait for the configured delay before starting fade
        yield return new WaitForSeconds(fadeInDelay);

        // Fade in to black
        if (crossFade != null)
        {
            yield return crossFade.FadeIn(fadeInDuration);
        }

        // Wait while screen is completely black
        yield return new WaitForSeconds(fadeOutDelay);

        // Reset player at checkpoint (teleport while screen is black)
        RespawnAtCheckpoint();

        // Wait a moment for physics to stabilize and player to be grounded
        yield return new WaitForSeconds(respawnStabilizeTime);

        // Ensure player is properly positioned and not falling
        StabilizePlayer();

        // Fade out from black
        if (crossFade != null)
        {
            yield return crossFade.FadeOut(fadeOutDuration);
        }
    }

    // Add this to your PlayerHealth class (around line 20, with other events)
    public static event Action OnPlayerRespawn;

    // Then in the RespawnAtCheckpoint method, trigger the event:
    private void RespawnAtCheckpoint()
    {
        // Reset player position to checkpoint (this happens while screen is black)
        transform.position = checkpointPosition;

        // Reset health
        currentHealth = maxHealth;
        UpdateHealthBars();

        // REFILL POTIONS ON RESPAWN
        if (playerController != null)
        {
            playerController.RefillAllPotionsWithFade();
        }

        // Re-enable everything
        isDead = false;

        // Re-enable colliders first
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }

        // Re-enable physics BEFORE enabling the controller
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true; // Ensure physics simulation is enabled
        }

        // Re-enable the PlayerController component
        if (playerController != null)
        {
            playerController.ResetControllerState(); // Add this line
            playerController.enabled = true;
        }

        // Re-enable attack components
        PlayerAttack playerAttack = GetComponentInChildren<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.enabled = true;
        }

        // Re-enable parry/blocking
        ParryScript parryScript = GetComponentInChildren<ParryScript>();
        if (parryScript != null)
        {
            parryScript.enabled = true;
        }

        // Re-enable root motion
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.ResetTrigger(deathTrigger);
            animator.Play("Idle", 0, 0f);
        }

        // Trigger respawn event for enemies
        OnPlayerRespawn?.Invoke();
    }

    private void StabilizePlayer()
    {
        // Ensure player is not falling or moving
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        // Force ground check to ensure player is properly grounded
        if (playerController != null)
        {
            // You might need to add a method in PlayerController to force ground detection
            // or manually check if player is grounded here
        }
    }

    // Method to set new checkpoint position (call this from your InteractiveObject script)
    public void SetCheckpoint(Vector3 newCheckpointPosition)
    {
        checkpointPosition = newCheckpointPosition;
        Debug.Log($"Checkpoint set at: {checkpointPosition}");
    }

    // Event for other scripts to listen to player death
    public event Action OnDeath;

    // Add these methods to your PlayerHealth class

    public void HealToFull()
    {
        if (isDead) return;

        currentHealth = maxHealth;
        UpdateHealthBars();
        Debug.Log("Player healed to full health!");
    }
    // Add this method to your PlayerHealth class
    public static void TriggerRespawnEvent()
    {
        OnPlayerRespawn?.Invoke();
    }
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }
}