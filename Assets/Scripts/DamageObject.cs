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

    private float nextDamageTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other, !continuousDamage);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (continuousDamage && other.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            TryDamagePlayer(other, false);
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void TryDamagePlayer(Collider2D playerCollider, bool canDestroy)
    {
        PlayerController playerController = playerCollider.GetComponent<PlayerController>();
        PlayerAttack playerAttack = playerCollider.GetComponentInChildren<PlayerAttack>();
        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
        ParryScript playerParry = playerCollider.GetComponentInChildren<ParryScript>();

        // 1. First check if the player is parrying (if object is parryable)
        if (isObjectParryable && playerParry != null)
        {
            EnemyMovement enemyMovement = GetComponentInParent<EnemyMovement>();
            if (enemyMovement != null)
            {
                bool parryRightSuccess = playerParry.IsParryingRight && enemyMovement.EnemyFacingLeft;
                bool parryLeftSuccess = playerParry.IsParryingLeft && enemyMovement.EnemyFacingRight;

                Debug.Log($"Parry Check - Right: {parryRightSuccess}, Left: {parryLeftSuccess}");

                if (parryRightSuccess || parryLeftSuccess)
                {
                    Debug.Log("Parry successful");
                    return; 
                }
            }
        }

        // 2. Then check other invulnerability states
        if ((playerController != null && playerController.isDashing) ||
            (playerAttack != null && playerAttack.isGroundSlamming)) 
        {
            Debug.Log("Player is invulnerable (dashing/ground slamming).");
            return;
        }

        // 3. If no parry/invulnerability, apply damage
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            SpawnHitEffect();

            if (applyKnockback && playerController != null)
            {
                Vector2 dir = (playerCollider.transform.position - transform.position).normalized;
                Vector2 finalDirection = new Vector2(
                    Mathf.Sign(dir.x) * knockbackDirection.x,
                    knockbackDirection.y
                ).normalized;

                playerController.ApplyKnockback(finalDirection * knockbackForce);
            }

            if (destroyOnContact && canDestroy)
            {
                Destroy(gameObject);
            }
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDestroyTime);
        }
    }
}