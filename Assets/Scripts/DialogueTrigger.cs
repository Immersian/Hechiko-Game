using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cues")]
    [SerializeField] private GameObject keyboardCue;
    [SerializeField] private GameObject gamepadCue;

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    private bool playerInRange;
    private bool isGamepad;
    private Transform playerTransform;
    private DialogueManager dialogueManager;

    private void Awake()
    {
        playerInRange = false;
        isGamepad = false;
        dialogueManager = GetComponent<DialogueManager>();

        // Ensure cues are disabled at start
        if (keyboardCue != null) keyboardCue.SetActive(false);
        if (gamepadCue != null) gamepadCue.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && !dialogueManager.dialogueIsPlaying)
        {
            CheckCurrentControlScheme();
            UpdateVisualCues();

            if (InputManager.instance.inputControl.Gameplay.Interact.WasPressedThisFrame())
            {
                bool playerIsLeft = playerTransform.position.x < transform.position.x;
                dialogueManager.StartDialogue(inkJSON, gameObject.name, playerIsLeft);
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
        // Simplified input detection
        if (Gamepad.current != null &&
            (Gamepad.current.leftStick.ReadValue().magnitude > 0.1f ||
             Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            isGamepad = true;
        }
        else if (Keyboard.current != null &&
                (Keyboard.current.anyKey.wasPressedThisFrame ||
                 Mouse.current.leftButton.wasPressedThisFrame))
        {
            isGamepad = false;
        }
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
            playerTransform = collider.transform;
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