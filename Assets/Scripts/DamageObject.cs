using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private bool destroyOnContact = false;
    [SerializeField] private bool continuousDamage = false;
    [SerializeField] private float damageInterval = 1f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float effectDestroyTime = 1f;

    private float nextDamageTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && !continuousDamage)
            {
                playerHealth.TakeDamage(damageAmount);
                SpawnHitEffect();

                if (destroyOnContact)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (continuousDamage && other.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                SpawnHitEffect();
                nextDamageTime = Time.time + damageInterval;
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