using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyHitShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float recoverySpeed = 5f;

    private Vector3 originalPosition;
    private float currentShakeTime;
    private bool isShaking;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (isShaking)
        {
            if (currentShakeTime > 0)
            {
                // Random offset within unit sphere
                transform.localPosition = originalPosition +
                    Random.insideUnitSphere * shakeIntensity;

                currentShakeTime -= Time.deltaTime;
            }
            else
            {
                // Smoothly return to original position
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    originalPosition,
                    Time.deltaTime * recoverySpeed
                );

                if (Vector3.Distance(transform.localPosition, originalPosition) < 0.01f)
                {
                    transform.localPosition = originalPosition;
                    isShaking = false;
                }
            }
        }
    }

    public void TriggerShake()
    {
        StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        Vector3 startPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Using Perlin noise for organic movement
            float x = Mathf.PerlinNoise(elapsed * 30f, 0) * 2 - 1;
            float y = Mathf.PerlinNoise(0, elapsed * 30f) * 2 - 1;

            transform.localPosition = startPos +
                new Vector3(x, y, 0) * shakeIntensity;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = startPos;
    }

    // Call this from your damage handler
    public void OnHit()
    {
        TriggerShake();
    }
}