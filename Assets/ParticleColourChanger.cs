using UnityEngine;
using System.Collections;

public class ParticleColorChanger : MonoBehaviour
{
    [Header("Particle System Reference")]
    [SerializeField] private ParticleSystem particleSystemToChange;

    [Header("Color Settings")]
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private float colorTransitionDuration = 2f;

    private Color originalColor;
    private Coroutine colorTransitionCoroutine;

    private void Start()
    {
        // If no particle system is assigned, try to get it from this object
        if (particleSystemToChange == null)
        {
            particleSystemToChange = GetComponent<ParticleSystem>();
        }

        if (particleSystemToChange != null)
        {
            // Store the original color
            originalColor = particleSystemToChange.main.startColor.color;
        }
        else
        {
            Debug.LogError("ParticleSystem not found!", this);
        }
    }

    private void OnEnable()
    {
        // When script is enabled, start changing to target color
        ChangeParticleColor(targetColor);
    }

    private void OnDisable()
    {
        // When script is disabled, revert to original color
        if (particleSystemToChange != null)
        {
            // Stop any ongoing transition
            if (colorTransitionCoroutine != null)
            {
                StopCoroutine(colorTransitionCoroutine);
                colorTransitionCoroutine = null;
            }

            // Instant revert when disabled
            var main = particleSystemToChange.main;
            main.startColor = originalColor;
        }
    }

    private void ChangeParticleColor(Color newColor)
    {
        if (particleSystemToChange == null) return;

        // Stop any ongoing color transition
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }

        // Start new color transition
        colorTransitionCoroutine = StartCoroutine(TransitionParticleColor(newColor));
    }

    private IEnumerator TransitionParticleColor(Color targetColor)
    {
        if (particleSystemToChange == null) yield break;

        // Get fresh main module reference each time
        var main = particleSystemToChange.main;
        Color currentColor = main.startColor.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / colorTransitionDuration);

            // Create a new color with the interpolated value
            var newColor = Color.Lerp(currentColor, targetColor, t);

            // Get fresh main module reference each frame to avoid the error
            main = particleSystemToChange.main;
            main.startColor = new Color(newColor.r, newColor.g, newColor.b, newColor.a);

            yield return null;
        }

        // Ensure final color is set
        main = particleSystemToChange.main;
        main.startColor = targetColor;
        colorTransitionCoroutine = null;
    }

    // Public method to change the target color at runtime if needed
    public void SetTargetColor(Color newTargetColor)
    {
        targetColor = newTargetColor;
        if (enabled) // Only change if script is currently enabled
        {
            ChangeParticleColor(targetColor);
        }
    }

    // Debug method to test color change
    [ContextMenu("Test Color Change")]
    private void TestColorChange()
    {
        ChangeParticleColor(targetColor);
    }

    [ContextMenu("Reset Color")]
    private void ResetColor()
    {
        ChangeParticleColor(originalColor);
    }
}