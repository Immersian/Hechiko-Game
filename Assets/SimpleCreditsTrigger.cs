using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    public CanvasGroup fadeOverlay;
    public GameObject targetObject; // Reference to the object that triggers transition when destroyed
    public float fadeDuration = 1f;
    public float delayBeforeFade = 0.5f;
    public string creditsSceneName = "Credits";

    private bool isTransitioning = false;

    private void Update()
    {
        // Check if target object was destroyed (becomes null)
        if (targetObject == null && !isTransitioning)
        {
            StartTransition();
        }
    }

    private void StartTransition()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionCoroutine());
        }
    }

    private IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;

        // Wait for the brief delay
        yield return new WaitForSeconds(delayBeforeFade);

        // Fade in (0 to 1)
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        fadeOverlay.alpha = 1f;

        // Load the credits scene
        SceneManager.LoadScene(creditsSceneName);
    }

    // Optional: Manual trigger method
    public void ManualTransition()
    {
        if (!isTransitioning)
        {
            StartTransition();
        }
    }
}