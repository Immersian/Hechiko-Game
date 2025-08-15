using SupanthaPaul;
using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private bool destroyOnContact = false;
    [SerializeField] private bool continuousDamage = false;
    [SerializeField] private float damageInterval = 1f;

    [Header("Knockback Settings")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private Vector2 knockbackDirection = new Vector2(0.5f, 1f);

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float effectDestroyTime = 1f;

    [Header("Parry Settings")]
    [SerializeField] private bool isObjectParryable;
    [SerializeField] private bool parryFromAnySide = false;
    [SerializeField] private bool perfectParryOnly = false; // New: Requires perfect timing
    [SerializeField] private float perfectParryWindow = 0.1f; // New: Timeframe for perfect parry

    private float nextDamageTime;
    private Collider2D myCollider;
    private float activeTime; // Tracks how long this object has been active

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        activeTime = 0f;
        if (myCollider == null)
        {
            Debug.LogError("DamageObject requires a Collider2D on the same GameObject!", this);
        }
    }

    private void Update()
    {
        activeTime += Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && myCollider != null && myCollider.IsTouching(other))
        {
            TryDamagePlayer(other, !continuousDamage);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (myCollider != null && myCollider.IsTouching(other) &&
            continuousDamage && other.CompareTag("Player") &&
            Time.time >= nextDamageTime)
        {
            TryDamagePlayer(other, false);
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void TryDamagePlayer(Collider2D playerCollider, bool canDestroy)
    {
        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.isDead) return;

        PlayerController playerController = playerCollider.GetComponent<PlayerController>();
        PlayerAttack playerAttack = playerCollider.GetComponentInChildren<PlayerAttack>();
        ParryScript playerParry = playerCollider.GetComponentInChildren<ParryScript>();

        // 1. Check parry conditions first
        if (isObjectParryable && playerParry != null)
        {
            bool canBeParried = false;

            if (parryFromAnySide)
            {
                canBeParried = playerParry.IsParryActive;
                if (perfectParryOnly)
                {
                    canBeParried &= activeTime <= perfectParryWindow;
                }
            }
            else
            {
                EnemyMovement enemyMovement = GetComponentInParent<EnemyMovement>();
                canBeParried = playerParry.CanParryAttack(enemyMovement);
                if (perfectParryOnly)
                {
                    canBeParried &= activeTime <= perfectParryWindow;
                }
            }

            if (canBeParried)
            {
                playerParry.Parried();
                OnParried();
                return;
            }
        }

        bool isSpecialAttack = gameObject.CompareTag("SpecialAttack");

        // 2. Check if we should apply damage
        bool shouldDamage = true;

        // Always respect post-hit invulnerability
        if (playerHealth.isInvulnerable)
        {
            shouldDamage = false;
        }
        // For non-special attacks, respect dash/slam invulnerability
        else if (!isSpecialAttack &&
                 ((playerController != null && playerController.isDashing) ||
                  (playerAttack != null && playerAttack.isGroundSlamming)))
        {
            shouldDamage = false;
        }

        // 3. Apply knockback if configured
        // Special attacks should always apply knockback, even during dash
        if (applyKnockback && playerController != null)
        {
            // Only skip knockback for non-special attacks during dash/slam
            if (!isSpecialAttack &&
                ((playerController.isDashing) ||
                 (playerAttack != null && playerAttack.isGroundSlamming)))
            {
                // Don't apply knockback for normal attacks during invulnerable states
            }
            else
            {
                ApplyKnockback(playerCollider, playerController);
            }
        }

        // 4. Apply damage if allowed
        if (shouldDamage)
        {
            playerHealth.TakeDamage(damageAmount, gameObject);
            SpawnHitEffect();

            if (destroyOnContact && canDestroy)
            {
                Destroy(gameObject);
            }
        }
        // Still destroy on contact even if no damage was dealt (if configured)
        else if (destroyOnContact && canDestroy)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyKnockback(Collider2D playerCollider, PlayerController playerController)
    {
        Vector2 dir = (playerCollider.transform.position - transform.position).normalized;
        Vector2 finalDirection = new Vector2(
            Mathf.Sign(dir.x) * knockbackDirection.x,
            knockbackDirection.y
        ).normalized;

        playerController.ApplyKnockback(finalDirection * knockbackForce);
    }

    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDestroyTime);
        }
    }

    private void OnParried()
    {
        // Handle what happens when this object is parried
        if (destroyOnContact)
        {
            Destroy(gameObject);
        }

        // You could add additional effects here like:
        // - Parry sound effect
        // - Special particles
        // - Stun the enemy if this is an enemy attack
    }
}