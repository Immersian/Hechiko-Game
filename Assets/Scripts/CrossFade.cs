using UnityEngine;
using System.Collections;

public class CrossFade : MonoBehaviour
{
    public CanvasGroup crossFade;
    public float fadeDuration = 1f;

    void Start()
    {
        // Ensure the crossfade is properly initialized
        if (crossFade != null)
        {
            crossFade.blocksRaycasts = false;
            crossFade.interactable = false;
        }
    }

    public IEnumerator FadeIn(float duration)
    {
        if (crossFade == null) yield break;

        crossFade.blocksRaycasts = true;
        float elapsedTime = 0f;
        float startAlpha = crossFade.alpha;

        while (elapsedTime < duration)
        {
            crossFade.alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        crossFade.alpha = 1f;
    }

    public IEnumerator FadeOut(float duration)
    {
        if (crossFade == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = crossFade.alpha;

        while (elapsedTime < duration)
        {
            crossFade.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        crossFade.alpha = 0f;
        crossFade.blocksRaycasts = false;
    }
}