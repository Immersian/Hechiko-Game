using SupanthaPaul;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyDamageHandler : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;
    public GameObject EnemyObject;

    [Header("Damage Feedback")]
    [SerializeField] private float invulnerabilityTime = 0.3f;
    [SerializeField] private string hurtTrigger = "Hurt";
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private float deathDestroyDelay = 2f;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private Vector2 soundPitchRange = new Vector2(0.9f, 1.1f);

    [Header("Knockback")]
    [SerializeField] private bool useKnockback = true;
    [SerializeField] private float knockbackResistance = 0.5f;

    [Header("Dash Attack Settings")]  // New section
    [SerializeField] private bool consumeAttackerStamina = true;
    [SerializeField] private float staminaCostPercent = 0.2f;
    [SerializeField] private PlayerController attacker;

    private float lastDamageTime;
    private Rigidbody2D rb;
    private Animator animator;
    private SimpleFlash flashEffect;
    private Collider2D[] colliders;
    private AudioSource audioSource;

    void Start()
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
        // Immediately stop any attack animations
        if (TryGetComponent<EnemyMovement>(out var enemyMovement))
        {
            enemyMovement.CancelAttack();
        }

        currentHealth -= damage;
        lastDamageTime = Time.time;

        // Visual Feedback
        flashEffect.CallHurtFlash();
        animator.ResetTrigger(hurtTrigger);
        animator.SetTrigger(hurtTrigger);

        // Sound Feedback
        if (hurtSound != null)
        {
            audioSource.pitch = Random.Range(soundPitchRange.x, soundPitchRange.y);
            audioSource.PlayOneShot(hurtSound);
        }

        // Knockback
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
        isDead = true;

        // Force stop all animations and transitions
        animator.ResetTrigger(hurtTrigger);
        animator.ResetTrigger(deathTrigger);
        animator.SetBool("Stun", false);
        animator.SetTrigger(deathTrigger);

        // Play death sound
        if (deathSound != null)
        {
            audioSource.pitch = 1f; // Reset to normal pitch for death sound
            audioSource.PlayOneShot(deathSound);
        }

        // Disable all colliders
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Disable physics and movement
        if (rb != null) rb.simulated = false;

        // Disable enemy behavior scripts
        if (TryGetComponent<EnemyMovement>(out var movement))
        {
            movement.enabled = false;
        }

        Destroy(EnemyObject, deathDestroyDelay);
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

    public interface IEnemyController
    {
        void OnPlayerDetected();
        void OnPlayerLost();
        bool HasLineOfSightToPlayer();
    }
}