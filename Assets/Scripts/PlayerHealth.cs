using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Health Bar Settings")]
    public RectTransform healthBar;
    private float healthBarFullWidth;

    [Header("Camera Shake Parameters")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 5;
    [SerializeField] private float shakeTime = 1;

    // Start is called before the first frame update
    void Start()
    {
        if (healthBar != null)
        {
            healthBarFullWidth = healthBar.sizeDelta.x;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            healthBar.sizeDelta = new Vector2(healthBarFullWidth * healthPercentage, healthBar.sizeDelta.y);
        }
    }
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        UpdateHealthBar();
    }

}
