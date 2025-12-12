using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakaBossHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 1000;
    public int currentHealth;
    public bool isDead = false;
    public bool isInvulnerable;
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private float invulnerabilityTimer = 0f;

    [Header("Health Bar")]
    public RectTransform bossHealthBar;
    private float healthBarFullWidth;

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash flashEffect;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float stunShakeIntensity = 3f;
    [SerializeField] private float stunShakeTime = 0.3f;
    [SerializeField] private float animationShakeIntensity = 1f;
    [SerializeField] private float animationShakeTime = 0.2f;

    [Header("Hit Sound Effects")]
    [SerializeField] private AudioClip[] hitSounds; // Array of hit sound effects
    [SerializeField] private float hitSoundVolume = 0.8f;
    [SerializeField] private bool enableHitPitchShifting = true;
    [SerializeField] private float hitMinPitch = 0.9f;
    [SerializeField] private float hitMaxPitch = 1.1f;
    [SerializeField] private bool useRandomHitPitch = true;
    [SerializeField] private bool hitPitchBasedOnHealth = false;
    [SerializeField] private float hitPitchAtFullHealth = 1.0f;
    [SerializeField] private float hitPitchAtLowHealth = 1.3f;

    [Header("Audio - Animation Shake")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip animationShakeSound;
    [SerializeField] private float animationShakeVolume = 0.7f;
    [SerializeField] private bool enablePitchShifting = true;
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.15f;
    [SerializeField] private bool useRandomPitch = true;
    [SerializeField] private float fixedPitch = 1.0f;
    [SerializeField] private bool pitchBasedOnHealth = false;
    [SerializeField] private AnimationCurve pitchHealthCurve = AnimationCurve.Linear(0f, 1.3f, 1f, 0.8f);

    [Header("Audio - Custom Animation Shake")]
    [SerializeField] private AudioClip customAnimationShakeSound;
    [SerializeField] private float customAnimationShakeVolume = 0.7f;
    [SerializeField] private bool enableCustomPitchShifting = true;
    [SerializeField] private float customMinPitch = 0.8f;
    [SerializeField] private float customMaxPitch = 1.2f;
    [SerializeField] private bool useCustomRandomPitch = true;
    [SerializeField] private float customFixedPitch = 1.0f;
    [SerializeField] private bool customPitchBasedOnHealth = false;

    [Header("Rumble Settings")]
    [SerializeField] private bool enableRumble = true;
    [SerializeField] private float animationRumbleLowFrequency = 0.3f;
    [SerializeField] private float animationRumbleHighFrequency = 0.4f;
    [SerializeField] private float animationRumbleDuration = 0.2f;
    [SerializeField] private float stunRumbleLowFrequency = 0.6f;
    [SerializeField] private float stunRumbleHighFrequency = 0.7f;
    [SerializeField] private float stunRumbleDuration = 0.3f;

    [Header("Damage Particles")]
    [SerializeField] private ParticleSystem hurtParticleSystem;
    [SerializeField] private Transform particleSpawnPoint;
    [SerializeField] private bool spawnParticleAtHitPoint = true;
    [SerializeField] private float particleDestroyDelay = 2f;

    [Header("Particle Pooling")]
    [SerializeField] private int particlePoolSize = 5;
    [SerializeField] private bool prewarmParticlePool = true;

    [Header("Stun Settings")]
    [SerializeField] private BakaBossStateManager stateManager;
    [SerializeField] private int stunDamage = 0;

    // Particle pooling system
    private Queue<ParticleSystem> particlePool;
    private Transform particlePoolParent;

    private Collider2D[] colliders;

    void Start()
    {
        flashEffect = GetComponent<SimpleFlash>();
        colliders = GetComponentsInChildren<Collider2D>();
        currentHealth = maxHealth;

        // Get or add AudioSource component
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

        if (stateManager == null)
        {
            stateManager = GetComponent<BakaBossStateManager>();
        }

        if (bossHealthBar != null)
        {
            healthBarFullWidth = bossHealthBar.sizeDelta.x;
            UpdateHealthBar();
        }

        // Auto-find particle spawn point if not assigned
        if (particleSpawnPoint == null)
        {
            particleSpawnPoint = transform;
        }

        // Initialize particle pool
        InitializeParticlePool();
    }

    void Update()
    {
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

    private void InitializeParticlePool()
    {
        if (hurtParticleSystem == null) return;

        // Create parent object for pooled particles
        GameObject poolParent = new GameObject("BossParticlePool_" + gameObject.name);
        particlePoolParent = poolParent.transform;
        particlePoolParent.SetParent(null); // Keep in root to avoid being destroyed with boss

        particlePool = new Queue<ParticleSystem>();

        for (int i = 0; i < particlePoolSize; i++)
        {
            ParticleSystem particleSystem = CreatePooledParticle();
            particlePool.Enqueue(particleSystem);
        }

        if (prewarmParticlePool)
        {
            Debug.Log($"Boss particle pool initialized with {particlePoolSize} particles for {gameObject.name}");
        }
    }

    private ParticleSystem CreatePooledParticle()
    {
        ParticleSystem newParticle = Instantiate(hurtParticleSystem, particlePoolParent);
        newParticle.gameObject.SetActive(false);

        // Ensure the particle system doesn't destroy on completion
        var main = newParticle.main;
        main.stopAction = ParticleSystemStopAction.None;

        return newParticle;
    }

    private ParticleSystem GetPooledParticle()
    {
        // Try to get from pool
        if (particlePool.Count > 0)
        {
            ParticleSystem particle = particlePool.Dequeue();
            if (particle != null)
            {
                return particle;
            }
        }

        // If pool is empty or particle is destroyed, create a new one
        Debug.LogWarning("Boss particle pool empty, creating new particle system");
        return CreatePooledParticle();
    }

    private void ReturnParticleToPool(ParticleSystem particleSystem)
    {
        if (particleSystem == null) return;

        // Reset particle system
        particleSystem.Stop(true);
        particleSystem.Clear();
        particleSystem.gameObject.SetActive(false);

        // Return to pool
        particlePool.Enqueue(particleSystem);
    }

    private void PlayHurtParticles(Vector2 hitDirection, Vector2? hitPoint = null)
    {
        if (hurtParticleSystem == null) return;

        Vector3 spawnPosition;

        if (spawnParticleAtHitPoint && hitPoint.HasValue)
        {
            // Spawn at the exact hit point
            spawnPosition = hitPoint.Value;
        }
        else
        {
            // Spawn at the designated spawn point
            spawnPosition = particleSpawnPoint.position;
        }

        // Get particle from pool instead of instantiating
        ParticleSystem particles = GetPooledParticle();

        // Set position and activate
        particles.transform.position = spawnPosition;
        particles.gameObject.SetActive(true);

        // Calculate particle rotation based on hit direction
        // This allows particles to emit in the direction opposite to the hit
        Vector3 particleDirection = -hitDirection.normalized;

        // Calculate the angle for rotation (in degrees)
        float angle = Mathf.Atan2(particleDirection.y, particleDirection.x) * Mathf.Rad2Deg;

        // Apply rotation to the particle system
        particles.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        particles.Play();

        // Start coroutine to return particle to pool after use
        StartCoroutine(ReturnParticleAfterPlay(particles));
    }

    private IEnumerator ReturnParticleAfterPlay(ParticleSystem particleSystem)
    {
        if (particleSystem == null) yield break;

        // Wait for the particle system to finish playing
        yield return new WaitForSeconds(particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);

        // Return to pool
        ReturnParticleToPool(particleSystem);
    }

    public void UpdateHealthBar()
    {
        if (bossHealthBar == null) return;

        float healthPercentage = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector2 newSize = new Vector2(healthBarFullWidth * healthPercentage, bossHealthBar.sizeDelta.y);
        bossHealthBar.sizeDelta = newSize;
    }

    public void TakeDamage(int damage, Vector2 hitDirection, Vector2? hitPoint = null)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        // Play directional hurt particles
        PlayHurtParticles(hitDirection, hitPoint);

        // Play hit sound effect
        PlayHitSound();

        if (flashEffect != null)
        {
            flashEffect.CallHurtFlash();
        }

        // REMOVED camera shake from regular damage

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeStun(Vector2 hitDirection, Vector2? hitPoint = null)
    {
        if (isDead || isInvulnerable) return;

        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        // Play directional hurt particles for stun as well
        PlayHurtParticles(hitDirection, hitPoint);

        // Play hit sound effect for stun (you could use a different sound if needed)
        PlayHitSound();

        if (flashEffect != null)
        {
            flashEffect.CallStunnedFlash(); // Use the new stunned flash
        }

        // Camera shake only during stun
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(stunShakeIntensity, stunShakeTime);
        }

        // Trigger stun rumble
        if (enableRumble)
        {
            TriggerStunRumble();
        }

        if (stateManager != null)
        {
            stateManager.TriggerStun();
        }
    }

    private void PlayHitSound()
    {
        // Check if there are any hit sounds to play
        if (hitSounds == null || hitSounds.Length == 0)
        {
            Debug.LogWarning("No hit sounds assigned to BakaBossHealth!");
            return;
        }

        // Select a random sound from the array
        int randomIndex = Random.Range(0, hitSounds.Length);
        AudioClip selectedClip = hitSounds[randomIndex];

        // Calculate pitch for hit sound
        float pitch = 1.0f;

        if (enableHitPitchShifting)
        {
            if (hitPitchBasedOnHealth)
            {
                // Calculate pitch based on health percentage
                float healthPercentage = (float)currentHealth / maxHealth;
                pitch = Mathf.Lerp(hitPitchAtLowHealth, hitPitchAtFullHealth, healthPercentage);
            }
            else if (useRandomHitPitch)
            {
                // Random pitch within range
                pitch = Random.Range(hitMinPitch, hitMaxPitch);
            }
        }

        // Play the hit sound with calculated pitch
        PlaySoundWithPitch(selectedClip, hitSoundVolume, pitch);
    }

    // Animation Event Camera Shake - Weaker than stun shake
    public void TriggerAnimationShake()
    {
        if (!isDead)
        {
            // Camera shake
            if (cameraShake != null)
            {
                cameraShake.ShakeCamera(animationShakeIntensity, animationShakeTime);
            }

            // Play animation shake sound with pitch shifting
            PlayAnimationShakeSound();

            // Trigger animation rumble
            if (enableRumble)
            {
                TriggerAnimationRumble();
            }
        }
    }

    // Animation Event Camera Shake with custom parameters
    public void TriggerCustomAnimationShake(float intensity, float duration)
    {
        if (!isDead)
        {
            // Camera shake
            if (cameraShake != null)
            {
                cameraShake.ShakeCamera(intensity, duration);
            }

            // Play custom animation shake sound with pitch shifting
            PlayCustomAnimationShakeSound();

            // Trigger animation rumble with scaled intensity
            if (enableRumble)
            {
                TriggerScaledAnimationRumble(intensity);
            }
        }
    }

    private void PlayAnimationShakeSound()
    {
        if (animationShakeSound == null) return;

        float pitch = GetPitch(enablePitchShifting, useRandomPitch, fixedPitch,
                              minPitch, maxPitch, pitchBasedOnHealth, true);

        PlaySoundWithPitch(animationShakeSound, animationShakeVolume, pitch);
    }

    private void PlayCustomAnimationShakeSound()
    {
        // Use custom sound if available, otherwise use default animation shake sound
        AudioClip soundToPlay = customAnimationShakeSound != null ? customAnimationShakeSound : animationShakeSound;
        float volume = customAnimationShakeSound != null ? customAnimationShakeVolume : animationShakeVolume;

        if (soundToPlay == null) return;

        float pitch = GetPitch(enableCustomPitchShifting, useCustomRandomPitch, customFixedPitch,
                              customMinPitch, customMaxPitch, customPitchBasedOnHealth, false);

        PlaySoundWithPitch(soundToPlay, volume, pitch);
    }

    private float GetPitch(bool enablePitchShift, bool useRandom, float fixedPitchValue,
                          float min, float max, bool basedOnHealth, bool useDefaultCurve = true)
    {
        if (!enablePitchShift)
            return 1.0f;

        if (basedOnHealth)
        {
            // Calculate pitch based on current health percentage
            float healthPercentage = (float)currentHealth / maxHealth;

            if (useDefaultCurve)
            {
                // Use the animation curve for health-based pitch
                return pitchHealthCurve.Evaluate(healthPercentage);
            }
            else
            {
                // Linear interpolation between min and max based on health
                return Mathf.Lerp(min, max, healthPercentage);
            }
        }
        else if (useRandom)
        {
            // Random pitch within range
            return Random.Range(min, max);
        }
        else
        {
            // Fixed pitch
            return fixedPitchValue;
        }
    }

    private void PlaySoundWithPitch(AudioClip clip, float volume, float pitch)
    {
        if (audioSource != null)
        {
            // Store original pitch
            float originalPitch = audioSource.pitch;

            // Set new pitch
            audioSource.pitch = pitch;

            // Play sound
            audioSource.PlayOneShot(clip, volume);

            // Reset pitch to original (optional, but good practice)
            StartCoroutine(ResetPitchAfterSound(originalPitch));
        }
        else
        {
            // Create a temporary AudioSource for pitch-shifted sound
            CreateTemporaryAudioSource(clip, volume, pitch);
        }
    }

    private void CreateTemporaryAudioSource(AudioClip clip, float volume, float pitch)
    {
        // Create a new GameObject for the temporary audio
        GameObject tempAudioObj = new GameObject("TempAudio_" + clip.name);
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

    private IEnumerator ResetPitchAfterSound(float originalPitch)
    {
        // Wait one frame to ensure sound has started playing
        yield return null;

        // Reset pitch to original
        audioSource.pitch = originalPitch;
    }

    private void TriggerAnimationRumble()
    {
        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(
                animationRumbleLowFrequency,
                animationRumbleHighFrequency,
                animationRumbleDuration
            );
        }
    }

    private void TriggerStunRumble()
    {
        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(
                stunRumbleLowFrequency,
                stunRumbleHighFrequency,
                stunRumbleDuration
            );
        }
    }

    private void TriggerScaledAnimationRumble(float intensity)
    {
        if (RumbleManager.instance != null)
        {
            // Scale rumble intensity based on the camera shake intensity
            float scaleFactor = Mathf.Clamp01(intensity / animationShakeIntensity);
            float scaledLowFreq = animationRumbleLowFrequency * scaleFactor;
            float scaledHighFreq = animationRumbleHighFrequency * scaleFactor;
            float scaledDuration = animationRumbleDuration * scaleFactor;

            RumbleManager.instance.RumblePulse(
                scaledLowFreq,
                scaledHighFreq,
                scaledDuration
            );
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Disable all colliders
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Trigger death cutscene through state manager
        if (stateManager != null)
        {
            stateManager.TriggerDeathCutscene();
        }
        else
        {
            Debug.LogError("StateManager reference is missing in BakaBossHealth!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player Attack") && !other.CompareTag("Shockwave")) return;

        // Get the exact hit point for particle spawning
        Vector2 hitPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Shockwave"))
        {
            Vector2 hitDirection = (transform.position - other.transform.position).normalized;
            TakeStun(hitDirection, hitPoint);
        }
        else if (other.CompareTag("Player Attack") && other.TryGetComponent<PlayerAttackHitbox>(out var attack))
        {
            Vector2 hitDirection = (transform.position - other.transform.position).normalized;
            TakeDamage(attack.damage, hitDirection, hitPoint);
        }
    }

    private void OnDestroy()
    {
        // Clean up particle pool
        if (particlePoolParent != null)
        {
            Destroy(particlePoolParent.gameObject);
        }
    }

    // Public method to manually set the particle system at runtime
    public void SetHurtParticleSystem(ParticleSystem particleSystem)
    {
        hurtParticleSystem = particleSystem;
        // Reinitialize pool if particle system changes
        if (particlePool != null)
        {
            InitializeParticlePool();
        }
    }

    // Public method to set the particle spawn point at runtime
    public void SetParticleSpawnPoint(Transform spawnPoint)
    {
        particleSpawnPoint = spawnPoint;
    }

    // Public method to set hit sounds
    public void SetHitSounds(AudioClip[] sounds, float volume = 0.8f)
    {
        hitSounds = sounds;
        hitSoundVolume = volume;
    }

    // Public method to configure hit pitch shifting
    public void ConfigureHitPitchShifting(bool enableShifting, float min = 0.9f, float max = 1.1f,
                                          bool randomPitch = true, bool healthBased = false,
                                          float pitchAtFullHealth = 1.0f, float pitchAtLowHealth = 1.3f)
    {
        enableHitPitchShifting = enableShifting;
        hitMinPitch = min;
        hitMaxPitch = max;
        useRandomHitPitch = randomPitch;
        hitPitchBasedOnHealth = healthBased;
        hitPitchAtFullHealth = pitchAtFullHealth;
        hitPitchAtLowHealth = pitchAtLowHealth;
    }

    // Public method to set animation shake sound
    public void SetAnimationShakeSound(AudioClip sound, float volume = 0.7f)
    {
        animationShakeSound = sound;
        animationShakeVolume = volume;
    }

    // Public method to set custom animation shake sound
    public void SetCustomAnimationShakeSound(AudioClip sound, float volume = 0.7f)
    {
        customAnimationShakeSound = sound;
        customAnimationShakeVolume = volume;
    }

    // Public method to configure pitch shifting
    public void ConfigurePitchShifting(bool enableShifting, float min = 0.85f, float max = 1.15f,
                                      bool randomPitch = true, bool healthBased = false)
    {
        enablePitchShifting = enableShifting;
        minPitch = min;
        maxPitch = max;
        useRandomPitch = randomPitch;
        pitchBasedOnHealth = healthBased;
    }

    // Public method to configure custom pitch shifting
    public void ConfigureCustomPitchShifting(bool enableShifting, float min = 0.8f, float max = 1.2f,
                                           bool randomPitch = true, bool healthBased = false)
    {
        enableCustomPitchShifting = enableShifting;
        customMinPitch = min;
        customMaxPitch = max;
        useCustomRandomPitch = randomPitch;
        customPitchBasedOnHealth = healthBased;
    }

    // Public method to manually play a sound with pitch shifting
    public void PlaySoundWithPitchShift(AudioClip clip, float volume = 1.0f, bool useHealthBasedPitch = false)
    {
        float pitch;

        if (useHealthBasedPitch)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            pitch = pitchHealthCurve.Evaluate(healthPercentage);
        }
        else
        {
            pitch = Random.Range(minPitch, maxPitch);
        }

        PlaySoundWithPitch(clip, volume, pitch);
    }

    // Public method to manually trigger a hit sound (useful for animation events)
    public void PlayManualHitSound()
    {
        PlayHitSound();
    }
}