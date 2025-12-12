using SupanthaPaul;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
    public static bool MenuActivated = false;

    [Header("UI References")]
    public GameObject inventoryPanel;
    public Button firstSelectedButton;
    public ItemSlot[] itemSlots; // Array of all inventory slots
    [SerializeField] private Animator inventoryAnimator;
    [SerializeField] private CanvasGroup canvasGroup; // Reference to the canvas group

    [Header("Panel Switching")]
    public GameObject[] panels; // Array of panels in order: Inventory, Controls, Settings
    public Animator panelSwitchAnimator;
    [SerializeField] private int currentPanelIndex = 0;
    [SerializeField] private int targetPanelIndex = 0;

    [Header("Transparency Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scene Management")]
    [SerializeField] public Image fadeOverlay; // Reference to a black UI Image for fading
    [SerializeField] private float sceneFadeDuration = 1f;
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Transition State")]
    private bool isTransitioning = false;

    [Header("Checkpoint Teleportation")]
    public Transform[] checkpointTransforms; // Drag your InteractiveObject transforms here
    public Button[] checkpointButtons;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openInventorySound;
    [SerializeField] private AudioClip closeInventorySound;
    [SerializeField] private AudioClip switchPanelSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.7f;

    [Header("Switch Sound Settings")]
    [SerializeField] private float switchSoundCooldown = 0.2f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private bool randomizePitch = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Coroutine fadeCoroutine;
    private float lastSwitchSoundTime = 0f;
    private bool canPlaySwitchSound = true;

    // Public getters for the animation handler
    public int GetCurrentPanelIndex() => currentPanelIndex;
    public int GetTargetPanelIndex() => targetPanelIndex;

    private void Start()
    {
        // Setup checkpoint button listeners
        SetupCheckpointButtons();

        // Ensure we have an AudioSource
        SetupAudioSource();
    }

    private void Awake()
    {
        if (inventoryAnimator == null && inventoryPanel != null)
        {
            inventoryAnimator = inventoryPanel.GetComponent<Animator>();
        }

        // Get or add CanvasGroup component
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    private void Update()
    {
        if (InputManager.instance.inputControl.Pause.Tab.WasPressedThisFrame())
        {
            ToggleInventory();
        }

        if (MenuActivated)
        {
            HandlePanelSwitching();
        }

        // Update switch sound cooldown
        UpdateSwitchSoundCooldown();
    }

    public void ToggleInventory()
    {
        if (MenuActivated)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    private void HandlePanelSwitching()
    {
        if (InputManager.instance.inputControl.Pause.LB.WasPressedThisFrame())
        {
            SwitchPanel(-1);
        }

        if (InputManager.instance.inputControl.Pause.RB.WasPressedThisFrame())
        {
            SwitchPanel(1);
        }
    }

    private void SetupCheckpointButtons()
    {
        for (int i = 0; i < checkpointButtons.Length; i++)
        {
            int index = i; // Important: capture the current index
            checkpointButtons[i].onClick.AddListener(() => OnCheckpointButtonClicked(index));
        }
    }

    private void OnCheckpointButtonClicked(int checkpointIndex)
    {
        PlayButtonClickSound();

        if (checkpointIndex < 0 || checkpointIndex >= checkpointTransforms.Length)
        {
            Debug.LogWarning($"Invalid checkpoint index: {checkpointIndex}");
            return;
        }

        Transform checkpointTransform = checkpointTransforms[checkpointIndex];

        // Get the PlayerHealth component and teleport
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null && checkpointTransform != null)
        {
            // Close inventory first
            CloseInventory();

            // Teleport to the checkpoint
            playerHealth.TeleportToCheckpoint(checkpointTransform.position);

            if (debugLogs) Debug.Log($"Teleporting to checkpoint: {checkpointTransform.name}");
        }
        else
        {
            Debug.LogWarning("PlayerHealth not found or checkpoint transform is null!");
        }
    }

    // Method to add checkpoints dynamically (optional)
    public void AddCheckpoint(Transform checkpointTransform)
    {
        // You could implement dynamic adding to arrays if needed
    }

    private void SwitchPanel(int direction)
    {
        int newIndex = (currentPanelIndex + direction + panels.Length) % panels.Length;
        if (newIndex == currentPanelIndex) return;

        targetPanelIndex = newIndex;

        // Play panel switch sound with cooldown protection
        PlaySwitchPanelSound();

        if (panelSwitchAnimator != null)
        {
            panelSwitchAnimator.SetInteger("Direction", direction);
            panelSwitchAnimator.SetTrigger("Switch");
        }
        else
        {
            DirectPanelSwitch();
        }

        if (debugLogs) Debug.Log($"Switching to panel: {panels[targetPanelIndex].name}");
    }

    // Called by animation handler when switch is complete
    public void CompletePanelSwitch()
    {
        currentPanelIndex = targetPanelIndex;
        SetPanelDefaultSelection();
    }

    // Fallback method for direct panel switching without animation
    private void DirectPanelSwitch()
    {
        panels[currentPanelIndex].SetActive(false);
        panels[targetPanelIndex].SetActive(true);
        currentPanelIndex = targetPanelIndex;
        SetPanelDefaultSelection();
    }

    private void DisableAllPanels()
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }
    }

    private void SetPanelDefaultSelection()
    {
        GameObject selectedObject = null;

        switch (currentPanelIndex)
        {
            case 0: // Inventory
                if (itemSlots.Length > 0 && itemSlots[0].gameObject.activeInHierarchy)
                {
                    selectedObject = itemSlots[0].gameObject;
                }
                break;
            case 1: // Controls
                // Add your controls panel default button here
                // selectedObject = controlsDefaultButton.gameObject;
                break;
            case 2: // Settings
                // Add your settings panel default button here
                // selectedObject = settingsDefaultButton.gameObject;
                break;
        }

        if (selectedObject != null)
        {
            EventSystem.current.SetSelectedGameObject(selectedObject);
        }
    }

    private void OpenInventory()
    {
        // Play open inventory sound
        PlayOpenInventorySound();

        // Reset switch sound cooldown when opening inventory
        ResetSwitchSoundCooldown();

        // Stop any existing fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start fade to transparent
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));

        inventoryPanel.SetActive(true);

        if (inventoryAnimator != null)
        {
            inventoryAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            inventoryAnimator.SetTrigger("Open");
        }
        else
        {
            Debug.LogWarning("Inventory Animator not assigned!");
        }

        DisableAllPanels();
        panels[0].SetActive(true);
        currentPanelIndex = 0;
        targetPanelIndex = 0;

        MenuActivated = true;
        Time.timeScale = 0f;
        SetPanelDefaultSelection();

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.DisableMovement();

        CameraFollowObject cameraFollow = FindObjectOfType<CameraFollowObject>();
        if (cameraFollow != null) cameraFollow.DisableLookUpDown();
    }

    private void CloseInventory()
    {
        // Play close inventory sound
        PlayCloseInventorySound();

        if (inventoryAnimator != null)
        {
            inventoryAnimator.SetTrigger("Close");
        }
        else
        {
            inventoryPanel.SetActive(false);
        }

        MenuActivated = false;
        Time.timeScale = 1f;

        // Stop any existing fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start fade back to opaque
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.EnableMovement();

        CameraFollowObject cameraFollow = FindObjectOfType<CameraFollowObject>();
        if (cameraFollow != null) cameraFollow.EnableLookUpDown();

        InputManager.instance.SetGameplayInputEnabled(true);
        EventSystem.current.SetSelectedGameObject(null);

        if (debugLogs) Debug.Log("Inventory closed");
    }

    private IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float curveProgress = fadeCurve.Evaluate(progress);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveProgress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }

    public void OnQuitButtonClicked()
    {
        if (isTransitioning) return;

        PlayButtonClickSound();
        isTransitioning = true;
        if (debugLogs) Debug.Log("Quit button clicked!");

        CloseInventory();
        StartCoroutine(FadeToBlackAndQuit());
    }

    public void OnMenuButtonClicked()
    {
        if (isTransitioning) return;

        PlayButtonClickSound();
        isTransitioning = true;
        if (debugLogs) Debug.Log("Menu button clicked!");

        CloseInventory();
        StartCoroutine(FadeToBlackAndLoadMenu());
    }

    private IEnumerator FadeToBlackAndQuit()
    {
        // Find CrossFade component
        CrossFade crossFade = FindObjectOfType<CrossFade>();
        if (crossFade == null)
        {
            Debug.LogError("CrossFade component not found!");
            yield break;
        }

        // Fade to black using CrossFade
        yield return crossFade.FadeIn(sceneFadeDuration);

        // 1 second delay
        yield return new WaitForSecondsRealtime(1f);

        // Quit the game
        if (debugLogs) Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeToBlackAndLoadMenu()
    {
        // Find CrossFade component
        CrossFade crossFade = FindObjectOfType<CrossFade>();
        if (crossFade == null)
        {
            Debug.LogError("CrossFade component not found!");
            yield break;
        }

        // Fade to black using CrossFade
        yield return crossFade.FadeIn(sceneFadeDuration);

        // 1 second delay
        yield return new WaitForSecondsRealtime(1f);

        // Load menu scene
        Time.timeScale = 1f; // Ensure time is running normally
        SceneManager.LoadScene(menuSceneName);

        if (debugLogs) Debug.Log($"Loaded menu scene: {menuSceneName}");
    }

    public void OnCloseAnimationComplete()
    {
        inventoryPanel.SetActive(false);
    }

    public bool AddItem(string itemName, Sprite itemSprite, string itemDescription)
    {
        foreach (ItemSlot slot in itemSlots)
        {
            if (!slot.isFull)
            {
                slot.AddItemToSlot(itemName, itemSprite, itemDescription);
                if (debugLogs) Debug.Log($"Added {itemName} to inventory");
                return true;
            }
        }

        if (debugLogs) Debug.Log("Inventory is full!");
        return false;
    }

    public bool HasItem(string itemName)
    {
        foreach (ItemSlot slot in itemSlots)
        {
            if (slot.isFull && slot.itemName == itemName)
            {
                return true;
            }
        }
        return false;
    }

    #region Sound Methods

    private void PlayOpenInventorySound()
    {
        if (audioSource != null && openInventorySound != null)
        {
            audioSource.PlayOneShot(openInventorySound, soundVolume);
        }
    }

    private void PlayCloseInventorySound()
    {
        if (audioSource != null && closeInventorySound != null)
        {
            audioSource.PlayOneShot(closeInventorySound, soundVolume);
        }
    }

    private void PlaySwitchPanelSound()
    {
        // Check if we can play the switch sound (cooldown protection)
        if (!canPlaySwitchSound || audioSource == null || switchPanelSound == null)
            return;

        // Apply pitch shift if enabled
        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f; // Reset to default if not randomizing
        }

        audioSource.PlayOneShot(switchPanelSound, soundVolume);

        // Start cooldown
        lastSwitchSoundTime = Time.unscaledTime;
        canPlaySwitchSound = false;
    }

    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            // Reset pitch for button clicks to avoid affecting other sounds
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(buttonClickSound, soundVolume);
        }
    }

    private void UpdateSwitchSoundCooldown()
    {
        // Check if cooldown period has passed
        if (!canPlaySwitchSound && Time.unscaledTime - lastSwitchSoundTime >= switchSoundCooldown)
        {
            canPlaySwitchSound = true;
        }
    }

    private void ResetSwitchSoundCooldown()
    {
        // Reset cooldown when inventory opens
        canPlaySwitchSound = true;
        lastSwitchSoundTime = 0f;
    }

    // Public methods to play sounds from other scripts if needed
    public void PlayInventoryOpenSound() => PlayOpenInventorySound();
    public void PlayInventoryCloseSound() => PlayCloseInventorySound();
    public void PlayPanelSwitchSound() => PlaySwitchPanelSound();
    public void PlayUIButtonClickSound() => PlayButtonClickSound();

    #endregion
}