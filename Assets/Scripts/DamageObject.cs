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

        // Check if player is invulnerable (either dashing or ground slamming)
        bool isInvulnerable = (playerController != null && playerController.isDashing) ||
                             (playerAttack != null && playerAttack.isGroundSlamming);

        if (isInvulnerable) return;

        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
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