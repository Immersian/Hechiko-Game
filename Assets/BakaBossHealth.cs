using UnityEngine;

public class BakaBossHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 1000;
    public int currentHealth;
    public bool isDead = false;
    public bool isInvulnerable;
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private float invulnerabilityTimer = 0f;

    [Header("Health Bar")]
    public RectTransform bossHealthBar;
    private float healthBarFullWidth;

    [Header("Visual Feedback")]
    [SerializeField] private SimpleFlash flashEffect;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 3f;
    [SerializeField] private float shakeTime = 0.3f;

    [Header("Stun Settings")]
    [SerializeField] private BakaBossStateManager stateManager;
    [SerializeField] private int stunDamage = 0;

    private Collider2D[] colliders;

    void Start()
    {
        flashEffect = GetComponent<SimpleFlash>();
        colliders = GetComponentsInChildren<Collider2D>();
        currentHealth = maxHealth;

        if (stateManager == null)
        {
            stateManager = GetComponent<BakaBossStateManager>();
        }

        if (bossHealthBar != null)
        {
            healthBarFullWidth = bossHealthBar.sizeDelta.x;
            UpdateHealthBar();
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

    private void UpdateHealthBar()
    {
        if (bossHealthBar == null) return;

        float healthPercentage = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector2 newSize = new Vector2(healthBarFullWidth * healthPercentage, bossHealthBar.sizeDelta.y);
        bossHealthBar.sizeDelta = newSize;
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        if (flashEffect != null)
        {
            flashEffect.CallHurtFlash();
        }

        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        }

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeStun(Vector2 hitDirection)
    {
        if (isDead || isInvulnerable) return;

        invulnerabilityTimer = invulnerabilityDuration;
        isInvulnerable = true;

        if (flashEffect != null)
        {
            flashEffect.CallHurtFlash();
        }

        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity * 1.5f, shakeTime * 1.5f);
        }

        if (stateManager != null)
        {
            stateManager.TriggerStun();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Disable all colliders
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Trigger death cutscene through state manager
        if (stateManager != null)
        {
            stateManager.TriggerDeathCutscene();
        }
        else
        {
            Debug.LogError("StateManager reference is missing in BakaBossHealth!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player Attack") && !other.CompareTag("Shockwave")) return;

        if (other.CompareTag("Shockwave"))
        {
            Vector2 hitDirection = (transform.position - other.transform.position).normalized;
            TakeStun(hitDirection);
        }
        else if (other.CompareTag("Player Attack") && other.TryGetComponent<PlayerAttackHitbox>(out var attack))
        {
            Vector2 hitDirection = (transform.position - other.transform.position).normalized;
            TakeDamage(attack.damage, hitDirection);
        }
    }
}