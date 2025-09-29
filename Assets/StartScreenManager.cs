using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text pressAnyButtonText;
    [SerializeField] private Image pressAnyButtonImage;
    [SerializeField] private Button[] menuButtons;
    [SerializeField] private Animator screenAnimator; // Reference to the animator

    [Header("Scene Management")]
    //[SerializeField] private string loadingSceneName = "LoadingScene"; // Loading scene first - COMMENTED OUT
    [SerializeField] private string gameSceneName = "Part 1"; // Actual game scene
    [SerializeField] private string creditsSceneName = "CreditsScene"; // Credits scene

    [Header("Fade Settings")]
    [SerializeField] private Image fadeOverlay; // Black overlay for fade effect
    [SerializeField] private float fadeDuration = 1f; // Duration of fade to black

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Timing")]
    [SerializeField] private float textFadeDuration = 0.5f;
    [SerializeField] private float buttonFadeDuration = 0.8f;
    [SerializeField] private float inputHoldTime = 0.1f;
    [SerializeField] private float delayBetweenButtons = 0.1f;
    [SerializeField] private float embarkToLoadDelay = 0.5f; // Delay after embark animation
    [SerializeField] private float initialImageFadeDuration = 1f; // Duration for initial image fade

    [Header("Input Settings")]
    [SerializeField] private InputActionReference interactActionReference; // Reference to the Interact action

    private bool waitingForInput = true;
    private bool inputDetected = false;
    private bool isTransitioning = false;
    private float inputHoldTimer = 0f;
    private Color originalTextColor;
    private Color originalImageColor;
    private CanvasGroup[] buttonCanvasGroups;
    private CanvasGroup textCanvasGroup;
    private CanvasGroup imageCanvasGroup;
    private InputAction interactAction;
    private InputAction navigateAction;
    private InputAction pointAction; // To detect mouse movement/click
    private bool buttonsActive = false;
    private Button lastSelectedButton;

    private void Start()
    {
        // Store original colors
        originalTextColor = pressAnyButtonText.color;
        if (pressAnyButtonImage != null)
        {
            originalImageColor = pressAnyButtonImage.color;
        }

        // Add CanvasGroup to text if it doesn't exist
        textCanvasGroup = pressAnyButtonText.GetComponent<CanvasGroup>();
        if (textCanvasGroup == null)
        {
            textCanvasGroup = pressAnyButtonText.gameObject.AddComponent<CanvasGroup>();
        }

        // Add CanvasGroup to image if it doesn't exist
        if (pressAnyButtonImage != null)
        {
            imageCanvasGroup = pressAnyButtonImage.GetComponent<CanvasGroup>();
            if (imageCanvasGroup == null)
            {
                imageCanvasGroup = pressAnyButtonImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Initialize button canvas groups
        buttonCanvasGroups = new CanvasGroup[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            buttonCanvasGroups[i] = menuButtons[i].GetComponent<CanvasGroup>();
            if (buttonCanvasGroups[i] == null)
            {
                buttonCanvasGroups[i] = menuButtons[i].gameObject.AddComponent<CanvasGroup>();
            }

            // Add event triggers to handle selection when mouse interacts
            AddButtonEventTriggers(menuButtons[i]);
        }

        // Initialize fade overlay - START WITH FULL OPACITY (BLACK)
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = new Color(0, 0, 0, 1); // Start with full opacity (black)
        }

        // Set up interact action
        if (interactActionReference != null)
        {
            interactAction = interactActionReference.action;
            interactAction.Enable();
        }
        else
        {
            Debug.LogWarning("Interact Action Reference not set in StartScreenManager!");
        }

        // Set up navigate action for controller/keyboard navigation
        navigateAction = new InputAction("Navigate", InputActionType.Value, "<Gamepad>/leftStick, <Gamepad>/dpad, <Keyboard>/arrowKeys");
        navigateAction.Enable();

        // Set up point action to detect mouse movement
        pointAction = new InputAction("Point", InputActionType.Value, "<Mouse>/position");
        pointAction.Enable();

        // Initialize UI state
        InitializeUI();

        // Assign button click listeners
        AssignButtonListeners();

        // Start fading out the overlay
        StartCoroutine(FadeOutOverlay());
    }

    private void AddButtonEventTriggers(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Add pointer enter event
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnButtonPointerEnter(button); });
        trigger.triggers.Add(pointerEnterEntry);

        // Add pointer exit event
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnButtonPointerExit(button); });
        trigger.triggers.Add(pointerExitEntry);

        // Add pointer click event
        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry();
        pointerClickEntry.eventID = EventTriggerType.PointerClick;
        pointerClickEntry.callback.AddListener((data) => { OnButtonPointerClick(button); });
        trigger.triggers.Add(pointerClickEntry);

        // Add select event
        EventTrigger.Entry selectEntry = new EventTrigger.Entry();
        selectEntry.eventID = EventTriggerType.Select;
        selectEntry.callback.AddListener((data) => { OnButtonSelected(button); });
        trigger.triggers.Add(selectEntry);

        // Add deselect event
        EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
        deselectEntry.eventID = EventTriggerType.Deselect;
        deselectEntry.callback.AddListener((data) => { OnButtonDeselected(button); });
        trigger.triggers.Add(deselectEntry);
    }

    private void OnButtonPointerEnter(Button button)
    {
        if (buttonsActive && !isTransitioning)
        {
            // Select the button when mouse hovers over it
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            lastSelectedButton = button;
        }
    }

    private void OnButtonPointerExit(Button button)
    {
        // Don't deselect when mouse exits - keep the last selected button active
    }

    private void OnButtonPointerClick(Button button)
    {
        if (buttonsActive && !isTransitioning)
        {
            // Ensure the clicked button remains selected
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            lastSelectedButton = button;
        }
    }

    private void OnButtonSelected(Button button)
    {
        lastSelectedButton = button;
    }

    private void OnButtonDeselected(Button button)
    {
        // If this button is being deselected and it was the last selected one,
        // try to prevent the deselection or immediately reselect it
        if (lastSelectedButton == button && buttonsActive && !isTransitioning)
        {
            // Check if we're deselecting because of a mouse click elsewhere
            if (IsMouseOverUI())
            {
                // Mouse is over UI, allow the selection to change
                return;
            }

            // Reselect the button if nothing else is being selected
            StartCoroutine(ReselectButtonAfterFrame(button));
        }
    }

    private IEnumerator ReselectButtonAfterFrame(Button button)
    {
        yield return null; // Wait one frame

        // If after one frame no other button is selected, reselect this one
        if (EventSystem.current.currentSelectedGameObject == null && buttonsActive && !isTransitioning)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            lastSelectedButton = button;
        }
    }

    private bool IsMouseOverUI()
    {
        // Check if mouse is over any UI element
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    private IEnumerator FadeOutOverlay()
    {
        if (fadeOverlay == null) yield break;

        float timer = 0f;
        Color startColor = fadeOverlay.color;
        Color targetColor = new Color(0, 0, 0, 0); // Fully transparent

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            fadeOverlay.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }

        // Ensure fully transparent
        fadeOverlay.color = targetColor;
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.Enable();
        }
        if (navigateAction != null)
        {
            navigateAction.Enable();
        }
        if (pointAction != null)
        {
            pointAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.Disable();
        }
        if (navigateAction != null)
        {
            navigateAction.Disable();
        }
        if (pointAction != null)
        {
            pointAction.Disable();
        }
    }

    private void AssignButtonListeners()
    {
        // Make sure we have at least 3 buttons
        if (menuButtons.Length >= 3)
        {
            // First button - Start Game (switch scene)
            menuButtons[0].onClick.AddListener(OnStartGameButtonClicked);

            // Second button - Credits
            menuButtons[1].onClick.AddListener(OnCreditsButtonClicked);

            // Third button - Quit Game
            menuButtons[2].onClick.AddListener(OnQuitGameButtonClicked);

            // Add listeners for any additional buttons if needed
            for (int i = 3; i < menuButtons.Length; i++)
            {
                menuButtons[i].onClick.AddListener(OnPlaceholderButtonClicked);
            }
        }
        else
        {
            Debug.LogWarning("Not enough buttons assigned! Need at least 3 for Start, Credits, and Quit.");
        }
    }

    private void InitializeUI()
    {
        // Show press any button text
        pressAnyButtonText.gameObject.SetActive(true);
        textCanvasGroup.alpha = 1f;
        pressAnyButtonText.color = normalColor;

        // Show image if assigned - set to normal color and DON'T fade it out
        if (pressAnyButtonImage != null)
        {
            pressAnyButtonImage.gameObject.SetActive(true);
            pressAnyButtonImage.color = normalColor; // Set to normal color, no fade
        }

        // Hide buttons initially and make them non-interactable
        foreach (var button in menuButtons)
        {
            button.gameObject.SetActive(true);
            button.interactable = false;

            CanvasGroup cg = button.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
        }

        // Set initial animation state
        if (screenAnimator != null)
        {
            screenAnimator.SetBool("Idle", false);
        }
    }

    private void Update()
    {
        if (waitingForInput && !inputDetected && !isTransitioning)
        {
            CheckForInput();
        }

        // Track input hold duration
        if (inputDetected && waitingForInput)
        {
            inputHoldTimer += Time.deltaTime;

            if (inputHoldTimer >= inputHoldTime)
            {
                StartCoroutine(TransitionToMenu());
                waitingForInput = false;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Check for interact button press to activate selected button
        if (!waitingForInput && !isTransitioning && interactAction != null && interactAction.triggered)
        {
            // Check if a button is currently selected
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                Button selectedButton = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
                if (selectedButton != null && selectedButton.interactable)
                {
                    selectedButton.onClick.Invoke();
                }
            }
        }

        // Ensure a button is always selected when buttons are active
        if (buttonsActive && !isTransitioning && EventSystem.current.currentSelectedGameObject == null)
        {
            // If nothing is selected, select the last selected button or the first button
            GameObject buttonToSelect = lastSelectedButton != null ? lastSelectedButton.gameObject : menuButtons[0].gameObject;
            EventSystem.current.SetSelectedGameObject(buttonToSelect);
        }

        // Detect navigation input and ensure selection stays on buttons
        if (buttonsActive && !isTransitioning && navigateAction != null && navigateAction.triggered)
        {
            // Navigation input detected, ensure something is selected
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectedButton != null ? lastSelectedButton.gameObject : menuButtons[0].gameObject);
            }
        }
    }

    private void CheckForInput()
    {
        // Check for any keyboard, mouse, or gamepad input
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
        {
            HandleInputPressed();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // Changed to wasPressedThisFrame
        {
            HandleInputPressed();
        }
        else if (Gamepad.current != null && Gamepad.current.allControls.Count > 0)
        {
            if (Gamepad.current.aButton.isPressed || Gamepad.current.bButton.isPressed ||
                Gamepad.current.xButton.isPressed || Gamepad.current.yButton.isPressed ||
                Gamepad.current.startButton.isPressed || Gamepad.current.selectButton.isPressed ||
                Gamepad.current.leftShoulder.isPressed || Gamepad.current.rightShoulder.isPressed ||
                Gamepad.current.dpad.up.isPressed || Gamepad.current.dpad.down.isPressed ||
                Gamepad.current.dpad.left.isPressed || Gamepad.current.dpad.right.isPressed)
            {
                HandleInputPressed();
            }
        }
        // Check for interact action from dialogue action map
        else if (interactAction != null && interactAction.ReadValue<float>() > 0.5f)
        {
            HandleInputPressed();
        }
        else if (inputDetected)
        {
            pressAnyButtonText.color = normalColor;
            if (pressAnyButtonImage != null)
            {
                pressAnyButtonImage.color = normalColor;
            }
            inputDetected = false;
            inputHoldTimer = 0f;
        }
    }

    private void HandleInputPressed()
    {
        inputDetected = true;
        pressAnyButtonText.color = pressedColor;
        if (pressAnyButtonImage != null)
        {
            pressAnyButtonImage.color = pressedColor;
        }
    }

    private IEnumerator TransitionToMenu()
    {
        isTransitioning = true;

        // Play the "Pressed" animation
        if (screenAnimator != null)
        {
            screenAnimator.SetTrigger("Pressed");

            // Wait for the animation to complete (you might need to adjust this based on your animation)
            // This assumes your animation has an exit time that determines its length
            yield return new WaitForSeconds(screenAnimator.GetCurrentAnimatorStateInfo(0).length);

            // Set Idle to true after the Pressed animation completes
            screenAnimator.SetBool("Idle", true);
        }

        // Fade out the "Press Any Button" text and image using CanvasGroup
        float timer = 0f;

        while (timer < textFadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / textFadeDuration);
            textCanvasGroup.alpha = alpha;
            if (pressAnyButtonImage != null)
            {
                imageCanvasGroup.alpha = alpha;
            }
            yield return null;
        }

        // Hide the text and image completely
        textCanvasGroup.alpha = 0f;
        pressAnyButtonText.gameObject.SetActive(false);

        if (pressAnyButtonImage != null)
        {
            imageCanvasGroup.alpha = 0f;
            pressAnyButtonImage.gameObject.SetActive(false);
        }

        // Fade in buttons one by one with delay
        for (int i = 0; i < menuButtons.Length; i++)
        {
            yield return StartCoroutine(FadeInButton(menuButtons[i], buttonCanvasGroups[i]));
            yield return new WaitForSeconds(delayBetweenButtons);
        }

        // Set first button as selected for controller navigation
        if (menuButtons.Length > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
            lastSelectedButton = menuButtons[0];
        }

        buttonsActive = true;
        isTransitioning = false;
    }

    private IEnumerator FadeInButton(Button button, CanvasGroup canvasGroup)
    {
        button.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < buttonFadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / buttonFadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    // BUTTON FUNCTIONS
    public void OnStartGameButtonClicked()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        buttonsActive = false;
        Debug.Log("Start Game button clicked! Playing embark animation.");

        // Play the "Embark" animation
        if (screenAnimator != null)
        {
            screenAnimator.SetTrigger("Embark");
            screenAnimator.SetBool("Idle", false);

            // Start coroutine to load scene after animation completes
            StartCoroutine(LoadSceneAfterAnimation(gameSceneName));
        }
        else
        {
            // Fallback if no animator - load directly
            StartCoroutine(FadeToBlackAndLoad(gameSceneName));
        }
    }

    public void OnCreditsButtonClicked()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        buttonsActive = false;
        Debug.Log("Credits button clicked!");

        // Play animation if available
        if (screenAnimator != null)
        {
            screenAnimator.SetTrigger("Embark"); // Use same animation or create a specific one
            screenAnimator.SetBool("Idle", false);
            StartCoroutine(LoadSceneAfterAnimation(creditsSceneName));
        }
        else
        {
            StartCoroutine(FadeToBlackAndLoad(creditsSceneName));
        }
    }

    public void OnQuitGameButtonClicked()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        buttonsActive = false;
        Debug.Log("Quit Game button clicked!");

        // Play animation if available
        if (screenAnimator != null)
        {
            screenAnimator.SetTrigger("Embark");
            screenAnimator.SetBool("Idle", false);
            StartCoroutine(QuitGameAfterAnimation());
        }
        else
        {
            StartCoroutine(QuitGameWithFade());
        }
    }

    private IEnumerator QuitGameAfterAnimation()
    {
        // Wait for the embark animation to actually start playing
        yield return null; // Wait one frame for the animator to process the trigger

        // Now get the length of the embark animation
        AnimatorStateInfo stateInfo = screenAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;

        // Wait for the animation to complete
        yield return new WaitForSeconds(animationLength);

        // Additional delay if needed
        yield return new WaitForSeconds(embarkToLoadDelay);

        // Quit the game
        StartCoroutine(QuitGameWithFade());
    }

    private IEnumerator QuitGameWithFade()
    {
        // Fade to black before quitting
        if (fadeOverlay != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // Ensure fully black
            fadeOverlay.color = new Color(0, 0, 0, 1f);

            // Small delay to ensure the fade is visible
            yield return new WaitForSeconds(0.5f);
        }

        // Quit the application
        Debug.Log("Quitting application...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadSceneAfterAnimation(string sceneName)
    {
        // Wait for the embark animation to actually start playing
        yield return null; // Wait one frame for the animator to process the trigger

        // Now get the length of the embark animation
        AnimatorStateInfo stateInfo = screenAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;

        // Wait for the animation to complete
        yield return new WaitForSeconds(animationLength);

        // Additional delay if needed
        yield return new WaitForSeconds(embarkToLoadDelay);

        // Load the scene
        StartCoroutine(FadeToBlackAndLoad(sceneName));
    }

    private IEnumerator FadeToBlackAndLoad(string sceneName)
    {
        // Fade to black (fade out)
        if (fadeOverlay != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // Ensure fully black
            fadeOverlay.color = new Color(0, 0, 0, 1f);

            // Make the fade overlay persist so it survives scene changes
            DontDestroyOnLoad(fadeOverlay.gameObject);
        }

        // Load the scene asynchronously
        if (!string.IsNullOrEmpty(sceneName))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            // Wait until the scene is fully loaded but not activated
            while (!asyncLoad.isDone)
            {
                // Check if the load has reached 90% (the last 10% requires activation)
                if (asyncLoad.progress >= 0.9f)
                {
                    // Now activate the scene
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }

            // Wait one frame for the new scene to initialize
            yield return null;

            // Now fade in from black (find the fade overlay in the new scene)
            Image newSceneFadeOverlay = FindObjectOfType<Image>();
            if (newSceneFadeOverlay != null && newSceneFadeOverlay.CompareTag("FadeOverlay")) // Add a tag to identify it
            {
                float timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                    newSceneFadeOverlay.color = new Color(0, 0, 0, alpha);
                    yield return null;
                }

                // Ensure fully transparent and destroy the overlay
                newSceneFadeOverlay.color = new Color(0, 0, 0, 0f);
                Destroy(newSceneFadeOverlay.gameObject);
            }
        }
        else
        {
            Debug.LogError("Scene name is not set!");
        }
    }

    public void OnPlaceholderButtonClicked()
    {
        Debug.Log("Placeholder button clicked! This button doesn't do anything yet.");
    }

    public void ResetStartScreen()
    {
        waitingForInput = true;
        inputDetected = false;
        inputHoldTimer = 0f;
        isTransitioning = false;
        buttonsActive = false;
        lastSelectedButton = null;

        // Reset text
        textCanvasGroup.alpha = 1f;
        pressAnyButtonText.gameObject.SetActive(true);
        pressAnyButtonText.color = normalColor;

        // Reset image
        if (pressAnyButtonImage != null)
        {
            pressAnyButtonImage.gameObject.SetActive(true);
            pressAnyButtonImage.color = normalColor;
        }

        // Reset all buttons
        foreach (var button in menuButtons)
        {
            button.interactable = false;
            CanvasGroup cg = button.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
        }

        // Reset fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0, 0, 0, 0);
        }

        // Reset animations
        if (screenAnimator != null)
        {
            screenAnimator.SetBool("Idle", false);
            screenAnimator.ResetTrigger("Pressed");
            screenAnimator.ResetTrigger("Embark");
            screenAnimator.Play("DefaultState"); // Replace with your default state name if needed
        }
    }
}