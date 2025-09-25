using UnityEngine;
using System.Collections;

public class ParticleColorChanger : MonoBehaviour
{
    [Header("Particle System Reference")]
    [SerializeField] private ParticleSystem particleSystemToChange;

    [Header("Color Settings")]
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private float colorTransitionDuration = 2f;

    [Header("Trigger Settings")]
    [SerializeField] private LayerMask playerLayer = 1 << 3; // Default to Layer 3 (Player)
    [SerializeField] private bool useTagInstead = true;
    [SerializeField] private string playerTag = "Player";

    private Color originalColor;
    private bool isPlayerInArea = false;
    private Coroutine colorTransitionCoroutine;

    private void Start()
    {
        // If no particle system is assigned, try to get it from this object
        if (particleSystemToChange == null)
        {
            particleSystemToChange = GetComponent<ParticleSystem>();
        }

        // Store the original color
        if (particleSystemToChange != null)
        {
            var mainModule = particleSystemToChange.main;
            originalColor = mainModule.startColor.color;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            isPlayerInArea = true;
            ChangeParticleColor(targetColor);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            isPlayerInArea = false;
            ChangeParticleColor(originalColor);
        }
    }

    private bool IsPlayer(Collider2D collision)
    {
        if (useTagInstead)
        {
            return collision.CompareTag(playerTag);
        }
        else
        {
            return playerLayer == (playerLayer | (1 << collision.gameObject.layer));
        }
    }

    private void ChangeParticleColor(Color newColor)
    {
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

        var mainModule = particleSystemToChange.main;
        Color currentColor = mainModule.startColor.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / colorTransitionDuration);

            // Only change the start color - existing particles keep their original color
            // New particles will spawn with the interpolated color
            mainModule.startColor = Color.Lerp(currentColor, targetColor, t);

            yield return null;
        }

        // Ensure final color is set
        mainModule.startColor = targetColor;
        colorTransitionCoroutine = null;
    }

    // Public method to change the target color at runtime if needed
    public void SetTargetColor(Color newTargetColor)
    {
        targetColor = newTargetColor;
        if (isPlayerInArea)
        {
            ChangeParticleColor(targetColor);
        }
    }

    // For debugging
    private void OnValidate()
    {
        // Update the color in editor when changed, but only if particle system is assigned
        if (particleSystemToChange != null && Application.isPlaying == false)
        {
            var mainModule = particleSystemToChange.main;
            mainModule.startColor = targetColor;
        }
    }
}