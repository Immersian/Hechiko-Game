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

    [Header("Knockback")]
    [SerializeField] private bool useKnockback = true;
    [SerializeField] private float knockbackResistance = 0.5f;

    [Header("Dash Attack Settings")]
    [SerializeField] private bool consumeAttackerStamina = true;
    [SerializeField] private float staminaCostPercent = 0.2f;
    [SerializeField] private PlayerController attacker;

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
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        flashEffect = GetComponent<SimpleFlash>();
        colliders = GetComponentsInChildren<Collider2D>();

        // Try to find the spawn manager in parent
        spawnManager = GetComponentInParent<EnemyRespawnManager>();

        // Add AudioSource if not present
        if (!TryGetComponent<AudioSource>(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead || Time.time < lastDamageTime + invulnerabilityTime) return;

        if (consumeAttackerStamina && attacker != null)
        {
            PlayerController playerController = attacker.GetComponent<PlayerController>();
            if (playerController != null && playerController.isDashing)
            {
                float staminaCost = playerController.dashCost * staminaCostPercent;
                playerController.currentStamina = Mathf.Max(0, playerController.currentStamina - staminaCost);
                playerController.UpdateStaminaBar();
            }
        }

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

        flashEffect.CallHurtFlash();
        animator.ResetTrigger(hurtTrigger);
        animator.SetTrigger(hurtTrigger);

        if (hurtSound != null)
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

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Notify spawn manager
        if (spawnManager != null)
        {
            spawnManager.OnEnemyDeath();
        }

        animator.ResetTrigger(hurtTrigger);
        animator.ResetTrigger(deathTrigger);
        animator.SetBool("Stun", false);
        animator.SetTrigger(deathTrigger);

        if (deathSound != null)
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

        // Just destroy this instance - the spawn manager will create a new one
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player Attack")) return;

        if (other.TryGetComponent<PlayerAttackHitbox>(out var attack))
        {
            Vector2 hitDirection = (transform.position - other.transform.position).normalized;
            TakeDamage(attack.damage, hitDirection);
        }
    }

    private void OnDestroy()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }
    }

    public interface IEnemyController
    {
        void OnPlayerDetected();
        void OnPlayerLost();
        bool HasLineOfSightToPlayer();
    }
}