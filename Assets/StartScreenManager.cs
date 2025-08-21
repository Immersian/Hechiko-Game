using UnityEngine;
using TMPro; // Add this namespace
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.InputSystem;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text pressAnyButtonText; // Changed from Text to TMP_Text
    [SerializeField] private CanvasGroup buttonCanvasGroup;
    [SerializeField] private GameObject[] menuButtons; // Assign your button GameObjects here

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Timing")]
    [SerializeField] private float textFadeDuration = 0.5f;
    [SerializeField] private float buttonFadeDuration = 0.8f;
    [SerializeField] private float inputHoldTime = 0.1f; // Minimum time to consider as a "press"

    private bool waitingForInput = true;
    private bool inputDetected = false;
    private float inputHoldTimer = 0f;
    private Color originalTextColor;

    private void Start()
    {
        // Store original color
        originalTextColor = pressAnyButtonText.color;

        // Initialize UI state
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Show press any button text
        pressAnyButtonText.gameObject.SetActive(true);
        pressAnyButtonText.color = normalColor;

        // Hide buttons initially
        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;

        // Disable all buttons
        foreach (var button in menuButtons)
        {
            button.SetActive(false);
        }
    }

    private void Update()
    {
        if (waitingForInput && !inputDetected)
        {
            CheckForInput();
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
            // Check for any gamepad button press (excluding sticks which are always active)
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
        else if (inputDetected)
        {
            // If input was detected but now released, reset color
            pressAnyButtonText.color = normalColor;
            inputDetected = false;
            inputHoldTimer = 0f;
        }
    }

    private void HandleInputPressed()
    {
        inputDetected = true;
        pressAnyButtonText.color = pressedColor;

        inputHoldTimer += Time.deltaTime;

        if (inputHoldTimer >= inputHoldTime)
        {
            // Input held long enough, transition to menu
            StartCoroutine(TransitionToMenu());
            waitingForInput = false;
        }
    }

    private IEnumerator TransitionToMenu()
    {
        // Fade out the "Press Any Button" text
        float timer = 0f;
        Color startColor = pressAnyButtonText.color;

        while (timer < textFadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / textFadeDuration);
            pressAnyButtonText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Hide the text
        pressAnyButtonText.gameObject.SetActive(false);

        // Enable all buttons
        foreach (var button in menuButtons)
        {
            button.SetActive(true);
        }

        // Fade in the buttons
        timer = 0f;
        buttonCanvasGroup.interactable = true;
        buttonCanvasGroup.blocksRaycasts = true;

        while (timer < buttonFadeDuration)
        {
            timer += Time.deltaTime;
            buttonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / buttonFadeDuration);
            yield return null;
        }

        buttonCanvasGroup.alpha = 1f;

        // Set first button as selected for controller navigation
        if (menuButtons.Length > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(menuButtons[0]);
        }
    }

    // Optional: Public method to reset the start screen
    public void ResetStartScreen()
    {
        waitingForInput = true;
        inputDetected = false;
        inputHoldTimer = 0f;
        InitializeUI();
    }
}