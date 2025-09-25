using UnityEngine;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class InteractiveObject : MonoBehaviour
{
    [Header("References")]
    public CameraShake cameraShake;
    public CrossFade crossFade;
    public CrossFade crossFade2;
    public Animator animator;

    [Header("UI References - Before Conquering")]
    public GameObject keyboardUIPrompt;    // UI for keyboard input (E-Cleanse)
    public GameObject controllerUIPrompt;  // UI for controller input (Before conquering)

    [Header("UI References - After Conquering")]
    public GameObject keyboardUIPromptConquered;    // UI for keyboard input (E-Rest)
    public GameObject controllerUIPromptConquered;  // UI for controller input (After conquering)

    [Header("Settings")]
    public float shakeIntensity = 2f;
    public float shakeDuration = 0.5f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDelay = 1f; // Time screen stays black before fading out
    public float fadeOutDuration = 0.5f;
    public bool refillHealthOnActivation = true; // New option to control health refill

    [Header("Rest Settings")]
    public float healDuration = 2f; // How long the healing lerp should take
    public float restFadeInDuration = 0.5f;
    public float restFadeOutDelay = 1f;
    public float restFadeOutDuration = 0.5f;
    public float restShakeIntensity = 1f; // Smaller shake for rest
    public float restShakeDuration = 0.3f; // Shorter shake for rest

    private bool _playerInTrigger = false;
    private bool _isConquered = false;
    private bool _isHealing = false;
    private GameObject _player;
    private PlayerHealth _playerHealth;

    private void Start()
    {
        // Hide all UI prompts initially
        SetAllUIPromptsVisible(false);

        // Subscribe to device changes
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged += OnInputDeviceChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from device changes to prevent memory leaks
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged -= OnInputDeviceChanged;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTrigger = true;
            _player = other.gameObject;
            _playerHealth = _player.GetComponent<PlayerHealth>();
            UpdateUIPrompts();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTrigger = false;
            _player = null;
            _playerHealth = null;
            SetAllUIPromptsVisible(false);
        }
    }

    private void Update()
    {
        if (_playerInTrigger && !_isConquered && InputManager.instance.inputControl.Gameplay.Interact.WasPressedThisFrame())
        {
            ActivateCheckpoint();
        }
        else if (_playerInTrigger && _isConquered && InputManager.instance.inputControl.Gameplay.Interact.WasPressedThisFrame())
        {
            // Handle post-conquering interaction (E-Rest)
            HandleRestInteraction();
        }
    }

    private void ActivateCheckpoint()
    {
        _isConquered = true;
        SetAllUIPromptsVisible(false); // Hide all UI after interaction

        // Set this as the new checkpoint
        if (_player != null && _playerHealth != null)
        {
            _playerHealth.SetCheckpoint(transform.position);
        }

        // Trigger camera shake
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeDuration);
        }

        // Play conquering animation
        if (animator != null)
        {
            animator.SetTrigger("Conquer");
        }
    }

    private void HandleRestInteraction()
    {
        if (_isHealing || _playerHealth == null) return;

        // Don't heal if already at full health
        if (_playerHealth.IsFullHealth()) return;

        // Start the rest sequence with camera shake first
        StartCoroutine(RestSequence());
    }

    private IEnumerator RestSequence()
    {
        _isHealing = true;
        SetAllUIPromptsVisible(false); // Hide UI during rest sequence

        // First, trigger a small camera shake
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(restShakeIntensity, restShakeDuration);
        }

        // Wait for the camera shake to finish
        yield return new WaitForSeconds(restShakeDuration);

        // Then fade in to black
        if (crossFade2 != null)
        {
            yield return crossFade2.FadeIn(restFadeInDuration);
        }

        // Set this as the checkpoint again (in case player wants to respawn here)
        _playerHealth.SetCheckpoint(transform.position);

        // RESPAWN ENEMIES (while screen is black)
        PlayerHealth.TriggerRespawnEvent();

        // Start healing coroutine (will complete during the black screen phase)
        yield return StartCoroutine(HealPlayerOverTime());

        // Wait while screen is black
        yield return new WaitForSeconds(restFadeOutDelay);

        // Fade out from black
        if (crossFade2 != null)
        {
            yield return crossFade2.FadeOut(restFadeOutDuration);
        }

        _isHealing = false;

        // Show UI prompts again if player is still in trigger
        if (_playerInTrigger)
        {
            UpdateUIPrompts();
        }
    }

    private IEnumerator HealPlayerOverTime()
    {
        int targetHealth = _playerHealth.maxHealth;
        int startHealth = _playerHealth.currentHealth;
        float elapsedTime = 0f;

        Debug.Log($"Starting healing from {startHealth} to {targetHealth} over {healDuration} seconds");

        while (elapsedTime < healDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / healDuration;

            // Use smooth step for smoother easing
            t = t * t * (3f - 2f * t);

            int newHealth = (int)Mathf.Lerp(startHealth, targetHealth, t);
            _playerHealth.Heal(newHealth - _playerHealth.currentHealth);

            yield return null;
        }

        // Ensure exact final health
        _playerHealth.Heal(targetHealth - _playerHealth.currentHealth);

        Debug.Log("Healing complete!");
    }

    // Called when input device changes
    private void OnInputDeviceChanged(InputManager.CurrentDevice newDevice)
    {
        if (_playerInTrigger)
        {
            UpdateUIPrompts();
        }
    }

    // Update which UI prompt to show based on current input device and conquered state
    private void UpdateUIPrompts()
    {
        if (InputManager.instance == null) return;

        SetAllUIPromptsVisible(true);

        bool isGamepad = InputManager.instance.IsGamepad();

        if (_isConquered)
        {
            // Show conquered UI (E-Rest)
            if (keyboardUIPrompt != null) keyboardUIPrompt.SetActive(false);
            if (controllerUIPrompt != null) controllerUIPrompt.SetActive(false);
            if (keyboardUIPromptConquered != null) keyboardUIPromptConquered.SetActive(!isGamepad);
            if (controllerUIPromptConquered != null) controllerUIPromptConquered.SetActive(isGamepad);
        }
        else
        {
            // Show pre-conquered UI (E-Cleanse)
            if (keyboardUIPrompt != null) keyboardUIPrompt.SetActive(!isGamepad);
            if (controllerUIPrompt != null) controllerUIPrompt.SetActive(isGamepad);
            if (keyboardUIPromptConquered != null) keyboardUIPromptConquered.SetActive(false);
            if (controllerUIPromptConquered != null) controllerUIPromptConquered.SetActive(false);
        }
    }

    // Helper method to show/hide all UI prompts
    private void SetAllUIPromptsVisible(bool visible)
    {
        if (!visible)
        {
            // Hide all prompts
            if (keyboardUIPrompt != null) keyboardUIPrompt.SetActive(false);
            if (controllerUIPrompt != null) controllerUIPrompt.SetActive(false);
            if (keyboardUIPromptConquered != null) keyboardUIPromptConquered.SetActive(false);
            if (controllerUIPromptConquered != null) controllerUIPromptConquered.SetActive(false);
        }
        else
        {
            // Show the appropriate prompts based on current state without creating a loop
            bool isGamepad = InputManager.instance != null && InputManager.instance.IsGamepad();

            if (_isConquered)
            {
                // Show conquered UI (E-Rest)
                if (keyboardUIPrompt != null) keyboardUIPrompt.SetActive(false);
                if (controllerUIPrompt != null) controllerUIPrompt.SetActive(false);
                if (keyboardUIPromptConquered != null) keyboardUIPromptConquered.SetActive(!isGamepad);
                if (controllerUIPromptConquered != null) controllerUIPromptConquered.SetActive(isGamepad);
            }
            else
            {
                // Show pre-conquered UI (E-Cleanse)
                if (keyboardUIPrompt != null) keyboardUIPrompt.SetActive(!isGamepad);
                if (controllerUIPrompt != null) controllerUIPrompt.SetActive(isGamepad);
                if (keyboardUIPromptConquered != null) keyboardUIPromptConquered.SetActive(false);
                if (controllerUIPromptConquered != null) controllerUIPromptConquered.SetActive(false);
            }
        }
    }

    // Called by animation event at end of conquering animation
    public void OnConqueringComplete()
    {
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // Fade in to black
        if (crossFade != null)
        {
            yield return crossFade.FadeIn(fadeInDuration);
        }

        // REFILL PLAYER HEALTH HERE (while screen is black)
        if (refillHealthOnActivation && _playerHealth != null)
        {
            _playerHealth.HealToFull();
            Debug.Log("Player health refilled at checkpoint!");
        }

        // RESPAWN ENEMIES when conquering checkpoint (while screen is black)
        PlayerHealth.TriggerRespawnEvent();
        Debug.Log("Enemies respawned after conquering checkpoint");

        // Switch to conquered idle animation
        if (animator != null)
        {
            animator.SetTrigger("Conquered");
        }

        // Wait while screen is black
        yield return new WaitForSeconds(fadeOutDelay);

        // Fade out from black
        if (crossFade != null)
        {
            yield return crossFade.FadeOut(fadeOutDuration);
        }

        // After fade sequence, update UI prompts if player is still in trigger
        if (_playerInTrigger)
        {
            UpdateUIPrompts();
        }
    }

    // Public method to manually trigger the checkpoint activation (for testing)
    public void ManualActivateCheckpoint()
    {
        if (!_isConquered)
        {
            ActivateCheckpoint();
        }
    }

    // Public method to reset the checkpoint (for testing or level reset)
    public void ResetCheckpoint()
    {
        _isConquered = false;
        _isHealing = false;

        // Reset animation if needed
        if (animator != null)
        {
            animator.ResetTrigger("Conquer");
            animator.ResetTrigger("Conquered");
            animator.Play("Idle", 0, 0f);
        }

        // Update UI if player is in trigger
        if (_playerInTrigger)
        {
            UpdateUIPrompts();
        }
    }

    // Public method to check if checkpoint is conquered
    public bool IsConquered()
    {
        return _isConquered;
    }

    // Public method to check if currently healing
    public bool IsHealing()
    {
        return _isHealing;
    }
}