using UnityEngine;
using BarthaSzabolcs.Tutorial_SpriteFlash;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SimpleFlash))]
public class EnemyDamageHandler : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Damage Feedback")]
    [SerializeField] private float invulnerabilityTime = 0.3f;
    [SerializeField] private string hurtTrigger = "Hurt";
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private float deathDestroyDelay = 2f;

    [Header("Knockback")]
    [SerializeField] private bool useKnockback = true;
    [SerializeField] private float knockbackResistance = 0.5f;

    private float lastDamageTime;
    private Rigidbody2D rb;
    private Animator animator;
    private SimpleFlash flashEffect;
    private Collider2D[] colliders;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        flashEffect = GetComponent<SimpleFlash>();
        colliders = GetComponentsInChildren<Collider2D>();
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead || Time.time < lastDamageTime + invulnerabilityTime) return;

        currentHealth -= damage;
        lastDamageTime = Time.time;

        // Visual Feedback
        flashEffect.Flash();
        animator.ResetTrigger(hurtTrigger);
        animator.SetTrigger(hurtTrigger);

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
        animator.SetTrigger(deathTrigger);

        // Disable all colliders
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Disable physics
        if (rb != null) rb.simulated = false;

        Destroy(gameObject, deathDestroyDelay);
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
}