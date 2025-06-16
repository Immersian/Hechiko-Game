using BarthaSzabolcs.Tutorial_SpriteFlash;
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;
    public bool isInvulnerable;

    [Header("Health Bar Settings")]
    public RectTransform healthBar;
    private float healthBarFullWidth;

    [Header("Camera Shake Parameters")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 5;
    [SerializeField] private float shakeTime = 1;

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash flashEffect; // Reference to the SimpleFlash component

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBarFullWidth = healthBar.sizeDelta.x;
            UpdateHealthBar();
        }

        // Auto-get the SimpleFlash component if not assigned
        if (flashEffect == null)
        {
            flashEffect = GetComponent<SimpleFlash>();
        }
    }

    public void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercentage = Mathf.Clamp01((float)currentHealth / maxHealth);
            healthBar.sizeDelta = new Vector2(healthBarFullWidth * healthPercentage, healthBar.sizeDelta.y);
        }
    }

    public event Action<int> OnTakeDamage;

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        if (isInvulnerable) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

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
            flashEffect.Flash();
        }
        else
        {
            Debug.LogWarning("Flash effect reference missing on PlayerHealth!");
        }
        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(0.1f, 0.1f, 0.25f); // Low-frequency rumble for 0.25s
        }

        UpdateHealthBar();

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