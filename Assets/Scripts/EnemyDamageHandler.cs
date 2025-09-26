using SupanthaPaul;
using UnityEditor;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyDamageHandler : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Respawn Settings")]
    [SerializeField] private bool respawnOnPlayerDeath = true;
    [SerializeField] private float deathAnimationDelay = 1f;

    [Header("Damage Feedback")]
    [SerializeField] private float invulnerabilityTime = 0.3f;
    [SerializeField] private string hurtTrigger = "Hurt";
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private Vector2 soundPitchRange = new Vector2(0.9f, 1.1f);

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem hurtParticleSystem;
    [SerializeField] private Transform particleSpawnPoint;
    [SerializeField] private bool spawnParticleAtHitPoint = false;
    [SerializeField] private float particleDestroyDelay = 2f;

    [Header("Knockback")]
    [SerializeField] private bool useKnockback = true;
    [SerializeField] private float knockbackResistance = 0.5f;

    [Header("Dash Attack Settings")]
    [SerializeField] private bool consumeAttackerStamina = true;
    [SerializeField] private float staminaCostPercent = 0.2f;

    [Header("Hitstop Settings")]
    [SerializeField] private bool enableHitstop = true;
    [SerializeField] private float hitstopDuration = 0.08f;

    [Header("Camera Shake Settings")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float cameraShakeIntensity = 1f;
    [SerializeField] private float cameraShakeDuration = 0.2f;
    [SerializeField] private float deathCameraShakeIntensity = 2f;
    [SerializeField] private float deathCameraShakeDuration = 0.3f;

    // Static variables for global hitstop management
    private static bool isGlobalHitstopActive = false;
    private static Coroutine activeGlobalHitstopCoroutine;
    private static float originalTimeScale = 1f;

    // Private reference that will be auto-assigned
    private PlayerController attacker;
    public CameraShake cameraShake;

    private float lastDamageTime;
    private Rigidbody2D rb;
    private Animator animator;
    private SimpleFlash flashEffect;
    private Collider2D[] colliders;
    private AudioSource audioSource;
    private Coroutine deathCoroutine;
    private EnemyRespawnManager spawnManager;

    void Start()
    {
        InitializeEnemy();
        FindPlayerController();
        FindCameraShake();

        // Auto-find particle spawn point if not assigned
        if (particleSpawnPoint == null)
        {
            particleSpawnPoint = transform;
        }
    }

    // Automatically find the player controller
    private void FindPlayerController()
    {
        // Method 1: Find by tag (most reliable)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            attacker = player.GetComponent<PlayerController>();
            if (attacker != null)
            {
                Debug.Log($"Found player controller: {attacker.gameObject.name}");
            }
        }

        // Method 2: Find by type (fallback)
        if (attacker == null)
        {
            attacker = FindObjectOfType<PlayerController>();
            if (attacker != null)
            {
                Debug.Log($"Found player controller by type: {attacker.gameObject.name}");
            }
        }

        if (attacker == null)
        {
            Debug.LogWarning("PlayerController not found! Stamina consumption will not work.");
        }
    }

    // Find the CameraShake component
    private void FindCameraShake()
    {
        // Method 1: Look for CameraShake on the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraShake = mainCamera.GetComponent<CameraShake>();
            if (cameraShake != null)
            {
                Debug.Log($"Found CameraShake on main camera: {mainCamera.gameObject.name}");
                return;
            }
        }

        // Method 2: Find by tag
        GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObj != null)
        {
            cameraShake = cameraObj.GetComponent<CameraShake>();
            if (cameraShake != null)
            {
                Debug.Log($"Found CameraShake on tagged camera: {cameraObj.name}");
                return;
            }
        }

        // Method 3: Find any CameraShake in scene
        cameraShake = FindObjectOfType<CameraShake>();
        if (cameraShake != null)
        {
            Debug.Log($"Found CameraShake in scene: {cameraShake.gameObject.name}");
            return;
        }

        Debug.LogWarning("CameraShake component not found! Camera shake effects will not work.");
    }

    public void SetRespawnManager(EnemyRespawnManager manager)
    {
        spawnManager = manager;
    }

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Re-enable components
        EnableEnemy();

        // Re-find player controller when enemy respawns (in case player changed)
        FindPlayerController();

        // Re-find camera shake in case camera changed
        FindCameraShake();
    }

    private void InitializeEnemy()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        flashEffect = GetComponent<SimpleFlash>();
        colliders = GetComponentsInChildren<Collider2D>();

        // Add AudioSource if not present
        if (!TryGetComponent<AudioSource>(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void EnableEnemy()
    {
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }

        if (rb != null) rb.simulated = true;

        if (TryGetComponent<EnemyMovement>(out var movement))
        {
            movement.enabled = true;
        }
    }

    public void TakeDamage(int damage, Vector2 hitDirection, PlayerController attackingPlayer = null, Vector2? hitPoint = null)
    {
        if (isDead || Time.time < lastDamageTime + invulnerabilityTime) return;

        // Use the provided attacker or fall back to the stored one
        PlayerController actualAttacker = attackingPlayer ?? attacker;

        if (consumeAttackerStamina && actualAttacker != null)
        {
            if (actualAttacker.isDashing)
            {
                float staminaCost = actualAttacker.dashCost * staminaCostPercent;
                actualAttacker.currentStamina = Mathf.Max(0, actualAttacker.currentStamina - staminaCost);
                actualAttacker.UpdateStaminaBar();
            }
        }

        // Trigger global hitstop before other effects
        if (enableHitstop)
        {
            TriggerGlobalHitstop();
        }

        // Trigger camera shake for damage
        if (enableCameraShake)
        {
            TriggerCameraShake(cameraShakeIntensity, cameraShakeDuration);
        }

        // Play hurt particle effect with direction
        PlayHurtParticles(hitDirection, hitPoint);

        if (TryGetComponent<EnemyHitShake>(out var hitShake))
        {
            hitShake.OnHit();
        }

        if (TryGetComponent<EnemyMovement>(out var enemyMovement))
        {
            enemyMovement.CancelAttack();
        }

        currentHealth -= damage;
        lastDamageTime = Time.time;

        if (flashEffect != null)
            flashEffect.CallHurtFlash();

        animator.ResetTrigger(hurtTrigger);
        animator.SetTrigger(hurtTrigger);

        if (hurtSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(soundPitchRange.x, soundPitchRange.y);
            audioSource.PlayOneShot(hurtSound);
        }

        if (useKnockback && rb != null)
        {
            rb.AddForce(hitDirection * (damage * knockbackResistance), ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
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

        // Instantiate and play the particle system
        ParticleSystem particles = Instantiate(hurtParticleSystem, spawnPosition, Quaternion.identity);

        // Flip particles based on hit direction (player's facing direction)
        float xScale = 1f;
        if (hitDirection.x > 0) // Player hit from left (particles should face right)
        {
            xScale = 1f;
        }
        else if (hitDirection.x < 0) // Player hit from right (particles should face left)
        {
            xScale = -1f;
        }

        // Apply scale to the root object (this will affect all children too)
        particles.transform.localScale = new Vector3(xScale, 1, 1);

        particles.Play();

        // Destroy the particle system after it finishes playing
        if (particleDestroyDelay > 0)
        {
            Destroy(particles.gameObject, particleDestroyDelay);
        }
        else
        {
            // Auto-destroy when the particle system finishes
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }
    }

    private void TriggerGlobalHitstop()
    {
        // Prevent multiple simultaneous global hitstops
        if (isGlobalHitstopActive) return;

        // Stop any existing global hitstop coroutine
        if (activeGlobalHitstopCoroutine != null)
        {
            MonoBehaviour coroutineOwner = GetCoroutineOwner();
            if (coroutineOwner != null)
            {
                coroutineOwner.StopCoroutine(activeGlobalHitstopCoroutine);
            }
        }

        // Start new global hitstop
        MonoBehaviour owner = GetCoroutineOwner();
        if (owner != null)
        {
            activeGlobalHitstopCoroutine = owner.StartCoroutine(GlobalHitstopCoroutine());
        }
    }

    private IEnumerator GlobalHitstopCoroutine()
    {
        isGlobalHitstopActive = true;
        originalTimeScale = Time.timeScale;

        // Freeze the entire game by setting timescale to 0
        Time.timeScale = 0f;

        // Wait for real seconds (unscaled time) for the hitstop duration
        yield return new WaitForSecondsRealtime(hitstopDuration);

        // Restore timescale
        EndGlobalHitstop();
    }

    private void EndGlobalHitstop()
    {
        if (!isGlobalHitstopActive) return;

        Time.timeScale = originalTimeScale;
        isGlobalHitstopActive = false;
        activeGlobalHitstopCoroutine = null;
    }

    private void TriggerCameraShake(float intensity, float duration)
    {
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(intensity, duration);
        }
    }

    // Helper method to find a suitable MonoBehaviour to run the coroutine
    private MonoBehaviour GetCoroutineOwner()
    {
        // Try to use this object first
        if (this != null) return this;

        // Fallback: find any active MonoBehaviour in the scene
        MonoBehaviour[] activeBehaviours = FindObjectsOfType<MonoBehaviour>();
        if (activeBehaviours.Length > 0)
        {
            return activeBehaviours[0];
        }

        return null;
    }

    // Static method to allow other scripts to end hitstop in emergency situations
    public static void EndGlobalHitstopImmediately()
    {
        if (activeGlobalHitstopCoroutine != null)
        {
            MonoBehaviour coroutineOwner = FindFirstObjectByType<EnemyDamageHandler>();
            if (coroutineOwner != null)
            {
                coroutineOwner.StopCoroutine(activeGlobalHitstopCoroutine);
            }
        }

        if (isGlobalHitstopActive)
        {
            Time.timeScale = originalTimeScale;
            isGlobalHitstopActive = false;
            activeGlobalHitstopCoroutine = null;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Trigger stronger camera shake for death
        if (enableCameraShake)
        {
            TriggerCameraShake(deathCameraShakeIntensity, deathCameraShakeDuration);
        }

        // End any active global hitstop immediately when dying
        if (isGlobalHitstopActive)
        {
            EndGlobalHitstopImmediately();
        }

        // Notify spawn manager
        if (spawnManager != null)
        {
            spawnManager.OnEnemyDeath(gameObject);
        }

        animator.ResetTrigger(hurtTrigger);
        animator.ResetTrigger(deathTrigger);
        animator.SetBool("Stun", false);
        animator.SetTrigger(deathTrigger);

        if (deathSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathSound);
        }

        DisableEnemy();

        if (deathCoroutine != null)
            StopCoroutine(deathCoroutine);
        deathCoroutine = StartCoroutine(WaitForDeathAnimation());
    }

    private void DisableEnemy()
    {
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        if (rb != null) rb.simulated = false;

        if (TryGetComponent<EnemyMovement>(out var movement))
        {
            movement.enabled = false;
        }
    }

    private IEnumerator WaitForDeathAnimation()
    {
        yield return new WaitForSeconds(deathAnimationDelay);
        // Spawn manager handles deactivation
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player Attack")) return;

        if (other.TryGetComponent<PlayerAttackHitbox>(out var attack))
        {
            // Try to get the player controller from the attack hitbox
            PlayerController attackingPlayer = other.GetComponentInParent<PlayerController>();

            Vector2 hitDirection = (transform.position - other.transform.position).normalized;

            // Get the exact hit point for particle spawning
            Vector2 hitPoint = other.ClosestPoint(transform.position);

            TakeDamage(attack.damage, hitDirection, attackingPlayer, hitPoint);
        }
    }

    private void OnDestroy()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }

        // If this object is destroyed during hitstop, clean up the global state
        if (isGlobalHitstopActive)
        {
            EndGlobalHitstopImmediately();
        }
    }

    // Public method to manually set camera shake reference
    public void SetCameraShake(CameraShake shakeComponent)
    {
        cameraShake = shakeComponent;
    }

    // Public method to manually set attacker (useful for special cases)
    public void SetAttacker(PlayerController playerController)
    {
        attacker = playerController;
    }

    // Public method to set the particle system at runtime
    public void SetHurtParticleSystem(ParticleSystem particleSystem)
    {
        hurtParticleSystem = particleSystem;
    }

    // Public method to set the particle spawn point at runtime
    public void SetParticleSpawnPoint(Transform spawnPoint)
    {
        particleSpawnPoint = spawnPoint;
    }

    public interface IEnemyController
    {
        void OnPlayerDetected();
        void OnPlayerLost();
        bool HasLineOfSightToPlayer();
    }
}