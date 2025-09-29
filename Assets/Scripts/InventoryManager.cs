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
    [SerializeField] private Image fadeOverlay; // Reference to a black UI Image for fading
    [SerializeField] private float sceneFadeDuration = 1f;
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Transition State")]
    private bool isTransitioning = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Coroutine fadeCoroutine;

    // Public getters for the animation handler
    public int GetCurrentPanelIndex() => currentPanelIndex;
    public int GetTargetPanelIndex() => targetPanelIndex;

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

    private void SwitchPanel(int direction)
    {
        int newIndex = (currentPanelIndex + direction + panels.Length) % panels.Length;
        if (newIndex == currentPanelIndex) return;

        targetPanelIndex = newIndex;

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

        isTransitioning = true;
        if (debugLogs) Debug.Log("Quit button clicked!");

        CloseInventory();
        StartCoroutine(FadeToBlackAndQuit());
    }

    public void OnMenuButtonClicked()
    {
        if (isTransitioning) return;

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
}