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

        // Initialize UI state
        InitializeUI();

        // Assign button click listeners
        AssignButtonListeners();

        // Start fading out the overlay
        StartCoroutine(FadeOutOverlay());
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
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.Disable();
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
                                                     // REMOVED the StartCoroutine(FadeOutImage()) call
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


    //private IEnumerator FadeOutImage()
    //{
    //    if (pressAnyButtonImage == null) yield break;

    //    float timer = 0f;
    //    Color startColor = pressAnyButtonImage.color;
    //    Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

    //    while (timer < initialImageFadeDuration)
    //    {
    //        timer += Time.deltaTime;
    //        float progress = timer / initialImageFadeDuration;
    //        pressAnyButtonImage.color = Color.Lerp(startColor, targetColor, progress);
    //        yield return null;
    //    }

    //    // Ensure fully transparent
    //    pressAnyButtonImage.color = targetColor;
    //}

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
    }

    private void CheckForInput()
    {
        // Check for any keyboard, mouse, or gamepad input
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
        {
            HandleInputPressed();
        }
        else if (Mouse.current != null && (Mouse.current.leftButton.isPressed ||
                 Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed))
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
        }

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