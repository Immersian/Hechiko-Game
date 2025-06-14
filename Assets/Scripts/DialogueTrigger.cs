using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cues")]
    [SerializeField] private GameObject keyboardCue;
    [SerializeField] private GameObject gamepadCue;

    [Header("Emote Animator")]
    [SerializeField] private Animator emoteAnimator;

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    private bool playerInRange;
    private bool isGamepad;
    private float lastInputTime;
    private const float inputTimeout = 1f; // Time before considering input inactive

    private void Awake()
    {
        playerInRange = false;
        // Start with keyboard by default
        isGamepad = false;
        lastInputTime = Time.time;
    }

    private void Update()
    {
        if (playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            CheckCurrentControlScheme();
            UpdateVisualCues();

            if (InputManager.instance.inputControl.Gameplay.Interact.WasPressedThisFrame())
            {
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON, gameObject.name);
            }
        }
        else
        {
            if (keyboardCue != null) keyboardCue.SetActive(false);
            if (gamepadCue != null) gamepadCue.SetActive(false);
        }
    }

    private void CheckCurrentControlScheme()
    {
        // Check for gamepad input
        bool gamepadActive = Gamepad.current != null &&
                           (Gamepad.current.leftStick.ReadValue().magnitude > 0.1f ||
                            Gamepad.current.buttonSouth.wasPressedThisFrame ||
                            Gamepad.current.rightStick.ReadValue().magnitude > 0.1f);

        // Check for keyboard input
        bool keyboardActive = Keyboard.current != null &&
                             (Keyboard.current.anyKey.wasPressedThisFrame ||
                              Mouse.current.leftButton.wasPressedThisFrame ||
                              Mouse.current.rightButton.wasPressedThisFrame);

        // Update control scheme state
        if (gamepadActive)
        {
            isGamepad = true;
            lastInputTime = Time.time;
        }
        else if (keyboardActive)
        {
            isGamepad = false;
            lastInputTime = Time.time;
        }
        // Optional: Timeout after inactivity (uncomment if needed)
        // else if (Time.time - lastInputTime > inputTimeout)
        // {
        //     // Could revert to default here if desired
        // }
    }

    private void UpdateVisualCues()
    {
        if (keyboardCue != null)
        {
            keyboardCue.SetActive(!isGamepad);
            LayoutRebuilder.ForceRebuildLayoutImmediate(keyboardCue.GetComponent<RectTransform>());
        }

        if (gamepadCue != null)
        {
            gamepadCue.SetActive(isGamepad);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gamepadCue.GetComponent<RectTransform>());
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
            CheckCurrentControlScheme();
            UpdateVisualCues();
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
            if (keyboardCue != null) keyboardCue.SetActive(false);
            if (gamepadCue != null) gamepadCue.SetActive(false);
        }
    }
}