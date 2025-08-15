using SupanthaPaul;
using UnityEngine;

public class DashRefresh : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private bool canRefreshAerialDash = true;
    [SerializeField] private float checkRadius = 0.5f;
    [SerializeField] private bool refreshOnlyWhenNeeded = true;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private float effectDuration = 1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string respawnTrigger = "Respawn";

    private Collider2D refreshCollider;
    private bool isActive = true;
    private float cooldownTimer = 0f;
    private LayerMask playerLayer;
    private SimpleFlash flashEffect;

    private void Awake()
    {
        refreshCollider = GetComponent<Collider2D>();
        playerLayer = LayerMask.GetMask("Player");
        flashEffect = GetComponentInChildren<SimpleFlash>();

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
    }

    private void FixedUpdate()
    {
        if (isActive)
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

        // Check if we should refresh based on conditions
        bool shouldRefresh = true;
        if (refreshOnlyWhenNeeded)
        {
            shouldRefresh = (!player.isGrounded && canRefreshAerialDash && player.m_hasDashedInAir) ||
                          (!player.CanDash() && (player.isGrounded || canRefreshAerialDash));
        }
        if (shouldRefresh)
        {
            Collect(player);
        }
    }

    private void Collect(PlayerController player)
    {
        // Refresh the player's dash
        player.RefreshDash();

        // Search for SimpleFlash in player or its children
        SimpleFlash flash = player.GetComponentInChildren<SimpleFlash>();
        if (flash != null)
        {
            flash.CallDashFlash();
        }


        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // Disable the refresh object
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
            animator.SetTrigger(respawnTrigger);
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
            animator.Play("Idle");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}