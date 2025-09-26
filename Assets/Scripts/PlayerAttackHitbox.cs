using UnityEngine;
using System.Collections;
using SupanthaPaul;

[RequireComponent(typeof(Collider2D))]
public class PlayerAttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] public int damage = 20;
    [SerializeField] private float attackCooldown = 0f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float effectDestroyTime = 0.5f;

    private bool canAttack = true;
    private Collider2D attackCollider;
    public CameraShake cameraShake;

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

        // Get player's facing direction for proper particle orientation
        PlayerController player = GetComponentInParent<PlayerController>();
        Vector2 playerFacingDirection = Vector2.right; // Default to right

        if (player != null)
        {
            playerFacingDirection = player.m_facingRight ? Vector2.right : Vector2.left;
        }
        else
        {
            // Fallback to hit direction if player not found
            playerFacingDirection = (other.transform.position - transform.position).normalized;
        }

        Vector2 hitPoint = other.ClosestPoint(transform.position);

        if (other.TryGetComponent<EnemyDamageHandler>(out var enemy))
        {
            enemy.TakeDamage(damage, playerFacingDirection, player, hitPoint);
            SpawnHitEffect(hitPoint);

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