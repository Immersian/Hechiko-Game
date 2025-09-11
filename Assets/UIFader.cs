using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class UIFader : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 1.0f; // Time in seconds for the fade
    public float postFadeDelay = 0.5f; // Time to wait after fade completes before loading scene
    public string menuSceneName = "Menu"; // Name of the menu scene to return to

    [Header("Components")]
    public Image fadeOverlay; // Optional: separate fade overlay image
    private CanvasGroup canvasGroup; // Reference to the CanvasGroup

    [Header("Input Settings")]
    public InputActionReference exitAction; // Changed to exit action specifically
    private bool isWaitingForInput = false;

    void Start()
    {
        // Get the CanvasGroup component on this GameObject
        canvasGroup = GetComponent<CanvasGroup>();

        // Check if it exists, just to be safe
        if (canvasGroup != null)
        {
            // Start the fade coroutine immediately
            StartCoroutine(FadeOut());
        }
        else
        {
            Debug.LogError("UIFader: No CanvasGroup found on this GameObject!");
        }
    }

    void OnEnable()
    {
        // Enable the action and subscribe to its events
        if (exitAction != null)
        {
            exitAction.action.Enable();
            exitAction.action.performed += OnExitActionPerformed;
        }
        else
        {
            Debug.LogWarning("Exit Action Reference is not assigned in UIFader!");
        }
    }

    void OnDisable()
    {
        // Disable the action and unsubscribe from events
        if (exitAction != null)
        {
            exitAction.action.performed -= OnExitActionPerformed;
            exitAction.action.Disable();
        }
    }

    // Input System callback for exit action
    private void OnExitActionPerformed(InputAction.CallbackContext context)
    {
        if (isWaitingForInput && context.performed)
        {
            ReturnToMenu();
        }
    }

    // Coroutine to handle the fade over time
    private IEnumerator FadeOut()
    {
        float currentTime = 0f;

        // Starting point: fully opaque (alpha = 1)
        canvasGroup.alpha = 1.0f;

        // Loop until the fade is complete
        while (currentTime < fadeDuration)
        {
            // Increase the timer by the time passed since the last frame
            currentTime += Time.deltaTime;

            // Calculate the new alpha value using Lerp (Linear Interpolation)
            // Lerp smoothly transitions from start value (1) to end value (0)
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);

            // Wait until the next frame before continuing the loop
            yield return null;
        }

        // Ensure it ends exactly at 0 (transparent) when the loop is done
        canvasGroup.alpha = 0f;

        // Set flag to start accepting input after fade completes
        isWaitingForInput = true;
    }

    // Public function to fade back to menu scene
    public void ReturnToMenu()
    {
        if (!isWaitingForInput) return; // Only allow if we're waiting for input

        isWaitingForInput = false; // Stop accepting input during transition
        StartCoroutine(FadeToMenu());
    }

    // Coroutine to fade to black and load menu scene
    // Coroutine to fade to black and load menu scene
    private IEnumerator FadeToMenu()
    {
        // Disable the exit action during transition
        if (exitAction != null)
        {
            exitAction.action.Disable();
        }

        // Fade to black (fade in) using CanvasGroup alpha
        float currentTime = 0f;
        float startAlpha = canvasGroup.alpha;

        // Ensure CanvasGroup is set up for fading
        canvasGroup.blocksRaycasts = true; // Allow raycasts to block input during fade
        canvasGroup.interactable = false;  // Disable interaction during fade

        // Fade CanvasGroup to completely opaque (alpha = 1) BEFORE proceeding
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, currentTime / fadeDuration);
            yield return null;
        }

        // Ensure fully opaque
        canvasGroup.alpha = 1f;

        // Wait for a moment to ensure the fade is fully visible
        yield return new WaitForSeconds(postFadeDelay);

        // Only after the fade is complete, load the menu scene
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogError("Menu scene name is not set!");
        }
    }

    // Clean up event subscription when this object is destroyed
    private void OnDestroy()
    {
        // Clean up input actions
        if (exitAction != null)
        {
            exitAction.action.performed -= OnExitActionPerformed;
            exitAction.action.Disable();
        }
    }
}