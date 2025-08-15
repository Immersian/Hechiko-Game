using UnityEngine;
using System.Collections;

public class CrossFade : MonoBehaviour
{
    public CanvasGroup crossFade;
    public float fadeDuration = 1f;

    public IEnumerator FadeIn(float duration)
    {
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
        float elapsedTime = 0f;
        float startAlpha = crossFade.alpha;
        while (elapsedTime < duration)
        {
            crossFade.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        crossFade.alpha = 0f;
    }
}