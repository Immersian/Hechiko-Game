using UnityEngine;

public class SimplePulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float pulseFrequency = 1f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.8f;

    [Header("Components")]
    public SpriteRenderer spriteRenderer;

    private Color originalColor;

    void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        // Calculate pulse using sine wave for smooth oscillation
        float alpha = Mathf.Lerp(minAlpha, maxAlpha,
            (Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f);

        SetAlpha(alpha);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color newColor = spriteRenderer.color;
            newColor.a = alpha;
            spriteRenderer.color = newColor;
        }
    }

    void OnDisable()
    {
        // Reset alpha when disabled
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}