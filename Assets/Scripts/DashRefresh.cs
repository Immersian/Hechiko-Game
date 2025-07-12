using SupanthaPaul;
using UnityEngine;

public class DashRefresh : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private bool canRefreshAerialDash = true;

    private SpriteRenderer spriteRenderer;
    private bool isActive = true;
    private float cooldownTimer = 0f;
    private Color activeColor = Color.red;
    private Color inactiveColor = Color.blue;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.isCurrentlyPlayable)
        {
            // Only refresh if player can't dash (unless aerial refresh is allowed)
            if ((!player.isGrounded && !canRefreshAerialDash) || player.CanDash()) return;

            Collect(player);
        }
    }

    private void Collect(PlayerController player)
    {
        // Refresh player's dash
        player.m_hasDashedInAir = false;
        player.currentStamina = player.maxStamina;
        player.UpdateStaminaBar();

        // Deactivate object
        isActive = false;
        cooldownTimer = respawnTime;
        UpdateVisuals();
    }

    private void Respawn()
    {
        isActive = true;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? activeColor : inactiveColor;
        }
    }
}