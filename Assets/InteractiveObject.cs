using UnityEngine;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using SupanthaPaul;
using UnityEngine.UI;

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

    [Header("UI Fade Settings")]
    [SerializeField] private float uiFadeInDuration = 0.3f;
    [SerializeField] private float uiFadeOutDuration = 0.2f;

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
    private bool _isActivating = false;

    // CanvasGroup references for smooth fading
    private CanvasGroup _keyboardUIPromptCanvasGroup;
    private CanvasGroup _controllerUIPromptCanvasGroup;
    private CanvasGroup _keyboardUIPromptConqueredCanvasGroup;
    private CanvasGroup _controllerUIPromptConqueredCanvasGroup;

    private void Start()
    {
        // Get CanvasGroup components for smooth fading
        InitializeCanvasGroups();

        // Hide all UI prompts initially
        SetAllUIPromptsVisible(false, true); // Force immediate hide

        // Subscribe to device changes
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged += OnInputDeviceChanged;
        }
    }

    private void InitializeCanvasGroups()
    {
        // Get or add CanvasGroup components to all UI prompts
        _keyboardUIPromptCanvasGroup = GetOrAddCanvasGroup(keyboardUIPrompt);
        _controllerUIPromptCanvasGroup = GetOrAddCanvasGroup(controllerUIPrompt);
        _keyboardUIPromptConqueredCanvasGroup = GetOrAddCanvasGroup(keyboardUIPromptConquered);
        _controllerUIPromptConqueredCanvasGroup = GetOrAddCanvasGroup(controllerUIPromptConquered);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject uiObject)
    {
        if (uiObject == null) return null;

        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObject.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
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
            SetAllUIPromptsVisible(false, false); // Smooth fade out
        }
    }

    private void Update()
    {
        // Don't process interactions if activation is already in progress
        if (_isActivating || _isHealing) return;

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
        // Prevent multiple activations
        if (_isActivating) return;

        _isActivating = true;

        _isConquered = true;
        SetAllUIPromptsVisible(false, false); // Smooth fade out

        PlayerController playerController = _player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;

            // ADD THIS: Stop any movement immediately
            Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

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

        // Stop player movement immediately before starting rest sequence
        PlayerController playerController = _player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;

            // ADD THIS: Stop any movement immediately
            Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        // Start the rest sequence with camera shake first
        StartCoroutine(RestSequence());
    }

    private IEnumerator RestSequence()
    {
        _isHealing = true;
        SetAllUIPromptsVisible(false, false); // Smooth fade out

        // Disable player movement
        PlayerController playerController = _player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

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

        // REFILL POTIONS (while screen is black)
        if (playerController != null)
        {
            playerController.RefillAllPotionsWithFade();
        }

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

        // Re-enable player movement after fade out
        if (playerController != null)
        {
            playerController.enabled = true;
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

        bool isGamepad = InputManager.instance.IsGamepad();

        if (_isConquered)
        {
            // Show conquered UI (E-Rest) with smooth fade
            StartCoroutine(ShowConqueredUIPrompts(isGamepad));
        }
        else
        {
            // Show pre-conquered UI (E-Cleanse) with smooth fade
            StartCoroutine(ShowPreConqueredUIPrompts(isGamepad));
        }
    }

    private IEnumerator ShowPreConqueredUIPrompts(bool isGamepad)
    {
        // Fade out all prompts first
        yield return StartCoroutine(FadeOutAllPrompts());

        // Then fade in the appropriate ones
        if (!isGamepad)
        {
            yield return StartCoroutine(FadeInCanvasGroup(_keyboardUIPromptCanvasGroup));
        }
        else
        {
            yield return StartCoroutine(FadeInCanvasGroup(_controllerUIPromptCanvasGroup));
        }
    }

    private IEnumerator ShowConqueredUIPrompts(bool isGamepad)
    {
        // Fade out all prompts first
        yield return StartCoroutine(FadeOutAllPrompts());

        // Then fade in the appropriate ones
        if (!isGamepad)
        {
            yield return StartCoroutine(FadeInCanvasGroup(_keyboardUIPromptConqueredCanvasGroup));
        }
        else
        {
            yield return StartCoroutine(FadeInCanvasGroup(_controllerUIPromptConqueredCanvasGroup));
        }
    }

    private IEnumerator FadeOutAllPrompts()
    {
        // Create a list of all canvas groups that need to be faded out
        List<CanvasGroup> groupsToFadeOut = new List<CanvasGroup>();

        if (_keyboardUIPromptCanvasGroup != null && _keyboardUIPromptCanvasGroup.alpha > 0)
            groupsToFadeOut.Add(_keyboardUIPromptCanvasGroup);
        if (_controllerUIPromptCanvasGroup != null && _controllerUIPromptCanvasGroup.alpha > 0)
            groupsToFadeOut.Add(_controllerUIPromptCanvasGroup);
        if (_keyboardUIPromptConqueredCanvasGroup != null && _keyboardUIPromptConqueredCanvasGroup.alpha > 0)
            groupsToFadeOut.Add(_keyboardUIPromptConqueredCanvasGroup);
        if (_controllerUIPromptConqueredCanvasGroup != null && _controllerUIPromptConqueredCanvasGroup.alpha > 0)
            groupsToFadeOut.Add(_controllerUIPromptConqueredCanvasGroup);

        // Fade out all groups simultaneously
        if (groupsToFadeOut.Count > 0)
        {
            float elapsedTime = 0f;
            while (elapsedTime < uiFadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / uiFadeOutDuration;

                foreach (var group in groupsToFadeOut)
                {
                    if (group != null)
                        group.alpha = Mathf.Lerp(1f, 0f, t);
                }
                yield return null;
            }

            // Ensure all are fully transparent
            foreach (var group in groupsToFadeOut)
            {
                if (group != null)
                    group.alpha = 0f;
            }
        }
    }

    private IEnumerator FadeInCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) yield break;

        // Ensure the GameObject is active
        canvasGroup.gameObject.SetActive(true);

        float elapsedTime = 0f;
        while (elapsedTime < uiFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / uiFadeInDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < uiFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / uiFadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    // Helper method to show/hide all UI prompts
    private void SetAllUIPromptsVisible(bool visible, bool immediate = false)
    {
        if (!visible)
        {
            if (immediate)
            {
                // Immediate hide
                if (keyboardUIPrompt != null) keyboardUIPrompt.SetActive(false);
                if (controllerUIPrompt != null) controllerUIPrompt.SetActive(false);
                if (keyboardUIPromptConquered != null) keyboardUIPromptConquered.SetActive(false);
                if (controllerUIPromptConquered != null) controllerUIPromptConquered.SetActive(false);
            }
            else
            {
                // Smooth fade out
                StartCoroutine(FadeOutAllPrompts());
            }
        }
        else
        {
            // Show the appropriate prompts based on current state
            bool isGamepad = InputManager.instance != null && InputManager.instance.IsGamepad();

            if (_isConquered)
            {
                StartCoroutine(ShowConqueredUIPrompts(isGamepad));
            }
            else
            {
                StartCoroutine(ShowPreConqueredUIPrompts(isGamepad));
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

        // REFILL PLAYER HEALTH AND POTIONS HERE (while screen is black)
        if (_playerHealth != null)
        {
            _playerHealth.HealToFull();

            // Also refill potions when conquering checkpoint
            PlayerController playerController = _player?.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.RefillAllPotionsWithFade();
            }

            Debug.Log("Player health and potions refilled at checkpoint!");
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

        // Use the playerController variable that was already declared above, or get it again
        PlayerController controller = _player?.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Reset activation flag after everything is complete
        _isActivating = false;
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
        _isActivating = false; // Reset activation flag

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