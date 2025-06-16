using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PlayerAttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] public int damage = 20;
    [SerializeField] private float attackCooldown = 0.5f;
    //[SerializeField] private float knockbackForce = 3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float effectDestroyTime = 0.5f;

    //[Header("Audio")]
    //[SerializeField] private AudioClip hitSound;
    //[SerializeField] private float soundVolume = 0.7f;

    private bool canAttack = true;
    private Collider2D attackCollider;
    private CameraShake cameraShake;

    void Awake()
    {
        attackCollider = GetComponent<Collider2D>();
        attackCollider.enabled = true;
        cameraShake = Camera.main.GetComponent<CameraShake>();
    }

    public void ActivateAttack()
    {
        if (!canAttack) return;
        attackCollider.enabled = true;
        StartCoroutine(AttackCooldown());
    }

    public void DeactivateAttack()
    {
        attackCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer, enemyLayer)) return;

        Vector2 hitDirection = (other.transform.position - transform.position).normalized;

        if (other.TryGetComponent<EnemyDamageHandler>(out var enemy))
        {
            enemy.TakeDamage(damage, hitDirection);
            SpawnHitEffect(other.ClosestPoint(transform.position));
            //PlayHitSound();

            if (cameraShake != null)
            {
                cameraShake.ShakeCamera(2f, 0.1f);
            }
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    private void SpawnHitEffect(Vector2 position)
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, position, Quaternion.identity);
            Destroy(effect, effectDestroyTime);
        }
    }

    //private void PlayHitSound()
    //{
    //    if (hitSound != null)
    //    {
    //        AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
    //    }
    //}

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void OnDrawGizmos()
    {
        if (attackCollider != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(attackCollider.bounds.center, attackCollider.bounds.size);
        }
    }
}