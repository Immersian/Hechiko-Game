using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AsyncLoader : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string levelToLoad = "Part 1";

    [Header("Loading Settings")]
    [SerializeField] private float minLoadingTime = 2f;

    [Header("Fade Settings")]
    [SerializeField] private SpriteRenderer spriteToFade; // Reference to the SpriteRenderer
    [SerializeField] private float fadeDuration = 1f; // How long the fade should take

    private void Start()
    {
        StartCoroutine(LoadLevelAsync());
    }

    IEnumerator LoadLevelAsync()
    {
        // Record when we started loading
        float loadingStartTime = Time.time;

        // Start loading the scene asynchronously
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelToLoad);
        loadOperation.allowSceneActivation = false;

        // Wait for the minimum loading time minus the fade duration
        float elapsedTime = 0f;
        float timeBeforeFade = Mathf.Max(0, minLoadingTime - fadeDuration);

        while (elapsedTime < timeBeforeFade)
        {
            elapsedTime = Time.time - loadingStartTime;
            yield return null;
        }

        // Start fade during the last part of the loading time
        if (spriteToFade != null)
        {
            yield return StartCoroutine(FadeSpriteToFullOpacity());
        }

        // Ensure we've waited the full minimum loading time
        elapsedTime = Time.time - loadingStartTime;
        if (elapsedTime < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
        }

        // Now allow the scene to activate
        loadOperation.allowSceneActivation = true;

        // Wait for the scene to fully load
        yield return loadOperation;
    }

    IEnumerator FadeSpriteToFullOpacity()
    {
        float startAlpha = spriteToFade.color.a;
        float targetAlpha = 1f;
        float fadeTimer = 0f;

        // Lerp the transparency
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, fadeTimer / fadeDuration);

            Color color = spriteToFade.color;
            spriteToFade.color = new Color(color.r, color.g, color.b, currentAlpha);

            yield return null;
        }

        // Ensure final alpha is exactly 1
        Color finalColor = spriteToFade.color;
        spriteToFade.color = new Color(finalColor.r, finalColor.g, finalColor.b, targetAlpha);
    }
}