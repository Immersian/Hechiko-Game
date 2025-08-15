using UnityEngine;
using System;
using SupanthaPaul;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;
    public bool isInvulnerable;
    [SerializeField] private float invulnerabilityDuration = 1f;
    private float invulnerabilityTimer = 0f;

    [Header("Health Bars")]
    public RectTransform healthBar1;
    private float healthBarFullWidth;

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash damageFlashEffect;
    [SerializeField] private PlayerController playerController;

    [Header("Camera Shake")]
    [SerializeField] public CameraShake cameraShake;
    [SerializeField] public float shakeIntensity = 5;
    [SerializeField] public float shakeTime = 0.1f;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar1 != null)
        {
            healthBarFullWidth = healthBar1.sizeDelta.x;
            UpdateHealthBars();
        }

        if (damageFlashEffect == null)
        {
            damageFlashEffect = GetComponent<SimpleFlash>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
    }

    void Update()
    {
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
        Vector2 newSize = new Vector2(healthBarFullWidth * healthPercentage, healthBar1.sizeDelta.y);
        healthBar1.sizeDelta = newSize;
    }

    public event Action<int> OnTakeDamage;

    // In PlayerHealth.cs
    public void TakeDamage(int damageAmount, GameObject damageSource = null)
    {
        if (isDead) return;

        bool isSpecialAttack = (damageSource != null && damageSource.CompareTag("SpecialAttack"));

        // Only check invulnerability if it's NOT a special attack AND we're not currently invulnerable
        // Special attacks bypass dash invulnerability but not post-hit invulnerability
        if (isInvulnerable) return;

        // Interrupt dash if active
        if (playerController != null)
        {
            playerController.InterruptDash();
            playerController.InterruptHealing();
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        // ALWAYS apply invulnerability after being hit, even from special attacks
        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        OnTakeDamage?.Invoke(damageAmount);

        if (damageFlashEffect != null)
        {
            damageFlashEffect.CallHurtFlash();
        }

        UpdateHealthBars();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        UpdateHealthBars();
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died!");
        // Add death handling here
    }
}