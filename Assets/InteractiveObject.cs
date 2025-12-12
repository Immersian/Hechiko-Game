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

    [Header("Checkpoint Activation Sound")]
    [SerializeField] private AudioClip checkpointActivationSound;
    [SerializeField] private AudioSource activationAudioSource; // Separate AudioSource for activation sound
    [SerializeField] private float activationSoundVolume = 1.0f;
    [SerializeField] private float activationFadeOutDuration = 1.0f;
    private Coroutine activationSoundFadeCoroutine;

    [Header("Delayed Statue Sound")]
    [SerializeField] private AudioClip delayedStatueSound;
    [SerializeField] private AudioSource delayedAudioSource; // Separate AudioSource for delayed sound
    [SerializeField] private float delayedSoundDelay = 0.5f; // Delay after activation
    [SerializeField] private float delayedSoundVolume = 0.8f;

    [Header("Rest Sound")]
    [SerializeField] private AudioClip restSound;
    [SerializeField] private AudioSource restAudioSource; // Separate AudioSource for rest sound
    [SerializeField] private float restSoundVolume = 0.7f;
    [SerializeField] private float restSoundFadeDelay = 0.5f; // NEW: Delay before fade out starts
    [SerializeField] private float restSoundFadeDuration = 1.0f; // NEW: Duration of fade out
    [SerializeField] private float restSoundStartDelay = 0.3f; // Delay before rest sound starts
    private Coroutine restSoundFadeCoroutine;

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

        // Initialize audio sources
        InitializeAudioSources();

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

    private void InitializeAudioSources()
    {
        // Create separate AudioSource for activation sound
        if (activationAudioSource == null)
        {
            activationAudioSource = gameObject.AddComponent<AudioSource>();
            activationAudioSource.playOnAwake = false;
            activationAudioSource.spatialBlend = 0f; // 2D sound
        }

        // Create separate AudioSource for delayed sound
        if (delayedAudioSource == null)
        {
            delayedAudioSource = gameObject.AddComponent<AudioSource>();
            delayedAudioSource.playOnAwake = false;
            delayedAudioSource.spatialBlend = 0f; // 2D sound
        }

        // Create separate AudioSource for rest sound
        if (restAudioSource == null)
        {
            restAudioSource = gameObject.AddComponent<AudioSource>();
            restAudioSource.playOnAwake = false;
            restAudioSource.spatialBlend = 0f; // 2D sound
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from device changes to prevent memory leaks
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged -= OnInputDeviceChanged;
        }

        // Stop any running coroutines
        if (activationSoundFadeCoroutine != null)
        {
            StopCoroutine(activationSoundFadeCoroutine);
        }
        if (restSoundFadeCoroutine != null)
        {
            StopCoroutine(restSoundFadeCoroutine);
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

        // Play activation sound (Sound 1)
        PlayCheckpointActivationSound();

        PlayerController playerController = _player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;

            // Stop any movement immediately
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

        // Play delayed statue sound (Sound 2) after specified delay
        if (delayedStatueSound != null && delayedAudioSource != null)
        {
            Invoke("PlayDelayedStatueSound", delayedSoundDelay);
        }

        // Play conquering animation
        if (animator != null)
        {
            animator.SetTrigger("Conquer");
        }
    }

    private void PlayCheckpointActivationSound()
    {
        if (checkpointActivationSound == null || activationAudioSource == null) return;

        // Stop any existing sound fade
        if (activationSoundFadeCoroutine != null)
        {
            StopCoroutine(activationSoundFadeCoroutine);
        }

        // Reset volume in case it was faded out previously
        activationAudioSource.volume = activationSoundVolume;

        // Play the activation sound at full volume
        activationAudioSource.clip = checkpointActivationSound;
        activationAudioSource.Play();
    }

    private void PlayDelayedStatueSound()
    {
        if (delayedStatueSound == null || delayedAudioSource == null) return;

        // Play the delayed statue sound using its own AudioSource
        delayedAudioSource.clip = delayedStatueSound;
        delayedAudioSource.volume = delayedSoundVolume;
        delayedAudioSource.Play();
    }

    private void FadeOutActivationSound()
    {
        if (activationAudioSource == null || !activationAudioSource.isPlaying || activationFadeOutDuration <= 0f) return;

        // Start fading out ONLY the activation sound
        activationSoundFadeCoroutine = StartCoroutine(FadeOutSoundCoroutine(activationAudioSource, activationFadeOutDuration));
    }

    private IEnumerator FadeOutSoundCoroutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        // Ensure volume is 0 and stop the sound
        source.volume = 0f;
        source.Stop();
        source.volume = startVolume; // Reset volume for next use
    }

    private void HandleRestInteraction()
    {
        if (_isHealing || _playerHealth == null) return;

        // Stop player movement immediately before starting rest sequence
        PlayerController playerController = _player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;

            // Stop any movement immediately
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

        // Play rest sound (Sound 3) after a short delay
        if (restSound != null && restAudioSource != null)
        {
            Invoke("PlayRestSound", restSoundStartDelay);
        }

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

    private void PlayRestSound()
    {
        if (restSound == null || restAudioSource == null) return;

        // Stop any existing sound fade
        if (restSoundFadeCoroutine != null)
        {
            StopCoroutine(restSoundFadeCoroutine);
        }

        // Reset volume and play the rest sound
        restAudioSource.volume = restSoundVolume;
        restAudioSource.clip = restSound;
        restAudioSource.Play();

        // Start fading out the rest sound with delay
        restSoundFadeCoroutine = StartCoroutine(FadeOutRestSoundWithDelay());
    }

    private IEnumerator FadeOutRestSoundWithDelay()
    {
        if (restAudioSource == null) yield break;

        // Wait for the fade delay before starting to fade out
        yield return new WaitForSeconds(restSoundFadeDelay);

        // Now fade out the rest sound
        float startVolume = restAudioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < restSoundFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / restSoundFadeDuration;
            restAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        // Ensure volume is 0 and stop the sound
        restAudioSource.volume = 0f;
        restAudioSource.Stop();
        restAudioSource.volume = restSoundVolume; // Reset volume for next use
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
        // Fade out ONLY the activation sound (Sound 1)
        FadeOutActivationSound();

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

        // Cancel any delayed sounds
        CancelInvoke("PlayDelayedStatueSound");
        CancelInvoke("PlayRestSound");

        // Stop any sound fades
        if (activationSoundFadeCoroutine != null)
        {
            StopCoroutine(activationSoundFadeCoroutine);
        }
        if (restSoundFadeCoroutine != null)
        {
            StopCoroutine(restSoundFadeCoroutine);
        }

        // Stop ALL audio sources
        if (activationAudioSource != null) activationAudioSource.Stop();
        if (delayedAudioSource != null) delayedAudioSource.Stop();
        if (restAudioSource != null) restAudioSource.Stop();

        // Reset volumes
        if (activationAudioSource != null) activationAudioSource.volume = activationSoundVolume;
        if (delayedAudioSource != null) delayedAudioSource.volume = delayedSoundVolume;
        if (restAudioSource != null) restAudioSource.volume = restSoundVolume;

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

    // Public methods to set sounds at runtime
    public void SetCheckpointActivationSound(AudioClip sound, float volume = 1.0f)
    {
        checkpointActivationSound = sound;
        activationSoundVolume = volume;
    }

    public void SetDelayedStatueSound(AudioClip sound, float volume = 0.8f, float delay = 0.5f)
    {
        delayedStatueSound = sound;
        delayedSoundVolume = volume;
        delayedSoundDelay = delay;
    }

    public void SetRestSound(AudioClip sound, float volume = 0.7f, float fadeDelay = 0.5f, float fadeDuration = 1.0f)
    {
        restSound = sound;
        restSoundVolume = volume;
        restSoundFadeDelay = fadeDelay;
        restSoundFadeDuration = fadeDuration;
    }
}