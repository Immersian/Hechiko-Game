using SupanthaPaul;
using UnityEngine;

public class DashRefresh : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private bool canRefreshAerialDash = true;
    [SerializeField] private float checkRadius = 0.5f;
    [SerializeField] private bool refreshOnlyWhenNeeded = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableSpinning = true;
    [SerializeField] private float spinSpeed = 90f; // Degrees per second
    [SerializeField] private bool spinOnlyWhenActive = true;
    [SerializeField] private bool reverseSpinOnRespawn = false;
    [SerializeField] private float respawnSpinBoost = 2f;
    [SerializeField] private float respawnBoostDuration = 0.5f;
    private float currentSpinSpeed;
    private float respawnBoostTimer = 0f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private float effectDuration = 1f;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip respawnSound;
    [SerializeField] private float collectSoundVolume = 0.8f;
    [SerializeField] private float respawnSoundVolume = 0.6f;
    [SerializeField] private AudioSource audioSource;

    [Header("Pitch Shift Settings")]
    [SerializeField] private bool enablePitchShift = true;
    [SerializeField] private float basePitch = 1f;
    [SerializeField] private float collectPitchMin = 0.9f;
    [SerializeField] private float collectPitchMax = 1.1f;
    [SerializeField] private float respawnPitchMin = 0.8f;
    [SerializeField] private float respawnPitchMax = 1.0f;
    [SerializeField] private bool randomizeCollectPitch = true;
    [SerializeField] private bool randomizeRespawnPitch = true;
    [SerializeField] private bool useProgressivePitch = false;
    [SerializeField] private float progressivePitchIncrement = 0.05f;
    [SerializeField] private float maxProgressivePitch = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string respawnTrigger = "Respawn";

    private Collider2D refreshCollider;
    private bool isActive = true;
    private float cooldownTimer = 0f;
    private LayerMask playerLayer;
    private SimpleFlash flashEffect;
    private int collectCount = 0;
    private float currentCollectPitch = 1f;
    private float currentRespawnPitch = 1f;

    private void Awake()
    {
        refreshCollider = GetComponent<Collider2D>();
        playerLayer = LayerMask.GetMask("Player");
        flashEffect = GetComponentInChildren<SimpleFlash>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Set up AudioSource if not assigned
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

        // Initialize pitch
        currentCollectPitch = basePitch;
        currentRespawnPitch = basePitch;
        ApplyCurrentPitch();

        // Initialize spin speed
        currentSpinSpeed = spinSpeed;
    }

    private void Update()
    {
        if (!isActive)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                Respawn();
            }
        }

        // Handle respawn spin boost
        if (respawnBoostTimer > 0f)
        {
            respawnBoostTimer -= Time.deltaTime;
            if (respawnBoostTimer <= 0f)
            {
                currentSpinSpeed = spinSpeed; // Return to normal speed
            }
        }

        // Rotate the object
        if (enableSpinning && (!spinOnlyWhenActive || isActive))
        {
            RotateObject();
        }
    }

    private void RotateObject()
    {
        // Calculate rotation based on current spin speed
        float rotationAmount = currentSpinSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, rotationAmount);
    }

    private void FixedUpdate()
    {
        if (isActive)
        {
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            TryCollect(hit.GetComponent<PlayerController>());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive) TryCollect(other.GetComponent<PlayerController>());
    }

    private void TryCollect(PlayerController player)
    {
        if (player == null || !player.isCurrentlyPlayable) return;

        // Check if we should refresh based on conditions
        bool shouldRefresh = true;
        if (refreshOnlyWhenNeeded)
        {
            shouldRefresh = (!player.isGrounded && canRefreshAerialDash && player.m_hasDashedInAir) ||
                          (!player.CanDash() && (player.isGrounded || canRefreshAerialDash));
        }
        if (shouldRefresh)
        {
            Collect(player);
        }
    }

    private void Collect(PlayerController player)
    {
        // Play collect sound with pitch
        PlayCollectSound();

        // Increment collect count for progressive pitch
        if (useProgressivePitch)
        {
            collectCount++;
        }

        // Refresh the player's dash
        player.RefreshDash();

        // Search for SimpleFlash in player or its children
        SimpleFlash flash = player.GetComponentInChildren<SimpleFlash>();
        if (flash != null)
        {
            flash.CallDashFlash();
        }

        // Spawn collect effect
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // Disable the refresh object
        isActive = false;
        cooldownTimer = respawnTime;
        refreshCollider.enabled = false;

        // Trigger animation
        if (animator != null)
        {
            animator.SetTrigger(hitTrigger);
        }
    }

    private void Respawn()
    {
        isActive = true;
        refreshCollider.enabled = true;

        // Apply spin effects on respawn
        if (enableSpinning)
        {
            if (reverseSpinOnRespawn)
            {
                spinSpeed = -spinSpeed; // Reverse spin direction
            }

            if (respawnSpinBoost > 1f)
            {
                currentSpinSpeed = spinSpeed * respawnSpinBoost;
                respawnBoostTimer = respawnBoostDuration;
            }
        }

        // Play respawn sound with pitch
        PlayRespawnSound();

        // Trigger animation
        if (animator != null)
        {
            animator.SetTrigger(respawnTrigger);
        }
    }

    private void PlayCollectSound()
    {
        if (collectSound != null && audioSource != null)
        {
            // Calculate pitch for collect sound
            if (enablePitchShift)
            {
                if (useProgressivePitch)
                {
                    // Progressive pitch: increases with each collection
                    float progressivePitch = basePitch + (collectCount * progressivePitchIncrement);
                    currentCollectPitch = Mathf.Clamp(progressivePitch, basePitch, maxProgressivePitch);
                }
                else if (randomizeCollectPitch)
                {
                    // Random pitch within range
                    currentCollectPitch = Random.Range(collectPitchMin, collectPitchMax);
                }
                else
                {
                    // Fixed pitch variation
                    currentCollectPitch = Mathf.PingPong(Time.time, collectPitchMax - collectPitchMin) + collectPitchMin;
                }

                ApplyCurrentPitch(currentCollectPitch);
            }

            audioSource.PlayOneShot(collectSound, collectSoundVolume);
        }
        else if (collectSound == null)
        {
            Debug.LogWarning("No collect sound assigned to DashRefresh!", this);
        }
    }

    private void PlayRespawnSound()
    {
        if (respawnSound != null && audioSource != null)
        {
            // Calculate pitch for respawn sound
            if (enablePitchShift)
            {
                if (randomizeRespawnPitch)
                {
                    // Random pitch within range
                    currentRespawnPitch = Random.Range(respawnPitchMin, respawnPitchMax);
                }
                else
                {
                    // Fixed pitch variation
                    currentRespawnPitch = Mathf.PingPong(Time.time, respawnPitchMax - respawnPitchMin) + respawnPitchMin;
                }

                ApplyCurrentPitch(currentRespawnPitch);
            }

            audioSource.PlayOneShot(respawnSound, respawnSoundVolume);
        }
        else if (respawnSound == null)
        {
            Debug.LogWarning("No respawn sound assigned to DashRefresh!", this);
        }
    }

    private void ApplyCurrentPitch(float pitch = 1f)
    {
        if (audioSource != null)
        {
            audioSource.pitch = Mathf.Clamp(pitch, 0.01f, 3f); // Clamp to reasonable values
        }
    }

    // Animation Events
    public void OnHitAnimationComplete()
    {
        // Animation ends on last frame automatically
    }

    public void OnRespawnAnimationComplete()
    {
        // Return to idle state after respawn completes
        if (animator != null)
        {
            animator.Play("Idle");
        }
    }

    // Animation Event methods for precise sound timing
    public void PlayCollectSoundEvent()
    {
        PlayCollectSound();
    }

    public void PlayRespawnSoundEvent()
    {
        PlayRespawnSound();
    }

    // Animation Event with specific pitch
    public void PlayCollectSoundWithPitch(float pitch)
    {
        if (enablePitchShift)
        {
            ApplyCurrentPitch(Mathf.Clamp(pitch, collectPitchMin, collectPitchMax));
        }
        PlayCollectSound();
    }

    public void PlayRespawnSoundWithPitch(float pitch)
    {
        if (enablePitchShift)
        {
            ApplyCurrentPitch(Mathf.Clamp(pitch, respawnPitchMin, respawnPitchMax));
        }
        PlayRespawnSound();
    }

    // Rotation control methods
    public void SetSpinSpeed(float newSpeed)
    {
        spinSpeed = newSpeed;
        if (!(respawnBoostTimer > 0f))
        {
            currentSpinSpeed = newSpeed;
        }
    }

    public void SetSpinningEnabled(bool enabled)
    {
        enableSpinning = enabled;
    }

    public void SetSpinOnlyWhenActive(bool onlyWhenActive)
    {
        spinOnlyWhenActive = onlyWhenActive;
    }

    public void ApplySpinBoost(float multiplier, float duration)
    {
        currentSpinSpeed = spinSpeed * multiplier;
        respawnBoostTimer = duration;
    }

    public void ResetRotation()
    {
        transform.rotation = Quaternion.identity;
    }

    // Optional: Public method to test collect sound
    public void TestCollectSound()
    {
        PlayCollectSound();
    }

    // Optional: Public method to test respawn sound
    public void TestRespawnSound()
    {
        PlayRespawnSound();
    }

    // Optional: Reset progressive pitch count
    public void ResetProgressivePitch()
    {
        collectCount = 0;
        currentCollectPitch = basePitch;
    }

    // Optional: Configure pitch settings at runtime
    public void ConfigurePitchSettings(
        bool enable,
        float newBasePitch = 1f,
        float newCollectMin = 0.9f,
        float newCollectMax = 1.1f,
        float newRespawnMin = 0.8f,
        float newRespawnMax = 1.0f,
        bool progressive = false,
        float increment = 0.05f,
        float maxProgressive = 1.5f)
    {
        enablePitchShift = enable;
        basePitch = newBasePitch;
        collectPitchMin = newCollectMin;
        collectPitchMax = newCollectMax;
        respawnPitchMin = newRespawnMin;
        respawnPitchMax = newRespawnMax;
        useProgressivePitch = progressive;
        progressivePitchIncrement = increment;
        maxProgressivePitch = maxProgressive;

        // Clamp current pitches to new ranges
        currentCollectPitch = Mathf.Clamp(currentCollectPitch, collectPitchMin, collectPitchMax);
        currentRespawnPitch = Mathf.Clamp(currentRespawnPitch, respawnPitchMin, respawnPitchMax);
    }

    // Optional: Get current collect count (useful for UI or achievements)
    public int GetCollectCount()
    {
        return collectCount;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}