using UnityEngine;
using System.Collections;

public class CanvasFadeTrigger : MonoBehaviour
{
    [Header("Fade Settings")]
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.5f;

    [Header("Starting State")]
    public bool startInvisible = true;
    [Range(0f, 1f)]
    public float startAlpha = 0f;

    [Header("Half Alpha Mode")]
    public bool useHalfAlpha = false;
    [Range(0f, 1f)]
    public float halfAlphaValue = 0.5f;

    [Header("Trigger Settings")]
    public string playerTag = "Player";

    private Coroutine fadeCoroutine;

    void Start()
    {
        // Set initial alpha based on settings
        if (canvasGroup != null)
        {
            if (startInvisible)
            {
                canvasGroup.alpha = startAlpha;
            }
            // If not starting invisible, ensure it's at the correct target alpha
            else
            {
                // If we want it to start visible, set to full or half alpha based on settings
                float targetAlpha = useHalfAlpha ? halfAlphaValue : 1f;
                canvasGroup.alpha = targetAlpha;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // Calculate target alpha based on the bool
            float targetAlpha = useHalfAlpha ? halfAlphaValue : 1f;

            // Start fade in
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, targetAlpha, fadeDuration));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // Start fade out
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, startAlpha, fadeDuration));
        }
    }

    private IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        // Ensure we reach the exact target alpha
        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }

    // Optional: Also handle 2D triggers
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // Calculate target alpha based on the bool
            float targetAlpha = useHalfAlpha ? halfAlphaValue : 1f;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, targetAlpha, fadeDuration));
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, startAlpha, fadeDuration));
        }
    }

    // Public method to toggle the half alpha mode at runtime
    public void SetHalfAlphaMode(bool enableHalfAlpha)
    {
        useHalfAlpha = enableHalfAlpha;

        // If currently faded in, update to the new alpha immediately
        if (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            float targetAlpha = useHalfAlpha ? halfAlphaValue : 1f;
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, targetAlpha, fadeDuration));
        }
    }

    // Public method to manually fade in with current settings
    public void FadeIn()
    {
        float targetAlpha = useHalfAlpha ? halfAlphaValue : 1f;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, targetAlpha, fadeDuration));
    }

    // Public method to manually fade out
    public void FadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, startAlpha, fadeDuration));
    }

    // Method to reset to starting state
    public void ResetToStartState()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (canvasGroup != null)
        {
            if (startInvisible)
            {
                canvasGroup.alpha = startAlpha;
            }
            else
            {
                float targetAlpha = useHalfAlpha ? halfAlphaValue : 1f;
                canvasGroup.alpha = targetAlpha;
            }
        }
    }
}