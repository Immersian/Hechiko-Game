using UnityEngine;
using System.Collections;

public class CanvasGroupFadeIn : MonoBehaviour
{
    [Header("Canvas Group Reference")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float fadeDelay = 0.5f;

    [Header("Trigger Settings")]
    [SerializeField] private bool disableTriggerAfterActivation = true;

    private void Awake()
    {
        // Ensure canvas group is transparent on start
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        // Double-check transparency on start
        if (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(FadeInCanvasGroup());

            // Disable the trigger if configured to do so
            if (disableTriggerAfterActivation)
            {
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    private IEnumerator FadeInCanvasGroup()
    {
        if (canvasGroup == null) yield break;

        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
            canvasGroup.alpha = newAlpha;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure full opacity
        canvasGroup.alpha = 1f;
    }

    // Public method to manually trigger fade in
    public void StartFadeIn()
    {
        StartCoroutine(FadeInCanvasGroup());
    }

    // Public method to instantly set to transparent
    public void SetTransparent()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    // Public method to instantly set to full opacity
    public void SetOpaque()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    // Public method to reset the trigger (enable collider and set transparent)
    public void ResetFadeTrigger()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
    }
}