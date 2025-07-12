using SupanthaPaul;
using UnityEngine;

public class DashRefresh : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private bool canRefreshAerialDash = true;
    [SerializeField] private float checkRadius = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string respawnTrigger = "Respawn";
    [SerializeField] private string idleState = "Idle";

    private Collider2D refreshCollider;
    private bool isActive = true;
    private float cooldownTimer = 0f;
    private LayerMask playerLayer;

    private void Awake()
    {
        refreshCollider = GetComponent<Collider2D>();
        playerLayer = LayerMask.GetMask("Player");

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!isActive)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                Respawn();
            }
        }
        else
        {
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            TryCollect(hit.GetComponent<PlayerController>());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive) TryCollect(other.GetComponent<PlayerController>());
    }

    private void TryCollect(PlayerController player)
    {
        if (player == null || !player.isCurrentlyPlayable) return;
        if ((!player.isGrounded && !canRefreshAerialDash) || player.CanDash()) return;

        Collect(player);
    }

    private void Collect(PlayerController player)
    {
        player.m_hasDashedInAir = false;
        player.currentStamina = player.maxStamina;
        player.UpdateStaminaBar();

        // Search for SimpleFlash in player or its children
        SimpleFlash flash = player.GetComponentInChildren<SimpleFlash>();
        if (flash != null)
        {
            flash.CallDashRefreshFlash();
        }

        isActive = false;
        cooldownTimer = respawnTime;
        refreshCollider.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger(hitTrigger);
        }
    }
    private void Respawn()
    {
        isActive = true;
        refreshCollider.enabled = true;

        if (animator != null)
        {
            // Use SetTrigger for respawn as well if it's a trigger
            animator.SetTrigger(respawnTrigger);
            // Or use Play if it's a state name
            // animator.Play(idleState);
        }
    }

    // Animation Events
    public void OnHitAnimationComplete()
    {
        // Animation ends on last frame automatically
    }

    public void OnRespawnAnimationComplete()
    {
        // Return to idle state after respawn completes
        if (animator != null)
        {
            animator.Play(idleState);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}