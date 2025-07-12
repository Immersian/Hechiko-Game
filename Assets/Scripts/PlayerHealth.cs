using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;
    public bool isInvulnerable;
    [SerializeField] private float invulnerabilityDuration = 1f;
    private float invulnerabilityTimer = 0f;

    [Header("Health Bars (Identical)")]
    public RectTransform healthBar1;
    private float healthBarFullWidth;

    [Header("Camera Shake Parameters")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 5;
    [SerializeField] private float shakeTime = 1;

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash flashEffect;

    void Start()
    {
        currentHealth = maxHealth;

        // Initialize health bars (assuming they're identical)
        if (healthBar1 != null)
        {
            healthBarFullWidth = healthBar1.sizeDelta.x;
            UpdateHealthBars();
        }

        // Auto-get the SimpleFlash component if not assigned
        if (flashEffect == null)
        {
            flashEffect = GetComponent<SimpleFlash>();
        }
    }

    void Update()
    {
        // Update invulnerability timer
        if (invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
            isInvulnerable = true;
        }
        else
        {
            isInvulnerable = false;
        }
    }

    private void UpdateHealthBars()
    {
        float healthPercentage = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector2 newSize = new Vector2(healthBarFullWidth * healthPercentage, healthBar1 != null ? healthBar1.sizeDelta.y : 0);

        if (healthBar1 != null) healthBar1.sizeDelta = newSize;
    }

    public event Action<int> OnTakeDamage;

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        if (isInvulnerable) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Start invulnerability period
        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        // Trigger damage event
        OnTakeDamage?.Invoke(damageAmount);

        // Visual feedback
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        }

        // Trigger flash effect
        if (flashEffect != null)
        {
            flashEffect.CallDFlash();
        }
        else
        {
            Debug.LogWarning("Flash effect reference missing on PlayerHealth!");
        }

        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(0.1f, 0.1f, 0.25f);
        }

        UpdateHealthBars();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died!");
    }
}