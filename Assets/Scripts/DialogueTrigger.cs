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
    private PlayerInput playerInput;
    private string currentControlScheme;

    private void Awake()
    {
        playerInRange = false;
        playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            currentControlScheme = playerInput.currentControlScheme;
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.onControlsChanged += OnControlsChanged;
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }
    }

    private void Update()
    {
        if (playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            UpdateVisualCues();

            if (!DialogueManager.GetInstance().dialogueIsPlaying &&
                InputManager.instance.inputControl.Gameplay.Interact.WasPressedThisFrame())
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

    private void OnControlsChanged(PlayerInput input)
    {
        currentControlScheme = input.currentControlScheme;
        UpdateVisualCues();
    }

    private void UpdateVisualCues()
    {
        if (playerInput == null) return;

        bool isGamepad = currentControlScheme == "Gamepad";

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