using UnityEngine;
using UnityEngine.UI;
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
    public RectTransform healthBar1; // Main green health bar
    public RectTransform healthBar2; // Red damage indicator bar (secondary)
    public Image healthBar2Image; // The Image component of the red bar
    private float healthBarFullWidth;
    private int previousHealth; // Track previous health to calculate damage
    private Coroutine healthBarFadeCoroutine;
    private bool isHealthBarFading = false;
    private float previousHealthPercentage = 1f; // Track previous health as percentage

    [Header("Damage Indicator Settings")]
    [SerializeField] private float damageIndicatorDelay = 0.5f; // Delay before fading starts
    [SerializeField] private float fadeOutDuration = 1.0f; // How long it takes to fade out completely
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Controls fade timing

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash damageFlashEffect;
    [SerializeField] private PlayerController playerController;

    [Header("Camera Shake")]
    [SerializeField] public CameraShake cameraShake;
    [SerializeField] public float shakeIntensity = 5;
    [SerializeField] public float shakeTime = 0.1f;

    [Header("Hurt Sound")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float hurtSoundVolume = 0.8f;
    [SerializeField] private bool enablePitchShift = true;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Crit Hit Sound")]
    [SerializeField] private AudioClip critHitSound;
    [SerializeField] private AudioSource critAudioSource;
    [SerializeField] private float critSoundVolume = 0.7f;
    [SerializeField] private bool critEnablePitchShift = true;
    [SerializeField] private float critMinPitch = 0.8f;
    [SerializeField] private float critMaxPitch = 1.2f;
    [SerializeField] private float critFadeInDuration = 0.5f;
    [SerializeField] private float critFadeOutDuration = 1.0f;
    private Coroutine critFadeCoroutine;
    private bool isCritSoundPlaying = false;
    private float originalCritVolume = 0f;

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundVolume = 1.0f;
    [SerializeField] private bool deathEnablePitchShift = true;
    [SerializeField] private float deathMinPitch = 0.7f;
    [SerializeField] private float deathMaxPitch = 1.0f;

    [Header("Rumble Settings")]
    [SerializeField] private float damageRumbleLowFrequency = 0.8f;
    [SerializeField] private float damageRumbleHighFrequency = 0.9f;
    [SerializeField] private float damageRumbleDuration = 0.3f;
    [SerializeField] private float deathRumbleLowFrequency = 1f;
    [SerializeField] private float deathRumbleHighFrequency = 1f;
    [SerializeField] private float deathRumbleDuration = 1f;

    [Header("Death Animation")]
    [SerializeField] private string deathTrigger = "Die"; // Animation trigger parameter

    [Header("Respawn Settings")]
    [SerializeField] private CrossFade crossFade;
    [SerializeField] private float fadeInDelay = 1f; // Delay before starting fade to black
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDelay = 1f; // Time screen stays black before fading out
    [SerializeField] private float fadeOutDurationRespawn = 0.5f;
    [SerializeField] private float respawnStabilizeTime = 0.1f; // Time to stabilize player after respawn
    private Vector3 checkpointPosition;

    [Header("Low Health Indicator")]
    [SerializeField] private GameObject lowHealthIndicator;
    [SerializeField] private int lowHealthThreshold = 30;
    [SerializeField] private Animator lowHealthAnimator;
    private bool lowHealthIndicatorHidden = false; // Track if we manually hid it

    void Start()
    {
        currentHealth = maxHealth;
        previousHealth = maxHealth; // Initialize previous health
        previousHealthPercentage = 1f; // Start at 100%
        checkpointPosition = transform.position; // Set initial checkpoint to starting position

        if (healthBar1 != null)
        {
            healthBarFullWidth = healthBar1.sizeDelta.x;
            UpdateHealthBars();
        }

        // Initialize secondary health bar
        if (healthBar2 != null)
        {
            // Make sure red bar starts at full width (100% health)
            healthBar2.sizeDelta = new Vector2(healthBarFullWidth, healthBar2.sizeDelta.y);

            // Get the Image component if not assigned
            if (healthBar2Image == null)
            {
                healthBar2Image = healthBar2.GetComponent<Image>();
            }

            // Start with red bar completely transparent
            if (healthBar2Image != null)
            {
                Color color = healthBar2Image.color;
                color.a = 0f;
                healthBar2Image.color = color;
            }
        }

        // Get or add AudioSource component for hurt sound
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

        // Get or add AudioSource component for crit sound
        if (critAudioSource == null)
        {
            critAudioSource = gameObject.AddComponent<AudioSource>();
            critAudioSource.playOnAwake = false;
            critAudioSource.loop = true; // Loop the crit sound
            critAudioSource.spatialBlend = 0f;
            critAudioSource.volume = 0f; // Start at 0 volume
            originalCritVolume = critSoundVolume;
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

        // Initialize low health indicator state
        UpdateLowHealthIndicator();
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

        // Update main health bar (green) to current health
        Vector2 newSize = new Vector2(healthBarFullWidth * healthPercentage, healthBar1.sizeDelta.y);
        healthBar1.sizeDelta = newSize;

        // Update red bar to show where health WAS before damage
        // Red bar stays at previous health percentage
        if (healthBar2 != null)
        {
            float redBarWidth = healthBarFullWidth * previousHealthPercentage;
            healthBar2.sizeDelta = new Vector2(redBarWidth, healthBar2.sizeDelta.y);
        }
    }

    private void UpdateDamageIndicator()
    {
        if (healthBar2Image == null) return;

        // Calculate previous health as percentage
        previousHealthPercentage = Mathf.Clamp01((float)previousHealth / maxHealth);

        // Update red bar width to show previous health level
        UpdateHealthBars();

        // Make red bar fully visible
        Color color = healthBar2Image.color;
        color.a = 1f;
        healthBar2Image.color = color;
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

        // Store previous health before taking damage
        previousHealth = currentHealth;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        // ALWAYS apply invulnerability after being hit, even from special attacks
        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        // Visual and audio feedback
        cameraShake.ShakeCamera(shakeIntensity, shakeTime);

        // Play hurt sound with pitch shifting
        PlayHurtSound();

        OnTakeDamage?.Invoke(damageAmount);

        if (damageFlashEffect != null)
        {
            damageFlashEffect.CallHurtFlash();
        }

        // RUMBLE FEEDBACK - Call rumble when taking damage
        TriggerDamageRumble();

        UpdateHealthBars();
        UpdateLowHealthIndicator(); // Check if we need to show/hide low health indicator

        // Update the red damage indicator
        UpdateDamageIndicator();

        // Start the damage indicator fade out process
        StartDamageIndicatorFadeOut();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHurtSound()
    {
        if (hurtSound == null) return;

        float pitch = 1.0f;

        // Apply pitch shift if enabled
        if (enablePitchShift)
        {
            pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        }

        // Play the sound with pitch shifting
        if (audioSource != null)
        {
            // Store original pitch
            float originalPitch = audioSource.pitch;

            // Set new pitch
            audioSource.pitch = pitch;

            // Play sound
            audioSource.PlayOneShot(hurtSound, hurtSoundVolume);

            // Reset pitch to original
            StartCoroutine(ResetPitchAfterSound(originalPitch));
        }
        else
        {
            // Create temporary audio source if main one doesn't exist
            CreateTemporaryAudioSource(hurtSound, hurtSoundVolume, pitch);
        }
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

    private void CreateTemporaryAudioSource(AudioClip clip, float volume, float pitch)
    {
        // Create a new GameObject for the temporary audio
        GameObject tempAudioObj = new GameObject("TempHurtSound");
        AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();

        // Configure the temporary AudioSource
        tempAudioSource.clip = clip;
        tempAudioSource.volume = volume;
        tempAudioSource.pitch = pitch;
        tempAudioSource.spatialBlend = 0f; // 2D sound

        // Play and destroy after completion
        tempAudioSource.Play();
        Destroy(tempAudioObj, clip.length + 0.1f);
    }

    private void StartDamageIndicatorFadeOut()
    {
        // If there's already a fade in progress, stop it
        if (isHealthBarFading && healthBarFadeCoroutine != null)
        {
            StopCoroutine(healthBarFadeCoroutine);
            isHealthBarFading = false;
        }

        // Start a new fade coroutine
        healthBarFadeCoroutine = StartCoroutine(DamageIndicatorFadeOut());
    }

    private IEnumerator DamageIndicatorFadeOut()
    {
        if (healthBar2Image == null) yield break;

        isHealthBarFading = true;

        // Get starting alpha
        float startAlpha = healthBar2Image.color.a;

        // Wait for the delay before starting the fade out
        yield return new WaitForSeconds(damageIndicatorDelay);

        // Fade out the red bar completely
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float curveT = fadeOutCurve.Evaluate(t);

            // Calculate current alpha (from startAlpha down to 0)
            float currentAlpha = Mathf.Lerp(startAlpha, 0f, curveT);

            // Apply alpha to red bar
            Color color = healthBar2Image.color;
            color.a = currentAlpha;
            healthBar2Image.color = color;

            yield return null;
        }

        // Ensure red bar is completely transparent
        Color finalColor = healthBar2Image.color;
        finalColor.a = 0f;
        healthBar2Image.color = finalColor;

        // Update previous health to match current health after fade completes
        previousHealth = currentHealth;
        previousHealthPercentage = Mathf.Clamp01((float)previousHealth / maxHealth);

        // Update red bar width to match new previous health (which is now current health)
        if (healthBar2 != null)
        {
            float redBarWidth = healthBarFullWidth * previousHealthPercentage;
            healthBar2.sizeDelta = new Vector2(redBarWidth, healthBar2.sizeDelta.y);
        }

        isHealthBarFading = false;
    }

    private void UpdateLowHealthIndicator()
    {
        bool isLowHealth = currentHealth <= lowHealthThreshold;

        if (lowHealthAnimator != null && !lowHealthIndicatorHidden)
        {
            lowHealthAnimator.SetBool("IsLowHealth", isLowHealth);

            // Keep the GameObject active at all times, let Animator control visibility
            if (lowHealthIndicator != null && !lowHealthIndicator.activeSelf)
            {
                lowHealthIndicator.SetActive(true);
            }
        }
        else if (lowHealthIndicator != null && !lowHealthIndicatorHidden)
        {
            // Fallback to simple on/off if no animator
            bool shouldBeActive = isLowHealth;
            if (lowHealthIndicator.activeSelf != shouldBeActive)
            {
                lowHealthIndicator.SetActive(shouldBeActive);
            }
        }

        // Handle crit hit sound based on low health state
        if (isLowHealth && !isCritSoundPlaying && critHitSound != null && !isDead)
        {
            // Start playing crit hit sound with fade in
            StartCritHitSound();
        }
        else if (!isLowHealth && isCritSoundPlaying)
        {
            // Fade out crit hit sound when healing above threshold
            StopCritHitSound();
        }
    }

    private void StartCritHitSound()
    {
        if (critHitSound == null || isDead) return;

        // Stop any existing fade coroutine
        if (critFadeCoroutine != null)
        {
            StopCoroutine(critFadeCoroutine);
        }

        // Apply pitch shift for crit sound
        if (critEnablePitchShift)
        {
            float pitch = UnityEngine.Random.Range(critMinPitch, critMaxPitch);
            critAudioSource.pitch = pitch;
        }
        else
        {
            critAudioSource.pitch = 1.0f;
        }

        // Set clip and start playing
        critAudioSource.clip = critHitSound;
        critAudioSource.volume = 0f; // Start at 0 volume
        critAudioSource.Play();

        // Start fade in
        critFadeCoroutine = StartCoroutine(FadeCritSound(0f, critSoundVolume, critFadeInDuration));
        isCritSoundPlaying = true;
    }

    private void StopCritHitSound()
    {
        if (!isCritSoundPlaying || critAudioSource == null) return;

        // Stop any existing fade coroutine
        if (critFadeCoroutine != null)
        {
            StopCoroutine(critFadeCoroutine);
        }

        // Start fade out
        critFadeCoroutine = StartCoroutine(FadeCritSound(critAudioSource.volume, 0f, critFadeOutDuration, true));
        isCritSoundPlaying = false;
    }

    private IEnumerator FadeCritSound(float startVolume, float targetVolume, float duration, bool stopAfterFade = false)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentVolume = Mathf.Lerp(startVolume, targetVolume, t);

            if (critAudioSource != null)
            {
                critAudioSource.volume = currentVolume;
            }

            yield return null;
        }

        // Ensure target volume is reached
        if (critAudioSource != null)
        {
            critAudioSource.volume = targetVolume;
        }

        // Stop audio if fading to 0 and stopAfterFade is true
        if (stopAfterFade && targetVolume == 0f && critAudioSource != null)
        {
            critAudioSource.Stop();
        }
    }

    private void PlayDeathSound()
    {
        // Stop crit sound if playing (we'll restart it with different pitch)
        if (isCritSoundPlaying)
        {
            StopCritHitSound();
        }

        // Play crit hit sound with lower pitch
        if (critHitSound != null)
        {
            float critPitch = 1.0f;
            if (critEnablePitchShift)
            {
                // Lower pitch for death (0.8x multiplier as requested)
                critPitch = UnityEngine.Random.Range(critMinPitch * 0.8f, critMaxPitch * 0.8f);
            }

            // Play crit hit sound immediately at full volume
            if (critAudioSource != null)
            {
                critAudioSource.pitch = critPitch;
                critAudioSource.volume = critSoundVolume;
                critAudioSource.clip = critHitSound;
                critAudioSource.Play();
            }
        }

        // Play additional death sound at the same time using main audio source
        if (deathSound != null && audioSource != null)
        {
            float deathPitch = 1.0f;
            if (deathEnablePitchShift)
            {
                deathPitch = UnityEngine.Random.Range(deathMinPitch, deathMaxPitch);
            }

            // Use main audio source for death sound (plays alongside crit sound)
            audioSource.pitch = deathPitch;
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
        }
    }

    private void StopDeathSound()
    {
        // Fade out crit sound on respawn
        if (critAudioSource != null && critAudioSource.isPlaying)
        {
            if (critFadeCoroutine != null)
            {
                StopCoroutine(critFadeCoroutine);
            }
            critFadeCoroutine = StartCoroutine(FadeCritSound(critAudioSource.volume, 0f, critFadeOutDuration, true));
        }

        // Reset audio source pitch
        if (audioSource != null)
        {
            audioSource.pitch = 1.0f;
        }
    }

    private void TriggerDamageRumble()
    {
        // Check if RumbleManager exists and call rumble
        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(
                damageRumbleLowFrequency,
                damageRumbleHighFrequency,
                damageRumbleDuration
            );
        }
    }

    private void TriggerDeathRumble()
    {
        // Check if RumbleManager exists and call death rumble
        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(
                deathRumbleLowFrequency,
                deathRumbleHighFrequency,
                deathRumbleDuration
            );
        }
    }

    public void Heal(int healAmount)
    {
        if (isDead) return; // Can't heal if dead

        // Update previous health before healing
        previousHealth = currentHealth;
        previousHealthPercentage = Mathf.Clamp01((float)previousHealth / maxHealth);

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        UpdateHealthBars();
        UpdateLowHealthIndicator(); // This will handle fading out crit sound if needed

        // When healing, immediately hide red bar and update previous health
        if (healthBar2Image != null)
        {
            Color color = healthBar2Image.color;
            color.a = 0f;
            healthBar2Image.color = color;

            // Update red bar width to match new health
            previousHealth = currentHealth;
            previousHealthPercentage = Mathf.Clamp01((float)previousHealth / maxHealth);

            if (healthBar2 != null)
            {
                float redBarWidth = healthBarFullWidth * previousHealthPercentage;
                healthBar2.sizeDelta = new Vector2(redBarWidth, healthBar2.sizeDelta.y);
            }
        }

        // Stop any ongoing fade since we're healing
        if (isHealthBarFading && healthBarFadeCoroutine != null)
        {
            StopCoroutine(healthBarFadeCoroutine);
            isHealthBarFading = false;
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died!");

        // Play death sound (crit hit + additional death sound)
        PlayDeathSound();

        // Trigger death rumble
        TriggerDeathRumble();

        // Stop any health bar fade coroutine
        if (healthBarFadeCoroutine != null)
        {
            StopCoroutine(healthBarFadeCoroutine);
            isHealthBarFading = false;
        }

        // Hide red bar on death
        if (healthBar2Image != null)
        {
            Color color = healthBar2Image.color;
            color.a = 0f;
            healthBar2Image.color = color;
        }

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

        // DON'T hide low health indicator when dead (only on respawn)
        // Keep it visible to show the player they died at low health

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
            yield return crossFade.FadeOut(fadeOutDurationRespawn);
        }
    }

    public static event Action OnPlayerRespawn;

    private void RespawnAtCheckpoint()
    {
        // Stop death sound on respawn
        StopDeathSound();

        // Reset player position to checkpoint (this happens while screen is black)
        transform.position = checkpointPosition;

        // Reset health
        currentHealth = maxHealth;
        previousHealth = maxHealth; // Reset previous health too
        previousHealthPercentage = 1f; // Reset to 100%
        UpdateHealthBars();

        // Reset low health indicator hidden flag and hide the indicator
        lowHealthIndicatorHidden = false;
        if (lowHealthIndicator != null)
        {
            lowHealthIndicator.SetActive(false);
        }

        // Update low health indicator (will hide it since health is full)
        UpdateLowHealthIndicator();

        // Hide red bar completely and reset its width
        if (healthBar2Image != null)
        {
            Color color = healthBar2Image.color;
            color.a = 0f;
            healthBar2Image.color = color;

            // Reset red bar to full width
            if (healthBar2 != null)
            {
                healthBar2.sizeDelta = new Vector2(healthBarFullWidth, healthBar2.sizeDelta.y);
            }
        }

        // Stop any fade coroutine
        if (healthBarFadeCoroutine != null)
        {
            StopCoroutine(healthBarFadeCoroutine);
            isHealthBarFading = false;
        }

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

    public void HealToFull()
    {
        if (isDead) return;

        previousHealth = currentHealth;
        previousHealthPercentage = Mathf.Clamp01((float)previousHealth / maxHealth);
        currentHealth = maxHealth;

        UpdateHealthBars();

        // Reset low health indicator hidden flag
        lowHealthIndicatorHidden = false;
        UpdateLowHealthIndicator(); // Ensure indicator is off after full heal

        // Immediately hide red bar when fully healed
        if (healthBar2Image != null)
        {
            Color color = healthBar2Image.color;
            color.a = 0f;
            healthBar2Image.color = color;

            // Update red bar width to match full health
            previousHealth = currentHealth;
            previousHealthPercentage = 1f;

            if (healthBar2 != null)
            {
                healthBar2.sizeDelta = new Vector2(healthBarFullWidth, healthBar2.sizeDelta.y);
            }
        }

        // Stop any fade
        if (healthBarFadeCoroutine != null)
        {
            StopCoroutine(healthBarFadeCoroutine);
            isHealthBarFading = false;
        }

        // Fade out crit sound if playing
        if (isCritSoundPlaying)
        {
            StopCritHitSound();
        }

        Debug.Log("Player healed to full health!");
    }

    public static void TriggerRespawnEvent()
    {
        OnPlayerRespawn?.Invoke();
    }

    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    public void TeleportToCheckpoint(Vector3 checkpointPosition)
    {
        if (isDead) return;

        StartCoroutine(TeleportSequence(checkpointPosition));
    }

    private IEnumerator TeleportSequence(Vector3 checkpointPosition)
    {
        // Disable player movement
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;

            // Stop any movement immediately
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        // Find CrossFade component for the fade effect
        CrossFade crossFade = FindObjectOfType<CrossFade>();
        if (crossFade != null)
        {
            // Fade to black
            yield return crossFade.FadeIn(0.5f);
        }

        // Teleport player to the checkpoint position
        transform.position = checkpointPosition;

        // Set this as the new checkpoint
        SetCheckpoint(checkpointPosition);

        // Wait briefly while screen is black
        yield return new WaitForSeconds(0.5f);

        // Fade back in
        if (crossFade != null)
        {
            yield return crossFade.FadeOut(0.5f);
        }

        // Re-enable player movement
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log($"Teleported to checkpoint at: {checkpointPosition}");
    }

    // Public method to adjust fade settings at runtime
    public void SetDamageIndicatorSettings(float delay, float duration)
    {
        damageIndicatorDelay = delay;
        fadeOutDuration = duration;
    }

    // Public method to set hurt sound at runtime
    public void SetHurtSound(AudioClip sound, float volume = 0.8f)
    {
        hurtSound = sound;
        hurtSoundVolume = volume;
    }

    // Public method to configure pitch shift settings at runtime
    public void SetPitchShiftSettings(bool enableShift, float min = 0.9f, float max = 1.1f)
    {
        enablePitchShift = enableShift;
        minPitch = min;
        maxPitch = max;
    }

    // Public method to set crit hit sound at runtime
    public void SetCritHitSound(AudioClip sound, float volume = 0.7f)
    {
        critHitSound = sound;
        critSoundVolume = volume;
        originalCritVolume = volume;
    }

    // Public method to configure crit pitch shift settings at runtime
    public void SetCritPitchShiftSettings(bool enableShift, float min = 0.8f, float max = 1.2f)
    {
        critEnablePitchShift = enableShift;
        critMinPitch = min;
        critMaxPitch = max;
    }

    // Public method to set death sound at runtime
    public void SetDeathSound(AudioClip sound, float volume = 1.0f)
    {
        deathSound = sound;
        deathSoundVolume = volume;
    }

    // Public method to configure death pitch shift settings at runtime
    public void SetDeathPitchShiftSettings(bool enableShift, float min = 0.7f, float max = 1.0f)
    {
        deathEnablePitchShift = enableShift;
        deathMinPitch = min;
        deathMaxPitch = max;
    }

    // Public method to manually trigger crit sound (for testing)
    public void TriggerCritSound()
    {
        StartCritHitSound();
    }

    // Public method to manually stop crit sound (for testing)
    public void StopCritSound()
    {
        StopCritHitSound();
    }
}